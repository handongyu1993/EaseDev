using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UnityMCP.Editor
{
    public class SimpleUnityMCP : EditorWindow
    {
        private static SimpleUnityMCP instance;
        private TcpListener tcpListener;
        private Thread tcpListenerThread;
        private bool isServerRunning = false;
        private int serverPort = 8765;
        private List<string> logs = new List<string>();
        private UnityEngine.Vector2 scrollPosition;

        [MenuItem("Tools/Unity MCP/Simple Bridge")]
        public static void ShowWindow()
        {
            instance = GetWindow<SimpleUnityMCP>("Simple Unity MCP");
            instance.Show();
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            StopServer();
        }

        private void OnGUI()
        {
            GUILayout.Label("Simple Unity MCP Bridge", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            GUILayout.Label($"Server Status: {(isServerRunning ? "Running" : "Stopped")}");

            serverPort = EditorGUILayout.IntField("Port:", serverPort);

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

            EditorGUILayout.Space();
            GUILayout.Label("Logs:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var log in logs)
            {
                EditorGUILayout.SelectableLabel(log, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Connect to: http://localhost:{serverPort}", MessageType.Info);
        }

        private void StartServer()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, serverPort);
                tcpListenerThread = new Thread(new ThreadStart(ListenForClients));
                tcpListenerThread.IsBackground = true;
                tcpListenerThread.Start();

                isServerRunning = true;
                AddLog($"Unity MCP Server started on port {serverPort}");
            }
            catch (System.Exception e)
            {
                AddLog($"Failed to start server: {e.Message}");
            }
        }

        private void StopServer()
        {
            try
            {
                isServerRunning = false;
                tcpListener?.Stop();
                tcpListenerThread?.Abort();
                AddLog("Unity MCP Server stopped");
            }
            catch (System.Exception e)
            {
                AddLog($"Error stopping server: {e.Message}");
            }
        }

        private void ListenForClients()
        {
            tcpListener.Start();

            while (isServerRunning)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);
                }
                catch
                {
                    break;
                }
            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                string jsonMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
                AddLog($"Received: {jsonMessage}");

                // Process the message and send response
                string response = ProcessMCPCommand(jsonMessage);
                byte[] data = Encoding.UTF8.GetBytes(response);
                clientStream.Write(data, 0, data.Length);
            }

            tcpClient.Close();
        }

        private string ProcessMCPCommand(string jsonMessage)
        {
            try
            {
                // Simple JSON parsing without external dependencies
                if (jsonMessage.Contains("unity.create_scene"))
                {
                    return ExecuteOnMainThreadSync(() => CreateScene());
                }
                else if (jsonMessage.Contains("unity.create_gameobject"))
                {
                    return ExecuteOnMainThreadSync(() => CreateGameObject());
                }
                else if (jsonMessage.Contains("unity.create_ui_canvas"))
                {
                    return ExecuteOnMainThreadSync(() => CreateUICanvas());
                }
                else if (jsonMessage.Contains("unity.get_scene_info"))
                {
                    return ExecuteOnMainThreadSync(() => GetSceneInfo());
                }
                else
                {
                    return "{\"success\": false, \"error\": \"Unknown command\"}";
                }
            }
            catch (System.Exception e)
            {
                return $"{{\"success\": false, \"error\": \"{e.Message}\"}}";
            }
        }

        private string ExecuteOnMainThreadSync(System.Func<string> action)
        {
            string result = null;
            bool completed = false;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    result = action();
                }
                catch (System.Exception e)
                {
                    result = $"{{\"success\": false, \"error\": \"{e.Message}\"}}";
                }
                finally
                {
                    completed = true;
                }
            };

            // Wait for completion
            while (!completed)
            {
                Thread.Sleep(10);
            }

            return result;
        }

        #region Unity Operations

        private string CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            return $"{{\"success\": true, \"sceneName\": \"{scene.name}\", \"scenePath\": \"{scene.path}\"}}";
        }

        private string CreateGameObject()
        {
            var go = new GameObject("NewGameObject");
            return $"{{\"success\": true, \"objectName\": \"{go.name}\", \"instanceId\": {go.GetInstanceID()}}}";
        }

        private string CreateUICanvas()
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

            return $"{{\"success\": true, \"canvasName\": \"{canvasGO.name}\", \"instanceId\": {canvasGO.GetInstanceID()}}}";
        }

        private string GetSceneInfo()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var gameObjects = scene.GetRootGameObjects();

            var objectNames = new List<string>();
            foreach (var go in gameObjects)
            {
                objectNames.Add($"\\\"{go.name}\\\"");
            }

            string objectList = string.Join(",", objectNames.ToArray());

            return $"{{\"success\": true, \"sceneName\": \"{scene.name}\", \"scenePath\": \"{scene.path}\", \"gameObjects\": [{objectList}]}}";
        }

        #endregion

        private void AddLog(string message)
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            logs.Add($"[{timestamp}] {message}");

            if (logs.Count > 100)
            {
                logs.RemoveAt(0);
            }

            Repaint();
        }
    }
}