using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("OpenCvLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitializeCamera();

        [DllImport("OpenCvLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseCamera();

        [DllImport("OpenCvLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetFrame(IntPtr buffer, out int width, out int height);
        [DllImport("OpenCvLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLineCoordinates(int x1, int y1, int x2, int y2);
        [DllImport("OpenCvLib.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CheckObjectCrossing();

        private int width = 1920; // Valoare implicită pentru lățime
        private int height = 1080; // Valoare implicită pentru înălțime
                                   // Buffer de cadre
        private byte[] buffer;
        private object bufferLock = new object(); // Sincronizare pentru buffer
        private volatile bool capturing = true; // Controlul capturii

        // Timer pentru afișarea FPS-ului
        private DispatcherTimer fpsTimer;
        private int frameCount = 0;
        private double fps = 0;
        private System.Windows.Point endPoint;
        private System.Windows.Point startPoint;
        private bool isDrawing = false;

        public MainWindow()
        {
            InitializeComponent();
            // Inițializare cameră și buffer
            InitializeCamera();
            buffer = new byte[width * height * 3]; // Alocare buffer

            VideoImage.MouseDown += VideoImage_MouseDown;
            VideoImage.MouseMove += VideoImage_MouseMove;
            VideoImage.MouseUp += VideoImage_MouseUp;

            // Pornire fir de execuție pentru captură
            Thread captureThread = new Thread(new ThreadStart(CaptureThread));
            captureThread.IsBackground = true;
            captureThread.Start();

            // Inițializare timer pentru afișarea FPS-ului
            fpsTimer = new DispatcherTimer();
            fpsTimer.Interval = TimeSpan.FromSeconds(1);
            fpsTimer.Tick += FpsTimer_Tick;
            fpsTimer.Start();
        }

        private void VideoImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                startPoint = e.GetPosition(VideoOverlay);
                isDrawing = true;
            }
        }
        private void VideoImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                endPoint = e.GetPosition(VideoOverlay);
                DrawOverlayLine();
            }
        }

        private void VideoImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                endPoint = e.GetPosition(VideoOverlay);
                isDrawing = false;
                DrawOverlayLine();

                // Adjust coordinates to match the video frame size
                //System.Windows.Point adjustedStartPoint = AdjustCoordinates(startPoint);
                //System.Windows.Point adjustedEndPoint = AdjustCoordinates(endPoint);

                // Send line coordinates to C++
                SetLineCoordinates((int)startPoint.X, (int)startPoint.Y, (int)endPoint.X, (int)endPoint.Y);
            }
        }

        private void DrawOverlayLine()
        {
            VideoOverlay.Children.Clear();
            Line line = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = endPoint.X,
                Y2 = endPoint.Y,
                Stroke = Brushes.Green,
                StrokeThickness = 2
            };
            VideoOverlay.Children.Add(line);
        }

        private System.Windows.Point AdjustCoordinates(System.Windows.Point point)
        {
            double imageAspectRatio = (double)width / height;
            double controlAspectRatio = VideoImage.ActualWidth / VideoImage.ActualHeight;

            double offsetX = 0;
            double offsetY = 0;
            double scaleFactor = 1;

            if (controlAspectRatio > imageAspectRatio)
            {
                // Control is wider proportionally than the image
                scaleFactor = VideoImage.ActualHeight / height;
                offsetX = (VideoImage.ActualWidth - width * scaleFactor) / 2;
            }
            else
            {
                // Control is taller proportionally than the image
                scaleFactor = VideoImage.ActualWidth / width;
                offsetY = (VideoImage.ActualHeight - height * scaleFactor) / 2;
            }

            double adjustedX = (point.X - offsetX) / scaleFactor;
            double adjustedY = (point.Y - offsetY) / scaleFactor;

            return new System.Windows.Point(adjustedX, adjustedY);
        }

        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            // Calcul FPS
            fps = frameCount;
            FpsTextBlock.Text = $"FPS: {fps:F1}";

            // Resetare număr cadre
            frameCount = 0;
        }

        private void CaptureThread()
        {
            while (capturing)
            {
                // Capturare cadru
                int currentWidth, currentHeight;
                IntPtr ptr;
                lock (bufferLock)
                {
                    GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    ptr = handle.AddrOfPinnedObject();
                    bool success = GetFrame(ptr, out currentWidth, out currentHeight);
                    handle.Free();
                }

                if (currentWidth > 0 && currentHeight > 0)
                {
                    // Actualizare dimensiuni și afișare cadru în interfață
                    Dispatcher.Invoke(() =>
                    {
                        if (buffer != null)
                        {
                            lock (bufferLock)
                            {
                                var bitmap = new WriteableBitmap(currentWidth, currentHeight, 96, 96, PixelFormats.Bgr24, null);
                                bitmap.WritePixels(new Int32Rect(0, 0, currentWidth, currentHeight), buffer, currentWidth * 3, 0);
                                VideoImage.Source = bitmap;
                            }
                        }
                    });

                    // Incrementare număr cadre pentru calcul FPS
                    Interlocked.Increment(ref frameCount);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Oprire captură și eliberare resurse
            capturing = false;
            Thread.Sleep(500); // Așteaptă puțin să se încheie captura
            ReleaseCamera();

            base.OnClosed(e);
        }
    }
}