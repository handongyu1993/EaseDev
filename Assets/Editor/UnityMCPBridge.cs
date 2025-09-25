using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using UnityMCP.Tools;
using WebSocketSharp;
using WebSocketSharp.Server;

public class UnityMCPBridge : EditorWindow
{
    private static UnityMCPBridge instance;
    private bool isServerRunning = false;
    private int serverPort = 8766;
    private List<string> logs = new List<string>();
    private Vector2 scrollPosition;

    // MCP Tools management
    private Dictionary<string, McpToolBase> mcpTools = new Dictionary<string, McpToolBase>();

    // WebSocket server
    private WebSocketServer webSocketServer;
    private int connectedClients = 0;
    private double lastHeartbeat = 0;

    public static UnityMCPBridge Instance => instance;

    // Auto-start functionality flag
    private bool isAutoStartMode = false;

    [MenuItem("Tools/Unity MCP/Bridge Window")]
    public static void ShowWindow()
    {
        Debug.Log("UnityMCPBridge: ShowWindow called");
        instance = GetWindow<UnityMCPBridge>("Unity MCP Bridge");
        instance.Show();
        Debug.Log("UnityMCPBridge: Window shown successfully");
    }

    private void OnEnable()
    {
        Debug.Log("UnityMCPBridge: OnEnable START");
        instance = this;

        // Initialize MCP tools
        try
        {
            InitializeTools();
            AddLog($"MCP tools initialized: {mcpTools.Count} tools registered");
        }
        catch (Exception e)
        {
            AddLog($"Failed to initialize MCP tools: {e.Message}");
            Debug.LogError($"MCP tools initialization error: {e}");
        }

        AddLog("OnEnable completed - moved outside assembly");
        Debug.Log("UnityMCPBridge: OnEnable END");
    }

