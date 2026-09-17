using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinComputerUse.Native
{
    public static class DesktopManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll")]
        private static extern bool SetProcessWindowStation(IntPtr hWinSta);

        [DllImport("user32.dll")]
        public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll")]
        public static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern bool CloseDesktop(IntPtr hDesktop);

        public static IntPtr GetInteractiveDesktop()
        {
            try
            {
                IntPtr hWinsta = OpenWindowStation("WinSta0", false, 0x037F);
                if (hWinsta != IntPtr.Zero)
                {
                    SetProcessWindowStation(hWinsta);
                }

                IntPtr hDesk = OpenDesktop("default", 0, false, 0x01FF);
                if (hDesk == IntPtr.Zero)
                {
                    hDesk = OpenDesktop("Default", 0, false, 0x0001 | 0x0040);
                }
                return hDesk;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public static T RunOnInteractiveDesktop<T>(Func<T> action)
        {
            T result = default(T);
            Exception caughtEx = null;

            Thread worker = new Thread(() =>
            {
                try
                {
                    IntPtr hDesk = GetInteractiveDesktop();
                    if (hDesk != IntPtr.Zero)
                    {
                        SetThreadDesktop(hDesk);
                    }
                    result = action();
                }
                catch (Exception ex)
                {
                    caughtEx = ex;
                }
            });

            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
            worker.Join();

            if (caughtEx != null)
            {
                throw caughtEx;
            }

            return result;
        }

        public static void RunOnInteractiveDesktop(Action action)
        {
            RunOnInteractiveDesktop<bool>(() =>
            {
                action();
                return true;
            });
        }
    }
}
