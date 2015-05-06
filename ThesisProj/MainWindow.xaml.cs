using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Shapes;
using System.Web.Script.Serialization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Color = System.Drawing.Color;

namespace ThesisProj
{
    /// <summary>
    /// Class responsible for rendering of the main window of application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _kinect = null;
        private MultiSourceFrameReader _reader = null;
        private FrameProcessor _frameProcessor = null;

        private int _frameCount = 0;
        private Rectangle _leftRect;
        private Rectangle _rightRect;
        private BitmapSource _emptyImage = null; 

        /// <summary>
        /// Public constructor for MainWindow.
        /// </summary>
        public MainWindow()
        {
            _emptyImage = Utility.ConvertImageToBitmapSource(new Image<Bgr, byte>(10, 10, new Bgr(Color.White)));

            _frameProcessor = new FrameProcessor();
            _frameProcessor.DepthReady += FrameProcessor_DepthReady;
            _frameProcessor.LeftImageReady += FrameProcessor_LeftImageReady;
            _frameProcessor.RightImageReady += FrameProcessor_RightImageReady;
            _frameProcessor.LeftGestureUpdated += FrameProcessor_LeftGestureUpdated;
            _frameProcessor.RightGestureUpdated += FrameProcessor_RightGestureUpdated;
            _frameProcessor.LeftDynamicGestureUpdated += FrameProcessor_LeftDynamicGestureUpdated;
            _frameProcessor.RightDynamicGestureUpdated += FrameProcessor_RightDynamicGestureUpdated;

            _kinect = KinectSensor.GetDefault();
            _kinect.IsAvailableChanged += Kinect_IsAvailableChanged;
            _kinect.Open();
            Utility.CoordinateMapper = _kinect.CoordinateMapper;

            _reader = _kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body);
            _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            InitializeComponent();
        }

        /// <summary>
        /// Updates the UI from the new depth data.
        /// </summary>
        /// <param name="depthImage">Depth image</param>
        private void FrameProcessor_DepthReady(BitmapSource depthImage)
        {
            DepthImage.Source = depthImage;
        }

        /// <summary>
        /// Updates the UI from the new left hand image.
        /// </summary>
        /// <param name="hand">Left hand</param>
        private void FrameProcessor_LeftImageReady(Hand hand)
        {
            if (_leftRect != null)
            {
                DepthCanvas.Children.Remove(_leftRect);
            }

            if (hand == null)
            {
                LeftImage.Source = _emptyImage;
                return;
            }

            LeftImage.Source = Utility.ConvertImageToBitmapSource(hand.DisplayImage);

            if (hand.Position != null)
            {
                _leftRect = new Rectangle();

                _leftRect.Width = hand.Position.Width;
                _leftRect.Height = hand.Position.Height;
                _leftRect.Stroke = new SolidColorBrush(Colors.Orange);
                _leftRect.StrokeThickness = 3;

                Canvas.SetTop(_leftRect, hand.Position.Y);
                Canvas.SetLeft(_leftRect, hand.Position.X);

                DepthCanvas.Children.Add(_leftRect);
            }
        }

        /// <summary>
        /// Updates the UI from the new right hand image.
        /// </summary>
        /// <param name="hand">Right hand</param>
        private void FrameProcessor_RightImageReady(Hand hand)
        {
            if (_rightRect != null)
            {
                DepthCanvas.Children.Remove(_rightRect);
            }


            if (hand == null)
            {
                RightImage.Source = _emptyImage;
                return;
            }

            RightImage.Source = Utility.ConvertImageToBitmapSource(hand.DisplayImage);

            if (hand.Position != null)
            {
                _rightRect = new Rectangle();

                _rightRect.Width = hand.Position.Width;
                _rightRect.Height = hand.Position.Height;
                _rightRect.Stroke = new SolidColorBrush(Colors.LightSkyBlue);
                _rightRect.StrokeThickness = 3;

                Canvas.SetTop(_rightRect, hand.Position.Y);
                Canvas.SetLeft(_rightRect, hand.Position.X);

                DepthCanvas.Children.Add(_rightRect);
            }
        }

        /// <summary>
        /// Notifies the user and executes defined actions for recognized gestures.
        /// </summary>
        /// <param name="leftGesture">Gesture (or null if none)</param>
        private void FrameProcessor_LeftGestureUpdated(Gesture leftGesture)
        {
            if (leftGesture != null)
            {
                String str = "Gesture detected:\n\n";

                str += leftGesture.Name;
                //str += "\nM : " + leftGesture.RecognizedData.ContourMatch.ToString("G5");
                //str += "\nC : " + leftGesture.RecognizedData.HistogramMatch.ToString("G5");
                str += "\nMxC: " + (leftGesture.RecognizedData.ContourMatch*leftGesture.RecognizedData.HistogramMatch).ToString("G5");
                //str += "\n# of fingers: " + leftGesture.FingersCount;
                //str += "\nDirection: " + "<...>";
  
                LeftResult.Text = str;
            }
            else
            {
                LeftResult.Text = "No gestures detected!";
            }  
        }

        /// <summary>
        /// Notifies the user and executes defined actions for recognized gestures.
        /// </summary>
        /// <param name="rightGesture">Gesture (or null if none)</param>
        private void FrameProcessor_RightGestureUpdated(Gesture rightGesture)
        {
            if (rightGesture != null)
            {
                String str = "Gesture detected:\n\n";

                str += rightGesture.Name;
                //str += "\nM : " + rightGesture.RecognizedData.ContourMatch.ToString("G5");
                //str += "\nC : " + rightGesture.RecognizedData.HistogramMatch.ToString("G5");
                str += "\nMxC: " + (rightGesture.RecognizedData.ContourMatch * rightGesture.RecognizedData.HistogramMatch).ToString("G5");
                //str += "\n# of fingers: " + rightGesture.FingersCount;
                //str += "\nDirection: " + "<...>";

                RightResult.Text = str;
            }
            else
            {
                RightResult.Text = "No gestures detected!";
            }
        }

        /// <summary>
        /// Notifies the user and executes defined actions for recognized dynamic gestures.
        /// </summary>
        /// <param name="leftGesture">Gesture (or null if none)</param>
        private void FrameProcessor_LeftDynamicGestureUpdated(DynamicGesture leftGesture)
        {
            if (leftGesture != null)
            {
                String str = "Dynamic gesture:\n\n";

                str += leftGesture.Name;

                LeftDynamicResult.Text = str;
            }
            else
            {
                LeftDynamicResult.Text = "";
            }
        }

        /// <summary>
        /// Notifies the user and executes defined actions for recognized dynamic gestures.
        /// </summary>
        /// <param name="rightGesture">Gesture (or null if none)</param>
        private void FrameProcessor_RightDynamicGestureUpdated(DynamicGesture rightGesture)
        {
            if (rightGesture != null)
            {
                String str = "Dynamic gesture:\n\n";

                str += rightGesture.Name;

                RightDynamicResult.Text = str;
            }
            else
            {
                RightDynamicResult.Text = "";
            }
        }

        /// <summary>
        /// Updates the UI regarding Kinect's avaliability.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private void Kinect_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (!_kinect.IsAvailable)
            {
               DepthImage.Source = (ImageSource) Resources["KinectNotAvailable"];
            }

            Console.WriteLine("Kinect status: " + (_kinect.IsAvailable ? "available" : "not available"));
        }

        /// <summary>
        /// Receives new frames from Kinect and hands them to the FrameProcessor.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame frame = e.FrameReference.AcquireFrame();

            if (frame != null)
            {
                ++_frameCount;

                // Let's drop some frames to increase performance, while keeping fluidity.
                if (_frameCount%3 == 0)
                {
                    return;
                }

                _frameProcessor.ProcessFrame(frame);
            }
        }

        private void SaveLeftGesture(object sender, RoutedEventArgs e)
        {
            Hand hand = _frameProcessor.FrameBuffer.LatestFrame().LeftHand;

            if (hand != null)
            {
                Image<Gray, byte> img = hand.MaskImage;
                img.ToBitmap().Save("left_gesture.jpg");
            }
        }

        private void SaveRightGesture(object sender, RoutedEventArgs e)
        {
            Hand hand = _frameProcessor.FrameBuffer.LatestFrame().RightHand;

            if (hand != null)
            {
                Image<Gray, byte> img = hand.MaskImage;
                img.ToBitmap().Save("right_gesture.jpg");
            }
        }
    }
}
