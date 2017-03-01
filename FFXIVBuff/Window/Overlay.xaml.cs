using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using FFXIVBuff.Core;
using System.Windows.Threading;

namespace FFXIVBuff.Window
{
    public partial class Overlay : System.Windows.Window
    {
        private readonly bool m_clickThrough;

        private readonly DispatcherTimer m_timer;

        private IntPtr m_handle;
        public IntPtr Handle { get { return this.m_handle; } }

        public Overlay(bool clickThrough)
        {
            this.m_clickThrough = clickThrough;

            InitializeComponent();

            this.DataContext = Core.Settings.Instance;

            this.ctlStatusesList.ItemsSource = Worker.Statuses;

            this.m_timer = new DispatcherTimer();
            this.m_timer.Tick += m_timer_Tick;
            this.m_timer.Interval = new TimeSpan(0, 0, 5);
            this.m_timer.Start();
        }

        private void m_timer_Tick(object sender, EventArgs e)
        {
            this.Topmost = false;
            this.Topmost = true;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);


            var interop = new WindowInteropHelper(this);
            interop.EnsureHandle();
            this.m_handle = new WindowInteropHelper(this).Handle;

            var v = NativeMethods.GetWindowLong(this.m_handle, NativeMethods.GWL_EXSTYLE);

            if (this.m_clickThrough)
                v |= NativeMethods.WS_EX_TRANSPARENT;

            v |= NativeMethods.WS_EX_NOACTIVATE;

            NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE, v);
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            public const int GWL_EXSTYLE = -20;
            public const int WS_EX_NOACTIVATE = 0x08000000;
            public const int WS_EX_TRANSPARENT = 0x00000020;
        }
    }
}
