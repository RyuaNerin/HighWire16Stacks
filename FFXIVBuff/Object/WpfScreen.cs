using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using DPoint = System.Drawing.Point;
using WPoint = System.Windows.Point;

namespace FFXIVBuff.Object
{
    using Window = System.Windows.Window;

    public class WpfScreen
    {
        private static Rect GetRect(Rectangle value)
        {
            return new Rect(value.X, value.Y, value.Width, value.Height);
        }

        public static WpfScreen[] AllScreens
        {
            get
            {
                var screens = Screen.AllScreens;
                var wpfScreens = new WpfScreen[screens.Length];
                for (int i = 0; i < screens.Length; ++i)
                    wpfScreens[i] = new WpfScreen(screens[i]);
                return wpfScreens;
            }
        }

        public static WpfScreen PrimaryScreen
        {
            get { return new WpfScreen(Screen.PrimaryScreen); }
        }

        public static Screen GetScreen(FrameworkElement element)
        {
            return WpfScreen.GetScreen(Window.GetWindow(element));
        }
        public static Screen GetScreen(WPoint point)
        {
            return Screen.FromPoint(new DPoint(
                (int)Math.Round(point.X),
                (int)Math.Round(point.Y)));
        }
        public static Screen GetScreen(Rect rect)
        {
            return Screen.FromRectangle(new Rectangle(
                (int)Math.Round(rect.X),
                (int)Math.Round(rect.Y),
                (int)Math.Round(rect.Width),
                (int)Math.Round(rect.Height)));
        }
        public static Screen GetScreen(Window window)
        {
            return Screen.FromHandle(new WindowInteropHelper(window).Handle);
        }

        public static WpfScreen FromElement(FrameworkElement element)
        {
            return new WpfScreen(WpfScreen.GetScreen(element));
        }
        public static WpfScreen FromPoint(WPoint point)
        {
            return new WpfScreen(WpfScreen.GetScreen(point));
        }
        public static WpfScreen FromRectangle(Rect rect)
        {
            return new WpfScreen(WpfScreen.GetScreen(rect));
        }
        public static WpfScreen FromWindow(Window window)
        {
            return new WpfScreen(WpfScreen.GetScreen(window));
        }

        public static Rect GetBounds(FrameworkElement element)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(element).Bounds);
        }
        public static Rect GetBounds(WPoint pt)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(pt).Bounds);
        }
        public static Rect GetBounds(Rect rect)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(rect).Bounds);
        }
        public static Rect GetBounds(Window window)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(window).Bounds);
        }

        public static Rect GetWorkingArea(FrameworkElement element)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(element).WorkingArea);
        }
        public static Rect GetWorkingArea(WPoint pt)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(pt).WorkingArea);
        }
        public static Rect GetWorkingArea(Rect rect)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(rect).WorkingArea);
        }
        public static Rect GetWorkingArea(Window window)
        {
            return WpfScreen.GetRect(WpfScreen.GetScreen(window).WorkingArea);
        }

        private readonly Screen m_screen;
        private WpfScreen(Screen screen)
        {
            this.m_screen = screen;
        }

        public Screen Screen
        {
            get { return this.m_screen; }
        }

        public int BitsPerPixel
        {
            get { return this.m_screen.BitsPerPixel; }
        }

        public Rect Bounds
        {
            get { return WpfScreen.GetRect(this.m_screen.Bounds); }
        }

        public string DeviceName
        {
            get { return this.m_screen.DeviceName; }
        }

        public bool Primary
        {
            get { return this.m_screen.Primary; }
        }

        public Rect WorkingArea
        {
            get { return WpfScreen.GetRect(this.m_screen.WorkingArea); }
        }

        public override bool Equals(object obj)
        {
            var wpfScreen = obj as WpfScreen;
            if (wpfScreen == null) return false;

            return this.m_screen.Equals(wpfScreen.m_screen);
        }
        public override int GetHashCode()
        {
            return this.m_screen.GetHashCode();
        }
        public override string ToString()
        {
            return this.m_screen.ToString();
        }


    }
}
