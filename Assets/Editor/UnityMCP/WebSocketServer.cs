using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMCP.Editor
{
    public class WebSocketServer
    {
        private HttpListener httpListener;
        private List<WebSocket> connectedClients;
        private CancellationTokenSource cancellationTokenSource;
        private int port;
        private bool isRunning = false;

        public WebSocketServer(int port)
        {
            this.port = port;
            this.connectedClients = new List<WebSocket>();
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://localhost:{port}/");
                httpListener.Start();
                isRunning = true;

                Debug.Log($"WebSocket server started on port {port}");

                // Start accepting connections
                _ = Task.Run(AcceptConnections);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start WebSocket server: {e.Message}");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                isRunning = false;
                cancellationTokenSource.Cancel();

                // Close all connected clients
                foreach (var client in connectedClients.ToArray())
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                    }
                }

                connectedClients.Clear();
                httpListener?.Stop();
                httpListener?.Close();

                Debug.Log("WebSocket server stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping WebSocket server: {e.Message}");
            }
        }

        private async Task AcceptConnections()
        {
            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when server is stopping
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error accepting connection: {e.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        private async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;

                lock (connectedClients)
                {
                    connectedClients.Add(webSocket);
                }

                Debug.Log($"WebSocket client connected. Total clients: {connectedClients.Count}");

                // Handle this connection
                await HandleWebSocketConnection(webSocket);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing WebSocket request: {e.Message}");
            }
        }

        private async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            var buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.Log($"Received: {message}");

                        // Process message (this will be handled by the bridge)
                        // For now, just echo back
                        await SendToClient(webSocket, $"Echo: {message}");
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when server is stopping
            }
            catch (WebSocketException e)
            {
                Debug.Log($"WebSocket connection closed: {e.Message}");
            }
            finally
            {
                lock (connectedClients)
                {
                    connectedClients.Remove(webSocket);
                }
                Debug.Log($"WebSocket client disconnected. Total clients: {connectedClients.Count}");
            }
        }

        public async Task SendToClient(WebSocket webSocket, string message)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            }
        }

        public async Task BroadcastMessage(string message)
        {
            var clientsCopy = new List<WebSocket>();
            lock (connectedClients)
            {
                clientsCopy.AddRange(connectedClients);
            }

            var tasks = new List<Task>();
            foreach (var client in clientsCopy)
            {
                tasks.Add(SendToClient(client, message));
            }

            await Task.WhenAll(tasks);
        }

        public async Task<string> ReceiveMessageAsync()
        {
            // This is a simplified version - in reality you'd want to implement proper message queuing
            await Task.Delay(100);
            return null;
        }

        public async Task SendMessageAsync(string message)
        {
            await BroadcastMessage(message);
        }
    }
}