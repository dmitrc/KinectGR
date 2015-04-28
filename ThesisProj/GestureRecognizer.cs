using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;

namespace ThesisProj
{
    /// <summary>
    /// Class, that matches the image of the hand with a set of pre-defined gestures.
    /// </summary>
    class GestureRecognizer
    {
        //public double MATCH_THRESHOLD = 0.01;

        public List<Gesture> Gestures;

        public GestureRecognizer()
        {
            Gestures = new List<Gesture>();
        }

        public GestureRecognizer(IEnumerable<Gesture> gestures)
        {
            Gestures = new List<Gesture>();
            Gestures = gestures.ToList();
        }

        public void AddGesture(Gesture g)
        {
            Gestures.Add(g);
        }

        public void AddGestures(List<Gesture> g)
        {
            if (g != null && g.Count > 0)
            {
                Gestures.AddRange(g);
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

                        g.RecognizedData.HistogramMatch = CvInvoke.cvCompareHist(hist1, hist2, HISTOGRAM_COMP_METHOD.CV_COMP_BHATTACHARYYA);
                        g.RecognizedData.ContourMatch = CvInvoke.cvMatchShapes(c1, c2, CONTOURS_MATCH_TYPE.CV_CONTOURS_MATCH_I3, 0);

                        double rating = g.RecognizedData.ContourMatch * g.RecognizedData.HistogramMatch;
                        double bestSoFar = bestFit.RecognizedData.ContourMatch * bestFit.RecognizedData.HistogramMatch;
                   
                        if (rating < bestSoFar) // && g.FingersCount == fingersCount)
                        {
                            bestFit = g;
                        }
                    }
                }
            }

            if (bestFit.RecognizedData.ContourMatch * bestFit.RecognizedData.HistogramMatch <= 0.01 //0.01
                && bestFit.RecognizedData.ContourMatch <= 0.80 //0.80
                && bestFit.RecognizedData.HistogramMatch <= 0.20) //0.20
            {
                return bestFit;
            }
            else
            {
                return null;
            }
        }
    }
}