    private void OnDisable()
    {
        Debug.Log("UnityMCPBridge: OnDisable called");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity MCP Bridge", EditorStyles.boldLabel);

        // Connection Status
        EditorGUILayout.Space();
        DrawConnectionStatus();
        EditorGUILayout.Space();

        // Port Configuration
        serverPort = EditorGUILayout.IntField("Port:", serverPort);

        // Control Buttons
        EditorGUILayout.BeginHorizontal();
        if (!isServerRunning)
        {
            if (GUILayout.Button("Start Server"))
            {
                StartServer();
            }
        }
        else
        {
            if (GUILayout.Button("Stop Server"))
            {
                StopServer();
            }
        }

        if (GUILayout.Button("Clear Logs"))
        {
            logs.Clear();
        }
        EditorGUILayout.EndHorizontal();

        // Logs Display
        EditorGUILayout.Space();
        GUILayout.Label("Logs:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (var log in logs)
        {
            EditorGUILayout.SelectableLabel(log, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        EditorGUILayout.EndScrollView();

        // Connection Info
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Connect to: ws://localhost:{serverPort}", MessageType.Info);
    }

    private void StartServer()
    {
        try
        {
            if (webSocketServer != null)
            {
                webSocketServer.Stop();
            }

            webSocketServer = new WebSocketServer($"ws://localhost:{serverPort}");
            webSocketServer.AddWebSocketService("/", () => new McpWebSocketBehavior());
            webSocketServer.Start();

            isServerRunning = true;
            AddLog($"WebSocket server started on port {serverPort}");
            Debug.Log($"Unity MCP Bridge WebSocket server started on ws://localhost:{serverPort}");
        }
        catch (System.Exception e)
        {
            AddLog($"Failed to start server: {e.Message}");
            Debug.LogError($"Failed to start Unity MCP Bridge: {e.Message}");
            isServerRunning = false;
        }
    }

    private void StopServer()
    {
        try
        {
            if (webSocketServer != null)
            {
                webSocketServer.Stop();
                webSocketServer = null;
            }

            isServerRunning = false;
            AddLog("WebSocket server stopped");
        }
        catch (System.Exception e)
        {
            AddLog($"Error stopping server: {e.Message}");
        }
    }

    private void AddLog(string message)
    {
        var timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}";

        if (isAutoStartMode)
        {
            Debug.Log($"[UnityMCPBridge] {logMessage}");
        }
        else
        {
            logs.Add(logMessage);
            if (logs.Count > 100)
            {
                logs.RemoveAt(0);
            }

            // Schedule Repaint on main thread to avoid threading issues
            EditorApplication.delayCall += () => {
                if (this != null) Repaint();
            };
        }
    }

    #region MCP Tools Management

    /// <summary>
    /// Initialize all MCP tools
    /// </summary>
    private void InitializeTools()
    {
        mcpTools.Clear();

        // Register basic tools first
        RegisterTool(new CreateGameObjectTool());
        RegisterTool(new UpdateGameObjectTool());
        RegisterTool(new GetGameObjectTool());
        RegisterTool(new CreatePrefabTool());

        // Register additional tools
        RegisterTool(new ExecuteMenuItemTool());
        RegisterTool(new CreateSceneTool());
        RegisterTool(new CreateUIElementTool());
        RegisterTool(new CreateMaterialTool());
        RegisterTool(new AddComponentTool());
        RegisterTool(new UpdateComponentTool());

        // Register UI validation tools
        RegisterTool(new ValidateUIComponentsTool());
        RegisterTool(new ApplyUIStyleTool());

        AddLog($"Initialized {mcpTools.Count} MCP tools");
    }

    /// <summary>
    /// Register a tool in the tool registry
    /// </summary>
    private void RegisterTool(McpToolBase tool)
    {
        if (tool != null && !string.IsNullOrEmpty(tool.Name))
        {
            mcpTools[tool.Name] = tool;
            AddLog($"Registered tool: {tool.Name}");
        }
        else
        {
            AddLog("Failed to register tool: invalid tool or name");
        }
    }

    #endregion

    #region GUI Methods

    /// <summary>
    /// Draw connection status with visual indicators
    /// </summary>
    private void DrawConnectionStatus()
    {
        Color statusColor;
        string statusText;
        string statusIcon;

        if (isServerRunning)
        {
            double timeSinceHeartbeat = EditorApplication.timeSinceStartup - lastHeartbeat;
            bool hasRecentActivity = timeSinceHeartbeat < 60;

            if (connectedClients > 0 && hasRecentActivity)
            {
                statusColor = Color.green;
                statusText = $"Active ({connectedClients} client{(connectedClients > 1 ? "s" : "")})";
                statusIcon = "● ";
            }
            else if (connectedClients > 0)
            {
                statusColor = new Color(1f, 0.8f, 0f); // Orange
                statusText = $"Connected ({connectedClients} client{(connectedClients > 1 ? "s" : "")})";
                statusIcon = "◐ ";
            }
            else
            {
                statusColor = new Color(0.5f, 0.8f, 1f); // Light blue
                statusText = "Waiting for connections";
                statusIcon = "○ ";
            }
        }
        else
        {
            statusColor = Color.red;
            statusText = "Stopped";
            statusIcon = "● ";
        }

        // Draw status indicator box
        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = statusColor * 0.3f;

        GUILayout.BeginHorizontal("box");

        // Status icon with color
        var originalContentColor = GUI.contentColor;
        GUI.contentColor = statusColor;
        GUILayout.Label(statusIcon, EditorStyles.boldLabel, GUILayout.Width(20));
        GUI.contentColor = originalContentColor;

        // Status text
        GUILayout.Label($"Status: {statusText}", EditorStyles.boldLabel);

        // Additional info
        if (isServerRunning)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Port: {serverPort} | Tools: {mcpTools.Count}", EditorStyles.miniLabel);
        }

        GUILayout.EndHorizontal();
        GUI.backgroundColor = originalColor;
    }

    #endregion

    #region Message Processing

    /// <summary>
    /// Process WebSocket message and return response
    /// </summary>
    public static async Task<string> ProcessWebSocketMessage(string messageJson)
    {
        try
        {
            if (Instance == null)
            {
                Debug.LogError("Unity MCP Bridge instance not found!");
                return CreateErrorResponse("Unity MCP Bridge not initialized", "bridge_error");
            }

            return await Instance.ProcessMCPMessageInternal(messageJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket message processing error: {e.Message}");
            return CreateErrorResponse($"Internal server error: {e.Message}", "internal_error");
        }
    }

    private async Task<string> ProcessMCPMessageInternal(string messageJson)
    {
        try
        {
            AddLog($"Received message: {messageJson}");
            var message = ParseMCPMessage(messageJson);
            AddLog($"Processing method: {message.Method}");

            JObject result = null;

            // Try to handle with registered MCP tools first
            if (mcpTools.ContainsKey(message.Method))
            {
                var tool = mcpTools[message.Method];
                AddLog($"Using tool: {tool.Name}");

                if (tool.IsAsync)
                {
                    var tcs = new TaskCompletionSource<JObject>();
                    tool.ExecuteAsync(message.Params, tcs);
                    result = await tcs.Task;
                }
                else
                {
                    result = tool.Execute(message.Params);
                }
            }
            else
            {
                // Handle built-in methods
                switch (message.Method)
                {
                    case "test":
                    case "ping":
                        result = new JObject
                        {
                            ["success"] = true,
                            ["message"] = "MCP Bridge is working!",
                            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            ["tools_count"] = mcpTools.Count
                        };
                        break;
                    default:
                        result = new JObject
                        {
                            ["success"] = false,
                            ["error"] = new JObject
                            {
                                ["type"] = "unknown_method",
                                ["message"] = $"Unknown method: {message.Method}"
                            }
                        };
                        break;
                }
            }

            var response = new JObject
            {
                ["id"] = message.Id,
                ["result"] = result
            };

            var responseJson = response.ToString(Newtonsoft.Json.Formatting.None);
            AddLog($"Sending response: {responseJson}");
            return responseJson;
        }
        catch (Exception e)
        {
            AddLog($"Error processing message: {e.Message}");
            return CreateErrorResponse($"Processing error: {e.Message}", "processing_error");
        }
    }

    private MCPMessage ParseMCPMessage(string json)
    {
        try
        {
            var jsonObj = JObject.Parse(json);
            return new MCPMessage
            {
                Id = jsonObj["id"]?.ToString(),
                Method = jsonObj["method"]?.ToString(),
                Params = jsonObj["params"] as JObject ?? new JObject()
            };
        }
        catch (Exception e)
        {
            AddLog($"JSON Parse Error: {e.Message}");
            return new MCPMessage { Method = "unknown", Id = "error" };
        }
    }

    private static string CreateErrorResponse(string message, string errorType)
    {
        var errorResponse = new JObject
        {
            ["id"] = "error",
            ["error"] = new JObject
            {
                ["type"] = errorType,
                ["message"] = message
            }
        };
        return errorResponse.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// Update connection status from WebSocket events
    /// </summary>
    public void UpdateConnectionStatus(int clientCount, double heartbeatTime)
    {
        connectedClients = clientCount;
        if (heartbeatTime > 0)
        {
            lastHeartbeat = heartbeatTime;
        }
        // Schedule Repaint on main thread to avoid threading issues
        EditorApplication.delayCall += () => {
            if (this != null) Repaint();
        };
    }

    #endregion

    #region Auto-Start Support Methods

    /// <summary>
    /// Start server if not already running (called by auto-starter)
    /// </summary>
    public void StartServerIfNotRunning()
    {
        if (!isServerRunning)
        {
            StartServer();
        }
    }

    /// <summary>
    /// Stop server if running (called by auto-starter)
    /// </summary>
    public void StopServerIfRunning()
    {
        if (isServerRunning)
        {
            StopServer();
        }
    }

    #endregion

    #region Auto-Start Support (Legacy)

    /// <summary>
    /// Initialize for auto-start mode (without GUI window)
    /// </summary>
    public void InitializeForAutoStart()
    {
        if (instance != null) return; // Already initialized

        Debug.Log("UnityMCPBridge: InitializeForAutoStart called");
        instance = this;
        isAutoStartMode = true;

        // Initialize MCP tools
        try
        {
            InitializeTools();
            AddLog($"Auto-start: MCP tools initialized: {mcpTools.Count} tools registered");

            // Auto-start the server
            StartServer();
            AddLog("Auto-start: WebSocket server started automatically");
        }
        catch (Exception e)
        {
            AddLog($"Auto-start failed: {e.Message}");
            Debug.LogError($"MCP auto-start error: {e}");
        }
    }


    #endregion
}

#region Data Structures

[System.Serializable]
public class MCPMessage
{
    public string Id;
    public string Method;
    public JObject Params;
}

#endregion

#region WebSocket Behavior

/// <summary>
/// WebSocket behavior for handling MCP messages
/// </summary>
public class McpWebSocketBehavior : WebSocketBehavior
{
    private static int connectedClientCount = 0;

    protected override void OnOpen()
    {
        Debug.Log($"WebSocket client connected: {ID}");
        connectedClientCount++;

        // Update bridge UI on main thread
        EditorApplication.delayCall += () => {
            if (UnityMCPBridge.Instance != null)
            {
                UnityMCPBridge.Instance.UpdateConnectionStatus(connectedClientCount, EditorApplication.timeSinceStartup);
            }
        };
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log($"WebSocket client disconnected: {ID}");
        connectedClientCount = Math.Max(0, connectedClientCount - 1);

        // Update bridge UI on main thread
        EditorApplication.delayCall += () => {
            if (UnityMCPBridge.Instance != null)
            {
                UnityMCPBridge.Instance.UpdateConnectionStatus(connectedClientCount, 0);
            }
        };
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"MCP WebSocket received message: {e.Data}");

        try
        {
            // Update activity time
            EditorApplication.delayCall += () => {
                if (UnityMCPBridge.Instance != null)
                {
                    UnityMCPBridge.Instance.UpdateConnectionStatus(connectedClientCount, EditorApplication.timeSinceStartup);
                }
            };

            // Process message
            var response = await UnityMCPBridge.ProcessWebSocketMessage(e.Data);
            Send(response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing WebSocket message: {ex.Message}");
            var errorResponse = new JObject
            {
                ["error"] = new JObject
                {
                    ["type"] = "processing_error",
                    ["message"] = ex.Message
                }
            };
            Send(errorResponse.ToString());
        }
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error: {e.Message}");
    }
}

#endregion