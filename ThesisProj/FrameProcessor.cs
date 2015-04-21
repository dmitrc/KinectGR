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
using Color = System.Drawing.Color;

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
        public delegate void LeftImageReadyHandler(BitmapSource leftImage, Rect position);
        public event LeftImageReadyHandler LeftImageReady;

        // Define callback that returns rendered right hand image.
        public delegate void RightImageReadyHandler(BitmapSource rightImage, Rect position);
        public event RightImageReadyHandler RightImageReady;

        // Define callback that returns recognized gestures.
        public delegate void GesturesRecognizedHandler(List<Gesture> gestures);
        public event GesturesRecognizedHandler GesturesRecognized;


        private HandRecognizer _leftHandRecognizer = null;
        private HandRecognizer _rightHandRecognizer = null;
        private GestureRecognizer _leftGestureRecognizer = null;
        private GestureRecognizer _rightGestureRecognizer = null;
        private BitmapSource _emptyImage = null; 

        public FrameBuffer FrameBuffer = null;

        public FrameProcessor()
        {
            _leftHandRecognizer = new HandRecognizer();
            _rightHandRecognizer = new HandRecognizer();
            _leftGestureRecognizer = new GestureRecognizer();
            _rightGestureRecognizer = new GestureRecognizer();
            _emptyImage = Utility.ConvertImageToBitmapSource(new Image<Bgr, byte>(Utility.HandWidth, Utility.HandHeight, new Bgr(Color.White)));

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
                    LeftImageReady(Utility.ConvertImageToBitmapSource(frameData.LeftHand.DisplayImage), frameData.LeftHand.Position);
                }
                else
                {
                    LeftImageReady(_emptyImage, null);
                }
            }

            // Identify right hand
            frameData.RightHand = _rightHandRecognizer.IdentifyHand(frameData.DepthData, rightJoints);
            if (RightImageReady != null)
            {
                if (frameData.RightHand != null)
                {
                    RightImageReady(Utility.ConvertImageToBitmapSource(frameData.RightHand.DisplayImage), frameData.RightHand.Position);
                }
                else
                {
                    RightImageReady(_emptyImage, null);
                }
            }

            // Scan left hand for gestures
            List<Gesture> leftGestures = new List<Gesture>();
            if (frameData.LeftHand != null)
            {
                leftGestures = _leftGestureRecognizer.RecognizeGestures(frameData.LeftHand.MaskImage);
            }

            // Scan right hand for gestures
            List<Gesture> rightGestures = new List<Gesture>();
            if (frameData.RightHand != null)
            {
                rightGestures = _rightGestureRecognizer.RecognizeGestures(frameData.RightHand.MaskImage);
            }

            List<Gesture> gestures = leftGestures.Concat(rightGestures).ToList();
            frameData.RecognizedGestures = gestures;

            if (gestures.Count > 0 && GesturesRecognized != null)
            {
                GesturesRecognized(gestures);
            }

            FrameBuffer.PushFrame(frameData);
        }
    }
}
