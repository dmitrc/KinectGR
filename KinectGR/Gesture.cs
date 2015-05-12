using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace KinectGR
{
    /// <summary>
    /// Recognized data from a gesture
    /// </summary>
    public class GestureRecognizedData
    {
        public double ContourMatch;
        public double HistogramMatch;
        public String Hand;
    }

    /// <summary>
    /// Class, representing a gesture.
    /// </summary>
    public class Gesture : IDisposable
    {
        public String Name;
        public Image<Gray, byte> Image;
        public int FingersCount;
        public GestureRecognizedData RecognizedData = new GestureRecognizedData();

        public Gesture() {}

        public Gesture(String name, Image<Gray, byte> img, int fingersCount)
        {
            Name = name;
            Image = img;
            FingersCount = fingersCount;
        }

        public Gesture(String name, String filename, int fingersCount)
        {
            Name = name;
            Image = new Image<Gray, byte>(filename);
            FingersCount = fingersCount;
        }

        /// <summary>
        /// Save gesture as an image
        /// </summary>
        /// <param name="filename">Name</param>
        public void ExportAs(String filename)
        {
            Image.ToBitmap().Save(filename);
        }

        /// <summary>
        /// Deallocate the memory associated with a gesture.
        /// </summary>
        public void Dispose()
        {
           Image.Dispose();
           Name = null;
           RecognizedData = null;
        }
    }

    /// <summary>
    /// Dynamic gesture types
    /// </summary>
    public enum DynamicGestureType
    {
        DynamicGestureWave,
        DynamicGestureAlternation
    }

    /// <summary>
    /// Recognized data from a dynamic gesture.
    /// </summary>
    public class DynamicGestureRecognizedData
    {
        public String Hand;
    }

    /// <summary>
    /// Class, that represents a dynamic gesture.
    /// </summary>
    public class DynamicGesture
    {
        public String Name;
        public DynamicGestureType Type;
        public DynamicGestureRecognizedData RecognizedData = new DynamicGestureRecognizedData();

        public List<Gesture> Gestures;

        public DynamicGesture() {}

        public DynamicGesture(String name, DynamicGestureType type, List<Gesture> gestures)
        {
            Name = name;
            Type = type;

            if (type == DynamicGestureType.DynamicGestureWave && gestures.Count != 1)
            {
                throw new Exception("Wave gesture takes only one static gesture as an argument.");
            }

            if (type == DynamicGestureType.DynamicGestureAlternation && gestures.Count != 2)
            {
                throw new Exception("Alternation gesture takes exactly two static gestures as an argument.");
            }

            Gestures = gestures;
        }
    }
}
