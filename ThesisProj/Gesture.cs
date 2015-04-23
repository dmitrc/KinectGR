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
        public String Name;
        public Image<Gray, byte> Image;
        public int FingersCount = 0;

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

        public void ExportAs(String filename)
        {
            Image.ToBitmap().Save(filename);
        }
    } 
}
