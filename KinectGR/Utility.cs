using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;
using Size = System.Drawing.Size;

namespace KinectGR
{
    /// <summary>
    /// Simple definition of a rectangle.
    /// </summary>
    public class Rect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Converts custom Rect to System.Drawing.Rectangle.
        /// </summary>
        /// <returns>System.Drawing.Rectangle</returns>
        public Rectangle ToRectangle()
        {
            return new Rectangle(X, Y, Width, Height);
        }
    }

    /// <summary>
    /// Collection of useful methods and constants.
    /// </summary>
    public class Utility
    {
        // Depth frame dimensions
        public static int FrameWidth = 512;
        public static int FrameHeight = 424;

        // Desired hand crop dimensions
        public static int HandWidth = 120;
        public static int HandHeight = 120;
        public static int HandBorder = 10; // 0 - no border

        // Reliability criteria for depth frames
        public static ushort MinReliableDepth = 500;
        public static ushort MaxReliableDepth = 4500;

        // Math constants
        public const double CosThreshold = 0.5;
        public const double EqualsThreshold = 1e-7;
        public const int Step = 8;
        public const int R = 16;
       
        // Coordinate mapper between frames
        public static CoordinateMapper CoordinateMapper = null;

        /// <summary>
        /// Returns Euclidean distance between a and b.
        /// </summary>
        /// <param name="a">a</param>
        /// <param name="b">b</param>
        /// <returns>Distance between a and b</returns>
        public static double Dist(Point a, Point b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }


        /// <summary>
        /// Returns Euclidean distance between a and b (PointF overload).
        /// </summary>
        /// <param name="a">a</param>
        /// <param name="b">b</param>
        /// <returns>Distance between a and b</returns>
        public static double Dist(PointF a, PointF b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        /// <summary>
        /// Magical function's purpose is mysterious, but functional.
        /// </summary>
        /// <param name="a">a</param>
        /// <param name="b">b</param>
        /// <param name="c">Point, with respect to which to compare</param>
        /// <returns>More than 0 if should be swapped</returns>
        public static double MagicalFunction(PointF a, PointF b, PointF c)
        {
            return a.X * b.Y + b.X * c.Y + a.Y * c.X - b.Y * c.X - a.X * c.Y - a.Y * b.X;
        }

        /// <summary>
        /// Checks the equality of two doubles
        /// </summary>
        /// <param name="a">a</param>
        /// <param name="b">b</param>
        /// <returns>true if equal, false otherwise</returns>
        public static bool IsEq(double a, double b)
        {
            return Math.Abs(a - b) <= EqualsThreshold;
        }
        
        /// <summary>
        /// Calculates angle between pt-R and pt+R
        /// </summary>
        /// <param name="contour">Contour</param>
        /// <param name="pt">Index</param>
        /// <returns>Angle</returns>
        public static double Angle(Contour<Point> contour, int pt)
        {
            int size = contour.Total;
            Point p0 = (pt > 0) ? contour[pt%size] : contour[size - 1 + pt];
            Point p1 = contour[(pt + R)%size];
            Point p2 = (pt > R) ? contour[pt - R] : contour[size - 1 - R];

            double ux = p0.X - p1.X;
            double uy = p0.Y - p1.Y;
            double vx = p0.X - p2.X;
            double vy = p0.Y - p2.Y;

            return (ux * vx + uy * vy) / Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        }

        /// <summary>
        /// Calculates rotation between points pt-R and pt+R
        /// </summary>
        /// <param name="contour">Contour</param>
        /// <param name="pt">Index</param>
        /// <returns>Rotation angle</returns>
        public static double Rotation(Contour<Point> contour, int pt)
        {
            int size = contour.Total;
            Point p0 = (pt > 0) ? contour[pt % size] : contour[size - 1 + pt];
            Point p1 = contour[(pt + R) % size];
            Point p2 = (pt > R) ? contour[pt - R] : contour[size - 1 - R];

            double ux = p0.X - p1.X;
            double uy = p0.Y - p1.Y;
            double vx = p0.X - p2.X;
            double vy = p0.Y - p2.Y;

            return (ux * vy - vx * uy);
        }

        /// <summary>
        /// Deletes int pointer object.
        /// </summary>
        /// <param name="o">Object.</param>
        /// <returns></returns>
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Convert EmguCV Image<,> to BitmapSource.
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>BitmapSource</returns>
        public static BitmapSource ConvertImageToBitmapSource(IImage image)
        {
            using (Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap();

                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        /// <summary>
        /// Converts boolean mask to bitmap source.
        /// </summary>
        /// <param name="mask">Mask</param>
        /// <param name="w">Width</param>
        /// <param name="h">Height</param>
        /// <returns>BitmapSource</returns>
        public static Image<Gray, byte> ConvertMaskToImage(bool[] mask, int w, int h)
        {
            Image<Gray, byte> img = new Image<Gray, byte>(w, h);

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    img[y, x] = mask[y * w + x] ? new Gray(255) : new Gray(0);
                }
            }

            return img;
        }

        /// <summary>
        /// Maps 3D coordinates to the 2D space of a depth map
        /// </summary>
        /// <param name="position">3D position</param>
        /// <returns>2D point</returns>
        public static DepthSpacePoint ConvertBodyToDepthCoordinate(CameraSpacePoint position)
        {
            if (position.Z < 0)
            {
                position.Z = 0;
            }

            if (CoordinateMapper != null)
            {
                return CoordinateMapper.MapCameraPointToDepthSpace(position);
            }
            else
            {
                throw new Exception("Initialize the CoordinateMapper before calling this helper function!");
            }
        }

        /// <summary>
        /// Maps 2D coordinate to 3D space position.
        /// </summary>
        /// <param name="position">Coordinate</param>
        /// <param name="depth">Depth value</param>
        /// <returns>3D position</returns>
        public static CameraSpacePoint ConvertDepthToBodyCoordinate(DepthSpacePoint position, ushort depth)
        {
            if (CoordinateMapper != null)
            {
                return CoordinateMapper.MapDepthPointToCameraSpace(position, depth);
            }
            else
            {
                throw new Exception("Initialize the CoordinateMapper before calling this helper function!");
            }
        }

        /// <summary>
        /// Maps 3D coordinate point to the 2D space of resized hand mask.
        /// </summary>
        /// <param name="point">3D point</param>
        /// <param name="frame">Rectangle</param>
        /// <returns>Point</returns>
        public static Point ConvertFrameToResizedMaskCoordinate(CameraSpacePoint point, Rect frame)
        {
            DepthSpacePoint p = ConvertBodyToDepthCoordinate(point);

            int cx = (int)p.X - frame.X;
            int cy = (int)p.Y - frame.Y;

            double ratio = 1.0;
            if (frame.Width > frame.Height)
            {
                ratio = (double)Utility.HandWidth / (double)frame.Width;
            }
            else
            {
                ratio = (double)Utility.HandHeight / (double)frame.Height;
            }

            int newWidth = (int)Math.Floor(ratio * frame.Width);
            int newHeight = (int)Math.Floor(ratio * frame.Height);

            int xOffset = (Utility.HandWidth - newWidth) / 2;
            int yOffset = (Utility.HandHeight - newHeight) / 2;

            cx = (int)Math.Floor(cx * ratio);
            cy = (int)Math.Floor(cy * ratio);

            return new Point(cx + xOffset + HandBorder, cy + yOffset + HandBorder);
        }

        /// <summary>
        /// Crops and resizes hand to standard dimensions.
        /// </summary>
        /// <param name="mask">Mask</param>
        /// <param name="x">X of hand</param>
        /// <param name="y">Y of hand</param>
        /// <param name="width">Width of hand</param>
        /// <param name="height">Height of hand</param>
        /// <returns>Cropped mask</returns>
        public static bool[] CropAndResize(bool[] mask, int x, int y, int width, int height)
        {
            bool[] newMask = new bool[width * height];

            int bi = 0;
            for (int yi = y; yi < y + height; ++yi)
            {
                for (int xi = x; xi < x + width; ++xi)
                {
                    newMask[bi] = mask[yi * FrameWidth + xi];
                    ++bi;
                }
            }

            bool[] resizedMask = new bool[HandWidth * HandHeight];

            double ratio = 1.0;
            if (width > height)
            {
                ratio = (double)(HandWidth - HandBorder * 2) / (double)width;
            }
            else
            {
                ratio = (double)(HandHeight - HandBorder * 2) / (double)height;
            }

            int newWidth = (int)Math.Floor(ratio * width);
            int newHeight = (int)Math.Floor(ratio * height);

            int xOffset = (HandWidth - newWidth) / 2;
            int yOffset = (HandHeight - newHeight) / 2;
            int px, py, yf, xf;

            for (int yi = 0; yi < newHeight; yi++)
            {
                for (int xi = 0; xi < newWidth; xi++)
                {
                    px = (int)Math.Floor(xi / ratio);
                    py = (int)Math.Floor(yi / ratio);

                    yf = yOffset + yi;
                    xf = xOffset + xi;
                    resizedMask[yf * HandWidth + xf] = newMask[py * width + px];
                }
            }

            newMask = null;
            return resizedMask;
        }

        /// <summary>
        /// Renders depth frame to bitmap source.
        /// </summary>
        /// <param name="depthFrame">Depth frame</param>
        /// <returns>Bitmap source</returns>
        public static BitmapSource DrawDepthFrame(ushort[] depthFrame)
        {
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixelData = new byte[FrameWidth * FrameHeight * (format.BitsPerPixel + 7) / 8];

            int colorIndex = 0;
            for (int i = 0; i < depthFrame.Length; ++i)
            {
                ushort depth = depthFrame[i];

                if (depth > MaxReliableDepth)
                {
                    pixelData[colorIndex] = 255; // blue
                    pixelData[colorIndex + 1] = 255; // green
                    pixelData[colorIndex + 2] = 255; // red
                }
                else if (depth < MinReliableDepth)
                {
                    pixelData[colorIndex] = 255; // blue
                    pixelData[colorIndex + 1] = 255; // green
                    pixelData[colorIndex + 2] = 255; // red
                }
                else
                {
                    byte intensity = (byte)((depth - 500) * 255 / (MaxReliableDepth - MinReliableDepth));
                    pixelData[colorIndex] = intensity; // blue
                    pixelData[colorIndex + 1] = 0; // green
                    pixelData[colorIndex + 2] = 0; // red
                }

                colorIndex += 4;
            }

            int stride = FrameWidth * format.BitsPerPixel / 8;
            return BitmapSource.Create(FrameWidth, FrameHeight, 96, 96, format, null, pixelData, stride);
        }

        /// <summary>
        /// Converts direction enum to printable string.
        /// </summary>
        /// <param name="direction">Direction</param>
        /// <returns>Direction as string</returns>
        public static String DirectionToString(Direction direction)
        {
            if (direction == Direction.DirectionDown)
            {
                return "Down";
            }
            else if (direction == Direction.DirectionRight)
            {
                return "Right";
            }
            else if (direction == Direction.DirectionUp)
            {
                return "Up";
            }
            else if (direction == Direction.DirectionLeft)
            {
                return "Left";
            }
            else
            {
                return "Unknown";
            }
        }
    }
}
