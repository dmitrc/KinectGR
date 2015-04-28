using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using Microsoft.Kinect;

namespace ThesisProj
{
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

    class FrameBuffer
    {
        private List<FrameData> _queue;
        private const int FRAME_BUFFER_SIZE = 30;

        public FrameBuffer()
        {
            _queue = new List<FrameData>();
        }

        public void PushFrame(FrameData frame)
        {
            _queue.Add(frame);

            if (_queue.Count > FRAME_BUFFER_SIZE)
            {
                _queue.RemoveAt(0);
            }
        }

        public FrameData LatestFrame()
        {
            if (_queue.Count <= 0)
            {
                return null;
            }

            return _queue.Last();
        }

        public FrameData OldestFrame()
        {
            if (_queue.Count <= 0)
            {
                return null;
            }

            return _queue.First();
        }
    }
}
