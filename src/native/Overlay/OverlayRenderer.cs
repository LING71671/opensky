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
            _surfaceGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

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

        public void RenderFrame(List<RippleItem> ripples, TargetWindowItem target, List<CursorHaloItem> halos, List<ElementFocusItem> focuses)
        {
            if (_surfaceBitmap == null) return;

            _surfaceGraphics.Clear(Color.FromArgb(0, 0, 0, 0));

            int originX = _virtualScreen.Left;
            int originY = _virtualScreen.Top;

            if (target != null)
            {
                DrawTargetReticle(target, originX, originY);
            }

            if (focuses != null && focuses.Count > 0)
            {
                DrawElementFocuses(focuses, originX, originY);
            }

            if (ripples != null && ripples.Count > 0)
            {
                DrawRipples(ripples, originX, originY);
            }

            if (halos != null && halos.Count > 0)
            {
                DrawCursorHalos(halos, originX, originY);
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

            if (p < 0.25f)
            {
                float springIn = SpringPhysics.Evaluate(p / 0.25f, Win32Constants.SPRING_ZETA, Win32Constants.SPRING_OMEGA);
                cornerOffset = (1.0f - Math.Min(1.0f, springIn)) * 12f;
                alphaFactor = Math.Min(1.0f, springIn);
            }
            else if (p > 0.72f)
            {
                float tOut = (p - 0.72f) / 0.28f;
                alphaFactor = 1.0f - (float)Math.Pow(tOut, 2);
            }

            int cornerAlpha = Math.Max(0, Math.Min(255, (int)(alphaFactor * 245)));
            int glowAlpha = Math.Max(0, Math.Min(255, (int)(alphaFactor * 50)));

            // Pure Frosted White Theme (Apple/Monochrome Aesthetic)
            Color whiteGlow = Color.FromArgb(glowAlpha, 255, 255, 255);
            Color whiteSolid = Color.FromArgb(cornerAlpha, 255, 255, 255);

            // Outer boundary guide
            using (Pen guidePen = new Pen(whiteGlow, 1.2f))
            {
                _surfaceGraphics.DrawRectangle(guidePen, relX, relY, bw, bh);
            }

            // Four Corner Precision Brackets
            using (Pen cornerPen = new Pen(whiteSolid, 2.2f))
            {
                cornerPen.StartCap = LineCap.Round;
                cornerPen.EndCap = LineCap.Round;
                int len = 24;

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

            // Status Pill Badge (Frosted White & Charcoal)
            string title = target.Title.Length > 24 ? target.Title.Substring(0, 24) + "..." : target.Title;
            string badgeText = "Target: " + (string.IsNullOrEmpty(title) ? "Window" : title);

            using (Font font = new Font("Segoe UI", 9f, FontStyle.Regular))
            {
                SizeF sz = _surfaceGraphics.MeasureString(badgeText, font);
                int pillW = (int)sz.Width + 24;
                int pillH = 22;
                int pillX = relX + 10;
                int pillY = Math.Max(0, relY - pillH - 6);

                using (GraphicsPath path = CreateRoundedRect(new Rectangle(pillX, pillY, pillW, pillH), 11))
                using (Brush bgBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 230), 20, 20, 20)))
                using (Pen borderPen = new Pen(Color.FromArgb((int)(alphaFactor * 180), 240, 240, 240), 1.0f))
                using (Brush dotBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 255), 255, 255, 255)))
                using (Brush textBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 250), 245, 245, 245)))
                {
                    _surfaceGraphics.FillPath(bgBrush, path);
                    _surfaceGraphics.DrawPath(borderPen, path);
                    _surfaceGraphics.FillEllipse(dotBrush, pillX + 8, pillY + 8, 6, 6);
                    _surfaceGraphics.DrawString(badgeText, font, textBrush, pillX + 18, pillY + 3);
                }
            }
        }

        private void DrawElementFocuses(List<ElementFocusItem> focuses, int originX, int originY)
        {
            lock (focuses)
            {
                foreach (var item in focuses)
                {
                    float p = item.Progress;
                    if (p >= 1.0f) continue;

                    RECT r = item.Bounds;
                    int bw = r.Right - r.Left;
                    int bh = r.Bottom - r.Top;
                    if (bw <= 0 || bh <= 0) continue;

                    int relX = r.Left - originX;
                    int relY = r.Top - originY;

                    // Spring pulsation
                    float spring = SpringPhysics.Evaluate(p, Win32Constants.SPRING_ZETA, Win32Constants.SPRING_OMEGA);
                    float expand = (1.0f - Math.Min(1.0f, spring)) * 6.0f;
                    float alphaFactor = (1.0f - p);

                    int alpha = Math.Max(0, Math.Min(255, (int)(alphaFactor * 220)));
                    using (Pen pen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 1.5f))
                    {
                        _surfaceGraphics.DrawRectangle(pen, relX - expand, relY - expand, bw + expand * 2, bh + expand * 2);
                    }
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

                    // Physical spring displacement
                    float spring = SpringPhysics.Evaluate(p, Win32Constants.SPRING_ZETA, Win32Constants.SPRING_OMEGA);

                    // Primary shockwave ring (Monochrome pure white)
                    float primaryRadius = 6f + 34f * spring;
                    int primaryAlpha = Math.Max(0, Math.Min(255, (int)((1.0f - p) * 230)));
                    float penWidth = Math.Max(1.0f, 2.4f * (1.0f - p * 0.7f));

                    using (Pen pen = new Pen(Color.FromArgb(primaryAlpha, 255, 255, 255), penWidth))
                    {
                        _surfaceGraphics.DrawEllipse(pen, cx - primaryRadius, cy - primaryRadius, primaryRadius * 2, primaryRadius * 2);
                    }

                    // Secondary ambient echo ring
                    if (p > 0.12f)
                    {
                        float p2 = (p - 0.12f) / 0.88f;
                        float spring2 = SpringPhysics.Evaluate(p2, Win32Constants.SPRING_ZETA, Win32Constants.SPRING_OMEGA);
                        float secondaryRadius = 4f + 20f * spring2;
                        int secondaryAlpha = Math.Max(0, Math.Min(255, (int)((1.0f - p2) * 110)));

                        using (Pen pen2 = new Pen(Color.FromArgb(secondaryAlpha, 240, 240, 240), 1.2f))
                        {
                            _surfaceGraphics.DrawEllipse(pen2, cx - secondaryRadius, cy - secondaryRadius, secondaryRadius * 2, secondaryRadius * 2);
                        }
                    }

                    // Tactile center pulse
                    if (p < 0.35f)
                    {
                        float pDot = p / 0.35f;
                        int dotAlpha = Math.Max(0, Math.Min(255, (int)((1.0f - pDot) * 255)));
                        using (Brush dotBrush = new SolidBrush(Color.FromArgb(dotAlpha, 255, 255, 255)))
                        {
                            _surfaceGraphics.FillEllipse(dotBrush, cx - 3, cy - 3, 6, 6);
                        }
                    }
                }
            }
        }

        private void DrawCursorHalos(List<CursorHaloItem> halos, int originX, int originY)
        {
            lock (halos)
            {
                foreach (var halo in halos)
                {
                    float p = halo.Progress;
                    if (p >= 1.0f) continue;

                    int cx = (int)(halo.X - originX);
                    int cy = (int)(halo.Y - originY);

                    float alphaFactor = p < 0.2f ? (p / 0.2f) : (1.0f - (p - 0.2f) / 0.8f);
                    alphaFactor = Math.Max(0f, Math.Min(1.0f, alphaFactor));

                    int ringAlpha = (int)(alphaFactor * 180);
                    int glowAlpha = (int)(alphaFactor * 60);

                    // 1. Cursor Halo Ring (Radius ~22px)
                    using (Pen glowPen = new Pen(Color.FromArgb(glowAlpha, 255, 255, 255), 4.0f))
                    using (Pen ringPen = new Pen(Color.FromArgb(ringAlpha, 255, 255, 255), 1.4f))
                    {
                        _surfaceGraphics.DrawEllipse(glowPen, cx - 20, cy - 20, 40, 40);
                        _surfaceGraphics.DrawEllipse(ringPen, cx - 20, cy - 20, 40, 40);
                    }

                    // 2. Action Status Pill below cursor
                    string text = string.IsNullOrEmpty(halo.ActionLabel) ? "Agent" : halo.ActionLabel;
                    using (Font font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                    {
                        SizeF sz = _surfaceGraphics.MeasureString(text, font);
                        int pillW = (int)sz.Width + 20;
                        int pillH = 20;
                        int pillX = cx + 14;
                        int pillY = cy + 14;

                        using (GraphicsPath path = CreateRoundedRect(new Rectangle(pillX, pillY, pillW, pillH), 10))
                        using (Brush bgBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 220), 20, 20, 20)))
                        using (Pen borderPen = new Pen(Color.FromArgb((int)(alphaFactor * 160), 255, 255, 255), 1.0f))
                        using (Brush textBrush = new SolidBrush(Color.FromArgb((int)(alphaFactor * 240), 250, 250, 250)))
                        {
                            _surfaceGraphics.FillPath(bgBrush, path);
                            _surfaceGraphics.DrawPath(borderPen, path);
                            _surfaceGraphics.DrawString(text, font, textBrush, pillX + 9, pillY + 3);
                        }
                    }
                }
            }
        }

        private GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void CommitLayeredSurface()
        {
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = _surfaceBitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(_memDc, hBitmap);

                POINT ptDst = new POINT(this.Location.X, this.Location.Y);
                SIZE size = new SIZE(this.Width, this.Height);
                POINT ptSrc = new POINT(0, 0);

                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = Win32Constants.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = Win32Constants.AC_SRC_ALPHA
                };

                UpdateLayeredWindow(
                    this.Handle,
                    _screenDc,
                    ref ptDst,
                    ref size,
                    _memDc,
                    ref ptSrc,
                    0,
                    ref blend,
                    Win32Constants.ULW_ALPHA
                );
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) SelectObject(_memDc, oldBitmap);
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
            }
        }
    }
}
