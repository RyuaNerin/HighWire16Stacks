using System;
using System.Runtime.InteropServices;

namespace FFXIVBuff.Core
{
    internal static class WinEventHook
    {
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        public static SafeHandle SetForegroundEvent(WinEventDelegate @delegate)
        {
            var hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                @delegate,
                0,
                0,
                NativeMethods.WINEVENT_OUTOFCONTEXT);

            if (hook == IntPtr.Zero)
                return null;
            else
                return new WinEventHookHandle(hook);
        }

        private class WinEventHookHandle : SafeHandle
        {
            public WinEventHookHandle(IntPtr ptr)
                : base(IntPtr.Zero, true)
            {
                this.handle = ptr;
                this.SetHandle(ptr);
            }

            protected override bool ReleaseHandle()
            {
                if (!NativeMethods.UnhookWinEvent(this.handle))
                    return false;

                this.handle = IntPtr.Zero;
                return true;
            }

            public override bool IsInvalid { get { return this.handle == IntPtr.Zero; } }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(
                uint eventMin,
                uint eventMax,
                IntPtr hmodWinEventProc,
                WinEventDelegate lpfnWinEventProc,
                uint idProcess,
                uint idThread,
                uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(
                IntPtr hWinEventHook);

            public const int EVENT_SYSTEM_FOREGROUND = 0x3;

            public const int WINEVENT_OUTOFCONTEXT = 0;
        }
    }
}
