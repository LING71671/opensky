using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WinComputerUse.Native
{
    public static class WindowService
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        public static List<Dictionary<string, object>> ListWindows()
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                IntPtr hDesk = DesktopManager.GetInteractiveDesktop();

                EnumWindowsProc proc = (hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;

                    int length = GetWindowTextLength(hWnd);
                    if (length == 0) return true;

                    StringBuilder sb = new StringBuilder(length + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString().Trim();
                    if (string.IsNullOrEmpty(title)) return true;

                    int cloaked = 0;
                    DwmGetWindowAttribute(hWnd, Win32Constants.DWMWA_CLOAKED, out cloaked, sizeof(int));
                    if (cloaked != 0) return true;

                    RECT rect;
                    if (DwmGetWindowAttribute(hWnd, Win32Constants.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT))) != 0)
                    {
                        GetWindowRect(hWnd, out rect);
                    }

                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    if (width <= 0 || height <= 0) return true;

                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    string processName = "";
                    try
                    {
                        processName = Process.GetProcessById((int)pid).ProcessName;
                    }
                    catch { }

                    result.Add(new Dictionary<string, object>()
                    {
                        { "id", hWnd.ToInt64() },
                        { "title", title },
                        { "processName", processName },
                        { "processId", (int)pid },
                        { "isMinimized", IsIconic(hWnd) },
                        { "bounds", new Dictionary<string, object>()
                            {
                                { "x", rect.Left },
                                { "y", rect.Top },
                                { "width", width },
                                { "height", height }
                            }
                        }
                    });
                    return true;
                };

                if (hDesk != IntPtr.Zero)
                    EnumDesktopWindows(hDesk, proc, IntPtr.Zero);
                else
                    EnumWindows(proc, IntPtr.Zero);

                return result;
            });
        }

        public static List<Dictionary<string, object>> ListApps()
        {
            List<Dictionary<string, object>> windows = ListWindows();
            Dictionary<string, Dictionary<string, object>> map = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

            foreach (var win in windows)
            {
                string proc = win["processName"].ToString();
                if (string.IsNullOrEmpty(proc)) continue;

                if (!map.ContainsKey(proc))
                {
                    map[proc] = new Dictionary<string, object>()
                    {
                        { "id", proc },
                        { "displayName", proc },
                        { "isRunning", true },
                        { "windows", new List<Dictionary<string, object>>() }
                    };
                }
                ((List<Dictionary<string, object>>)map[proc]["windows"]).Add(win);
            }

            return new List<Dictionary<string, object>>(map.Values);
        }

        public static bool LaunchApp(string app)
        {
            if (string.IsNullOrEmpty(app)) throw new ArgumentException("App path or name cannot be empty");
            Process.Start(new ProcessStartInfo()
            {
                FileName = app,
                UseShellExecute = true
            });
            return true;
        }

        public static bool ActivateWindow(long hwndVal)
        {
            if (hwndVal == 0) return false;
            IntPtr hWnd = new IntPtr(hwndVal);

            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, Win32Constants.SW_RESTORE);
                }

                uint dummyPid;
                uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), out dummyPid);
                uint appThread = GetCurrentThreadId();

                if (foreThread != appThread)
                {
                    AttachThreadInput(foreThread, appThread, true);
                    BringWindowToTop(hWnd);
                    ShowWindow(hWnd, Win32Constants.SW_SHOW);
                    SetForegroundWindow(hWnd);
                    AttachThreadInput(foreThread, appThread, false);
                }
                else
                {
                    BringWindowToTop(hWnd);
                    ShowWindow(hWnd, Win32Constants.SW_SHOW);
                    SetForegroundWindow(hWnd);
                }

                Thread.Sleep(80);

                try
                {
                    RECT bounds = GetBounds(hWnd);
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    OverlayService.HighlightWindow(bounds, sb.ToString().Trim());
                }
                catch { }

                return true;
            });
        }

        public static RECT GetBounds(IntPtr hWnd)
        {
            RECT rect;
            if (DwmGetWindowAttribute(hWnd, Win32Constants.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT))) != 0)
            {
                GetWindowRect(hWnd, out rect);
            }
            return rect;
        }
    }
}
