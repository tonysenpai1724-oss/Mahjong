using UnityEngine;

namespace MahjongOut3D.Utilities
{
    /// <summary>
    /// Provides a centralized logging wrapper for the Mahjong runtime module.
    /// </summary>
    public static class MahjongRuntimeLogger
    {
        private static bool verboseLoggingEnabled;

        /// <summary>
        /// Configures whether verbose runtime logs are enabled.
        /// </summary>
        /// <param name="isEnabled">True to enable verbose logs; otherwise false.</param>
        public static void Configure(bool isEnabled)
        {
            verboseLoggingEnabled = isEnabled;
        }

        /// <summary>
        /// Writes a standard runtime log message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void Log(string message)
        {
            Debug.Log($"[MahjongOut3D] {message}");
        }

        /// <summary>
        /// Writes a verbose log message when verbose logging is enabled.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void LogVerbose(string message)
        {
            if (!verboseLoggingEnabled)
            {
                return;
            }

            Debug.Log($"[MahjongOut3D:Verbose] {message}");
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[MahjongOut3D] {message}");
        }

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="message">Message to write.</param>
        public static void LogError(string message)
        {
            Debug.LogError($"[MahjongOut3D] {message}");
        }
    }
}
