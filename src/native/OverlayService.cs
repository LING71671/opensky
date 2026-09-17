using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace WinComputerUse.Native
{
    public static class OverlayService
    {
        private static LayeredOverlayForm _overlayForm;
        private static Thread _overlayThread;
        private static readonly object _lock = new object();
        private static bool _enabled = true;

        private static readonly List<RippleItem> _ripples = new List<RippleItem>();
        private static TargetWindowItem _currentTargetWindow = null;
        private static System.Windows.Forms.Timer _animTimer;
        private static bool _needsClearFrame = false;

        public static bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public static void EnsureOverlayRunning()
        {
            if (!_enabled) return;

            lock (_lock)
            {
                if (_overlayThread != null && _overlayThread.IsAlive) return;

                _overlayThread = new Thread(() =>
                {
                    DesktopManager.RunOnInteractiveDesktop(() =>
                    {
                        _overlayForm = new LayeredOverlayForm();
                        _animTimer = new System.Windows.Forms.Timer();
                        _animTimer.Interval = 16; // ~60 FPS
                        _animTimer.Tick += (s, e) => UpdateAndRender();

                        Application.Run(_overlayForm);
                        return true;
                    });
                });

                _overlayThread.SetApartmentState(ApartmentState.STA);
                _overlayThread.IsBackground = true;
                _overlayThread.Start();
            }
        }

        public static void ShowClick(int x, int y, string button)
        {
            if (!_enabled) return;
            EnsureOverlayRunning();

            Color rippleColor = Color.FromArgb(0, 163, 255); // Electric Azure
            if (button != null && (button.ToLowerInvariant().Contains("right") || button.ToLowerInvariant() == "r"))
            {
                rippleColor = Color.FromArgb(255, 77, 77); // Coral Crimson
            }

            lock (_ripples)
            {
                _ripples.Add(new RippleItem()
                {
                    X = x,
                    Y = y,
                    Progress = 0.0f,
                    Color = rippleColor
                });
            }

            WakeAnimationLoop();
        }

        public static void HighlightWindow(RECT bounds, string title)
        {
            if (!_enabled) return;
            EnsureOverlayRunning();

            lock (_lock)
            {
                _currentTargetWindow = new TargetWindowItem()
                {
                    Bounds = bounds,
                    Title = title ?? "",
                    Progress = 0.0f
                };
            }

            WakeAnimationLoop();
        }

        public static void ClearHighlight()
        {
            lock (_lock)
            {
                _currentTargetWindow = null;
            }
            WakeAnimationLoop();
        }

        private static void WakeAnimationLoop()
        {
            if (_overlayForm != null && !_overlayForm.IsDisposed && _overlayForm.IsHandleCreated)
            {
                try
                {
                    _overlayForm.BeginInvoke(new Action(() =>
                    {
                        if (_animTimer != null && !_animTimer.Enabled)
                        {
                            _animTimer.Start();
                        }
                    }));
                }
                catch { }
            }
        }

        private static void UpdateAndRender()
        {
            if (_overlayForm == null || _overlayForm.IsDisposed) return;

            bool hasActiveAnimations = false;

            // 1. Advance ripples (~380ms)
            lock (_ripples)
            {
                if (_ripples.Count > 0)
                {
                    hasActiveAnimations = true;
                    for (int i = _ripples.Count - 1; i >= 0; i--)
                    {
                        _ripples[i].Progress += 0.045f;
                        if (_ripples[i].Progress >= 1.0f)
                        {
                            _ripples.RemoveAt(i);
                        }
                    }
                }
            }

            // 2. Advance target window reticle (~1000ms)
            lock (_lock)
            {
                if (_currentTargetWindow != null)
                {
                    hasActiveAnimations = true;
                    _currentTargetWindow.Progress += 0.016f;
                    if (_currentTargetWindow.Progress >= 1.0f)
                    {
                        _currentTargetWindow = null;
                    }
                }
            }

            // 3. Delegate rendering to OverlayRenderer
            if (hasActiveAnimations)
            {
                _needsClearFrame = true;
                _overlayForm.RenderFrame(_ripples, _currentTargetWindow);
            }
            else if (_needsClearFrame)
            {
                _overlayForm.RenderEmpty();
                _needsClearFrame = false;
                _animTimer.Stop(); // Zero-CPU idle
            }
        }
    }
}
