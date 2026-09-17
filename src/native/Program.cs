using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WinComputerUse.Native
{
    public class Program
    {
        private static JavaScriptSerializer _serializer = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };

        [STAThread]
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0 || args[0] == "--daemon")
            {
                return RunDaemon();
            }

            return RunCli(args);
        }

        private static int RunDaemon()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line == "exit" || line == "quit") break;

                try
                {
                    Dictionary<string, object> req = _serializer.Deserialize<Dictionary<string, object>>(line);
                    object id = req.ContainsKey("id") ? req["id"] : null;
                    string method = req.ContainsKey("method") ? req["method"].ToString() : "";
                    Dictionary<string, object> parameters = req.ContainsKey("params") && req["params"] is Dictionary<string, object>
                        ? (Dictionary<string, object>)req["params"]
                        : new Dictionary<string, object>();

                    object result = Dispatch(method, parameters);
                    Console.WriteLine(_serializer.Serialize(new Dictionary<string, object>()
                    {
                        { "id", id },
                        { "ok", true },
                        { "result", result }
                    }));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(_serializer.Serialize(new Dictionary<string, object>()
                    {
                        { "ok", false },
                        { "error", ex.Message }
                    }));
                }
            }
            return 0;
        }

        private static int RunCli(string[] args)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string action = "";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--action" && i + 1 < args.Length) action = args[++i];
                else if (args[i].StartsWith("--") && i + 1 < args.Length) parameters[args[i].Substring(2)] = args[++i];
            }

            if (string.IsNullOrEmpty(action))
            {
                Console.Error.WriteLine("Error: Missing --action parameter");
                return 1;
            }

            try
            {
                object result = Dispatch(action, parameters);
                Console.WriteLine(_serializer.Serialize(new Dictionary<string, object>()
                {
                    { "ok", true },
                    { "result", result }
                }));
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(_serializer.Serialize(new Dictionary<string, object>()
                {
                    { "ok", false },
                    { "error", ex.Message }
                }));
                return 1;
            }
        }

        private static object Dispatch(string action, Dictionary<string, object> p)
        {
            switch (action.ToLowerInvariant())
            {
                case "list_windows": return WindowService.ListWindows();
                case "list_apps": return WindowService.ListApps();
                case "launch_app": return WindowService.LaunchApp(GetString(p, "app"));
                case "activate_window": return WindowService.ActivateWindow(GetLong(p, "hwnd", 0));
                case "screenshot":
                    return CaptureService.Capture(
                        GetLong(p, "hwnd", 0),
                        GetString(p, "format", "jpeg"),
                        GetInt(p, "quality", 80),
                        GetString(p, "out", null)
                    );
                case "get_ui_tree":
                    return AccessibilityService.GetUiTree(
                        GetLong(p, "hwnd", 0),
                        GetInt(p, "max_depth", 6)
                    );
                case "click":
                    return InputService.Click(
                        GetLong(p, "hwnd", 0),
                        GetInt(p, "element_index", -1),
                        GetNullableInt(p, "x"),
                        GetNullableInt(p, "y"),
                        GetString(p, "mouse_button", "left"),
                        GetInt(p, "click_count", 1)
                    );
                case "move":
                    return InputService.Move(GetLong(p, "hwnd", 0), GetInt(p, "x", 0), GetInt(p, "y", 0));
                case "drag":
                    return InputService.Drag(
                        GetLong(p, "hwnd", 0),
                        GetInt(p, "from_x", 0),
                        GetInt(p, "from_y", 0),
                        GetInt(p, "to_x", 0),
                        GetInt(p, "to_y", 0)
                    );
                case "scroll":
                    return InputService.Scroll(
                        GetLong(p, "hwnd", 0),
                        GetNullableInt(p, "x"),
                        GetNullableInt(p, "y"),
                        GetInt(p, "delta_x", 0),
                        GetInt(p, "delta_y", 0)
                    );
                case "type_text": return InputService.TypeText(GetString(p, "text", ""));
                case "press_key": return InputService.PressKey(GetString(p, "chord", ""));
                case "set_value": return InputService.SetValue(GetInt(p, "element_index", -1), GetString(p, "value", ""));
                case "get_cursor_position":
                    Point curPos = Cursor.Position;
                    return new Dictionary<string, object>() { { "x", curPos.X }, { "y", curPos.Y } };
                default:
                    throw new ArgumentException("Unknown action: " + action);
            }
        }

        private static string GetString(Dictionary<string, object> p, string key, string def)
        {
            if (p.ContainsKey(key) && p[key] != null) return p[key].ToString();
            return def;
        }
        private static string GetString(Dictionary<string, object> p, string key)
        {
            return GetString(p, key, "");
        }

        private static int GetInt(Dictionary<string, object> p, string key, int def)
        {
            if (p.ContainsKey(key) && p[key] != null)
            {
                int val;
                if (int.TryParse(p[key].ToString(), out val)) return val;
            }
            return def;
        }

        private static int? GetNullableInt(Dictionary<string, object> p, string key)
        {
            if (p.ContainsKey(key) && p[key] != null)
            {
                int val;
                if (int.TryParse(p[key].ToString(), out val)) return val;
            }
            return null;
        }

        private static long GetLong(Dictionary<string, object> p, string key, long def)
        {
            if (p.ContainsKey(key) && p[key] != null)
            {
                long val;
                if (long.TryParse(p[key].ToString(), out val)) return val;
            }
            return def;
        }
    }
}
