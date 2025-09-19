using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityMCP.Editor
{
    public class UnityMCPBridge : EditorWindow
    {
        private static UnityMCPBridge instance;
        private WebSocketServer webSocketServer;
        private bool isServerRunning = false;
        private int serverPort = 8765;
        private List<string> logs = new List<string>();
        private Vector2 scrollPosition;

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
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopServer();
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity MCP Bridge", EditorStyles.boldLabel);

            // Server Status
            EditorGUILayout.Space();
            GUILayout.Label($"Server Status: {(isServerRunning ? "Running" : "Stopped")}",
                isServerRunning ? EditorStyles.boldLabel : EditorStyles.label);

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
                webSocketServer = new WebSocketServer(serverPort);
                await webSocketServer.StartAsync();
                isServerRunning = true;
                AddLog($"Unity MCP Bridge started on port {serverPort}");

                // Start message processing
                _ = Task.Run(ProcessMessages);
            }
            catch (Exception e)
            {
                AddLog($"Failed to start server: {e.Message}");
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

        private async Task ProcessMessages()
        {
            while (isServerRunning)
            {
                try
                {
                    var message = await webSocketServer.ReceiveMessageAsync();
                    if (!string.IsNullOrEmpty(message))
                    {
                        await ProcessMCPMessage(message);
                    }
                }
                catch (Exception e)
                {
                    AddLog($"Message processing error: {e.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        private async Task ProcessMCPMessage(string messageJson)
        {
            try
            {
                var message = ParseMCPMessage(messageJson);
                AddLog($"Received: {message.Method}");

                object result = null;
                switch (message.Method)
                {
                    case "unity.create_scene":
                        result = await CreateScene(message);
                        break;
                    case "unity.create_gameobject":
                        result = await CreateGameObject(message);
                        break;
                    case "unity.create_ui_canvas":
                        result = await CreateUICanvas(message);
                        break;
                    case "unity.get_scene_info":
                        result = await GetSceneInfo();
                        break;
                    case "unity.get_console_logs":
                        result = await GetConsoleLogs();
                        break;
                    case "unity.execute_menu_item":
                        result = await ExecuteMenuItem(message);
                        break;
                    case "unity.select_gameobject":
                        result = await SelectGameObject(message);
                        break;
                    default:
                        result = new { error = $"Unknown method: {message.Method}" };
                        break;
                }

                // Send response
                var response = new MCPResponse
                {
                    Id = message.Id,
                    Result = result
                };

                await webSocketServer.SendMessageAsync(SerializeMCPResponse(response));
            }
            catch (Exception e)
            {
                AddLog($"Error processing message: {e.Message}");
            }
        }

        #region Unity Operations

        private async Task<object> CreateScene(MCPMessage message)
        {
            return await ExecuteOnMainThread(() =>
            {
                CreateSceneParams parameters = null;
                if (!string.IsNullOrEmpty(message.Params))
                {
                    parameters = JsonUtility.FromJson<CreateSceneParams>(message.Params);
                }

                string sceneName = parameters?.sceneName ?? "NewScene";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                return new {
                    success = true,
                    sceneName = scene.name,
                    scenePath = scene.path,
                    isLoaded = scene.isLoaded
                };
            });
        }

        private async Task<object> CreateGameObject(dynamic parameters)
        {
            return await ExecuteOnMainThread(() =>
            {
                string objectName = parameters?.name ?? "GameObject";
                var go = new GameObject(objectName);

                if (parameters?.parent != null)
                {
                    var parent = GameObject.Find((string)parameters.parent);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
                    }
                }

                return new {
                    success = true,
                    objectName = go.name,
                    instanceId = go.GetInstanceID()
                };
            });
        }

        private async Task<object> CreateUICanvas(dynamic parameters)
        {
            return await ExecuteOnMainThread(() =>
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
            return await ExecuteOnMainThread(() =>
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

        private async Task<object> GetConsoleLogs()
        {
            // This would need to be implemented with LogEntries API
            return await Task.FromResult(new {
                logs = new List<string>(logs)
            });
        }

        private async Task<object> ExecuteMenuItem(dynamic parameters)
        {
            return await ExecuteOnMainThread(() =>
            {
                string menuPath = parameters?.menuPath;
                if (string.IsNullOrEmpty(menuPath))
                {
                    return new { success = false, error = "Menu path is required" };
                }

                EditorApplication.ExecuteMenuItem(menuPath);
                return new { success = true, menuPath = menuPath };
            });
        }

        private async Task<object> SelectGameObject(dynamic parameters)
        {
            return await ExecuteOnMainThread(() =>
            {
                string objectName = parameters?.name;
                if (string.IsNullOrEmpty(objectName))
                {
                    return new { success = false, error = "Object name is required" };
                }

                var go = GameObject.Find(objectName);
                if (go != null)
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                    return new { success = true, objectName = go.name };
                }

                return new { success = false, error = "GameObject not found" };
            });
        }

        #endregion

        #region JSON Utilities

        private MCPMessage ParseMCPMessage(string json)
        {
            try
            {
                return JsonUtility.FromJson<MCPMessage>(json);
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
                return JsonUtility.ToJson(response);
            }
            catch (Exception e)
            {
                AddLog($"JSON Serialize Error: {e.Message}");
                return "{\"error\": \"Serialization failed\"}";
            }
        }

        #endregion

        #region Utility Methods

        private async Task<T> ExecuteOnMainThread<T>(Func<T> action)
        {
            var tcs = new TaskCompletionSource<T>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var result = action();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            };

            return await tcs.Task;
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

            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            AddLog($"Play mode changed: {state}");
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class MCPMessage
    {
        public string jsonrpc = "2.0";
        public string id;
        public string method;
        public string @params; // JSON string of parameters

        // Properties for easier access
        public string Id => id;
        public string Method => method;
        public string Params => @params;
    }

    [System.Serializable]
    public class MCPResponse
    {
        public string jsonrpc = "2.0";
        public string id;
        public string result; // JSON string of result
        public string error;   // JSON string of error

        public string Id
        {
            get => id;
            set => id = value;
        }

        public object Result
        {
            get => result;
            set => result = JsonUtility.ToJson(value);
        }

        public object Error
        {
            get => error;
            set => error = value?.ToString();
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

    #endregion
}