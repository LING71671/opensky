using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Automation;

namespace WinComputerUse.Native
{
    public static class AccessibilityService
    {
        private static JavaScriptSerializer _serializer = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
        private static string _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".win-computer-use");
        private static string _cacheFile = Path.Combine(_cacheDir, "element-cache.json");

        public static Dictionary<string, object> GetUiTree(long hwndVal, int maxDepth)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                AutomationElement root = hwndVal != 0
                    ? AutomationElement.FromHandle(new IntPtr(hwndVal))
                    : AutomationElement.RootElement;

                if (root == null) throw new Exception("Target UI automation root element could not be found");

                List<Dictionary<string, object>> indexedElements = new List<Dictionary<string, object>>();
                StringBuilder treeBuilder = new StringBuilder();

                int currentIndex = 0;
                WalkUiElement(root, 0, maxDepth, ref currentIndex, indexedElements, treeBuilder);

                string focusedText = "";
                try
                {
                    AutomationElement focused = AutomationElement.FocusedElement;
                    if (focused != null)
                    {
                        focusedText = string.Format("[Focused] {0} \"{1}\"",
                            focused.Current.ControlType.ProgrammaticName.Replace("ControlType.", ""),
                            focused.Current.Name ?? "");
                    }
                }
                catch { }

                try
                {
                    if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                    File.WriteAllText(_cacheFile, _serializer.Serialize(indexedElements), Encoding.UTF8);
                }
                catch { }

                return new Dictionary<string, object>()
                {
                    { "tree", treeBuilder.ToString() },
                    { "focusedElement", focusedText },
                    { "elements", indexedElements }
                };
            });
        }

        public static Point? ResolveElementCenter(int elementIndex)
        {
            if (!File.Exists(_cacheFile)) return null;
            try
            {
                string json = File.ReadAllText(_cacheFile, Encoding.UTF8);
                object[] elements = _serializer.Deserialize<object[]>(json);
                if (elementIndex < 0 || elementIndex >= elements.Length) return null;

                Dictionary<string, object> el = (Dictionary<string, object>)elements[elementIndex];
                return new Point(Convert.ToInt32(el["x"]), Convert.ToInt32(el["y"]));
            }
            catch
            {
                return null;
            }
        }

        private static void WalkUiElement(AutomationElement element, int depth, int maxDepth, ref int index,
            List<Dictionary<string, object>> indexedElements, StringBuilder treeBuilder)
        {
            if (element == null || depth > maxDepth) return;

            try
            {
                if (element.Current.IsOffscreen) return;

                System.Windows.Rect r = element.Current.BoundingRectangle;
                if (r.Width <= 0 || r.Height <= 0) return;

                string typeName = element.Current.ControlType.ProgrammaticName.Replace("ControlType.", "");
                string name = (element.Current.Name ?? "").Trim();
                bool isFocused = element.Current.HasKeyboardFocus;

                string valStr = "";
                object valPatternObj;
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valPatternObj))
                {
                    ValuePattern vp = valPatternObj as ValuePattern;
                    if (vp != null && !string.IsNullOrEmpty(vp.Current.Value))
                    {
                        valStr = vp.Current.Value;
                    }
                }

                int myIndex = index++;
                int cx = (int)(r.Left + r.Width / 2);
                int cy = (int)(r.Top + r.Height / 2);

                indexedElements.Add(new Dictionary<string, object>()
                {
                    { "index", myIndex },
                    { "type", typeName },
                    { "name", name },
                    { "x", cx },
                    { "y", cy },
                    { "bounds", new Dictionary<string, object>()
                        {
                            { "x", (int)r.Left },
                            { "y", (int)r.Top },
                            { "width", (int)r.Width },
                            { "height", (int)r.Height }
                        }
                    },
                    { "value", valStr },
                    { "isFocused", isFocused }
                });

                string indent = new string(' ', depth * 2);
                treeBuilder.AppendFormat("{0}[{1}] {2} \"{3}\"", indent, myIndex, typeName, name);
                if (!string.IsNullOrEmpty(valStr))
                {
                    treeBuilder.AppendFormat(" [Value: \"{0}\"]", valStr.Length > 40 ? valStr.Substring(0, 40) + "..." : valStr);
                }
                if (isFocused) treeBuilder.Append(" (focused)");
                treeBuilder.AppendLine();

                AutomationElement child = TreeWalker.ControlViewWalker.GetFirstChild(element);
                while (child != null)
                {
                    WalkUiElement(child, depth + 1, maxDepth, ref index, indexedElements, treeBuilder);
                    child = TreeWalker.ControlViewWalker.GetNextSibling(child);
                }
            }
            catch { }
        }
    }
}
