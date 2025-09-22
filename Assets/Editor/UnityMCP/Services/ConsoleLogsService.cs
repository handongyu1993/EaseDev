using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor.Services
{
    /// <summary>
    /// Service for managing Unity console logs based on the professional MCP Unity implementation
    /// </summary>
    public class ConsoleLogsService
    {
        // Structure to store log information
        private class LogEntry
        {
            public string Message { get; set; }
            public string StackTrace { get; set; }
            public LogType Type { get; set; }
            public DateTime Timestamp { get; set; }
        }

        // Constants for log management
        private const int MaxLogEntries = 1000;
        private const int CleanupThreshold = 200;

        // Collection to store all log messages
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();

        // Singleton instance
        private static ConsoleLogsService _instance;
        public static ConsoleLogsService Instance => _instance ??= new ConsoleLogsService();

        /// <summary>
        /// Constructor
        /// </summary>
        private ConsoleLogsService()
        {
            StartListening();
            Debug.Log("[ConsoleLogsService] Initialized and started listening for logs");
        }

        /// <summary>
        /// Start listening for logs
        /// </summary>
        public void StartListening()
        {
            // Only use logMessageReceived to avoid duplicates
            Application.logMessageReceived += OnLogMessageReceived;

            // Unity 2022.3 implementation using reflection to detect console clears
            EditorApplication.update += CheckConsoleClearViaReflection;

            Debug.Log("[ConsoleLogsService] Started listening with logMessageReceived");
        }

        /// <summary>
        /// Stop listening for logs
        /// </summary>
        public void StopListening()
        {
            // Unregister from log messages
            Application.logMessageReceived -= OnLogMessageReceived;
            EditorApplication.update -= CheckConsoleClearViaReflection;
        }

        /// <summary>
        /// Get logs as a structured JObject with pagination support (Professional MCP pattern)
        /// </summary>
        /// <param name="logType">Filter by log type (empty for all)</param>
        /// <param name="offset">Starting index (0-based)</param>
        /// <param name="limit">Maximum number of logs to return (default: 100)</param>
        /// <param name="includeStackTrace">Whether to include stack trace in logs (default: true)</param>
        /// <returns>JObject containing logs array and pagination info</returns>
        public JObject GetLogsAsJson(string logType = "", int offset = 0, int limit = 100, bool includeStackTrace = true)
        {
            JArray logsArray = new JArray();
            bool filter = !string.IsNullOrEmpty(logType) && !string.Equals(logType, "all", StringComparison.OrdinalIgnoreCase);
            int totalCount = 0;
            int filteredCount = 0;
            int currentIndex = 0;

            lock (_logEntries)
            {
                totalCount = _logEntries.Count;

                // Single pass: count filtered entries and collect the requested page (newest first)
                for (int i = _logEntries.Count - 1; i >= 0; i--)
                {
                    var entry = _logEntries[i];

                    // Skip if filtering and entry doesn't match the filter
                    if (filter && !string.Equals(entry.Type.ToString(), logType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Count filtered entries
                    filteredCount++;

                    // Check if we're in the offset range and haven't reached the limit yet
                    if (currentIndex >= offset && logsArray.Count < limit)
                    {
                        var logObject = new JObject
                        {
                            ["message"] = entry.Message,
                            ["type"] = entry.Type.ToString(),
                            ["timestamp"] = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        };

                        if (includeStackTrace)
                        {
                            logObject["stackTrace"] = entry.StackTrace;
                        }

                        logsArray.Add(logObject);
                    }

                    currentIndex++;

                    // Early exit if we've collected enough logs
                    if (currentIndex >= offset + limit) break;
                }
            }

            return new JObject
            {
                ["logs"] = logsArray,
                ["totalCount"] = totalCount,
                ["filteredCount"] = filteredCount,
                ["returnedCount"] = logsArray.Count,
                ["success"] = true,
                ["message"] = $"Retrieved {logsArray.Count} of {filteredCount} log entries" +
                             (filter ? $" of type '{logType}'" : "") +
                             $" (offset: {offset}, limit: {limit}, includeStackTrace: {includeStackTrace}, total: {totalCount})"
            };
        }

        /// <summary>
        /// Clear all stored logs
        /// </summary>
        public void ClearLogs()
        {
            lock (_logEntries)
            {
                _logEntries.Clear();
            }
        }

        /// <summary>
        /// Get current log count
        /// </summary>
        /// <returns>Number of stored log entries</returns>
        public int GetLogCount()
        {
            lock (_logEntries)
            {
                return _logEntries.Count;
            }
        }

        /// <summary>
        /// Check if console was cleared using reflection (for Unity 2022.3)
        /// </summary>
        private void CheckConsoleClearViaReflection()
        {
            try
            {
                // Get current log counts using LogEntries (internal Unity API)
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType == null) return;

                var getCountMethod = logEntriesType.GetMethod("GetCount",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (getCountMethod == null) return;

                int currentTotalCount = (int)getCountMethod.Invoke(null, null);

                // If we had logs before, but now we don't, console was likely cleared
                if (currentTotalCount == 0 && _logEntries.Count > 0)
                {
                    ClearLogs();
                }
            }
            catch (Exception ex)
            {
                // Just log the error but don't break functionality
                Debug.LogError($"[Unity MCP] Error checking console clear: {ex.Message}");
            }
        }

        /// <summary>
        /// Callback for when a log message is received
        /// </summary>
        /// <param name="logString">The log message</param>
        /// <param name="stackTrace">The stack trace</param>
        /// <param name="type">The log type</param>
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            // Skip our own debug messages to avoid infinite loops
            if (logString.StartsWith("[ConsoleLogsService]") ||
                logString.StartsWith("[UnityMCPBridge]") ||
                logString.StartsWith("[UnityMCPAutoStarter]"))
                return;

            // Add the log entry to our collection
            lock (_logEntries)
            {
                _logEntries.Add(new LogEntry
                {
                    Message = logString,
                    StackTrace = stackTrace,
                    Type = type,
                    Timestamp = DateTime.Now
                });

                // Clean up old entries if we exceed the maximum
                if (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveRange(0, CleanupThreshold);
                }

                // Debug output for capturing activity (temporarily enabled)
                if (_logEntries.Count <= 10 || _logEntries.Count % 10 == 0)
                {
                    Debug.Log($"[ConsoleLogsService] Captured log #{_logEntries.Count}: {type} - {logString.Substring(0, Math.Min(50, logString.Length))}...");
                }
            }
        }
    }
}