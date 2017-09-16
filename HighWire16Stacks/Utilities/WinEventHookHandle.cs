using System;
using System.Runtime.InteropServices;

namespace HighWire16Stacks.Utilities
{
    internal class WinEventHookHandle : SafeHandle
    {
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        private readonly WinEventDelegate m_delegate;

        public const int EVENT_SYSTEM_FOREGROUND = 0x3;
        public const int WINEVENT_OUTOFCONTEXT = 0x0;

        public static SafeHandle SetForegroundEvent(WinEventDelegate @delegate)
        {
            return new WinEventHookHandle(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                @delegate,
                0,
                0,
                WINEVENT_OUTOFCONTEXT);
        }

        private WinEventHookHandle(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags)
            : base(IntPtr.Zero, true)
        {
            this.m_delegate = lpfnWinEventProc;

            this.handle = NativeMethods.SetWinEventHook(eventMin, eventMax, hmodWinEventProc, this.m_delegate, idProcess, idThread, dwFlags);
            this.SetHandle(this.handle);
        }

        protected override bool ReleaseHandle()
        {
            if (!NativeMethods.UnhookWinEvent(this.handle))
                return false;

            this.handle = IntPtr.Zero;
            return true;
        }

        public override bool IsInvalid { get { return this.handle == IntPtr.Zero; } }

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
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWinEvent(
                IntPtr hWinEventHook);
        }
    }
}
