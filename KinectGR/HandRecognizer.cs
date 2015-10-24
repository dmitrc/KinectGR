using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace KinectGR
{
    /// <summary>
    /// Segments hands from the depth mask.
    /// </summary>
    internal class HandRecognizer
    {
        // Constants.
        public static ushort FwdThreshold = 200; //mm
        public static ushort BwdThreshold = 25; //mm
        public static ushort BodyDepthCutoff = 350; //mm

        // Frame and joints.
        private ushort[] _depthFrame = null;
        private Dictionary<String, Joint> _joints = null;

        /// <summary>
        /// Identifies hand in a multi-source frame.
        /// </summary>
        /// <param name="depthData">Depth frame</param>
        /// <param name="joints">Relevant joints</param>
        /// <returns>Hand object (or null if none)</returns>
        public Hand IdentifyHand(ushort[] depthData, Dictionary<String, Joint> joints)
        {
            _depthFrame = depthData;
            _joints = joints;

            if (_joints["hand"].TrackingState != TrackingState.Tracked
                || _joints["wrist"].TrackingState == TrackingState.NotTracked
                || _joints["handtip"].TrackingState == TrackingState.NotTracked
                || _joints["thumb"].TrackingState == TrackingState.NotTracked)
            {
                return null;
            }

            DepthSpacePoint point = Utility.ConvertBodyToDepthCoordinate(_joints["hand"].Position);
            ushort handZ = (ushort)(_joints["hand"].Position.Z * 1000);
            ushort bodyZ = 0;

            if (_joints["shoulder"].TrackingState == TrackingState.Tracked)
            {
                bodyZ = (ushort)(_joints["shoulder"].Position.Z * 1000);
            }
            else if (_joints["head"].TrackingState == TrackingState.Tracked)
            {
                bodyZ = (ushort)(_joints["head"].Position.Z * 1000);
            }
            else if (_joints["spine"].TrackingState == TrackingState.Tracked)
            {
                bodyZ = (ushort)(_joints["spine"].Position.Z * 1000);
            }

            // Check if far enough from the body.
            if (bodyZ - handZ < BodyDepthCutoff)
            {
                return null;
            }

            return FloodFill(point, handZ);
        }

        /// <summary>
        /// Checks whether Flood Fill criteria are satisfied.
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="baseDepth">Depth of reference</param>
        /// <param name="mask">Mask</param>
        /// <returns>true if passed, false otherwise</returns>
        private Boolean FloodFillCriteriaCheck(int x, int y, ushort baseDepth, bool[] mask)
        {
            int i = y * Utility.FrameWidth + x;

            // Discard if out of bounds.
            if (x < 0 || y < 0 || x >= Utility.FrameWidth || y >= Utility.FrameHeight)
            {
                return false;
            }

            // Discard if out of reliable range.
            if (_depthFrame[i] <= Utility.MinReliableDepth ||
                _depthFrame[i] >= Utility.MaxReliableDepth)
            {
                return false;
            }

            // Discard if already seen.
            if (mask[i])
            {
                return false;
            }

            // Discard if beyond threshold.
            if (_depthFrame[i] > baseDepth + BwdThreshold
                || _depthFrame[i] < baseDepth - FwdThreshold)
            {
                return false;
            }

            // Discard if over the wrist.
            // consider to modify handPoint to some other point (center of mas?)
            //DepthSpacePoint handPoint = Utility.ConvertBodyToDepthCoordinate(_joints["hand"].Position);
            //DepthSpacePoint wristPoint = Utility.ConvertBodyToDepthCoordinate(_joints["wrist"].Position);

            //Vector n = new Vector(handPoint.X - wristPoint.X, handPoint.Y - wristPoint.Y);
            //Vector q = new Vector(x - wristPoint.X, y - wristPoint.Y);
            //if (n.X * q.X + n.Y * q.Y < 0)
            //{
            //    return false;
            //}

            return true;
        }

        /// <summary>
        /// Performs Flood Fill from a starting point with depth given.
        /// </summary>
        /// <param name="start">Point</param>
        /// <param name="baseDepth">Depth</param>
        /// <returns>Hand (or null if none)</returns>
        private Hand FloodFill(DepthSpacePoint start, ushort baseDepth)
        {
            Queue<DepthSpacePoint> queue = new Queue<DepthSpacePoint>();
            bool[] handMask = new bool[Utility.FrameWidth * Utility.FrameHeight];

            handMask[(int)start.Y * Utility.FrameWidth + (int)start.X] = true;

            queue.Enqueue(start);

            int minX = (int)start.X;
            int minY = (int)start.Y;
            int maxX = (int)start.X;
            int maxY = (int)start.Y;

            while (queue.Count > 0)
            {
                DepthSpacePoint p = queue.Dequeue();
                int x = (int)p.X;
                int y = (int)p.Y;

                int w = x;
                while (FloodFillCriteriaCheck(w - 1, y, baseDepth, handMask))
                {
                    --w;
                    handMask[y * Utility.FrameWidth + w] = true;
                    if (w < minX) minX = w;
                }

                int e = x;
                while (FloodFillCriteriaCheck(e + 1, y, baseDepth, handMask))
                {
                    ++e;
                    handMask[y * Utility.FrameWidth + e] = true;
                    if (e > maxX) maxX = e;
                }

                for (int i = w + 1; i < e; i++)
                {
                    if (FloodFillCriteriaCheck(i, y - 1, baseDepth, handMask))
                    {
                        if (y - 1 < minY) minY = y - 1;
                        queue.Enqueue(new DepthSpacePoint { X = i, Y = (y - 1) });
                    }

                    if (FloodFillCriteriaCheck(i, y + 1, baseDepth, handMask))
                    {
                        if (y + 1 > maxY) maxY = y + 1;
                        queue.Enqueue(new DepthSpacePoint { X = i, Y = (y + 1) });
                    }
                }
            }


            int width = maxX - minX;
            int height = maxY - minY;

            // Some quality standard.
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            // Get position
            Rect position = new Rect(minX, minY, width, height);

            // Get mask
            bool[] mask = Utility.CropAndResize(handMask, minX, minY, width, height);

            handMask = null;
            return new Hand(position, _joints, mask);
        }
    }
}