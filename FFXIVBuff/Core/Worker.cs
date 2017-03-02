using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FFXIVBuff.Windows;
using Newtonsoft.Json;

namespace FFXIVBuff.Core
{
    internal static class Worker
    {
        //v3.21, 2016.12.26.0000.0000(2245781, ex1:2016.12.26.0000.0000)
        public static readonly MemoryOffsets DefaultMemoryOffset =
            new MemoryOffsets
            {
                count = 21,
                x86 = new MemoryOffset
                {
                    ptr = 0x0F67940,
                    off = 0x0001518
                },
                x64 = new MemoryOffset
                {
                    ptr = 0x1551FE0,
                    off = 0x00018E0
                }
            };

        private static object m_overlayInstanceSync = new object();
        private static Overlay m_overlayInstance;
        public static Overlay OverlayInstance
        {
            get
            {
                lock (m_overlayInstanceSync)
                {
                    if (m_overlayInstance == null)
                        m_overlayInstance = new Overlay(Settings.Instance.ClickThrough);

                    return m_overlayInstance;
                }
            }
        }

        public static void SetClickThrough(bool enabled)
        {
            lock (m_overlayInstanceSync)
            {
                if (m_overlayInstance != null)
                {
                    m_overlayInstance.Close();
                    m_overlayInstance = null;
                }

                m_overlayInstance = new Overlay(enabled);
                m_overlayInstance.Show();
            }
        }

        public static UStatus[] Statuses;

        private static volatile bool m_running = false;
        private static Task m_task;
        private static Process m_ffxivProcess;
        private static IntPtr m_ffxivHandle;
        private static IntPtr m_ffxivModulePtr;
        private static bool m_ffxivDx11;

        private static MemoryOffsets m_memoryOffsets;
        private static MemoryOffset m_memoryOffset;

        private static int m_delay = 1;
        public static int Delay
        {
            get { return m_delay; }
            set { m_delay = value; }
        }

        public static IntPtr m_ffxivWindowHandle { get; private set; }

        static Worker()
        {
            if (Settings.Instance != null)
                Delay = (int)Settings.Instance.RefreshTime;
        }

        public static void Update()
        {
            try
            {
                var req = HttpWebRequest.Create("https://raw.githubusercontent.com/RyuaNerin/FBOverlay/master/patch.json") as HttpWebRequest;
                req.UserAgent = Assembly.GetExecutingAssembly().FullName;
                req.Timeout = 5000;
                using (var res = req.GetResponse())
                using (var stream = res.GetResponseStream())
                using (var reader = new StreamReader(stream))
                    m_memoryOffsets = JsonConvert.DeserializeObject<MemoryOffsets>(reader.ReadToEnd());
            }
            catch
            {
                m_memoryOffsets = DefaultMemoryOffset;
            }
            
            Statuses = new UStatus[m_memoryOffsets.count];
            for (int i = 0; i < m_memoryOffsets.count; ++i)
                Statuses[i] = new UStatus();
        }


        public static void Stop()
        {
            for (int i = 0; i < m_memoryOffsets.count; ++i)
                Statuses[i].Clear();

            m_running = false;
            MainWindow.Instance.Dispatcher.BeginInvoke(new Action(MainWindow.Instance.ExitedProcess));
        }

        public static bool SetProcess(Process process)
        {
            if (m_ffxivProcess != null)
            {
                m_ffxivProcess.Dispose();
                m_ffxivProcess = null;
            }

            try
            {
                m_ffxivProcess = process;
                m_ffxivProcess.EnableRaisingEvents = true;

                m_ffxivModulePtr = m_ffxivProcess.MainModule.BaseAddress;
                m_ffxivHandle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, m_ffxivProcess.Id);
                m_ffxivDx11 = !NativeMethods.IsX86Process(m_ffxivHandle);

                m_ffxivWindowHandle = process.MainWindowHandle;

                m_memoryOffset = m_ffxivDx11 ? m_memoryOffsets.x64 : m_memoryOffsets.x86;

                m_running = true;
                m_task = Task.Factory.StartNew(WorkerThread);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static void WorkerThread()
        {
            IntPtr ptr;
            byte[] buff = new byte[12 * m_memoryOffsets.count];
            int i;

            int id;
            short param;
            float remain;
            uint owner;

            while (m_running)
            {
                ptr = NativeMethods.ReadPointer(m_ffxivHandle, m_ffxivDx11, buff, m_ffxivModulePtr + m_memoryOffset.ptr);
                if (ptr == IntPtr.Zero)
                {
                    Stop();
                    return;
                }

                if (NativeMethods.ReadBytes(m_ffxivHandle, ptr + m_memoryOffset.off, buff, buff.Length) == buff.Length)
                {
                    for (i = 0; i < m_memoryOffsets.count; ++i)
                    {
                        id = BitConverter.ToInt16(buff, 12 * i + 0);
                        if (id == 0)
                        {
                            try
                            {
                                Statuses[i].Clear();
                            }
                            catch
                            {
                            }
                            continue;
                        }

                        param  = BitConverter.ToInt16(buff, 12 * i + 2);
                        remain = BitConverter.ToSingle(buff, 12 * i + 4);
                        owner  = BitConverter.ToUInt32(buff, 12 * i + 8);

                        try
                        {
                            Statuses[i].Update(id, param, remain);
                        }
                        catch
                        {
                        }
                    }
                }

                Thread.Sleep(m_delay);
            }
        }

        private static NativeMethods.WinEventDelegate m_autohideDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
        private static IntPtr m_eventhook;
        public static void SetAutohide(bool enabled)
        {
            if (enabled)
            {
                m_eventhook = NativeMethods.SetWinEventHook(
                    NativeMethods.EVENT_SYSTEM_FOREGROUND,
                    NativeMethods.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    m_autohideDelegate,
                    0,
                    0,
                    NativeMethods.WINEVENT_OUTOFCONTEXT);
            }
            else
            {
                NativeMethods.UnhookWinEvent(m_eventhook);

                m_overlayInstance.Visibility = Visibility.Visible;
            }
        }
        
        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == m_overlayInstance.Handle)
                return;

