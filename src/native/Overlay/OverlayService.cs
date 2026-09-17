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
        private static readonly List<CursorHaloItem> _halos = new List<CursorHaloItem>();
        private static readonly List<ElementFocusItem> _focuses = new List<ElementFocusItem>();
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

            // Pure white tactile shockwave
            Color rippleColor = Color.FromArgb(255, 255, 255);

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

        public static void ShowCursorHalo(int x, int y, string label)
        {
            if (!_enabled) return;
            EnsureOverlayRunning();

            lock (_halos)
            {
                _halos.Add(new CursorHaloItem()
                {
                    X = x,
                    Y = y,
                    Progress = 0.0f,
                    ActionLabel = label ?? "Agent"
                });
            }

            WakeAnimationLoop();
        }

        public static void ShowElementFocus(RECT bounds, string name)
        {
            if (!_enabled) return;
            EnsureOverlayRunning();

            lock (_focuses)
            {
                _focuses.Add(new ElementFocusItem()
                {
                    Bounds = bounds,
                    Name = name ?? "",
                    Progress = 0.0f
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

            // 1. Advance ripples (~360ms)
            lock (_ripples)
            {
                if (_ripples.Count > 0)
                {
                    hasActiveAnimations = true;
                    for (int i = _ripples.Count - 1; i >= 0; i--)
                    {
                        _ripples[i].Progress += 0.048f;
                        if (_ripples[i].Progress >= 1.0f)
                        {
                            _ripples.RemoveAt(i);
                        }
                    }
                }
            }

            // 2. Advance halos & action pills (~650ms)
            lock (_halos)
            {
                if (_halos.Count > 0)
                {
                    hasActiveAnimations = true;
                    for (int i = _halos.Count - 1; i >= 0; i--)
                    {
                        _halos[i].Progress += 0.026f;
                        if (_halos[i].Progress >= 1.0f)
                        {
                            _halos.RemoveAt(i);
                        }
                    }
                }
            }

            // 3. Advance element focus pulses (~450ms)
            lock (_focuses)
            {
                if (_focuses.Count > 0)
                {
                    hasActiveAnimations = true;
                    for (int i = _focuses.Count - 1; i >= 0; i--)
                    {
                        _focuses[i].Progress += 0.038f;
                        if (_focuses[i].Progress >= 1.0f)
                        {
                            _focuses.RemoveAt(i);
                        }
                    }
                }
            }

            // 4. Advance target window reticle (~950ms)
            lock (_lock)
            {
                if (_currentTargetWindow != null)
                {
                    hasActiveAnimations = true;
                    _currentTargetWindow.Progress += 0.017f;
                    if (_currentTargetWindow.Progress >= 1.0f)
                    {
                        _currentTargetWindow = null;
                    }
                }
            }

            // 5. Delegate rendering to OverlayRenderer
            if (hasActiveAnimations)
            {
                _needsClearFrame = true;
                _overlayForm.RenderFrame(_ripples, _currentTargetWindow, _halos, _focuses);
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
