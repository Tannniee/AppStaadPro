using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StaadSteelApp.Core
{
    internal static class WindowPlacementHelper
    {
        public static Point EnsureVisibleLocation(Point location, Size size)
        {
            return EnsureVisibleBounds(new Rectangle(location, size)).Location;
        }

        public static Rectangle EnsureVisibleBounds(Rectangle bounds)
        {
            Rectangle workingArea = Screen.FromRectangle(bounds).WorkingArea;
            return ClampToWorkingArea(bounds, workingArea);
        }

        public static Point GetDefaultVisibleLocation(Size size)
        {
            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Point.Empty).WorkingArea;
            Rectangle defaultBounds = ClampToWorkingArea(
                new Rectangle(
                    workingArea.Left + 100,
                    workingArea.Top + 100,
                    size.Width,
                    size.Height),
                workingArea);
            return defaultBounds.Location;
        }

        public static Point GetCenteredLocation(Size size)
        {
            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Point.Empty).WorkingArea;
            int x = workingArea.Left + ((workingArea.Width - size.Width) / 2);
            int y = workingArea.Top + ((workingArea.Height - size.Height) / 2);
            Rectangle centeredBounds = ClampToWorkingArea(new Rectangle(new Point(x, y), size), workingArea);
            return centeredBounds.Location;
        }

        public static string GetDisplaySignature()
        {
            return string.Join(
                "|",
                Screen.AllScreens
                    .OrderBy(screen => screen.Bounds.Left)
                    .ThenBy(screen => screen.Bounds.Top)
                    .Select(screen =>
                        $"{screen.DeviceName}:{screen.Primary}:{screen.Bounds.Left},{screen.Bounds.Top},{screen.Bounds.Width},{screen.Bounds.Height}"));
        }

        private static Rectangle ClampToWorkingArea(Rectangle bounds, Rectangle workingArea)
        {
            int width = System.Math.Min(bounds.Width, workingArea.Width);
            int height = System.Math.Min(bounds.Height, workingArea.Height);

            int x = bounds.X;
            if (x < workingArea.Left)
            {
                x = workingArea.Left;
            }
            else if (x + width > workingArea.Right)
            {
                x = workingArea.Right - width;
            }

            int y = bounds.Y;
            if (y < workingArea.Top)
            {
                y = workingArea.Top;
            }
            else if (y + height > workingArea.Bottom)
            {
                y = workingArea.Bottom - height;
            }

            return new Rectangle(x, y, width, height);
        }
    }
}
