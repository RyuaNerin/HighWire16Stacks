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
using HighWire16Stacks.Utilities;
using HighWire16Stacks.Windows;
using Newtonsoft.Json;

namespace HighWire16Stacks.Core
{
    internal static class Worker
    {
        private static object overlayInstanceSync = new object();
        private static Overlay overlayInstance;
        public static Overlay OverlayInstance
        {
            get
            {
                lock (overlayInstanceSync)
                {
                    if (overlayInstance == null)
                        overlayInstance = new Overlay(Settings.Instance.ClickThrough);

                    return overlayInstance;
                }
            }
        }

        public static void SetClickThrough(bool enabled)
        {
            lock (overlayInstanceSync)
            {
                var old = overlayInstance;

                overlayInstance = new Overlay(enabled);
                overlayInstance.Show();

                if (old != null)
                {
                    old.Close();
                    old = null;
                }
            }
        }

        public          static      UStatus[] Statuses;
        public readonly static List<UStatus>  SortedStatuses = new List<UStatus>();
        public readonly static List<UStatus>  SortedStatusesByTime = new List<UStatus>();

        private static volatile bool running = false;
        private static Task task;
        private static Process ffxivProcess;
        private static IntPtr ffxivHandle;
        private static IntPtr ffxivModulePtr;
        private static bool ffxivDx11;

        private static MemoryOffsets memoryOffsets;
        private static MemoryOffset memoryOffset;

        public static IntPtr FFXIVHandle
        {
            get { return ffxivHandle; }
        }

        private static int delay = 1;
        public static void SetDelay(int value)
        {
            delay = value;
        }

        public static IntPtr ffxivWindowHandle { get; private set; }

        static Worker()
        {
            if (Settings.Instance != null)
                SetDelay(Settings.Instance.OverlayRefreshCycle);
        }

        public static void Load()
        {
        }

        public static void Unload()
        {
            if (eventhook != null && !eventhook.IsClosed)
                eventhook.Close();
            running = false;
        }

        public static void Update()
        {
            string body;

#if DEBUG
            body = Properties.Resources.offset;
#else
            try
            {
                var req = HttpWebRequest.Create("https://raw.githubusercontent.com/RyuaNerin/HighWire16Stacks/master/HighWire16Stacks/Resources/offset.json") as HttpWebRequest;
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
#endif
            
            memoryOffsets = JsonConvert.DeserializeObject<MemoryOffsets>(body);
            
            UStatus ustatus;
            Statuses = new UStatus[memoryOffsets.count];
            for (int i = 0; i < memoryOffsets.count; ++i)
            {
                ustatus = new UStatus(i);
                Statuses[i] = ustatus;
                SortedStatuses.Add(ustatus);
                SortedStatusesByTime.Add(ustatus);
            }
        }
        
        public static void Stop()
        {
            for (int i = 0; i < memoryOffsets.count; ++i)
                Statuses[i].Clear();

            task?.Wait();

            running = false;
            MainWindow.Instance.Dispatcher.BeginInvoke(new Action(MainWindow.Instance.ExitedProcess));
        }

        public static bool SetProcess(Process process)
        {
            if (ffxivProcess != null)
            {
                ffxivProcess.Dispose();
                ffxivProcess = null;
            }

            try
            {
                ffxivProcess = process;
                ffxivProcess.EnableRaisingEvents = true;

                ffxivModulePtr = ffxivProcess.MainModule.BaseAddress;
                ffxivHandle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, ffxivProcess.Id);
                ffxivDx11 = !NativeMethods.IsX86Process(ffxivHandle);
                if (!ffxivDx11)
                    return false;

                ffxivWindowHandle = process.MainWindowHandle;

                memoryOffset = memoryOffsets.x64;

                running = true;
                task = Task.Factory.StartNew(WorkerThread);
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
            byte[] buff = new byte[12 * memoryOffsets.count];
            int i;

            int id;
            short param;
            float remain;
            //uint owner;

            bool orderUpdated;

            while (running)
            {
                ptr = NativeMethods.ReadPointer(ffxivHandle, ffxivDx11, buff, ffxivModulePtr + memoryOffset.ptr);
                if (ptr == IntPtr.Zero)
                {
                    Stop();
                    return;
                }

                if (NativeMethods.ReadBytes(ffxivHandle, ptr + memoryOffset.off, buff, buff.Length) == buff.Length)
                {
                    orderUpdated = false;

                    for (i = 0; i < memoryOffsets.count; ++i)
                    {
                        id = BitConverter.ToInt16(buff, 12 * i + 0);
                        if (id == 0)
                        {
                            Statuses[i].Clear();
                            continue;
                        }

                        param  = BitConverter.ToInt16(buff, 12 * i + 2);
                        remain = BitConverter.ToSingle(buff, 12 * i + 4);
                        //owner  = BitConverter.ToUInt32(buff, 12 * i + 8);

                        orderUpdated |= Statuses[i].Update(id, param, remain);
                    }

                    if (orderUpdated)
                    {
                        SortedStatuses.Sort(UStatus.Compare);
                        SortedStatusesByTime.Sort(UStatus.CompareWithTime);
                        
                        if (overlayInstance != null)
                            overlayInstance.Refresh();
                    }
                }

                Thread.Sleep(delay);
            }
        }

        private static WinEventHookHandle.WinEventDelegate autohideDelegate = new WinEventHookHandle.WinEventDelegate(WinEventProc);
        private static SafeHandle eventhook;

        public static void SetAutohide(bool enabled)
        {
            if (enabled)
            {
                eventhook = WinEventHookHandle.SetForegroundEvent(autohideDelegate);
            }
            else
            {
                eventhook.Close();

                overlayInstance.Visibility = Visibility.Visible;
            }
        }
        
        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var ptr = NativeMethods.GetForegroundWindow();

            if (ptr == overlayInstance.Handle)
                return;

            if (ptr == Worker.ffxivWindowHandle ||
                ptr == MainWindow.Instance.Handle)
            {
                overlayInstance.Visibility = Visibility.Visible;
            }
            else
            {
                overlayInstance.Visibility = Visibility.Hidden;
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
