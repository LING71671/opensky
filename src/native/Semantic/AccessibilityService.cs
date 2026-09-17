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

        // Live in-memory cache of traversed elements for 0ms direct pattern invocation
        private static List<AutomationElement> _liveElements = new List<AutomationElement>();
        private static readonly object _liveElementsLock = new object();

        public static Dictionary<string, object> GetUiTree(long hwndVal, int maxDepth)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                AutomationElement root = hwndVal != 0
                    ? AutomationElement.FromHandle(new IntPtr(hwndVal))
                    : AutomationElement.RootElement;

                if (root == null) throw new Exception("Target UI automation root element could not be found");

                List<Dictionary<string, object>> indexedElements = new List<Dictionary<string, object>>();
                List<AutomationElement> liveList = new List<AutomationElement>();
                StringBuilder treeBuilder = new StringBuilder();

                int currentIndex = 0;
                WalkUiElement(root, 0, maxDepth, ref currentIndex, indexedElements, liveList, treeBuilder);

                lock (_liveElementsLock)
                {
                    _liveElements = liveList;
                }

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
            lock (_liveElementsLock)
            {
                if (elementIndex >= 0 && elementIndex < _liveElements.Count)
                {
                    try
                    {
                        System.Windows.Rect r = _liveElements[elementIndex].Current.BoundingRectangle;
                        if (r.Width > 0 && r.Height > 0)
                        {
                            return new Point((int)(r.Left + r.Width / 2), (int)(r.Top + r.Height / 2));
                        }
                    }
                    catch { }
                }
            }

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

        public static Dictionary<string, object> InvokeElement(int elementIndex, string action, string value)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                AutomationElement target = null;
                lock (_liveElementsLock)
                {
                    if (elementIndex >= 0 && elementIndex < _liveElements.Count)
                    {
                        target = _liveElements[elementIndex];
                    }
                }

                if (target == null)
                {
                    // Fallback to resolving coordinate and looking up element from point
                    Point? pt = ResolveElementCenter(elementIndex);
                    if (pt.HasValue)
                    {
                        try
                        {
                            target = AutomationElement.FromPoint(new System.Windows.Point(pt.Value.X, pt.Value.Y));
                        }
                        catch { }
                    }
                }

                if (target == null)
                {
                    throw new Exception(string.Format("Element index [{0}] not found in live cache or point lookup", elementIndex));
                }

                string elementName = "";
                RECT bounds = new RECT();
                try
                {
                    elementName = target.Current.Name ?? "";
                    System.Windows.Rect r = target.Current.BoundingRectangle;
                    bounds.Left = (int)r.Left;
                    bounds.Top = (int)r.Top;
                    bounds.Right = (int)r.Right;
                    bounds.Bottom = (int)r.Bottom;
                }
                catch { }

                Dictionary<string, object> result = PatternService.ExecutePattern(target, action, value);

                // Visual non-intrusive feedback on the element itself if successful
                if (Convert.ToBoolean(result["success"]) && bounds.Right > bounds.Left && bounds.Bottom > bounds.Top)
                {
                    OverlayService.ShowElementFocus(bounds, elementName);
                }

                return result;
            });
        }

        private static void WalkUiElement(AutomationElement element, int depth, int maxDepth, ref int index,
            List<Dictionary<string, object>> indexedElements, List<AutomationElement> liveList, StringBuilder treeBuilder)
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

                // Discover supported patterns
                List<string> supportedActions = new List<string>();
                object testPattern;
                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out testPattern)) supportedActions.Add("invoke");
                if (element.TryGetCurrentPattern(TogglePattern.Pattern, out testPattern)) supportedActions.Add("toggle");
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out testPattern)) supportedActions.Add("select");
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out testPattern)) supportedActions.Add("set_value");
                if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out testPattern)) supportedActions.Add("expand_collapse");

                string valStr = "";
                if (supportedActions.Contains("set_value"))
                {
                    try
                    {
                        ValuePattern vp = element.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                        if (vp != null) valStr = vp.Current.Value ?? "";
                    }
                    catch { }
                }

                int myIndex = index++;
                int cx = (int)(r.Left + r.Width / 2);
                int cy = (int)(r.Top + r.Height / 2);

                liveList.Add(element);

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
                    { "isFocused", isFocused },
                    { "actions", supportedActions }
                });

                string indent = new string(' ', depth * 2);
                string actionStr = supportedActions.Count > 0 ? " [" + string.Join(",", supportedActions.ToArray()) + "]" : "";
                treeBuilder.AppendFormat("{0}[{1}] {2} \"{3}\"{4}", indent, myIndex, typeName, name, actionStr);
                if (!string.IsNullOrEmpty(valStr))
                {
                    treeBuilder.AppendFormat(" (Value: \"{0}\")", valStr.Length > 40 ? valStr.Substring(0, 40) + "..." : valStr);
                }
                if (isFocused) treeBuilder.Append(" (focused)");
                treeBuilder.AppendLine();

                // Efficient semantic traversal via ControlViewWalker
                AutomationElement child = TreeWalker.ControlViewWalker.GetFirstChild(element);
                while (child != null)
                {
                    WalkUiElement(child, depth + 1, maxDepth, ref index, indexedElements, liveList, treeBuilder);
                    child = TreeWalker.ControlViewWalker.GetNextSibling(child);
                }
            }
            catch { }
        }
    }
}
