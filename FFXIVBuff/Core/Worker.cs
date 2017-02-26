using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFXIVBuff.Window;

namespace FFXIVBuff.Core
{
    internal static class Worker
    {
        private const int StatusesCount = 21;

        private const int X86Pointer = 0x1554D40;
        private const int X86Offset  = 0x00018E0;

        private const int X64Pointer = 0x1554D40;
        private const int X64Offset  = 0x00018E0;

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

        public readonly static UStatus[] Statuses = new UStatus[StatusesCount];

        private static volatile bool m_running = false;
        private static Task m_task;
        private static Process m_ffxivProcess;
        private static IntPtr m_ffxivHandle;
        private static IntPtr m_ffxivModulePtr;
        private static bool m_ffxivDx11;

        private static int m_arrayPointer;
        private static int m_arrayOffset;

        private static int m_delay = 1;
        public static int Delay
        {
            get { return m_delay; }
            set { m_delay = value; }
        }

        static Worker()
        {
            for (int i = 0; i < StatusesCount; ++i)
                Statuses[i] = new UStatus();

            if (Settings.Instance != null)
                Delay = (int)Settings.Instance.RefreshTime;
        }

        public static void Stop()
        {
            for (int i = 0; i < StatusesCount; ++i)
                Statuses[i].Update();

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

                if (!m_ffxivDx11)
                {
                    m_arrayOffset  = X86Offset;
                    m_arrayPointer = X86Pointer;
                }
                else
                {
                    m_arrayOffset  = X64Offset;
                    m_arrayPointer = X64Pointer;
                }

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
            byte[] buff = new byte[12 * 21];
            int i;

            int id;
            short param;
            float remain;
            uint owner;

            int iconIndex;

            try
            {
                while (m_running)
                {
                    ptr = NativeMethods.ReadPointer(m_ffxivHandle, m_ffxivDx11, buff, m_ffxivModulePtr + m_arrayPointer);
                    if (ptr == IntPtr.Zero)
                        Stop();

                    NativeMethods.ReadBytes(m_ffxivHandle, ptr + m_arrayOffset, buff, 12 * 21);

                    for (i = 0; i < 21; ++i)
                    {
                        id     = BitConverter.ToInt16(buff, 12 * i + 0);

                        if (id == 0)
                        {
                            Statuses[i].Update();
                            continue;
                        }

                        param  = BitConverter.ToInt16 (buff, 12 * i + 2);
                        remain = BitConverter.ToSingle(buff, 12 * i + 4);
                        owner  = BitConverter.ToUInt32(buff, 12 * i + 8);

                        if (owner == 0xE0000000)
                        {
                            remain = 0;
                            param = 0;
                        }

                        if (0 < param)
                        {
                            if (param < 15)
                                --param;
                            else
                                param = 0;
                        }
                        
                        iconIndex = param;
                        if (Statuses[i].Id == id)
                            Statuses[i].Update(iconIndex, remain);
                        else
                            Statuses[i].Update(FResource.StatusListDic[id], iconIndex, remain);
                    }

                    Thread.Sleep(m_delay);
                }
            }
            catch
            {
            }
        }

        private static class NativeMethods
        {
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

            public static int ReadInt8(IntPtr handle, IntPtr address, byte[] buffer)
            {
                byte[] lpBuffer = new byte[1];
                IntPtr read;
                if (!NativeMethods.ReadProcessMemory(handle, address, lpBuffer, new IntPtr(1), out read) || read.ToInt64() != 1)
                    return 0;

                return lpBuffer[0];
            }

            public static int ReadInt32(IntPtr handle, IntPtr address, byte[] buffer)
            {
                byte[] lpBuffer = new byte[4];
                IntPtr read;
                if (!NativeMethods.ReadProcessMemory(handle, address, lpBuffer, new IntPtr(4), out read) || read.ToInt64() != 4)
                    return 0;

                return BitConverter.ToInt32(lpBuffer, 0);
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
