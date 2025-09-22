using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading;

namespace UnityMCP.Editor
{
    /// <summary>
    /// WebSocket server using WebSocketSharp library
    /// </summary>
    public class WebSocketSharpServer
    {
        private WebSocketServer server;
        private int port;
        private bool isRunning = false;

        public bool IsRunning => isRunning;
        public int ConnectedClients => McpWebSocketBehavior.ConnectedClientCount;

        public WebSocketSharpServer(int port)
        {
            this.port = port;
        }

        public async Task StartAsync()
        {
            try
            {
                server = new WebSocketServer(port);
                server.ReuseAddress = true;

                // Enhanced WebSocket configuration for better stability
                server.KeepClean = true;
                server.WaitTime = TimeSpan.FromSeconds(5);

                server.AddWebSocketService("/", () => new McpWebSocketBehavior());

                server.Start();
                isRunning = true;

                Debug.Log($"WebSocketSharp server started on ws://localhost:{port} with enhanced stability features");

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start WebSocketSharp server: {e.Message}");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    isRunning = false;
                    Debug.Log("WebSocketSharp server stopped");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping WebSocketSharp server: {e.Message}");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            // This would need to be implemented to send to all connected clients
            // For now, we'll rely on the behavior to handle responses
            await Task.CompletedTask;
        }

        public async Task<string> ReceiveMessageAsync()
        {
            // This is handled by the WebSocketBehavior
            await Task.Delay(100);
            return null;
        }

        public void ProcessPendingMessages()
        {
            // 强制处理待处理的消息，确保后台通信正常
            if (server != null && isRunning)
            {
                // WebSocketSharp会在后台线程处理消息，这里主要是触发Unity的EditorApplication.delayCall
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }

    /// <summary>
    /// WebSocket behavior for handling MCP messages with enhanced stability
    /// </summary>
    public class McpWebSocketBehavior : WebSocketBehavior
    {
        private System.Timers.Timer heartbeatTimer;
        private static int connectedClientCount = 0;

        public static int ConnectedClientCount => connectedClientCount;

        protected override void OnOpen()
        {
            Debug.Log($"WebSocket client connected: {ID}");
            connectedClientCount++;

            // Configure connection for background operation
            IgnoreExtensions = true;

            // Start heartbeat to maintain connection
            StartHeartbeat();

            // Update bridge UI (use main thread)
            EditorApplication.delayCall += () => {
                if (UnityMCPBridge.Instance != null)
                {
                    UnityMCPBridge.Instance.UpdateConnectionStatus(connectedClientCount, EditorApplication.timeSinceStartup);
                }
            };
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log($"WebSocket client disconnected: {ID}, Code: {e.Code}, Reason: {e.Reason}");
            connectedClientCount = Math.Max(0, connectedClientCount - 1);
            StopHeartbeat();

            // Update bridge UI (use main thread to avoid errors)
            EditorApplication.delayCall += () => {
                if (UnityMCPBridge.Instance != null)
                {
                    UnityMCPBridge.Instance.UpdateConnectionStatus(connectedClientCount, 0);
                }
            };
        }

        /// <summary>
        /// Start heartbeat mechanism to keep connection alive
        /// </summary>
        private void StartHeartbeat()
        {
            if (heartbeatTimer != null) return;

            heartbeatTimer = new System.Timers.Timer(30000); // 30 seconds
            heartbeatTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    if (State == WebSocketState.Open)
                    {
                        // Send a simple heartbeat message to keep connection alive
                        Send("{\"heartbeat\":true}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Heartbeat failed: {ex.Message}");
                }
            };
            heartbeatTimer.Start();
        }

        /// <summary>
        /// Stop heartbeat mechanism
        /// </summary>
        private void StopHeartbeat()
        {
            if (heartbeatTimer != null)
            {
                heartbeatTimer.Stop();
                heartbeatTimer.Dispose();
                heartbeatTimer = null;
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Debug.Log($"MCP WebSocket received message: {e.Data}");

            try
            {
                // Update heartbeat time on message activity (use main thread)
                EditorApplication.delayCall += () => {
                    if (UnityMCPBridge.Instance != null)
                    {
                        UnityMCPBridge.Instance.UpdateConnectionStatus(connectedClientCount, EditorApplication.timeSinceStartup);
                    }
                };

                // Process the message using EditorCoroutineUtility for better background handling
                var messageJson = e.Data;
                ProcessMessageOnMainThread(messageJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing MCP WebSocket message: {ex.Message}");
                Send($"{{\"jsonrpc\":\"2.0\",\"error\":\"Processing failed: {ex.Message}\"}}");
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.LogError($"WebSocket error: {e.Message}");
        }

        private async void ProcessMessageOnMainThread(string messageJson)
        {
            try
            {
                Debug.Log($"Processing MCP message on background thread: {messageJson}");

                // Process message asynchronously to avoid blocking
                var response = await ProcessMessageAsync(messageJson);
                Debug.Log($"Bridge returned response: {response}");

                // Ensure we're still connected before sending
                if (State == WebSocketState.Open)
                {
                    try
                    {
                        Send(response);
                        Debug.Log("Response sent successfully");
                    }
                    catch (Exception sendEx)
                    {
                        Debug.LogError($"Failed to send response: {sendEx.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot send response - WebSocket state is: {State}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ProcessMessageOnMainThread: {e.Message}");

                // Only try to send error if connection is still open
                if (State == WebSocketState.Open)
                {
                    try
                    {
                        Send($"{{\"jsonrpc\":\"2.0\",\"error\":\"Background processing error: {e.Message}\"}}");
                    }
                    catch (Exception sendEx)
                    {
                        Debug.LogError($"Failed to send error response: {sendEx.Message}");
                    }
                }
            }
        }

        private async Task<string> ProcessMessageAsync(string messageJson)
        {
            // 直接在当前线程处理，不依赖EditorApplication.delayCall
            try
            {
                return await UnityMCPBridge.ProcessWebSocketMessage(messageJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ProcessMessageAsync: {e.Message}");
                return $"{{\"jsonrpc\":\"2.0\",\"error\":\"Processing error: {e.Message}\"}}";
            }
        }
    }
}