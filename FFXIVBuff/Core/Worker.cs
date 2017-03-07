using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FFXIVBuff.Utilities;
using FFXIVBuff.Windows;
using Newtonsoft.Json;

namespace FFXIVBuff.Core
{
    internal static class Worker
    {
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
                var old = m_overlayInstance;

                m_overlayInstance = new Overlay(enabled);
                m_overlayInstance.Show();

                if (old != null)
                {
                    old.Close();
                    old = null;
                }
            }
        }

        public readonly static List<UStatus> SortedStatuses = new List<UStatus>();
        public readonly static List<UStatus> SortedStatusesByTime = new List<UStatus>();
        public readonly static List<UStatus> Statuses = new List<UStatus>();

        private static volatile bool m_running = false;
        private static Task m_task;
        private static Process m_ffxivProcess;
        private static IntPtr m_ffxivHandle;
        private static IntPtr m_ffxivModulePtr;
        private static bool m_ffxivDx11;

        private static MemoryOffsets m_memoryOffsets;
        private static MemoryOffset m_memoryOffset;

        public static IntPtr FFXIVHandle
        {
            get { return m_ffxivHandle; }
        }

        private static int m_delay = 1;
        public static void SetDelay(int value)
        {
            m_delay = value;
        }

        public static IntPtr m_ffxivWindowHandle { get; private set; }

        static Worker()
        {
            if (Settings.Instance != null)
                SetDelay((int)Settings.Instance.RefreshTime);
        }

        public static void Load()
        {
        }

        public static void Unload()
        {
            if (m_eventhook != null && !m_eventhook.IsClosed)
                m_eventhook.Close();
            m_running = false;
        }

        public static void Update()
        {
            string body;

            try
            {
                var req = HttpWebRequest.Create("https://raw.githubusercontent.com/RyuaNerin/FBOverlay/master/patch.json") as HttpWebRequest;
                req.UserAgent = Assembly.GetExecutingAssembly().FullName;
                req.Timeout = 5000;
                using (var res = req.GetResponse())
                using (var stream = res.GetResponseStream())
                using (var reader = new StreamReader(stream))
                    body = reader.ReadToEnd();
            }
            catch
            {
                body = Properties.Resources.offset;
            }
            
            m_memoryOffsets = JsonConvert.DeserializeObject<MemoryOffsets>(body);
            
            UStatus ustatus;
            for (int i = 0; i < m_memoryOffsets.count; ++i)
            {
                ustatus = new UStatus(i);
                Statuses.Add(ustatus);
                SortedStatuses.Add(ustatus);
            }
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
            catch (Exception ex)
            {
                Sentry.Error(ex);
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

            bool orderUpdated;

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
                    orderUpdated = false;

                    for (i = 0; i < m_memoryOffsets.count; ++i)
                    {
                        id = BitConverter.ToInt16(buff, 12 * i + 0);
                        if (id == 0)
                        {
                            Statuses[i].Clear();
                            continue;
                        }

                        param  = BitConverter.ToInt16(buff, 12 * i + 2);
                        remain = BitConverter.ToSingle(buff, 12 * i + 4);
                        owner  = BitConverter.ToUInt32(buff, 12 * i + 8);

                        orderUpdated |= Statuses[i].Update(id, param, remain);
                    }

                    if (orderUpdated)
                    {
                        SortedStatuses.Sort(UStatus.Compare);
                        SortedStatusesByTime.Sort(UStatus.CompareWithTime);
                        m_overlayInstance.Refresh();
                    }
                }

                Thread.Sleep(m_delay);
            }
        }

        private static WinEventHookHandle.WinEventDelegate m_autohideDelegate = new WinEventHookHandle.WinEventDelegate(WinEventProc);
        private static SafeHandle m_eventhook;

        public static void SetAutohide(bool enabled)
        {
            if (enabled)
            {
                m_eventhook = WinEventHookHandle.SetForegroundEvent(m_autohideDelegate);
            }
            else
            {
                m_eventhook.Close();

                m_overlayInstance.Visibility = Visibility.Visible;
            }
        }
        
        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var ptr = NativeMethods.GetForegroundWindow();

            if (ptr == m_overlayInstance.Handle)
                return;

            if (ptr == Worker.m_ffxivWindowHandle ||
                ptr == MainWindow.Instance.Handle)
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
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

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
