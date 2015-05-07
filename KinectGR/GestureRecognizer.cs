using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using System.Timers;

namespace KinectGR
{
    /// <summary>
    /// Class, that matches the image of the hand with a set of pre-defined gestures.
    /// </summary>
    class GestureRecognizer
    {
        public List<Gesture> Gestures = new List<Gesture>();
        public List<DynamicGesture> DynamicGestures = new List<DynamicGesture>(); 
        public String Hand;

        private FrameBuffer _frameBuffer;

        public GestureRecognizer(String hand, FrameBuffer frameBuffer)
        {
            if (hand != "Left" && hand != "Right")
            {
                throw new Exception("Hand should have one of the following values: Left, Right.");
            }
            if (frameBuffer == null)
            {
                throw new Exception("Gesture recognizer needs a frame buffer to detect gestures!");
            }

            Hand = hand;
            _frameBuffer = frameBuffer;
        }

        public void AddGesture(Gesture g)
        {
            if (g != null)
            {
                Gestures.Add(g); 
            }
        }

        public void AddGestures(List<Gesture> g)
        {
            if (g != null && g.Count > 0)
            {
                Gestures.AddRange(g);
            }
        }

        public void AddDynamicGesture(DynamicGesture g)
        {
            if (g != null)
            {
                DynamicGestures.Add(g);
            }
        }

        public void AddDynamicGestures(List<DynamicGesture> g)
        {
            if (g != null && g.Count > 0)
            {
                DynamicGestures.AddRange(g);
            }
        }

        public double CompareShapes(Image<Gray, byte> first, Image<Gray, byte> second)
        {
            return CvInvoke.cvMatchShapes(first, second, CONTOURS_MATCH_TYPE.CV_CONTOURS_MATCH_I3, 0);
        }

        public Gesture RecognizeGesture(Image<Gray, byte> contour, int fingersCount)
        {
            List<Gesture> recognizedGestures = new List<Gesture>(Gestures);

            Gesture bestFit = new Gesture();
            bestFit.RecognizedData.ContourMatch = 999;
            bestFit.RecognizedData.HistogramMatch = 999;

            foreach (var g in recognizedGestures)
            {
                using (MemStorage storage = new MemStorage())
                {
                    Contour<Point> c1 = contour.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                        RETR_TYPE.CV_RETR_LIST, storage);
                    Contour<Point> c2 = g.Image.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                        RETR_TYPE.CV_RETR_LIST, storage);

                    if (c1 != null && c2 != null)
                    {
                        DenseHistogram hist1 = new DenseHistogram(new int[2] { 8, 8 }, new RangeF[2] { new RangeF(-180, 180), new RangeF(100, 100) });
                        DenseHistogram hist2 = new DenseHistogram(new int[2] { 8, 8 }, new RangeF[2] { new RangeF(-180, 180), new RangeF(100, 100) });
                        
                        CvInvoke.cvCalcPGH(c1, hist1.Ptr);
                        CvInvoke.cvCalcPGH(c2, hist2.Ptr);
                        CvInvoke.cvNormalizeHist(hist1.Ptr, 100.0);
                        CvInvoke.cvNormalizeHist(hist2.Ptr, 100.0);

                        g.RecognizedData.Hand = Hand;
                        g.RecognizedData.HistogramMatch = CvInvoke.cvCompareHist(hist1, hist2, HISTOGRAM_COMP_METHOD.CV_COMP_BHATTACHARYYA);
                        g.RecognizedData.ContourMatch = CvInvoke.cvMatchShapes(c1, c2, CONTOURS_MATCH_TYPE.CV_CONTOURS_MATCH_I3, 0);

                        double rating = g.RecognizedData.ContourMatch * g.RecognizedData.HistogramMatch;
                        double bestSoFar = bestFit.RecognizedData.ContourMatch * bestFit.RecognizedData.HistogramMatch;
                   
                        if (rating < bestSoFar && g.FingersCount == fingersCount)
                        {
                            bestFit = g;
                        }
                    }
                }
            }

            // Reliable, but strict: 0.01, 0.80, 0.20
            if (bestFit.RecognizedData.ContourMatch * bestFit.RecognizedData.HistogramMatch <= 0.0125
                && bestFit.RecognizedData.ContourMatch <= 0.80
                && bestFit.RecognizedData.HistogramMatch <= 0.20)
            {
                return bestFit;
            }
            else
            {
                return null;
            }
        }

        public DynamicGesture RecognizeDynamicGesture()
        {
            DynamicFeatures features = _frameBuffer.GetDynamicFeatures(Hand);
            if (features == null)
            {
                return null;
            }

            foreach (DynamicGesture gesture in DynamicGestures)
            {
                if (gesture.Type == DynamicGestureType.DynamicGestureWave)
                {
                    int gestureCount = features.RecognizedGestures.Count(s => s == gesture.Gestures[0].Name);
                    int positiveYCount = features.HandElbowOffsets.Count(p => p.Y < 0);

                    double maxX = -999;
                    double minX = 999;


                    foreach (PointF offset in features.HandElbowOffsets)
                    {
                        if (offset.X > maxX) maxX = offset.X;
                        if (offset.X < minX) minX = offset.X;
                    }

                    double gestureCutoff = features.RecognizedGestures.Count*0.4;
                    double positiveYCutoff = features.HandElbowOffsets.Count*0.9;
                    double offsetCutoff = 0.12;

                    if (gestureCount > gestureCutoff &&
                        positiveYCount > positiveYCutoff &&
                        maxX - minX > offsetCutoff)
                    {
                        gesture.RecognizedData.Hand = Hand;
                        return gesture;
                    }
                }
                else if (gesture.Type == DynamicGestureType.DynamicGestureAlternation)
                {
                    int countA = 0;
                    int countB = 0;

                    foreach (String s in features.RecognizedGestures)
                    {
                        if (s == gesture.Gestures[0].Name)
                        {
                            countA++;
                        }
                        else if (s == gesture.Gestures[1].Name)
                        {
                            countB++;
                        }
                    }

                    double cutoff = features.RecognizedGestures.Count*0.3;

                    if (countA > cutoff && countB > cutoff)
                    {
                        gesture.RecognizedData.Hand = Hand;
                        return gesture;
                    }
                }
            }

            return null;
        }
    }
}
