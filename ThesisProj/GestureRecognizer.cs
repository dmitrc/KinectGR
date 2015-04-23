using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace ThesisProj
{
    /// <summary>
    /// Class, that matches the image of the hand with a set of pre-defined gestures.
    /// </summary>
    class GestureRecognizer
    {
        public double MATCH_THRESHOLD = 0.025;

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

        public double CompareShapes(Image<Gray, byte> first, Image<Gray, byte> second)
        {
            return CvInvoke.cvMatchShapes(first, second, CONTOURS_MATCH_TYPE.CV_CONTOURS_MATCH_I2, 0);
        }

        public List<Gesture> RecognizeGestures(Image<Gray, byte> contour)
        {
            List<Gesture> recognizedGestures = new List<Gesture>();
            for (int i = 0; i < Gestures.Count; ++i)
            {
                double match = CompareShapes(contour, Gestures[i].Image);
                // contour.FingersNum == Gestures[i].FingersNum <- !
                // ...


                if (match <= MATCH_THRESHOLD && true) // !
                {
                    recognizedGestures.Add(Gestures[i]);
                }
            }

            return recognizedGestures;
        }
    }
}
