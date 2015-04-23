using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Drawing;
using System.Web.Script.Serialization;
using Point = System.Drawing.Point;

namespace ThesisProj
{
    public class Finger
    {
        public Point Tip;
    }

    public class Hand
    {
        public Rect Position;
        public bool[] Mask;

        public Image<Gray, byte> MaskImage;
        public Image<Bgr, byte> DisplayImage = new Image<Bgr, byte>(Utility.HandWidth, Utility.HandHeight, new Bgr(Color.White));

        public Point Center;
        public List<Finger> Fingers = new List<Finger>();

        private Point _handJoint;
        private Point _wristJoint;
        private Point _handtipJoint;
        private Point _thumbJoint;

        public Hand(Rect position, Dictionary<String, Joint> joints, bool[] mask)
        {
            Position = position;
            Mask = mask;

            DepthSpacePoint handJoint = Utility.ConvertBodyToDepthCoordinate(joints["hand"].Position);

            _handJoint = Utility.ConvertFrameToResizedMaskCoordinate(joints["hand"].Position, Position);
            _wristJoint = Utility.ConvertFrameToResizedMaskCoordinate(joints["wrist"].Position, Position);
            _handtipJoint = Utility.ConvertFrameToResizedMaskCoordinate(joints["handtip"].Position, Position);
            _thumbJoint = Utility.ConvertFrameToResizedMaskCoordinate(joints["thumb"].Position, Position);

            Analyze();
        }

        public void Analyze()
        {
            MaskImage = Utility.ConvertMaskToImage(Mask, Utility.HandWidth, Utility.HandHeight);

            using (var storage = new MemStorage())
            {
                Contour<Point> contours = MaskImage.FindContours(
                    Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST,
                    storage);

                Contour<Point> biggestContour = null;

                double currentArea = 0;
                double maxArea = 0;

                while (contours != null)
                {
                    currentArea = contours.Area;
                    if (currentArea > maxArea)
                    {
                        maxArea = currentArea;
                        biggestContour = contours;
                    }
                    contours = contours.HNext;
                }

                if (biggestContour != null)
                {
                    MCvMoments m = biggestContour.GetMoments();
                    Center = new Point((int)(m.m10/m.m00), (int)(m.m01/m.m00));

                    int size = biggestContour.Total;
                    for (int i = 0; i < size; i += Utility.Step)
                    {
                        double cos0 = Utility.Angle(biggestContour, i);

                        if ((cos0 > Utility.CosThreshold) && (i + Utility.Step < size))
                        {
                            double cos1 = Utility.Angle(biggestContour, i - Utility.Step);
                            double cos2 = Utility.Angle(biggestContour, i + Utility.Step);
                            double maxCos = Math.Max(Math.Max(cos0, cos1), cos2);

                            bool equal = Utility.IsEq(maxCos, cos0);
                            double z = Utility.Rotation(biggestContour, i);

                            if (equal && z < 0)
                            {
                                Finger f = new Finger();
                                f.Tip = biggestContour[i];

                                Fingers.Add(f);
                            }
                        }
                    }

                    DisplayImage.Draw(biggestContour, new Bgr(Color.Black), 1);
                    DisplayImage.Draw(new CircleF(Center, 5), new Bgr(Color.Red), 2);

                    for (int i = 0; i < Fingers.Count; ++i)
                    {
                        DisplayImage.Draw(new LineSegment2D(Center, Fingers[i].Tip), new Bgr(Color.Purple), 1);
                        DisplayImage.Draw(new CircleF(Fingers[i].Tip, 5), new Bgr(Color.Blue), 2);
                    }
                }
            }
        }
    }
}