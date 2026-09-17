using System.Drawing;

namespace WinComputerUse.Native
{
    public class RippleItem
    {
        public float X;
        public float Y;
        public float Progress; // 0.0f -> 1.0f
        public Color Color;
    }

    public class TargetWindowItem
    {
        public RECT Bounds;
        public string Title;
        public float Progress; // 0.0f -> 1.0f
    }

    public class CursorHaloItem
    {
        public float X;
        public float Y;
        public float Progress; // 0.0f -> 1.0f
        public string ActionLabel; // e.g. "Click", "Scroll", "Drag", "Invoke"
    }

    public class ElementFocusItem
    {
        public RECT Bounds;
        public string Name;
        public float Progress; // 0.0f -> 1.0f
    }

}
