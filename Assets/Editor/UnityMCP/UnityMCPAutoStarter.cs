using UnityEngine;
using UnityEditor;
using System;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Auto-starts Unity MCP Bridge when Unity loads
    /// Inspired by GameLovers MCP Unity implementation
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMCPAutoStarter
    {
        private static UnityMCPBridge _bridgeInstance;
        private static bool _autoStartEnabled = true; // Can be made configurable

        /// <summary>
        /// Static constructor called when Unity loads due to InitializeOnLoad attribute
        /// </summary>
        static UnityMCPAutoStarter()
        {
            // Ensure proper initialization after Unity loads
            EditorApplication.delayCall += Initialize;

            // Subscribe to Unity lifecycle events for robust handling
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Initialize the Unity MCP Bridge system
        /// </summary>
        private static void Initialize()
        {
            if (!_autoStartEnabled) return;

            try
            {
                // Ensure Unity runs in background for continuous communication
                Application.runInBackground = true;

                Debug.Log("[UnityMCPAutoStarter] Initializing Unity MCP Bridge system");

                // Auto-start the bridge if it's not already running
                EnsureBridgeIsRunning();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UnityMCPAutoStarter] Failed to initialize: {e.Message}");
            }
        }

        /// <summary>
        /// Ensure the Unity MCP Bridge is running
        /// </summary>
        private static void EnsureBridgeIsRunning()
        {
            // Try to find existing bridge window
            _bridgeInstance = Resources.FindObjectsOfTypeAll<UnityMCPBridge>()?.Length > 0
                ? Resources.FindObjectsOfTypeAll<UnityMCPBridge>()[0]
                : null;

            // If no bridge exists or it's not running, start it
            if (_bridgeInstance == null)
            {
                // Create bridge window and ensure it's initialized (OnEnable called)
                _bridgeInstance = EditorWindow.GetWindow<UnityMCPBridge>("Unity MCP Bridge", false);

                // Force OnEnable to be called by briefly showing then hiding
                _bridgeInstance.Show();

                // Move completely off-screen and minimize to system tray equivalent
                _bridgeInstance.position = new UnityEngine.Rect(-2000, -2000, 1, 1);
                _bridgeInstance.minSize = new UnityEngine.Vector2(1, 1);
                _bridgeInstance.maxSize = new UnityEngine.Vector2(1, 1);

                // Keep the window but make it invisible to user
                EditorApplication.delayCall += () => {
                    if (_bridgeInstance != null) {
                        _bridgeInstance.position = new UnityEngine.Rect(-2000, -2000, 1, 1);
                    }
                };

                // Auto-start the server
                if (_bridgeInstance != null)
                {
                    // Delay the start to ensure proper initialization
                    EditorApplication.delayCall += () =>
                    {
                        _bridgeInstance.StartServerIfNotRunning();
                        Debug.Log("[UnityMCPAutoStarter] Unity MCP Bridge auto-started");
                    };
                }
            }
            else
            {
                // Bridge exists, ensure it's running
                EditorApplication.delayCall += () =>
                {
                    _bridgeInstance.StartServerIfNotRunning();
                };
            }
        }

        /// <summary>
        /// Handle Unity Editor quitting
        /// </summary>
        private static void OnEditorQuitting()
        {
            Debug.Log("[UnityMCPAutoStarter] Editor quitting, stopping MCP Bridge");
            _bridgeInstance?.StopServerIfRunning();
        }

        /// <summary>
        /// Handle before assembly reload
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            Debug.Log("[UnityMCPAutoStarter] Before assembly reload, stopping MCP Bridge");
            _bridgeInstance?.StopServerIfRunning();
        }

        /// <summary>
        /// Handle after assembly reload
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
            if (_autoStartEnabled)
            {
                Debug.Log("[UnityMCPAutoStarter] After assembly reload, restarting MCP Bridge");
                EditorApplication.delayCall += EnsureBridgeIsRunning;
            }
        }

        /// <summary>
        /// Handle play mode state changes
        /// </summary>
        /// <param name="state">Play mode state</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    Debug.Log("[UnityMCPAutoStarter] Exiting edit mode, stopping MCP Bridge");
                    _bridgeInstance?.StopServerIfRunning();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    if (_autoStartEnabled)
                    {
                        Debug.Log("[UnityMCPAutoStarter] Entered edit mode, starting MCP Bridge");
                        EditorApplication.delayCall += EnsureBridgeIsRunning;
                    }
                    break;
            }
        }

        /// <summary>
        /// Enable or disable auto-start functionality
        /// </summary>
        /// <param name="enabled">Whether to enable auto-start</param>
        [MenuItem("Tools/Unity MCP/Toggle Auto Start")]
        public static void ToggleAutoStart()
        {
            _autoStartEnabled = !_autoStartEnabled;
            Debug.Log($"[UnityMCPAutoStarter] Auto-start {(_autoStartEnabled ? "enabled" : "disabled")}");

            if (_autoStartEnabled)
            {
                EnsureBridgeIsRunning();
            }
            else
            {
                _bridgeInstance?.StopServerIfRunning();
            }
        }
    }
}