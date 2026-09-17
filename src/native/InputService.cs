using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WinComputerUse.Native
{
    public static class InputService
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInputs, int cbSize);

        public static bool Click(long hwndVal, int elementIndex, int? x, int? y, string button, int clickCount)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                int targetX;
                int targetY;

                if (elementIndex >= 0)
                {
                    Point? pt = AccessibilityService.ResolveElementCenter(elementIndex);
                    if (!pt.HasValue) throw new Exception("Element index " + elementIndex + " not found in element cache");
                    targetX = pt.Value.X;
                    targetY = pt.Value.Y;
                }
                else if (x.HasValue && y.HasValue)
                {
                    if (hwndVal != 0)
                    {
                        WindowService.ActivateWindow(hwndVal);
                        RECT rect = WindowService.GetBounds(new IntPtr(hwndVal));
                        targetX = rect.Left + x.Value;
                        targetY = rect.Top + y.Value;
                    }
                    else
                    {
                        targetX = x.Value;
                        targetY = y.Value;
                    }
                }
                else
                {
                    throw new ArgumentException("Click requires either element_index or (x, y) coordinates");
                }

                Cursor.Position = new Point(targetX, targetY);
                OverlayService.ShowClick(targetX, targetY, button);
                Thread.Sleep(30);

                uint downFlag = Win32Constants.MOUSEEVENTF_LEFTDOWN;
                uint upFlag = Win32Constants.MOUSEEVENTF_LEFTUP;

                string b = button.ToLowerInvariant();
                if (b == "right" || b == "r")
                {
                    downFlag = Win32Constants.MOUSEEVENTF_RIGHTDOWN;
                    upFlag = Win32Constants.MOUSEEVENTF_RIGHTUP;
                }
                else if (b == "middle" || b == "m")
                {
                    downFlag = Win32Constants.MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = Win32Constants.MOUSEEVENTF_MIDDLEUP;
                }

                for (int i = 0; i < clickCount; i++)
                {
                    mouse_event(downFlag, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(40);
                    mouse_event(upFlag, 0, 0, 0, UIntPtr.Zero);
                    if (i < clickCount - 1) Thread.Sleep(80);
                }

                return true;
            });
        }

        public static bool Move(long hwndVal, int x, int y)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                int targetX = x;
                int targetY = y;
                if (hwndVal != 0)
                {
                    RECT rect = WindowService.GetBounds(new IntPtr(hwndVal));
                    targetX = rect.Left + x;
                    targetY = rect.Top + y;
                }
                Cursor.Position = new Point(targetX, targetY);
                return true;
            });
        }

        public static bool Drag(long hwndVal, int fromX, int fromY, int toX, int toY)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                Move(hwndVal, fromX, fromY);
                Thread.Sleep(40);
                mouse_event(Win32Constants.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);

                Point start = Cursor.Position;
                int steps = 15;
                for (int i = 1; i <= steps; i++)
                {
                    int currX = start.X + ((toX - fromX) * i / steps);
                    int currY = start.Y + ((toY - fromY) * i / steps);
                    Cursor.Position = new Point(currX, currY);
                    Thread.Sleep(15);
                }

                mouse_event(Win32Constants.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                return true;
            });
        }

        public static bool Scroll(long hwndVal, int? x, int? y, int deltaX, int deltaY)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                if (x.HasValue && y.HasValue)
                {
                    Move(hwndVal, x.Value, y.Value);
                    Thread.Sleep(20);
                }
                if (deltaY != 0) mouse_event(Win32Constants.MOUSEEVENTF_WHEEL, 0, 0, (uint)deltaY, UIntPtr.Zero);
                if (deltaX != 0) mouse_event(Win32Constants.MOUSEEVENTF_HWHEEL, 0, 0, (uint)deltaX, UIntPtr.Zero);
                return true;
            });
        }

        public static bool TypeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                List<INPUT> inputs = new List<INPUT>();
                foreach (char c in text)
                {
                    INPUT down = new INPUT();
                    down.type = Win32Constants.INPUT_KEYBOARD;
                    down.u.ki.wScan = (ushort)c;
                    down.u.ki.dwFlags = Win32Constants.KEYEVENTF_UNICODE;
                    inputs.Add(down);

                    INPUT up = new INPUT();
                    up.type = Win32Constants.INPUT_KEYBOARD;
                    up.u.ki.wScan = (ushort)c;
                    up.u.ki.dwFlags = Win32Constants.KEYEVENTF_UNICODE | Win32Constants.KEYEVENTF_KEYUP;
                    inputs.Add(up);
                }

                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
                return true;
            });
        }

        public static bool PressKey(string chord)
        {
            if (string.IsNullOrEmpty(chord)) return true;

            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                string[] tokens = chord.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                List<ushort> modifiers = new List<ushort>();
                ushort mainKey = 0;

                for (int i = 0; i < tokens.Length; i++)
                {
                    ushort vk = ParseVk(tokens[i].Trim());
                    if (i == tokens.Length - 1 && !IsModifier(vk))
                        mainKey = vk;
                    else
                        modifiers.Add(vk);
                }

                foreach (ushort mod in modifiers)
                {
                    SendVk(mod, false);
                    Thread.Sleep(15);
                }

                if (mainKey != 0)
                {
                    SendVk(mainKey, false);
                    Thread.Sleep(30);
                    SendVk(mainKey, true);
                    Thread.Sleep(15);
                }

                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    SendVk(modifiers[i], true);
                    Thread.Sleep(15);
                }

                return true;
            });
        }

        public static bool SetValue(int elementIndex, string value)
        {
            Point? pt = AccessibilityService.ResolveElementCenter(elementIndex);
            if (!pt.HasValue) throw new Exception("Element index " + elementIndex + " not found");

            Click(0, elementIndex, null, null, "left", 1);
            Thread.Sleep(40);
            PressKey("Ctrl+A");
            Thread.Sleep(30);
            TypeText(value);
            return true;
        }

        private static void SendVk(ushort vk, bool isKeyUp)
        {
            INPUT input = new INPUT();
            input.type = Win32Constants.INPUT_KEYBOARD;
            input.u.ki.wVk = vk;
            input.u.ki.dwFlags = isKeyUp ? Win32Constants.KEYEVENTF_KEYUP : 0;
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        private static bool IsModifier(ushort vk)
        {
            return vk == Win32Constants.VK_CONTROL || vk == Win32Constants.VK_SHIFT || vk == Win32Constants.VK_MENU;
        }

        private static ushort ParseVk(string key)
        {
            string k = key.ToLowerInvariant();
            switch (k)
            {
                case "ctrl": case "control": case "control_l": case "control_r": return Win32Constants.VK_CONTROL;
                case "shift": case "shift_l": case "shift_r": return Win32Constants.VK_SHIFT;
                case "alt": case "alt_l": case "alt_r": return Win32Constants.VK_MENU;
                case "win": case "windows": case "meta": case "super": return Win32Constants.VK_LWIN;
                case "enter": case "return": return Win32Constants.VK_RETURN;
                case "tab": return Win32Constants.VK_TAB;
                case "escape": case "esc": return Win32Constants.VK_ESCAPE;
                case "backspace": case "back": return Win32Constants.VK_BACK;
                case "delete": case "del": return Win32Constants.VK_DELETE;
                case "insert": return Win32Constants.VK_INSERT;
                case "space": return Win32Constants.VK_SPACE;
                case "up": return Win32Constants.VK_UP;
                case "down": return Win32Constants.VK_DOWN;
                case "left": return Win32Constants.VK_LEFT;
                case "right": return Win32Constants.VK_RIGHT;
                case "home": return Win32Constants.VK_HOME;
                case "end": return Win32Constants.VK_END;
                case "pageup": return Win32Constants.VK_PRIOR;
                case "pagedown": return Win32Constants.VK_NEXT;
                case "f1": return 0x70; case "f2": return 0x71; case "f3": return 0x72; case "f4": return 0x73;
                case "f5": return 0x74; case "f6": return 0x75; case "f7": return 0x76; case "f8": return 0x77;
                case "f9": return 0x78; case "f10": return 0x79; case "f11": return 0x7A; case "f12": return 0x7B;
                default:
                    if (k.Length == 1)
                    {
                        char c = char.ToUpperInvariant(k[0]);
                        if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) return (ushort)c;
                    }
                    return 0;
            }
        }
    }
}
