using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for creating and managing Unity scenes based on GameLovers MCP architecture
    /// </summary>
    public class CreateSceneTool : McpToolBase
    {
        public CreateSceneTool()
        {
            Name = "create_scene";
            Description = "Creates a new Unity scene and optionally saves it to a specified path";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                    string sceneName = parameters["sceneName"]?.ToObject<string>() ?? "NewScene";
                    string savePath = parameters["savePath"]?.ToObject<string>();
                    bool setAsActiveScene = parameters["setAsActiveScene"]?.ToObject<bool>() ?? true;
                    bool additive = parameters["additive"]?.ToObject<bool>() ?? false;

                    Scene newScene;

                    if (additive)
                    {
                        // Create scene additively (don't close current scene)
                        newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    }
                    else
                    {
                        // Create new scene and close current one
                        newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    }

                    if (!newScene.IsValid())
                    {
                        tcs.SetResult(CreateErrorResponse("Failed to create new scene.", "scene_creation_failed"));
                        return;
                    }

                    // Set scene name if it's different from default
                    if (!string.IsNullOrEmpty(sceneName) && sceneName != "NewScene")
                    {
                        newScene.name = sceneName;
                    }

                    // Set as active scene if requested
                    if (setAsActiveScene)
                    {
                        SceneManager.SetActiveScene(newScene);
                    }

                    // Save scene if path is provided
                    string finalPath = "";
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        // Ensure .unity extension
                        if (!savePath.EndsWith(".unity"))
                        {
                            savePath += ".unity";
                        }

                        // Ensure Assets/ prefix if not absolute path
                        if (!savePath.StartsWith("Assets/") && !System.IO.Path.IsPathRooted(savePath))
                        {
                            savePath = $"Assets/Scenes/{savePath}";
                        }

                        // Ensure directory exists
                        string directory = System.IO.Path.GetDirectoryName(savePath);
                        if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                        {
                            // Create folder structure
                            string[] folders = directory.Split('/');
                            string currentPath = "";
                            for (int i = 0; i < folders.Length; i++)
                            {
                                if (i == 0 && folders[i] == "Assets")
                                {
                                    currentPath = "Assets";
                                    continue;
                                }

                                string newPath = currentPath + "/" + folders[i];
                                if (!AssetDatabase.IsValidFolder(newPath))
                                {
                                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                                }
                                currentPath = newPath;
                            }
                        }

                        // Save the scene
                        bool saved = EditorSceneManager.SaveScene(newScene, savePath);
                        if (saved)
                        {
                            finalPath = savePath;
                            AssetDatabase.Refresh();
                        }
                        else
                        {
                            Debug.LogWarning($"[UnityMCP] Failed to save scene to path: {savePath}");
                        }
                    }

                    // Log the action
                    Debug.Log($"[UnityMCP] Created scene '{newScene.name}'" + (string.IsNullOrEmpty(finalPath) ? "" : $" saved to '{finalPath}'"));

                    var data = new
                    {
                        sceneName = newScene.name,
                        sceneHandle = newScene.handle,
                        scenePath = finalPath,
                        isActive = SceneManager.GetActiveScene() == newScene,
                        isLoaded = newScene.isLoaded,
                        isDirty = newScene.isDirty,
                        gameObjectCount = newScene.rootCount
                    };

                    tcs.SetResult(CreateSuccessResponse($"Successfully created scene '{newScene.name}'", data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to create scene: {e.Message}", "scene_error"));
                }
            };
        }
    }
}