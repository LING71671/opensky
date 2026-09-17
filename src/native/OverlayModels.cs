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
}
