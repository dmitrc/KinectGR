using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace ThesisProj
{

    class Gesture
    {
        public Image<Gray, byte> Image;
        public int FingersCount = 0;

        public Gesture(Image<Gray, byte> img, int fingersCount)
        {
            Image = img;
            FingersCount = fingersCount;
        }

        public Gesture(String filename)
        {
            // !
        }

        public void ExportAs(String filename)
        {
            Image.ToBitmap().Save(filename);
        }
    } 
}
