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
    public class Hand
    {
        public const int AreaCutoff = 30;
        public const double OuterCircleMultiplier = 1.7;

        public Rect Position;
        public bool[] Mask;

        public Image<Gray, byte> MaskImage;
        public Image<Bgr, byte> DisplayImage = new Image<Bgr, byte>(Utility.HandWidth, Utility.HandHeight, new Bgr(Color.White));

        public Point PalmCenter;
        public int InnerCircleRadius = 0;
        public int FingersCount = 0;

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
            PalmCenter = LocatePalmCenter();
            InnerCircleRadius = CalculateInnerRadius();

            DisplayImage.Draw(new CircleF(PalmCenter, InnerCircleRadius), new Bgr(Color.Orange), 5);
            DisplayImage.Draw(new CircleF(PalmCenter, 5), new Bgr(Color.Red), 2);

            using (var storage = new MemStorage())
            {

                Contour<System.Drawing.Point> contours = MaskImage.FindContours(
                    Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST,
                    storage);

                Contour<System.Drawing.Point> biggestContour = null;

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
                    DisplayImage.Draw(biggestContour, new Bgr(Color.Black), 1);
                }
            }
        }

        private Point LocatePalmCenter()
        {
            double[] dt = DistanceTransform2D();
            double max = Double.NegativeInfinity;

            // Find maximum Euclidean distance
            for (int y = 0; y < Utility.HandHeight; ++y)
            {
                for (int x = 0; x < Utility.HandWidth; ++x)
                {
                    if (dt[y * Utility.HandWidth + x] > max)
                    {
                        max = dt[y * Utility.HandWidth + x];
                    }
                }
            }

            // Retrieve possible palm candidates
            List<Point> candidates = new List<Point>();
            for (int y = 0; y < Utility.HandHeight; ++y)
            {
                for (int x = 0; x < Utility.HandWidth; ++x)
                {
                    if (Math.Abs(dt[y * Utility.HandWidth + x] - max) < 0.1)
                    {
                        candidates.Add(new Point(x, y));
                    }
                }
            }

            double rx = 0;
            double ry = 0;

            for (int i = 0; i < candidates.Count; ++i)
            {
                rx += candidates[i].X;
                ry += candidates[i].Y;
            }
            rx /= candidates.Count;
            ry /= candidates.Count;

            // Return the one closest to hand joint
            //double min = Double.PositiveInfinity;
            //int rx = 0;
            //int ry = 0;
            //for (int i = 0; i < candidates.Count; ++i)
            //{
            //    double dist = Utility.Dist(_handJoint, candidates[i]);
            //    if (dist < min)
            //    {
            //        min = dist;
            //        rx = candidates[i].X;
            //        ry = candidates[i].Y;
            //    }
            //}

            return new Point((int)rx, (int)ry);
        }

        public int CalculateInnerRadius()
        {
            int r = 1;

            while (IsValidInnerRadius(r))
            {
                r = r + 1;
            }

            return r;
        }

        private bool IsValidInnerRadius(int r)
        {
            if (PalmCenter.X + r >= Utility.HandWidth
                || PalmCenter.X - r < 0
                || PalmCenter.Y + r >= Utility.HandHeight
                || PalmCenter.Y - r < 0)
            {
                return false;
            }

            const int SAMPLE_COUNT = 360;
            const double INTERVAL = (360.0 / SAMPLE_COUNT) * (Math.PI / 200);

            double alpha = 0;
            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                int x = (int)Math.Floor(PalmCenter.X + r * Math.Cos(alpha));
                int y = (int)Math.Floor(PalmCenter.Y + r * Math.Sin(alpha));

                if (Mask[y * Utility.HandWidth + x] == false)
                {
                    return false;
                }

                alpha += INTERVAL;
            }

            return true;
        }

        private double[] DistanceTransform1D(double[] arr, int n)
        {
            int[] v = new int[n];
            double[] z = new double[n + 1];
            int k = 0;

            v[0] = 0;
            z[0] = Double.NegativeInfinity;
            z[1] = Double.PositiveInfinity;

            double s;
            for (int i = 1; i < n; i++)
            {
                s = ((arr[i] + i * i) - (arr[v[k]] + v[k] * v[k])) / (2.0 * i - 2.0 * v[k]);

                while (s <= z[k])
                {
                    --k;
                    s = ((arr[i] + i * i) - (arr[v[k]] + v[k] * v[k])) / (2.0 * i - 2.0 * v[k]);
                }

                ++k;
                v[k] = i;
                z[k] = s;
                z[k + 1] = Double.PositiveInfinity;
            }

            k = 0;
            double[] result = new double[n];

            for (int i = 0; i < n; i++)
            {
                while (z[k + 1] < i) ++k;
                result[i] = ((i - v[k]) * (i - v[k]) + arr[v[k]]);
            }

            v = null;
            z = null;
            return result;
        }

        private Double[] DistanceTransform2D()
        {
            double[] result = new double[Utility.HandWidth * Utility.HandHeight];
            double[] tmp = new double[Math.Max(Utility.HandWidth, Utility.HandHeight)];

            // For columns
            for (int x = 0; x < Utility.HandWidth; ++x)
            {
                for (int y = 0; y < Utility.HandHeight; ++y)
                {
                    tmp[y] = Mask[y * Utility.HandWidth + x] ? Double.MaxValue - 1 : 0;
                }

                double[] d = DistanceTransform1D(tmp, Utility.HandHeight);

                for (int y = 0; y < Utility.HandHeight; y++)
                {
                    result[y * Utility.HandWidth + x] = d[y];
                }
            }

            // For rows
            for (int y = 0; y < Utility.HandHeight; y++)
            {
                for (int x = 0; x < Utility.HandWidth; x++)
                {
                    tmp[x] = result[y * Utility.HandWidth + x];
                }

                double[] d = DistanceTransform1D(tmp, Utility.HandWidth);

                for (int x = 0; x < Utility.HandWidth; x++)
                {
                    result[y * Utility.HandWidth + x] = d[x];
                }
            }

            tmp = null;
            return result;
        }
    }
}