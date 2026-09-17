using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace WinComputerUse.Native
{
    /// <summary>
    /// Single Responsibility: Executes in-memory UI Automation Control Patterns (Invoke, Toggle, Value, Select, Expand/Collapse).
    /// Decoupled from input simulation and tree traversal.
    /// </summary>
    public static class PatternService
    {
        public static Dictionary<string, object> ExecutePattern(AutomationElement element, string action, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element", "Target AutomationElement cannot be null");
            }

            string normalizedAction = (action ?? "auto").ToLowerInvariant();
            string executedMethod = "";
            string elementName = "";

            try
            {
                elementName = element.Current.Name ?? "";
            }
            catch { }

            // 1. ValuePattern (Direct text replacement without keyboard simulation)
            if (normalizedAction == "set_value" || (!string.IsNullOrEmpty(value) && normalizedAction == "auto"))
            {
                object patternObj;
                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out patternObj))
                {
                    ValuePattern vp = (ValuePattern)patternObj;
                    vp.SetValue(value ?? "");
                    executedMethod = "ValuePattern.SetValue";
                }
            }

            // 2. InvokePattern (Button, Hyperlink, MenuItem direct activation)
            if (string.IsNullOrEmpty(executedMethod) && (normalizedAction == "auto" || normalizedAction == "invoke" || normalizedAction == "click"))
            {
                object patternObj;
                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out patternObj))
                {
                    InvokePattern ip = (InvokePattern)patternObj;
                    ip.Invoke();
                    executedMethod = "InvokePattern.Invoke";
                }
            }

            // 3. TogglePattern (Checkbox, Switch state flip)
            if (string.IsNullOrEmpty(executedMethod) && (normalizedAction == "auto" || normalizedAction == "toggle"))
            {
                object patternObj;
                if (element.TryGetCurrentPattern(TogglePattern.Pattern, out patternObj))
                {
                    TogglePattern tp = (TogglePattern)patternObj;
                    tp.Toggle();
                    executedMethod = "TogglePattern.Toggle";
                }
            }

            // 4. SelectionItemPattern (TabItem, ListItem, RadioButton selection)
            if (string.IsNullOrEmpty(executedMethod) && (normalizedAction == "auto" || normalizedAction == "select"))
            {
                object patternObj;
                if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out patternObj))
                {
                    SelectionItemPattern sp = (SelectionItemPattern)patternObj;
                    sp.Select();
                    executedMethod = "SelectionItemPattern.Select";
                }
            }

            // 5. ExpandCollapsePattern (Tree nodes, ComboBox dropdowns)
            if (string.IsNullOrEmpty(executedMethod) && (normalizedAction == "expand" || normalizedAction == "collapse"))
            {
                object patternObj;
                if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out patternObj))
                {
                    ExpandCollapsePattern ep = (ExpandCollapsePattern)patternObj;
                    if (normalizedAction == "expand") ep.Expand();
                    else ep.Collapse();
                    executedMethod = "ExpandCollapsePattern." + normalizedAction;
                }
            }

            if (string.IsNullOrEmpty(executedMethod))
            {
                return new Dictionary<string, object>()
                {
                    { "success", false },
                    { "patternSupported", false },
                    { "message", "Target element does not support in-memory control pattern for action: " + normalizedAction },
                    { "elementName", elementName }
                };
            }

            return new Dictionary<string, object>()
            {
                { "success", true },
                { "patternSupported", true },
                { "method", executedMethod },
                { "elementName", elementName }
            };
        }

        public static List<string> DiscoverSupportedActions(AutomationElement element)
        {
            List<string> actions = new List<string>();
            if (element == null) return actions;

            object testPattern;
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out testPattern)) actions.Add("invoke");
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out testPattern)) actions.Add("toggle");
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out testPattern)) actions.Add("select");
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out testPattern)) actions.Add("set_value");
            if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out testPattern)) actions.Add("expand_collapse");

            return actions;
        }
    }
}
