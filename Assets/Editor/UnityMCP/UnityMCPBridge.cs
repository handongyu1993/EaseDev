using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityMCP.Editor.Services;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor
{
    public class UnityMCPBridge : EditorWindow
    {
        private static UnityMCPBridge instance;
        private WebSocketSharpServer webSocketServer;
        private bool isServerRunning = false;
        private int serverPort = 8766;
        private List<string> logs = new List<string>();
        private Vector2 scrollPosition;
        private ConsoleLogsService consoleLogsService;
        private int connectedClients = 0;
        private double lastHeartbeat = 0;

        public static UnityMCPBridge Instance => instance ?? FindObjectOfType<UnityMCPBridge>();

        [MenuItem("Tools/Unity MCP/Bridge Window")]
        public static void ShowWindow()
        {
            instance = GetWindow<UnityMCPBridge>("Unity MCP Bridge");
            instance.Show();
        }

        private void OnEnable()
        {
            instance = this;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // 确保Unity在后台运行时仍能处理请求
            EditorApplication.wantsToQuit += OnWantsToQuit;
            Application.runInBackground = true;

            // 添加持续的更新回调确保后台通信
            EditorApplication.update += BackgroundUpdate;

            // Initialize console logs service - force initialization
            consoleLogsService = ConsoleLogsService.Instance;
            Debug.Log("[UnityMCPBridge] ConsoleLogsService initialized");

            // Test that the service is working
            EditorApplication.delayCall += () => {
                Debug.Log("[UnityMCPBridge] Testing console logs service initialization");
                Debug.Log($"[UnityMCPBridge] Service log count: {consoleLogsService.GetLogCount()}");
            };
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.wantsToQuit -= OnWantsToQuit;
            EditorApplication.update -= BackgroundUpdate;
            StopServer();
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity MCP Bridge", EditorStyles.boldLabel);

            // Connection Status Indicator with colors
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

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var log in logs)
            {
                EditorGUILayout.SelectableLabel(log, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUILayout.EndScrollView();

            // Connection Info
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Connect to: ws://localhost:{serverPort}", MessageType.Info);
        }

        private async void StartServer()
        {
            try
            {
                webSocketServer = new WebSocketSharpServer(serverPort);
                await webSocketServer.StartAsync();
                isServerRunning = webSocketServer.IsRunning;
                AddLog($"Unity MCP Bridge started on port {serverPort}");
                Debug.Log($"Unity MCP Bridge started on ws://localhost:{serverPort}");
            }
            catch (Exception e)
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
                webSocketServer?.Stop();
                isServerRunning = false;
                AddLog("Unity MCP Bridge stopped");
            }
            catch (Exception e)
            {
                AddLog($"Error stopping server: {e.Message}");
            }
        }


        #region Unity Operations

        private async Task<object> CreateScene(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    // Extract parameters from professional MCP JObject format
                    string sceneName = message.Params["sceneName"]?.ToString() ?? "NewScene";
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                    var data = new JObject
                    {
                        ["sceneName"] = scene.name,
                        ["scenePath"] = scene.path,
                        ["isLoaded"] = scene.isLoaded
                    };

                    return Response.Success($"Scene '{scene.name}' created successfully", data);
                }
                catch (Exception e)
                {
                    return Response.Error($"Failed to create scene: {e.Message}");
                }
            });
        }

        private async Task<object> CreateGameObject(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    // Extract parameters from professional MCP JObject format
                    string objectName = message.Params["name"]?.ToString() ?? "GameObject";
                    string parentName = message.Params["parent"]?.ToString();

                    var go = new GameObject(objectName);

                    if (!string.IsNullOrEmpty(parentName))
                    {
                        var parent = GameObject.Find(parentName);
                        if (parent != null)
                        {
                            go.transform.SetParent(parent.transform);
                        }
                    }

                    var data = new JObject
                    {
                        ["objectName"] = go.name,
                        ["instanceId"] = go.GetInstanceID()
                    };

                    return Response.Success($"GameObject '{go.name}' created successfully", data);
                }
                catch (Exception e)
                {
                    return Response.Error($"Failed to create GameObject: {e.Message}");
                }
            });
        }

        private async Task<object> CreateUICanvas(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                var canvasGO = new GameObject("Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                // Create EventSystem if it doesn't exist
                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var eventSystemGO = new GameObject("EventSystem");
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                return new {
                    success = true,
                    canvasName = canvasGO.name,
                    instanceId = canvasGO.GetInstanceID()
                };
            });
        }

        private async Task<object> GetSceneInfo()
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                var gameObjects = new List<object>();

                foreach (var rootGO in scene.GetRootGameObjects())
                {
                    gameObjects.Add(new {
                        name = rootGO.name,
                        instanceId = rootGO.GetInstanceID(),
                        activeInHierarchy = rootGO.activeInHierarchy,
                        tag = rootGO.tag,
                        layer = rootGO.layer
                    });
                }

                return new {
                    sceneName = scene.name,
                    scenePath = scene.path,
                    isLoaded = scene.isLoaded,
                    isDirty = scene.isDirty,
                    gameObjects = gameObjects
                };
            });
        }

        private async Task<object> GetConsoleLogs(MCPMessage message)
        {
            try
            {
                AddLog("Getting console logs...");

                // Extract parameters from professional MCP format
                var logType = message.Params["logType"]?.ToString() ?? "";
                var offset = message.Params["offset"]?.ToObject<int>() ?? 0;
                var limit = message.Params["limit"]?.ToObject<int>() ?? 50;
                var includeStackTrace = message.Params["includeStackTrace"]?.ToObject<bool>() ?? true;

                AddLog($"Parameters: logType={logType}, offset={offset}, limit={limit}, includeStackTrace={includeStackTrace}");

                // Use the professional MCP console logs service (thread-safe)
                var consoleLogsResult = consoleLogsService.GetLogsAsJson(logType, offset, limit, includeStackTrace);

                // Create professional MCP response with success field
                var result = new JObject
                {
                    ["success"] = true,
                    ["data"] = consoleLogsResult,
                    ["bridgeLogs"] = JArray.FromObject(logs.ToArray()), // Thread-safe copy
                    ["message"] = $"Retrieved console logs successfully (logType: {logType}, offset: {offset}, limit: {limit})"
                };

                AddLog($"Professional MCP response created successfully");
                return await Task.FromResult(result);
            }
            catch (Exception e)
            {
                AddLog($"Error getting console logs: {e.Message}");
                AddLog($"Error stack trace: {e.StackTrace}");

                // Professional MCP error response
                var errorResult = new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to get console logs: {e.Message}",
                    ["bridgeLogs"] = JArray.FromObject(logs.ToArray()) // Thread-safe copy
                };

                return await Task.FromResult(errorResult);
            }
        }

        private async Task<object> ClearConsole()
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    // Clear Unity's console via reflection
                    var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                    if (logEntriesType != null)
                    {
                        var clearMethod = logEntriesType.GetMethod("Clear");
                        if (clearMethod != null)
                        {
                            clearMethod.Invoke(null, null);
                        }
                    }

                    // Also clear our service's logs
                    consoleLogsService.ClearLogs();

                    AddLog("Unity Console and service logs cleared");
                    return Response.Success("Console cleared successfully");
                }
                catch (Exception e)
                {
                    AddLog($"Error clearing console: {e.Message}");
                    return Response.Error($"Failed to clear console: {e.Message}");
                }
            });
        }

        private async Task<object> ExecuteMenuItem(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                // Extract parameters from professional MCP JObject format
                string menuPath = message.Params["menuPath"]?.ToString();
                if (string.IsNullOrEmpty(menuPath))
                {
                    return Response.Error("Menu path is required", "validation_error");
                }

                EditorApplication.ExecuteMenuItem(menuPath);
                return Response.Success($"Menu item '{menuPath}' executed successfully", new { menuPath = menuPath });
            });
        }

        private async Task<object> SelectGameObject(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                // Extract parameters from professional MCP JObject format
                string objectName = message.Params["name"]?.ToString();
                if (string.IsNullOrEmpty(objectName))
                {
                    return Response.Error("Object name is required", "validation_error");
                }

                var go = GameObject.Find(objectName);
                if (go != null)
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                    return Response.Success($"GameObject '{go.name}' selected successfully", new { objectName = go.name });
                }

                return Response.Error("GameObject not found", "not_found");
            });
        }

        #endregion

        #region New MCP Methods (GameLovers Compatible)

        private async Task<object> GetAssets(MCPMessage message)
        {
            try
            {
                AddLog("Getting project assets...");

                // Extract parameters
                var filter = message.Params["filter"]?.ToString() ?? "";
                var assetType = message.Params["assetType"]?.ToString() ?? "";
                var searchInFolders = message.Params["searchInFolders"]?.ToString() ?? "Assets";
                var limit = message.Params["limit"]?.ToObject<int>() ?? 100;

                var assets = new List<object>();

                // Use EditorApplication.delayCall to ensure main thread execution
                var tcs = new TaskCompletionSource<object>();

                EditorApplication.delayCall += () => {
                    try
                    {
                        string[] searchPaths = searchInFolders.Split(',');

                        // Find assets based on filter
                        string[] guids;
                        if (!string.IsNullOrEmpty(assetType))
                        {
                            guids = AssetDatabase.FindAssets($"t:{assetType} {filter}", searchPaths);
                        }
                        else
                        {
                            guids = AssetDatabase.FindAssets(filter, searchPaths);
                        }

                        var limitedGuids = guids.Take(limit);

                        foreach (var guid in limitedGuids)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            var asset = AssetDatabase.LoadMainAssetAtPath(path);

                            if (asset != null)
                            {
                                long fileSize = 0;
                                string lastModified = "";
                                try
                                {
                                    if (System.IO.File.Exists(path))
                                    {
                                        var fileInfo = new System.IO.FileInfo(path);
                                        fileSize = fileInfo.Length;
                                        lastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                }
                                catch
                                {
                                    // Skip file info if not accessible
                                }

                                assets.Add(new
                                {
                                    guid = guid,
                                    path = path,
                                    name = asset.name,
                                    type = asset.GetType().Name,
                                    extension = System.IO.Path.GetExtension(path),
                                    size = fileSize,
                                    lastModified = lastModified
                                });
                            }
                        }

                        AddLog($"Retrieved {assets.Count} assets");
                        var result = Response.Success($"Retrieved {assets.Count} assets", new {
                            assets = assets,
                            totalFound = guids.Length,
                            limitApplied = limit,
                            filter = filter,
                            assetType = assetType
                        });
                        tcs.SetResult(result);
                    }
                    catch (Exception e)
                    {
                        AddLog($"Error getting assets: {e.Message}");
                        tcs.SetResult(Response.Error($"Failed to get assets: {e.Message}"));
                    }
                };

                return await tcs.Task;
            }
            catch (Exception e)
            {
                AddLog($"Error getting assets: {e.Message}");
                return Response.Error($"Failed to get assets: {e.Message}");
            }
        }

        private async Task<object> GetPackages(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    AddLog("Getting Unity packages...");

                    var packages = new List<object>();
                    var manifestPath = "Packages/manifest.json";

                    if (System.IO.File.Exists(manifestPath))
                    {
                        var manifestContent = System.IO.File.ReadAllText(manifestPath);
                        var manifest = JObject.Parse(manifestContent);
                        var dependencies = manifest["dependencies"] as JObject;

                        if (dependencies != null)
                        {
                            foreach (var package in dependencies)
                            {
                                packages.Add(new
                                {
                                    name = package.Key,
                                    version = package.Value.ToString(),
                                    isBuiltIn = package.Key.StartsWith("com.unity.modules."),
                                    source = package.Value.ToString().StartsWith("file:") ? "local" :
                                           package.Value.ToString().StartsWith("git") ? "git" : "registry"
                                });
                            }
                        }
                    }

                    AddLog($"Retrieved {packages.Count} packages");
                    return Response.Success($"Retrieved {packages.Count} packages", new { packages = packages });
                }
                catch (Exception e)
                {
                    AddLog($"Error getting packages: {e.Message}");
                    return Response.Error($"Failed to get packages: {e.Message}");
                }
            });
        }

        private async Task<object> SendConsoleLog(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    var logMessage = message.Params["message"]?.ToString();
                    var logType = message.Params["logType"]?.ToString() ?? "Log";

                    if (string.IsNullOrEmpty(logMessage))
                    {
                        return Response.Error("Message parameter is required", "validation_error");
                    }

                    // Send log to Unity console
                    switch (logType.ToLower())
                    {
                        case "error":
                            Debug.LogError($"[MCP] {logMessage}");
                            break;
                        case "warning":
                            Debug.LogWarning($"[MCP] {logMessage}");
                            break;
                        default:
                            Debug.Log($"[MCP] {logMessage}");
                            break;
                    }

                    AddLog($"Sent {logType} to console: {logMessage}");
                    return Response.Success("Log sent to Unity console successfully", new {
                        message = logMessage,
                        logType = logType
                    });
                }
                catch (Exception e)
                {
                    AddLog($"Error sending console log: {e.Message}");
                    return Response.Error($"Failed to send console log: {e.Message}");
                }
            });
        }

        private async Task<object> UpdateGameObject(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    var objectName = message.Params["objectName"]?.ToString();
                    var instanceId = message.Params["instanceId"]?.ToObject<int?>();

                    GameObject targetObject = null;

                    // Find the GameObject
                    if (instanceId.HasValue)
                    {
                        targetObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    }
                    else if (!string.IsNullOrEmpty(objectName))
                    {
                        targetObject = GameObject.Find(objectName);
                    }

                    if (targetObject == null)
                    {
                        return Response.Error("GameObject not found", "not_found");
                    }

                    // Update properties
                    var newName = message.Params["newName"]?.ToString();
                    var position = message.Params["position"];
                    var rotation = message.Params["rotation"];
                    var scale = message.Params["scale"];
                    var active = message.Params["active"]?.ToObject<bool?>();

                    if (!string.IsNullOrEmpty(newName))
                    {
                        targetObject.name = newName;
                    }

                    if (position != null && position.Type != JTokenType.Null)
                    {
                        var pos = position.ToObject<Vector3>();
                        targetObject.transform.position = pos;
                    }

                    if (rotation != null && rotation.Type != JTokenType.Null)
                    {
                        var rot = rotation.ToObject<Vector3>();
                        targetObject.transform.eulerAngles = rot;
                    }

                    if (scale != null && scale.Type != JTokenType.Null)
                    {
                        var scl = scale.ToObject<Vector3>();
                        targetObject.transform.localScale = scl;
                    }

                    if (active.HasValue)
                    {
                        targetObject.SetActive(active.Value);
                    }

                    EditorUtility.SetDirty(targetObject);
                    AddLog($"Updated GameObject: {targetObject.name}");

                    return Response.Success($"GameObject '{targetObject.name}' updated successfully", new {
                        objectName = targetObject.name,
                        instanceId = targetObject.GetInstanceID(),
                        position = targetObject.transform.position,
                        rotation = targetObject.transform.eulerAngles,
                        scale = targetObject.transform.localScale,
                        active = targetObject.activeInHierarchy
                    });
                }
                catch (Exception e)
                {
                    AddLog($"Error updating GameObject: {e.Message}");
                    return Response.Error($"Failed to update GameObject: {e.Message}");
                }
            });
        }

        private async Task<object> GetGameObject(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    var objectName = message.Params["objectName"]?.ToString();
                    var instanceId = message.Params["instanceId"]?.ToObject<int?>();

                    GameObject targetObject = null;

                    if (instanceId.HasValue)
                    {
                        targetObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    }
                    else if (!string.IsNullOrEmpty(objectName))
                    {
                        targetObject = GameObject.Find(objectName);
                    }

                    if (targetObject == null)
                    {
                        return Response.Error("GameObject not found", "not_found");
                    }

                    // Get components
                    var components = new List<object>();
                    foreach (var component in targetObject.GetComponents<Component>())
                    {
                        if (component != null)
                        {
                            components.Add(new
                            {
                                type = component.GetType().Name,
                                enabled = (component as Behaviour)?.enabled ?? true
                            });
                        }
                    }

                    // Get children
                    var children = new List<object>();
                    for (int i = 0; i < targetObject.transform.childCount; i++)
                    {
                        var child = targetObject.transform.GetChild(i);
                        children.Add(new
                        {
                            name = child.name,
                            instanceId = child.gameObject.GetInstanceID(),
                            active = child.gameObject.activeInHierarchy
                        });
                    }

                    var result = new
                    {
                        name = targetObject.name,
                        instanceId = targetObject.GetInstanceID(),
                        active = targetObject.activeInHierarchy,
                        position = targetObject.transform.position,
                        rotation = targetObject.transform.eulerAngles,
                        localScale = targetObject.transform.localScale,
                        tag = targetObject.tag,
                        layer = targetObject.layer,
                        components = components,
                        children = children,
                        parent = targetObject.transform.parent?.name,
                        scene = targetObject.scene.name
                    };

                    AddLog($"Retrieved GameObject info: {targetObject.name}");
                    return Response.Success($"GameObject '{targetObject.name}' info retrieved successfully", result);
                }
                catch (Exception e)
                {
                    AddLog($"Error getting GameObject: {e.Message}");
                    return Response.Error($"Failed to get GameObject: {e.Message}");
                }
            });
        }

        private async Task<object> CreatePrefab(MCPMessage message)
        {
            return await ExecuteOnMainThread<object>(() =>
            {
                try
                {
                    var objectName = message.Params["objectName"]?.ToString();
                    var prefabPath = message.Params["prefabPath"]?.ToString();
                    var instanceId = message.Params["instanceId"]?.ToObject<int?>();

                    if (string.IsNullOrEmpty(prefabPath))
                    {
                        return Response.Error("prefabPath parameter is required", "validation_error");
                    }

                    GameObject targetObject = null;

                    if (instanceId.HasValue)
                    {
                        targetObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    }
                    else if (!string.IsNullOrEmpty(objectName))
                    {
                        targetObject = GameObject.Find(objectName);
                    }

                    if (targetObject == null)
                    {
                        return Response.Error("GameObject not found", "not_found");
                    }

                    // Ensure the directory exists
                    var directory = System.IO.Path.GetDirectoryName(prefabPath);
                    if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                    {
                        var folders = directory.Split('/');
                        var currentPath = "";
                        foreach (var folder in folders)
                        {
                            var newPath = string.IsNullOrEmpty(currentPath) ? folder : $"{currentPath}/{folder}";
                            if (!AssetDatabase.IsValidFolder(newPath))
                            {
                                var parentPath = string.IsNullOrEmpty(currentPath) ? "" : currentPath;
                                AssetDatabase.CreateFolder(parentPath, folder);
                            }
                            currentPath = newPath;
                        }
                    }

                    // Create the prefab
                    var prefab = PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
                    AssetDatabase.Refresh();

                    AddLog($"Created prefab: {prefabPath}");
                    return Response.Success($"Prefab created successfully at {prefabPath}", new {
                        prefabPath = prefabPath,
                        sourceObject = targetObject.name,
                        prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath)
                    });
                }
                catch (Exception e)
                {
                    AddLog($"Error creating prefab: {e.Message}");
                    return Response.Error($"Failed to create prefab: {e.Message}");
                }
            });
        }

        #endregion

        #region JSON Utilities

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

        private string SerializeMCPResponse(MCPResponse response)
        {
            try
            {
                // Professional MCP format - no jsonrpc field
                var responseObj = new JObject
                {
                    ["id"] = response.Id
                };

                if (response.Error != null)
                {
                    responseObj["error"] = response.Error;
                }
                else if (response.Result != null)
                {
                    responseObj["result"] = response.Result;
                }

                return responseObj.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception e)
            {
                AddLog($"JSON Serialize Error: {e.Message}");
                var errorResponse = new JObject
                {
                    ["id"] = response.Id,
                    ["error"] = new JObject
                    {
                        ["type"] = "serialization_error",
                        ["message"] = $"Serialization failed: {e.Message}"
                    }
                };
                return errorResponse.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        #endregion

        #region GUI Methods

        /// <summary>
        /// Draw connection status with visual indicators
        /// </summary>
        private void DrawConnectionStatus()
        {
            // Status colors
            Color statusColor;
            string statusText;
            string statusIcon;

            if (isServerRunning)
            {
                // Check connection activity
                double timeSinceHeartbeat = EditorApplication.timeSinceStartup - lastHeartbeat;
                bool hasRecentActivity = timeSinceHeartbeat < 60; // Consider active if heartbeat within 60 seconds

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
                statusText = "Disconnected";
                statusIcon = "● ";
            }

            // Draw status indicator box
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = statusColor * 0.3f; // Lighter background

            GUILayout.BeginHorizontal("box");

            // Status icon with color
            var originalContentColor = GUI.contentColor;
            GUI.contentColor = statusColor;

            GUILayout.Label(statusIcon, EditorStyles.boldLabel, GUILayout.Width(20));

            GUI.contentColor = originalContentColor;

            // Status text
            GUILayout.Label($"Status: {statusText}", EditorStyles.boldLabel);

            // Port info
            if (isServerRunning)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Port: {serverPort}", EditorStyles.miniLabel);
            }

            GUILayout.EndHorizontal();

            GUI.backgroundColor = originalColor;

            // Additional connection info
            if (isServerRunning)
            {
                EditorGUILayout.HelpBox($"WebSocket server running on ws://localhost:{serverPort}\n" +
                    "External tools can connect to this endpoint.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Server is stopped. Click 'Start Server' to enable MCP communication.",
                    MessageType.Warning);
            }
        }

        #endregion

        #region Utility Methods

        private async Task<T> ExecuteOnMainThread<T>(Func<T> action)
        {
            // 直接执行，不等待主线程调度
            return await Task.Run(() => action());
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logs.Add($"[{timestamp}] {message}");

            // Keep only last 100 logs
            if (logs.Count > 100)
            {
                logs.RemoveAt(0);
            }

            // Schedule Repaint on main thread to avoid threading issues
            EditorApplication.delayCall += () => {
                if (this != null) Repaint();
            };
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            AddLog($"Play mode changed: {state}");
        }

        private bool OnWantsToQuit()
        {
            // 允许Unity正常退出，但会自动停止服务器
            return true;
        }

        // 后台更新方法，确保即使Unity失去焦点也能处理消息
        private void BackgroundUpdate()
        {
            if (webSocketServer != null && isServerRunning)
            {
                try
                {
                    webSocketServer.ProcessPendingMessages();
                }
                catch (Exception e)
                {
                    AddLog($"Background processing error: {e.Message}");
                }
            }
        }

        public static async Task<string> ProcessWebSocketMessage(string messageJson)
        {
            try
            {
                Debug.Log($"[ProcessWebSocketMessage] Received: {messageJson}");
                Debug.Log($"[ProcessWebSocketMessage] Instance is null: {instance == null}");
                Debug.Log($"[ProcessWebSocketMessage] Instance property: {Instance == null}");

                if (Instance == null)
                {
                    Debug.LogError("[ProcessWebSocketMessage] Unity MCP Bridge instance not found!");
                    return JsonUtility.ToJson(new { error = "Unity MCP Bridge not initialized" });
                }

                Debug.Log("[ProcessWebSocketMessage] Calling ProcessMCPMessageInternal");
                var result = await Instance.ProcessMCPMessageInternal(messageJson);
                Debug.Log($"[ProcessWebSocketMessage] Result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProcessWebSocketMessage] Exception: {e.Message}");
                return JsonUtility.ToJson(new { error = e.Message });
            }
        }

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

        /// <summary>
        /// Update connection status from WebSocket events
        /// </summary>
        /// <param name="clientCount">Number of connected clients</param>
        /// <param name="heartbeatTime">Time of last heartbeat (0 if disconnecting)</param>
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

        private async Task<string> ProcessMCPMessageInternal(string messageJson)
        {
            try
            {
                AddLog($"Received raw message: {messageJson}");
                var message = ParseMCPMessage(messageJson);
                AddLog($"Processing method: {message.Method} with ID: {message.Id}");

                JObject result = null;
                switch (message.Method)
                {
                    // Professional MCP method names (matching GameLovers MCP Unity)
                    case "get_console_logs":
                        result = await GetConsoleLogs(message) as JObject;
                        break;
                    case "clear_console":
                        result = await ClearConsole() as JObject;
                        break;
                    case "get_assets":
                        result = await GetAssets(message) as JObject;
                        break;
                    case "get_packages":
                        result = await GetPackages(message) as JObject;
                        break;
                    case "send_console_log":
                        result = await SendConsoleLog(message) as JObject;
                        break;

                    // Legacy Unity methods (for backward compatibility)
                    case "unity.create_scene":
                        result = JObject.FromObject(await CreateScene(message));
                        break;
                    case "unity.create_gameobject":
                        result = JObject.FromObject(await CreateGameObject(message));
                        break;
                    case "unity.create_ui_canvas":
                        result = JObject.FromObject(await CreateUICanvas(message));
                        break;
                    case "unity.get_scene_info":
                        result = JObject.FromObject(await GetSceneInfo());
                        break;
                    case "unity.execute_menu_item":
                        result = JObject.FromObject(await ExecuteMenuItem(message));
                        break;
                    case "unity.select_gameobject":
                        result = JObject.FromObject(await SelectGameObject(message));
                        break;
                    case "unity.update_gameobject":
                    case "update_gameobject":
                        result = JObject.FromObject(await UpdateGameObject(message));
                        break;
                    case "unity.get_gameobject":
                    case "get_gameobject":
                        result = JObject.FromObject(await GetGameObject(message));
                        break;
                    case "unity.create_prefab":
                    case "create_prefab":
                        result = JObject.FromObject(await CreatePrefab(message));
                        break;
                    case "unity.test":
                    case "test":
                    case "ping":
                        result = new JObject
                        {
                            ["success"] = true,
                            ["message"] = "MCP Bridge is working!",
                            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        };
                        break;
                    default:
                        // Professional MCP error format
                        var errorResponse = new MCPResponse
                        {
                            Id = message.Id,
                            Error = new JObject
                            {
                                ["type"] = "unknown_method",
                                ["message"] = $"Unknown method: {message.Method}"
                            }
                        };
                        return SerializeMCPResponse(errorResponse);
                }

                var response = new MCPResponse
                {
                    Id = message.Id,
                    Result = result
                };

                var responseJson = SerializeMCPResponse(response);
                AddLog($"Sending response: {responseJson}");
                return responseJson;
            }
            catch (Exception e)
            {
                AddLog($"Error processing message: {e.Message}");
                var errorResponse = new MCPResponse
                {
                    Id = "unknown",
                    Error = new JObject
                    {
                        ["type"] = "internal_error",
                        ["message"] = $"Internal server error: {e.Message}"
                    }
                };
                return SerializeMCPResponse(errorResponse);
            }
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class MCPMessage
    {
        public string id;
        public string method;
        public JObject @params; // JObject parameters (professional MCP format)

        // Properties for easier access
        public string Id
        {
            get => id;
            set => id = value;
        }

        public string Method
        {
            get => method;
            set => method = value;
        }

        public JObject Params
        {
            get => @params;
            set => @params = value;
        }
    }

    [System.Serializable]
    public class MCPResponse
    {
        public string id;
        public JObject result;
        public JObject error;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public JObject Result
        {
            get => result;
            set => result = value;
        }

        public JObject Error
        {
            get => error;
            set => error = value;
        }
    }

    [System.Serializable]
    public class UnityOperationResult
    {
        public bool success;
        public string message;
        public string data; // JSON data
    }

    [System.Serializable]
    public class CreateSceneParams
    {
        public string sceneName;
    }

    [System.Serializable]
    public class CreateGameObjectParams
    {
        public string name;
        public string parent;
    }

    [System.Serializable]
    public class SelectGameObjectParams
    {
        public string name;
    }

    [System.Serializable]
    public class ExecuteMenuParams
    {
        public string menuPath;
    }

    [System.Serializable]
    public class TestResult
    {
        public bool success;
        public string message;
        public string timestamp;
    }

    [System.Serializable]
    public class ErrorResult
    {
        public string error;
    }

    #endregion
}