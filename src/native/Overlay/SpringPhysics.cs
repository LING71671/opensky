using System;

namespace WinComputerUse.Native
{
    /// <summary>
    /// Single Responsibility: Mathematical solver for 2nd-order damped harmonic spring motion.
    /// Used across all visual overlays for natural, organic acceleration, deceleration, and rebound.
    /// </summary>
    public static class SpringPhysics
    {
        /// <summary>
        /// Evaluates normalized spring displacement x(t) in [0, 1].
        /// </summary>
        /// <param name="progress">Normalized time in [0, 1].</param>
        /// <param name="zeta">Damping ratio (underdamped: 0.7-0.8).</param>
        /// <param name="omega">Natural angular frequency (rad/s).</param>
        /// <returns>Normalized displacement factor with controlled overshoot.</returns>
        public static float Evaluate(float progress, double zeta, double omega)
        {
            if (progress <= 0.0f) return 0.0f;
            if (progress >= 1.0f) return 1.0f;

            double tSec = progress * 0.45;
            double decay = Math.Exp(-zeta * omega * tSec);
            double omegaD = omega * Math.Sqrt(Math.Max(0.0, 1.0 - zeta * zeta));
            double osc = Math.Cos(omegaD * tSec) + (zeta / Math.Sqrt(Math.Max(0.0001, 1.0 - zeta * zeta))) * Math.Sin(omegaD * tSec);

            double val = 1.0 - decay * osc;
            return (float)Math.Max(0.0, Math.Min(1.08, val));
        }

        /// <summary>
        /// Solves cubic ease-out curve for fast non-oscillating transitions.
        /// </summary>
        public static float EaseOutCubic(float t)
        {
            float f = 1.0f - Math.Max(0f, Math.Min(1f, t));
            return 1.0f - (f * f * f);
        }

        /// <summary>
        /// Computes symmetrical fade factor for temporary action badges.
        /// </summary>
        public static float ComputeBadgeAlpha(float progress, float peakAt)
        {
            if (progress < peakAt)
            {
                return Math.Max(0f, Math.Min(1f, progress / peakAt));
            }
            return Math.Max(0f, Math.Min(1f, 1.0f - ((progress - peakAt) / (1.0f - peakAt))));
        }
    }
}
