using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
                    return overlayInstance ?? (overlayInstance = CreateOverlay(Settings.Instance.ClickThrough));
            }
        }

        public static void SetClickThrough(bool enabled)
        {
            CreateOverlay(enabled).Show();
        }

        private static Overlay CreateOverlay(bool clickThrough)
        {
            return (Overlay)Application.Current.Dispatcher.Invoke(new Func<bool, Overlay>(CreateOverlayPriv), new object[] { clickThrough });
        }
        private static Overlay CreateOverlayPriv(bool clickThrough)
        {
            lock (overlayInstanceSync)
            {
                if (overlayInstance == null || overlayInstance.ClickThourgh != clickThrough)
                {
                    if (overlayInstance != null)
                        Application.Current.Dispatcher.Invoke(new Action(overlayInstance.Close));

                    overlayInstance = new Overlay(clickThrough);
                }

                return overlayInstance;
            }
        }

        public          static      UStatus[] Statuses;
        public readonly static List<UStatus>  SortedStatuses = new List<UStatus>();
        public readonly static List<UStatus>  SortedStatusesByTime = new List<UStatus>();

        private static volatile bool running = false;
        private static Task taskWorker;
        private static Process ffxivProcess;
        private static IntPtr ffxivHandle;
        private static IntPtr ffxivModulePtr;
        
        private static MemoryOffset memoryOffset;

        private static int memoryPtr;
        private static int memoryOff;
        private static int memoryCount;
        public static void SetOverlayMode(bool showTargetStatus)
        {
            if (memoryOffset == null)
                return;

            memoryPtr   = !showTargetStatus ? memoryOffset.ptr   : memoryOffset.ptr_target;
            memoryOff   = !showTargetStatus ? memoryOffset.off   : memoryOffset.off_target;
            memoryCount = !showTargetStatus ? memoryOffset.count : memoryOffset.count_target;
        }

        private static volatile int delay = 1;
        public static void SetDelay(int value)
        {
            delay = value;
        }

        public static IntPtr GameWindowHandle { get; private set; }

        static Worker()
        {
            if (Settings.Instance != null)
                SetDelay((int)Math.Ceiling(1000 / Settings.Instance.OverlayFPS));
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

        public static bool SetOffset(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            {
                try
                {
                    var serializer = new JsonSerializer();

                    using (var jsonReader = new JsonTextReader(reader))
                        memoryOffset = serializer.Deserialize<MemoryOffset>(jsonReader);
                }
                catch
                {
                    return false;
                }
            }

            SetOverlayMode(Settings.Instance.ShowTargetStatus);

            int count = Math.Max(memoryOffset.count, memoryOffset.count_target);

            UStatus ustatus;
            Statuses = new UStatus[count];
            for (int i = 0; i < count; ++i)
            {
                ustatus = new UStatus(i);
                Statuses[i] = ustatus;
                SortedStatuses.Add(ustatus);
                SortedStatusesByTime.Add(ustatus);
            }

            return true;
        }
        
        public static void Stop()
        {
            for (int i = 0; i < memoryOffset.count; ++i)
                Statuses[i].Clear();

            running = false;
            taskWorker?.Wait();

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
                if (NativeMethods.IsX86Process(ffxivHandle))
                    return false;

                GameWindowHandle = process.MainWindowHandle;

                running = true;
                taskWorker = Task.Factory.StartNew(WorkerThread);
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
#if DEBUG
            try
            {
#endif
            IntPtr ptr;
            IntPtr ptrOld = IntPtr.Zero;
            byte[] buff = new byte[12 * memoryOffset.count];
            int i;

            int id;
            short param;
            float remain;
            //uint owner;

            bool orderUpdated;

            while (running)
            {
                ptr = NativeMethods.ReadPointer(ffxivHandle, buff, ffxivModulePtr + memoryPtr);
                if (ptr == IntPtr.Zero)
                {
                    Stop();
                    return;
                }
                if (ptr != ptrOld)
                {
                    if (ptrOld != IntPtr.Zero)
                        for (i = 0; i < Statuses.Length; ++i)
                            Statuses[i].Clear();
                    else
                        ptrOld = ptr;
                }
                
                if (NativeMethods.ReadBytes(ffxivHandle, ptr + memoryOff, buff, buff.Length) == buff.Length)
                {
                    orderUpdated = false;

                    for (i = 0; i < memoryCount; ++i)
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
#if DEBUG
            }
            catch
            {
            }
#endif
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
                eventhook?.Close();

                overlayInstance.Visibility = Visibility.Visible;
            }
        }
        
        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var ptr = NativeMethods.GetForegroundWindow();

            if (ptr == overlayInstance.Handle)
                return;

            if (GameWindowHandle == IntPtr.Zero)
            {
                try
                {
                    GameWindowHandle = ffxivProcess.MainWindowHandle;
                }
                catch
                { }
            }

            if (ptr == Worker.GameWindowHandle ||
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

            public static IntPtr ReadPointer(IntPtr handle, byte[] buffer, IntPtr address)
            {
                IntPtr read;
                if (!NativeMethods.ReadProcessMemory(handle, address, buffer, new IntPtr(8), out read) || read.ToInt64() != 8)
                    return IntPtr.Zero;

                return new IntPtr(BitConverter.ToInt64(buffer, 0));
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