            if (hwnd == Worker.m_ffxivWindowHandle ||
                hwnd == MainWindow.Instance.Handle)
            {
                m_overlayInstance.Visibility = Visibility.Visible;
            }
            else
            {
                m_overlayInstance.Visibility = Visibility.Hidden;
            }
        }

        private static class NativeMethods
        {
            public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

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

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int X,
                int Y,
                int cx,
                int cy,
                uint uFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool WriteProcessMemory(
                IntPtr hProcess,
                IntPtr lpBaseAddress,
                byte[] lpBuffer,
                IntPtr nSize,
                [Out]
                out IntPtr lpNumberOfBytesWritten);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ReadProcessMemory(
                IntPtr hProcess,
                IntPtr lpBaseAddress,
                byte[] lpBuffer,
                IntPtr nSize,
                [Out]
                out IntPtr lpNumberOfBytesRead);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(
                ProcessAccessFlags dwDesiredAccess,
                [MarshalAs(UnmanagedType.Bool)]
                bool bInheritHandle,
                int dwProcessId);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process(
                IntPtr process,
                [Out, MarshalAs(UnmanagedType.Bool)]
                out bool wow64Process);

            [StructLayout(LayoutKind.Sequential)]
            public struct SYSTEM_INFO
            {
                public int ProcessorArchitecture; // WORD
                public uint PageSize; // DWORD
                public IntPtr MinimumApplicationAddress; // (long)void*
                public IntPtr MaximumApplicationAddress; // (long)void*
                public IntPtr ActiveProcessorMask; // DWORD*
                public uint NumberOfProcessors; // DWORD (WTF)
                public uint ProcessorType; // DWORD
                public uint AllocationGranularity; // DWORD
                public ushort ProcessorLevel; // WORD
                public ushort ProcessorRevision; // WORD
            }

            [Flags]
            public enum ProcessAccessFlags : uint
            {
                All = 0x001F0FFF,
                Terminate = 0x00000001,
                CreateThread = 0x00000002,
                VirtualMemoryOperation = 0x00000008,
                VirtualMemoryRead = 0x00000010,
                VirtualMemoryWrite = 0x00000020,
                DuplicateHandle = 0x00000040,
                CreateProcess = 0x000000080,
                SetQuota = 0x00000100,
                SetInformation = 0x00000200,
                QueryInformation = 0x00000400,
                QueryLimitedInformation = 0x00001000,
                Synchronize = 0x00100000
            }

            
            public const int HWND_TOPMOST = -1;
            public const int SWP_NOMOVE = 0x2;
            public const int SWP_NOSIZE = 0x1;

            public const int EVENT_SYSTEM_FOREGROUND = 0x3;

            public const int WINEVENT_OUTOFCONTEXT = 0;

            public static bool IsX86Process(IntPtr handle)
            {
                if (IntPtr.Size == 8)
                {
                    bool ret;
                    try
                    {
                        return NativeMethods.IsWow64Process(handle, out ret) && ret;
                    }
                    catch
                    { }
                }
                return true;
            }

            public static IntPtr ReadPointer(IntPtr handle, bool isX64, byte[] buffer, IntPtr address)
            {
                int size_t = isX64 ? 8 : 4;

                IntPtr read;
                if (!NativeMethods.ReadProcessMemory(handle, address, buffer, new IntPtr(size_t), out read) || read.ToInt64() != size_t)
                    return IntPtr.Zero;

                if (isX64)
                    return new IntPtr(BitConverter.ToInt64(buffer, 0));
                else
                    return new IntPtr(BitConverter.ToInt32(buffer, 0));
            }

            public static int ReadBytes(IntPtr handle, IntPtr address, byte[] array, int length)
            {
                IntPtr read = IntPtr.Zero;

                NativeMethods.ReadProcessMemory(handle, address, array, new IntPtr(length), out read);

                return read.ToInt32();
            }
        }
    }
}
