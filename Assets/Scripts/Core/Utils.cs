/* =========================================
   Utils.cs — Utility Functions
   Static helper methods for math, easing,
   formatting, and color operations.
   
   Ported from: js/utils.js
   ========================================= */

using UnityEngine;

namespace SyncBreaker.Core
{
    /// <summary>
    /// Collection of static utility methods used throughout the game.
    /// Replaces the JavaScript Utils object.
    /// </summary>
    public static class Utils
    {
        // ════════════════════════════════════════
        //  MATH
        // ════════════════════════════════════════

        /// <summary>
        /// Clamp a value between min and max.
        /// </summary>
        public static float Clamp(float val, float min, float max)
        {
            return Mathf.Clamp(val, min, max);
        }

        /// <summary>
        /// Linear interpolation between a and b.
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return Mathf.Lerp(a, b, t);
        }

        /// <summary>
        /// Degrees to radians.
        /// </summary>
        public static float DegToRad(float deg)
        {
            return deg * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Radians to degrees.
        /// </summary>
        public static float RadToDeg(float rad)
        {
            return rad * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Random integer between min and max (inclusive).
        /// </summary>
        public static int RandInt(int min, int max)
        {
            return Random.Range(min, max + 1);
        }

        /// <summary>
        /// Random float between min and max.
        /// </summary>
        public static float RandFloat(float min, float max)
        {
            return Random.Range(min, max);
        }

        /// <summary>
        /// Remap a value from one range to another.
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        /// <summary>
        /// Shortest angular distance between two angles (in degrees).
        /// Always returns positive value.
        /// </summary>
        public static float AngleDistance(float a, float b)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(a, b));
            return diff;
        }

        /// <summary>
        /// Check if angle A has passed angle B (clockwise, with margin).
        /// </summary>
        public static bool IsAnglePast(float cursorAngle, float nodeAngle, float margin)
        {
            float diff = ((cursorAngle - nodeAngle) % 360f + 360f) % 360f;
            return diff > margin && diff < 180f;
        }

        // ════════════════════════════════════════
        //  EASING FUNCTIONS
        // ════════════════════════════════════════

        /// <summary>
        /// Quadratic ease out: decelerating from zero velocity.
        /// </summary>
        public static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// Elastic ease out: overshooting then settling.
        /// Great for UI bounce effects.
        /// </summary>
        public static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>
        /// Cubic ease in-out: smooth acceleration and deceleration.
        /// </summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>
        /// Back ease out: slight overshoot before settling.
        /// </summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Bounce ease out: bouncing effect.
        /// </summary>
        public static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;
            else if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        // ════════════════════════════════════════
        //  FORMATTING
        // ════════════════════════════════════════

        /// <summary>
        /// Format seconds as "M:SS".
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}:{s:D2}";
        }

        /// <summary>
        /// Format score with leading zeros: "000150".
        /// </summary>
        public static string FormatScore(int score, int digits = 6)
        {
            return score.ToString().PadLeft(digits, '0');
        }

        // ════════════════════════════════════════
        //  COLOR
        // ════════════════════════════════════════

        /// <summary>
        /// Parse a hex color string (#RRGGBB or #RRGGBBAA) to Unity Color.
        /// </summary>
        public static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            Debug.LogWarning($"[Utils] Failed to parse color: {hex}");
            return Color.white;
        }

        /// <summary>
        /// Create a color with modified alpha.
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // ════════════════════════════════════════
        //  PHYSICS HELPERS
        // ════════════════════════════════════════

        /// <summary>
        /// Calculate velocity vector from angle (degrees) and speed.
        /// </summary>
        public static Vector2 AngleToVelocity(float angleDeg, float speed)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
        }

        /// <summary>
        /// Wrap a position within bounds (for seamless particle wrapping).
        /// </summary>
        public static Vector2 WrapPosition(Vector2 pos, Rect bounds)
        {
            if (pos.x < bounds.xMin) pos.x = bounds.xMax;
            if (pos.x > bounds.xMax) pos.x = bounds.xMin;
            if (pos.y < bounds.yMin) pos.y = bounds.yMax;
            if (pos.y > bounds.yMax) pos.y = bounds.yMin;
            return pos;
        }

        // ════════════════════════════════════════
        //  SCREEN
        // ════════════════════════════════════════

        /// <summary>
        /// Get the world-space bounds of the camera view.
        /// </summary>
        public static Rect GetCameraBounds(Camera cam = null)
        {
            cam ??= Camera.main;
            if (cam == null) return new Rect(0, 0, 16, 9);

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector3 pos = cam.transform.position;

            return new Rect(
                pos.x - width / 2f,
                pos.y - height / 2f,
                width,
                height
            );
        }
    }
}
