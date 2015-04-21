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

        public List<Gesture> RecognizedGestures; 

        public FrameData()
        {
            DepthData = new ushort[Utility.FrameHeight * Utility.FrameWidth];
            LeftJoints = new Dictionary<String, Joint>();
            RightJoints = new Dictionary<String, Joint>();
            RecognizedGestures = new List<Gesture>();
        }
    }

    class FrameBuffer
    {
        private Queue<FrameData> _queue;
        private const int FRAME_BUFFER_SIZE = 30;

        public FrameBuffer()
        {
            _queue = new Queue<FrameData>();
        }

        public void PushFrame(FrameData frame)
        {
            _queue.Enqueue(frame);

            if (_queue.Count > FRAME_BUFFER_SIZE)
            {
                FrameData f = _queue.Dequeue();
                f.DepthData = null;
                f.LeftJoints = null;
                f.RightJoints = null;
                f.LeftHand = null;
                f.RightHand = null;
                f = null;
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
