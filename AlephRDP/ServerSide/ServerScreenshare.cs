using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace AlephRDP.ServerSide
{
    internal class ServerScreenshare
    {
        public static void CreateFullScreenshot()
        {
            Screen[] allScreens = Screen.AllScreens;
            int totalWidth = 0;
            int maxHeight = 0;

            foreach (Screen screen in allScreens)
            {
                totalWidth += screen.Bounds.Width;
                if (screen.Bounds.Height > maxHeight)
                    maxHeight = screen.Bounds.Height;
            }
            using (Bitmap combinedBitmap = new Bitmap(totalWidth, maxHeight))
            {
                using (Graphics graphics = Graphics.FromImage(combinedBitmap))
                {
                    int offsetX = 0;
                    foreach (Screen screen in allScreens)
                    {
                        Rectangle screenBounds = screen.Bounds;
                        using (Bitmap screenBitmap = new Bitmap(screenBounds.Width, screenBounds.Height))
                        {
                            using (Graphics screenGraphics = Graphics.FromImage(screenBitmap))
                            {
                                screenGraphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
                            }
                            graphics.DrawImage(screenBitmap, offsetX, 0);
                            offsetX += screenBounds.Width;
                        }
                    }
                }
                string filePath = "screenshot.png";
                combinedBitmap.Save(filePath, ImageFormat.Png);
            }
        }
        public static void CaptureMouse()
        {
            Point cursorPosition = Cursor.Position;
            Screen[] allScreens = Screen.AllScreens;
            for (int i = 0; i < allScreens.Length; i++)
            {
                if (allScreens[i].Bounds.Contains(cursorPosition))
                {
                    CreateScreenshot(i + 1);
                    return;
                }
            }
            CreateScreenshot(1);
        }
        public static void CreateScreenshot(int screen)
        {
            if (screen == 0)
            {
                CaptureMouse();
                return;
            }
            Rectangle screenBounds = Screen.AllScreens[screen - 1].Bounds;
            using (Bitmap bitmap = new Bitmap(screenBounds.Width, screenBounds.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
                    if (IsCursorOnSelectedScreen(screen - 1))
                    {
                        int relativeX = Cursor.Position.X - screenBounds.Left;
                        int relativeY = Cursor.Position.Y - screenBounds.Top;
                        graphics.FillEllipse(Brushes.Red, relativeX - 5, relativeY - 5, 10, 10);
                    }
                }
                Bitmap resizedBitmap = new Bitmap(bitmap.Width / 2, bitmap.Height / 2);

                // Create a Graphics object to draw on the new Bitmap
                using (Graphics graphics = Graphics.FromImage(resizedBitmap))
                {
                    // Set the interpolation mode to achieve better quality scaling
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    // Draw the original Bitmap onto the new one with the new dimensions
                    graphics.DrawImage(bitmap, 0, 0, bitmap.Width / 2, bitmap.Height / 2);
                }

                string filePath = @"screenshot.png";
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }
        private static bool IsCursorOnSelectedScreen(int screenIndex)
        {
            Screen[] allScreens = Screen.AllScreens;
            if (screenIndex >= 0 && screenIndex < allScreens.Length)
            {
                Rectangle selectedScreenBounds = allScreens[screenIndex].Bounds;
                return selectedScreenBounds.Contains(Cursor.Position);
            }
            return false;
        }
        public static int GetScreenCount()
        {
            return Screen.AllScreens.Length;
        }
    }
}
