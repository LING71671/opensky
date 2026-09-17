using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinComputerUse.Native
{
    public class LayeredOverlayForm : Form
    {
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, uint crKey, [In] ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private Bitmap _surfaceBitmap;
        private Graphics _surfaceGraphics;
        private IntPtr _screenDc = IntPtr.Zero;
        private IntPtr _memDc = IntPtr.Zero;
        private Rectangle _virtualScreen;

        public LayeredOverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            _virtualScreen = SystemInformation.VirtualScreen;
            this.Location = _virtualScreen.Location;
            this.Size = _virtualScreen.Size;

            // Pre-allocate 32-bit ARGB surface for hardware alpha compositing
            _surfaceBitmap = new Bitmap(_virtualScreen.Width, _virtualScreen.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            _surfaceGraphics = Graphics.FromImage(_surfaceBitmap);
            _surfaceGraphics.SmoothingMode = SmoothingMode.HighQuality;
            _surfaceGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            _screenDc = GetDC(IntPtr.Zero);
            _memDc = CreateCompatibleDC(_screenDc);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Win32Constants.WS_EX_LAYERED |
                              Win32Constants.WS_EX_TRANSPARENT |
                              Win32Constants.WS_EX_TOOLWINDOW |
                              Win32Constants.WS_EX_TOPMOST |
                              Win32Constants.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_surfaceGraphics != null) { _surfaceGraphics.Dispose(); _surfaceGraphics = null; }
                if (_surfaceBitmap != null) { _surfaceBitmap.Dispose(); _surfaceBitmap = null; }
                if (_memDc != IntPtr.Zero) { DeleteDC(_memDc); _memDc = IntPtr.Zero; }
                if (_screenDc != IntPtr.Zero) { ReleaseDC(IntPtr.Zero, _screenDc); _screenDc = IntPtr.Zero; }
            }
            base.Dispose(disposing);
        }

        public void RenderEmpty()
        {
            if (_surfaceBitmap == null) return;
            _surfaceGraphics.Clear(Color.FromArgb(0, 0, 0, 0));
            CommitLayeredSurface();
        }

        public void RenderFrame(List<RippleItem> ripples, TargetWindowItem target)
        {
            if (_surfaceBitmap == null) return;

            _surfaceGraphics.Clear(Color.FromArgb(0, 0, 0, 0));

            int originX = _virtualScreen.Left;
            int originY = _virtualScreen.Top;

            if (target != null)
            {
                DrawTargetReticle(target, originX, originY);
            }

            if (ripples != null && ripples.Count > 0)
            {
                DrawRipples(ripples, originX, originY);
            }

            CommitLayeredSurface();
        }

        private void DrawTargetReticle(TargetWindowItem target, int originX, int originY)
        {
            RECT r = target.Bounds;
            int bw = r.Right - r.Left;
            int bh = r.Bottom - r.Top;
            int relX = r.Left - originX;
            int relY = r.Top - originY;

            if (bw <= 20 || bh <= 20) return;

            float p = target.Progress;
            float alphaFactor = 1.0f;
            float cornerOffset = 0f;

            if (p < 0.2f)
            {
                float tIn = p / 0.2f;
                float easeIn = 1.0f - (float)Math.Pow(1.0f - tIn, 3);
                cornerOffset = (1.0f - easeIn) * 10f; // Glide inward from 10px
                alphaFactor = easeIn;
            }
            else if (p > 0.7f)
            {
                float tOut = (p - 0.7f) / 0.3f;
                alphaFactor = 1.0f - (float)Math.Pow(tOut, 2);
            }

            int cornerAlpha = Math.Max(0, Math.Min(255, (int)(alphaFactor * 240)));
            int glowAlpha = Math.Max(0, Math.Min(255, (int)(alphaFactor * 60)));

            // Outer boundary guide
            using (Pen guidePen = new Pen(Color.FromArgb(glowAlpha, 0, 163, 255), 1.5f))
            {
                _surfaceGraphics.DrawRectangle(guidePen, relX, relY, bw, bh);
            }

            // Four Corner Precision Brackets
            using (Pen cornerPen = new Pen(Color.FromArgb(cornerAlpha, 0, 163, 255), 2.5f))
            {
                cornerPen.StartCap = LineCap.Round;
                cornerPen.EndCap = LineCap.Round;
                int len = 26;

                int x1 = (int)(relX - cornerOffset);
                int y1 = (int)(relY - cornerOffset);
                int x2 = (int)(relX + bw + cornerOffset);
                int y2 = (int)(relY + bh + cornerOffset);

                _surfaceGraphics.DrawLine(cornerPen, x1, y1, x1 + len, y1);
                _surfaceGraphics.DrawLine(cornerPen, x1, y1, x1, y1 + len);

                _surfaceGraphics.DrawLine(cornerPen, x2, y1, x2 - len, y1);
                _surfaceGraphics.DrawLine(cornerPen, x2, y1, x2, y1 + len);

                _surfaceGraphics.DrawLine(cornerPen, x1, y2, x1 + len, y2);
                _surfaceGraphics.DrawLine(cornerPen, x1, y2, x1, y2 - len);

                _surfaceGraphics.DrawLine(cornerPen, x2, y2, x2 - len, y2);
                _surfaceGraphics.DrawLine(cornerPen, x2, y2, x2, y2 - len);
            }

            // Status Pill Badge
            string title = target.Title.Length > 24 ? target.Title.Substring(0, 24) + "..." : target.Title;
            string badgeText = "Target: " + (string.IsNullOrEmpty(title) ? "Window" : title);

            using (Font font = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                SizeF sz = _surfaceGraphics.MeasureString(badgeText, font);
                int pillW = (int)sz.Width + 28;
                int pillH = 24;
                int pillX = relX + 12;
                int pillY = Math.Max(0, relY - pillH - 6);

                using (GraphicsPath path = CreateRoundedRect(new Rectangle(pillX, pillY, pillW, pillH), 12))
                using (Brush bgBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 225), 15, 23, 42)))
                using (Pen borderPen = new Pen(Color.FromArgb((int)(alphaFactor * 160), 0, 163, 255), 1.2f))
                using (Brush dotBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 255), 0, 220, 255)))
                using (Brush textBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 245), 241, 245, 249)))
                {
                    _surfaceGraphics.FillPath(bgBrush, path);
                    _surfaceGraphics.DrawPath(borderPen, path);
                    _surfaceGraphics.FillEllipse(dotBrush, pillX + 9, pillY + 9, 6, 6);
                    _surfaceGraphics.DrawString(badgeText, font, textBrush, pillX + 20, pillY + 4);
                }
            }
        }

        private void DrawRipples(List<RippleItem> ripples, int originX, int originY)
        {
            lock (ripples)
            {
                foreach (var rip in ripples)
                {
                    float p = rip.Progress;
                    if (p >= 1.0f) continue;

                    int cx = (int)(rip.X - originX);
                    int cy = (int)(rip.Y - originY);
                    float ease = 1.0f - (float)Math.Pow(1.0f - p, 3);

                    // Primary shockwave ring
                    float primaryRadius = 8f + 36f * ease;
                    int primaryAlpha = Math.Max(0, Math.Min(255, (int)((1.0f - p) * 235)));
                    float penWidth = Math.Max(1.2f, 2.8f * (1.0f - p * 0.6f));

                    using (Pen pen = new Pen(Color.FromArgb(primaryAlpha, rip.Color), penWidth))
                    {
                        _surfaceGraphics.DrawEllipse(pen, cx - primaryRadius, cy - primaryRadius, primaryRadius * 2, primaryRadius * 2);
                    }

                    // Secondary echo wave
                    if (p > 0.12f)
                    {
                        float p2 = (p - 0.12f) / 0.88f;
                        float ease2 = 1.0f - (float)Math.Pow(1.0f - p2, 3);
                        float secondaryRadius = 6f + 22f * ease2;
                        int secondaryAlpha = Math.Max(0, Math.Min(255, (int)((1.0f - p2) * 115)));

                        using (Pen pen2 = new Pen(Color.FromArgb(secondaryAlpha, rip.Color), 1.5f))
                        {
                            _surfaceGraphics.DrawEllipse(pen2, cx - secondaryRadius, cy - secondaryRadius, secondaryRadius * 2, secondaryRadius * 2);
                        }
                    }

                    // Center tactile impact flash
                    if (p < 0.32f)
                    {
                        float pDot = p / 0.32f;
                        int dotAlpha = Math.Max(0, Math.Min(255, (int)((1.0f - pDot) * 255)));
                        float dotR = 4f * (1.0f - pDot * 0.5f);

                        using (Brush dotBrush = new SolidBrush(Color.FromArgb(dotAlpha, Color.White)))
                        {
                            _surfaceGraphics.FillEllipse(dotBrush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);
                        }
                    }
                }
            }
        }

        private void CommitLayeredSurface()
        {
            if (_memDc == IntPtr.Zero || _surfaceBitmap == null) return;

            IntPtr hBitmap = _surfaceBitmap.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = SelectObject(_memDc, hBitmap);

            try
            {
                POINT topPos = new POINT(_virtualScreen.Left, _virtualScreen.Top);
                SIZE size = new SIZE(_virtualScreen.Width, _virtualScreen.Height);
                POINT pointSource = new POINT(0, 0);

                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = Win32Constants.AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = Win32Constants.AC_SRC_ALPHA;

                UpdateLayeredWindow(this.Handle, _screenDc, ref topPos, ref size, _memDc, ref pointSource, 0, ref blend, Win32Constants.ULW_ALPHA);
            }
            finally
            {
                SelectObject(_memDc, oldBitmap);
                DeleteObject(hBitmap);
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
