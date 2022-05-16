using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace FbDelete
{
    // Import fun stuff
    static partial class Program
    {
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, uint dwExtraInfo);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder ss, int count);
    }

    // Program
    static partial class Program
    {
        enum SearchState
        {
            Running,
            Finished
        }

        static SearchState state = SearchState.Finished;
        static Thread searchThread;

        // TODO: Turn into array
        static Bitmap messageShort = new Bitmap("search/message_short.png");
        static Bitmap messageWide = new Bitmap("search/message_wide.png");
        static Bitmap messageEnd = new Bitmap("search/message_end.png");
        static Bitmap messageTop = new Bitmap("search/message_top.png");
        static Bitmap messageCorner = new Bitmap("search/message_corner.png");

        static Bitmap dotdotdot = new Bitmap("search/dotdotdot.png");
        static Bitmap remove1 = new Bitmap("search/remove1.png");
        static Bitmap remove2 = new Bitmap("search/remove2.png");

        static Point lastPoint;

        static void Main()
        {
            Console.WriteLine("Dear Facebook Engineers,\nyour html obfuscation hurt my feelings so I made this.\n...Get rekt'd nerds!");

            bool runApp = true;
            while (runApp)
            {
                if (state == SearchState.Finished && ActiveWindowTitle().Contains("Messenger | Facebook"))
                {
                    state = SearchState.Running;
                    searchThread = new Thread(() => Search());
                    searchThread.IsBackground = true;
                    searchThread.Start();
                }

                // Check input
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) runApp = false;
                }

                Thread.Sleep(1);
            }
        }

        static void Search()
        {
            Console.WriteLine($"Searching...");
            Point point;
            double tol = 0.0;
            
            // TODO: Turn into array
            point = SearchBitmap(messageEnd, CaptureScreen(), tol);
            if (point == Point.Empty) point = SearchBitmap(messageShort, CaptureScreen(), tol);
            if (point == Point.Empty) point = SearchBitmap(messageWide, CaptureScreen(), tol);
            if (point == Point.Empty) point = SearchBitmap(messageTop, CaptureScreen(), tol);
            if (point == Point.Empty) point = SearchBitmap(messageCorner, CaptureScreen(), tol);

            // Weird bug where it sometimes grabs the same point as last time despite the screen having updated since then.
            // I did try making it run slower - it didn't work.
            if (point != Point.Empty && point != lastPoint)
            {
                try
                {
                    lastPoint = point;

                    Console.WriteLine(point);

                    // Move to ...
                    point.Offset(-50, 10);
                    Cursor.Position = point;
                    Thread.Sleep(200);
                    // TODO: Make `SearchBitmap()` also check the current window is appropriate
                    Point dotdotdotPoint = SearchBitmap(dotdotdot, CaptureScreen(), tol);
                    Cursor.Position = dotdotdotPoint;

                    // Click it
                    SendMouseDown();
                    Thread.Sleep(10);
                    SendMouseUp();

                    // Move to remove1
                    Thread.Sleep(100);
                    Point remove1Point = SearchBitmap(remove1, CaptureScreen(), tol);
                    Cursor.Position = remove1Point;

                    // Click it
                    Thread.Sleep(10);
                    Click();

                    // Move to remove2
                    Thread.Sleep(200);
                    Point remove2Point = SearchBitmap(remove2, CaptureScreen(), tol);
                    Cursor.Position = remove2Point;

                    // Click it
                    Thread.Sleep(10);
                    Click();
                }
                catch (Exception ex)
                {
                    // TODO: Handle this differently
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Found no points...");
                for(int i = 0; i < 10; i++)
                {
                    // Scroll up
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, 120, 0);
                }
                Thread.Sleep(400);
            }

            Thread.Sleep(600);
            state = SearchState.Finished;
        }

        static Bitmap CaptureScreen()
        {
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(Point.Empty, Point.Empty, Screen.PrimaryScreen.Bounds.Size);

            return bmp;
        }

        static void Click()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 50, 50, 0, 0);
            Thread.Sleep(10); // Probably not needed
            mouse_event(MOUSEEVENTF_LEFTUP, 50, 50, 0, 0);
        }

        private static string ActiveWindowTitle()
        {
            //Create the variable
            const int nChar = 256;
            StringBuilder ss = new StringBuilder(nChar);

            //Run GetForeGroundWindows and get active window informations
            //assign them into handle pointer variable
            IntPtr handle = IntPtr.Zero;
            handle = GetForegroundWindow();

            if (GetWindowText(handle, ss, nChar) > 0) return ss.ToString();
            else return "";
        }

        // Source: https://www.codeproject.com/Articles/38619/Finding-a-Bitmap-contained-inside-another-Bitmap
        private static Point SearchBitmap(Bitmap smallBmp, Bitmap bigBmp, double tolerance)
        {
            BitmapData smallData =
              smallBmp.LockBits(new Rectangle(0, 0, smallBmp.Width, smallBmp.Height),
                       ImageLockMode.ReadOnly,
                       PixelFormat.Format24bppRgb);
            BitmapData bigData =
              bigBmp.LockBits(new Rectangle(0, 0, bigBmp.Width, bigBmp.Height),
                       ImageLockMode.ReadOnly,
                       PixelFormat.Format24bppRgb);

            int smallStride = smallData.Stride;
            int bigStride = bigData.Stride;

            int bigWidth = bigBmp.Width;
            int bigHeight = bigBmp.Height - smallBmp.Height + 1;
            int smallWidth = smallBmp.Width * 3;
            int smallHeight = smallBmp.Height;

            Point location = Point.Empty;
            int margin = Convert.ToInt32(255.0 * tolerance);

            unsafe
            {
                byte* pSmall = (byte*)(void*)smallData.Scan0;
                byte* pBig = (byte*)(void*)bigData.Scan0;

                int smallOffset = smallStride - smallBmp.Width * 3;
                int bigOffset = bigStride - bigBmp.Width * 3;

                bool matchFound = true;

                // TODO: Make this search from bottom to top
                for (int y = 0; y < bigHeight; y++)
                {
                    for (int x = 0; x < bigWidth; x++)
                    {
                        byte* pBigBackup = pBig;
                        byte* pSmallBackup = pSmall;

                        //Look for the small picture.
                        for (int i = 0; i < smallHeight; i++)
                        {
                            int j = 0;
                            matchFound = true;
                            for (j = 0; j < smallWidth; j++)
                            {
                                //With tolerance: pSmall value should be between margins.
                                int inf = pBig[0] - margin;
                                int sup = pBig[0] + margin;
                                if (sup < pSmall[0] || inf > pSmall[0])
                                {
                                    matchFound = false;
                                    break;
                                }

                                pBig++;
                                pSmall++;
                            }

                            if (!matchFound) break;

                            //We restore the pointers.
                            pSmall = pSmallBackup;
                            pBig = pBigBackup;

                            //Next rows of the small and big pictures.
                            pSmall += smallStride * (1 + i);
                            pBig += bigStride * (1 + i);
                        }

                        //If match found, we return.
                        if (matchFound)
                        {
                            location.X = x;
                            location.Y = y;
                            break;
                        }
                        //If no match found, we restore the pointers and continue.
                        else
                        {
                            pBig = pBigBackup;
                            pSmall = pSmallBackup;
                            pBig += 3;
                        }
                    }

                    if (matchFound) break;

                    pBig += bigOffset;
                }
            }

            bigBmp.UnlockBits(bigData);
            smallBmp.UnlockBits(smallData);

            return location;
        }

    }
}