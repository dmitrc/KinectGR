using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ThesisProj
{
    /// <summary>
    /// Class that processes Kinect's frames and returns relevant results.
    /// </summary>
    class FrameProcessor
    {
        // Define callback that returns rendered depth image.
        public delegate void DepthReadyHandler(BitmapSource depthImage);
        public event DepthReadyHandler DepthReady;

        // Define callback that returns rendered left hand image.
        public delegate void LeftImageReadyHandler(Hand hand);
        public event LeftImageReadyHandler LeftImageReady;

        // Define callback that returns rendered right hand image.
        public delegate void RightImageReadyHandler(Hand hand);
        public event RightImageReadyHandler RightImageReady;

        // Define callback that returns state of left hand's gesture.
        public delegate void LeftGestureUpdatedHandler(Gesture gesture);
        public event LeftGestureUpdatedHandler LeftGestureUpdated;

        // Define callback that returns state of right hand's gesture.
        public delegate void RightGestureUpdatedHandler(Gesture gesture);
        public event RightGestureUpdatedHandler RightGestureUpdated;


        private HandRecognizer _leftHandRecognizer = null;
        private HandRecognizer _rightHandRecognizer = null;
        private GestureRecognizer _leftGestureRecognizer = null;
        private GestureRecognizer _rightGestureRecognizer = null;

        public FrameBuffer FrameBuffer = null;

        public FrameProcessor()
        {
            _leftHandRecognizer = new HandRecognizer();
            _rightHandRecognizer = new HandRecognizer();
            _leftGestureRecognizer = new GestureRecognizer();
            _rightGestureRecognizer = new GestureRecognizer();

            List<Gesture> gestures = new List<Gesture>();

            gestures.Add(new Gesture("Open hand", "C:/Gestures/open_hand.jpg", 5));
            gestures.Add(new Gesture("Peace", "C:/Gestures/peace.jpg", 2));
            gestures.Add(new Gesture("Spock", "C:/Gestures/spock.jpg", 4));
            gestures.Add(new Gesture("Rock'n'roll!", "C:/Gestures/rocknroll.jpg", 5));
            gestures.Add(new Gesture("Pointer", "C:/Gestures/pointer.jpg", 1));
            gestures.Add(new Gesture("OK", "C:/Gestures/ok.jpg", 3));
            gestures.Add(new Gesture("Thumbs up!", "C:/Gestures/thumbs_up.jpg", 1));

            _leftGestureRecognizer.AddGestures(gestures);
            _rightGestureRecognizer.AddGestures(gestures);

            FrameBuffer = new FrameBuffer();
        }

        public void AddGesture(Gesture g)
        {
            _leftGestureRecognizer.AddGesture(g);
            _rightGestureRecognizer.AddGesture(g);
        }


        public void ProcessFrame(MultiSourceFrame frame)
        {
            FrameData frameData = new FrameData();

            // Get depth information
            using (var depthFrame = frame.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    depthFrame.CopyFrameDataToArray(frameData.DepthData);
                    //Utility.MinReliableDepth = depthFrame.DepthMinReliableDistance;
                    //Utility.MaxReliableDepth = depthFrame.DepthMaxReliableDistance;

                    if (DepthReady != null)
                    {
                        BitmapSource depthImage = Utility.DrawDepthFrame(frameData.DepthData);
                        DepthReady(depthImage);
                    }
                    
                }
                else
                {
                    frameData = null;
                    return;
                }
            }

            // Get skeleton information
            Dictionary<String, Joint> leftJoints = new Dictionary<String, Joint>();
            Dictionary<String, Joint> rightJoints = new Dictionary<String, Joint>();
                
            using (var bodyFrame = frame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    Body[] bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    Body _activeBody = bodies.FirstOrDefault(body => body != null && body.IsTracked);

                    if (_activeBody == null)
                    {
                        frameData = null;
                        return;
                    }

                    leftJoints["hand"] = _activeBody.Joints[JointType.HandLeft];
                    leftJoints["wrist"] = _activeBody.Joints[JointType.WristLeft];
                    leftJoints["thumb"] = _activeBody.Joints[JointType.ThumbLeft];
                    leftJoints["handtip"] = _activeBody.Joints[JointType.HandTipLeft];
                    leftJoints["shoulder"] = _activeBody.Joints[JointType.ShoulderLeft];
                    leftJoints["elbow"] = _activeBody.Joints[JointType.ElbowLeft];
                    leftJoints["head"] = _activeBody.Joints[JointType.Head];
                    leftJoints["spine"] = _activeBody.Joints[JointType.SpineBase];

                    rightJoints["hand"] = _activeBody.Joints[JointType.HandRight];
                    rightJoints["wrist"] = _activeBody.Joints[JointType.WristRight];
                    rightJoints["thumb"] = _activeBody.Joints[JointType.ThumbRight];
                    rightJoints["handtip"] = _activeBody.Joints[JointType.HandTipRight];
                    rightJoints["shoulder"] = _activeBody.Joints[JointType.ShoulderRight];
                    rightJoints["elbow"] = _activeBody.Joints[JointType.ElbowRight];
                    rightJoints["head"] = _activeBody.Joints[JointType.Head];
                    rightJoints["spine"] = _activeBody.Joints[JointType.SpineBase];
                }
                else
                {
                    frameData = null;
                    return;
                }
            }
           
            frameData.LeftJoints = leftJoints;
            frameData.RightJoints = rightJoints;

            // Identify left hand
            frameData.LeftHand = _leftHandRecognizer.IdentifyHand(frameData.DepthData, leftJoints);
            if (LeftImageReady != null)
            {
                if (frameData.LeftHand != null)
                {
                    LeftImageReady(frameData.LeftHand);
                }
                else
                {
                    LeftImageReady(null);
                }
            }

            // Identify right hand
            frameData.RightHand = _rightHandRecognizer.IdentifyHand(frameData.DepthData, rightJoints);
            if (RightImageReady != null)
            {
                if (frameData.RightHand != null)
                {
                    RightImageReady(frameData.RightHand);
                }
                else
                {
                    RightImageReady(null);
                }
            }

            // Scan left hand for gestures
            Gesture leftGesture = null;
            if (frameData.LeftHand != null)
            {
                frameData.LeftGesture = _leftGestureRecognizer.RecognizeGesture(frameData.LeftHand.MaskImage, frameData.LeftHand.FingersCount);

                if (LeftGestureUpdated != null)
                {
                    LeftGestureUpdated(frameData.LeftGesture);
                }
            }

            // Scan right hand for gestures
            Gesture rightGesture = null;
            if (frameData.RightHand != null)
            {
                frameData.RightGesture = _rightGestureRecognizer.RecognizeGesture(frameData.RightHand.MaskImage, frameData.RightHand.FingersCount);

                if (RightGestureUpdated != null)
                {
                    RightGestureUpdated(frameData.RightGesture);
                }
            }

            FrameBuffer.PushFrame(frameData);
        }
    }
}
