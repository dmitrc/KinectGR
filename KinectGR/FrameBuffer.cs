using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using Microsoft.Kinect;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;

namespace KinectGR
{
    /// <summary>
    /// Represents a frame
    /// </summary>
    class FrameData
    {
        public ushort[] DepthData;
        public Dictionary<String, Joint> LeftJoints;
        public Dictionary<String, Joint> RightJoints;
 
        public Hand LeftHand;
        public Hand RightHand;

        public Gesture LeftGesture;
        public Gesture RightGesture;

        public FrameData()
        {
            DepthData = new ushort[Utility.FrameHeight * Utility.FrameWidth];
            LeftJoints = new Dictionary<String, Joint>();
            RightJoints = new Dictionary<String, Joint>();
        }
    }

    /// <summary>
    /// Dynamic features of interest, to be extracted from the frames
    /// </summary>
    class DynamicFeatures
    {
        public List<PointF> HandElbowOffsets = new List<PointF>();
        public List<String> RecognizedGestures = new List<String>();
        // ...
    }

    /// <summary>
    /// Frame buffer stores last N frames and allows to extract data from them.
    /// </summary>
    class FrameBuffer : IDisposable
    {
        private List<FrameData> _queue = new List<FrameData>();
        private const int FRAME_BUFFER_SIZE = 35;
        private Mutex _mutex = new Mutex();

        /// <summary>
        /// Add a new frame
        /// </summary>
        /// <param name="frame">Frame</param>
        public void PushFrame(FrameData frame)
        {
            _mutex.WaitOne();
            _queue.Add(frame);

            if (_queue.Count > FRAME_BUFFER_SIZE)
            {
                _queue.RemoveAt(0);
            }
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Gets a latest frame
        /// </summary>
        /// <returns>Frame</returns>
        public FrameData LatestFrame()
        {
            _mutex.WaitOne();
            if (_queue.Count <= 0)
            {
                _mutex.ReleaseMutex();
                return null;
            }

            FrameData frame = _queue.Last();
            _mutex.ReleaseMutex();

            return frame;
        }

        /// <summary>
        /// Gets oldest frame in the buffer
        /// </summary>
        /// <returns>Frame</returns>
        public FrameData OldestFrame()
        {
            _mutex.WaitOne();
            if (_queue.Count <= 0)
            {
                _mutex.ReleaseMutex();
                return null;
            }

            FrameData frame = _queue.First();
            _mutex.ReleaseMutex();

            return frame;
        }

        /// <summary>
        /// Gets dynamic features from the entire buffer.
        /// </summary>
        /// <param name="hand">Hand</param>
        /// <returns>Dynamic features</returns>
        public DynamicFeatures GetDynamicFeatures(String hand)
        {
            if (hand != "Left" && hand != "Right")
            {
                throw new Exception("Hand should have one of the following values: Left, Right.");
            }

            if (_queue.Count < FRAME_BUFFER_SIZE/2)
            {
                return null;
            }

            DynamicFeatures features = new DynamicFeatures();

            _mutex.WaitOne();

            foreach (FrameData frame in _queue)
            {
                if (hand == "Left")
                {
                    if (frame.LeftGesture != null)
                    {
                        features.RecognizedGestures.Add(frame.LeftGesture.Name);
                    }
                    else
                    {
                        features.RecognizedGestures.Add("");
                    }

                    if (frame.LeftJoints["elbow"].TrackingState != TrackingState.NotTracked &&
                        frame.LeftJoints["hand"].TrackingState != TrackingState.NotTracked)
                    {
                        float dx = frame.LeftJoints["elbow"].Position.X - frame.LeftJoints["hand"].Position.X;
                        float dy = frame.LeftJoints["elbow"].Position.Y - frame.LeftJoints["hand"].Position.Y;
                        features.HandElbowOffsets.Add(new PointF(dx, dy));
                    }
                    else
                    {
                        features.HandElbowOffsets.Add(new PointF(0, 0));
                    }
                    
                }
                else // Right
                {
                    if (frame.RightGesture != null)
                    {
                        features.RecognizedGestures.Add(frame.RightGesture.Name);
                    }
                    else
                    {
                        features.RecognizedGestures.Add("");
                    }

                    if (frame.RightJoints["elbow"].TrackingState != TrackingState.NotTracked &&
                        frame.RightJoints["hand"].TrackingState != TrackingState.NotTracked)
                    {
                        float dx = frame.RightJoints["elbow"].Position.X - frame.RightJoints["hand"].Position.X;
                        float dy = frame.RightJoints["elbow"].Position.Y - frame.RightJoints["hand"].Position.Y;
                        features.HandElbowOffsets.Add(new PointF(dx, dy));
                    }
                    else
                    {
                        features.HandElbowOffsets.Add(new PointF(0, 0));
                    }
                }
            }

            _mutex.ReleaseMutex();
            return features;
        }

        /// <summary>
        /// Deallocates buffer and all its frames
        /// </summary>
        public void Dispose()
        {
            _mutex.Dispose();
            _queue = null;
        }
    }
}
