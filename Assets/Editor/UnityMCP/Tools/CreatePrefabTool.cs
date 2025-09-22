using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for creating prefabs based on GameLovers MCP architecture
    /// </summary>
    public class CreatePrefabTool : McpToolBase
    {
        public CreatePrefabTool()
        {
            Name = "create_prefab";
            Description = "Creates a prefab from an existing GameObject";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                string gameObjectName = parameters["gameObjectName"]?.ToObject<string>();
                string objectPath = parameters["objectPath"]?.ToObject<string>();
                int? instanceId = parameters["instanceId"]?.ToObject<int?>();
                string prefabPath = parameters["prefabPath"]?.ToObject<string>();
                string prefabName = parameters["prefabName"]?.ToObject<string>();

                // Validate required parameters
                if (string.IsNullOrEmpty(prefabPath) && string.IsNullOrEmpty(prefabName))
                {
                    tcs.SetResult(CreateErrorResponse("Either 'prefabPath' or 'prefabName' must be provided.", "validation_error"));
                }

                // Find the source GameObject
                GameObject sourceObject = null;
                string identifierInfo = "";

                if (instanceId.HasValue)
                {
                    sourceObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    identifierInfo = $"instance ID {instanceId.Value}";
                }
                else if (!string.IsNullOrEmpty(objectPath))
                {
                    try
                    {
                        sourceObject = GameObjectHierarchyCreator.FindOrCreateHierarchicalGameObject(objectPath);
                        identifierInfo = $"path '{objectPath}'";
                    }
                    catch
                    {
                        sourceObject = GameObject.Find(objectPath.Split('/')[^1]);
                        identifierInfo = $"path '{objectPath}' (found by name)";
                    }
                }
                else if (!string.IsNullOrEmpty(gameObjectName))
                {
                    sourceObject = GameObject.Find(gameObjectName);
                    identifierInfo = $"name '{gameObjectName}'";
                }
                else
                {
                    tcs.SetResult(CreateErrorResponse("Either 'gameObjectName', 'objectPath', or 'instanceId' must be provided.", "validation_error"));
                }

                if (sourceObject == null && !tcs.Task.IsCompleted)
                {
                    tcs.SetResult(CreateErrorResponse($"Source GameObject not found using {identifierInfo}.", "not_found"));
                }

                if (!tcs.Task.IsCompleted)
                {
                    // Determine final prefab path
                    string finalPrefabPath;
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    finalPrefabPath = prefabPath;
                    // Ensure .prefab extension
                    if (!finalPrefabPath.EndsWith(".prefab"))
                    {
                        finalPrefabPath += ".prefab";
                    }
                }
                else
                {
                    // Use prefabName to generate path
                    finalPrefabPath = $"Assets/{prefabName}.prefab";
                }

                // Ensure the directory exists
                string directory = System.IO.Path.GetDirectoryName(finalPrefabPath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    // Try to create the folder structure
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

                // For safety, create a unique name if prefab already exists
                int counter = 1;
                string originalPath = finalPrefabPath;
                string baseName = System.IO.Path.GetFileNameWithoutExtension(finalPrefabPath);
                string directoryPath = System.IO.Path.GetDirectoryName(finalPrefabPath);

                while (AssetDatabase.LoadAssetAtPath<GameObject>(finalPrefabPath) != null)
                {
                    string newName = $"{baseName}_{counter}";
                    finalPrefabPath = System.IO.Path.Combine(directoryPath, newName + ".prefab");
                    counter++;
                }

                // Create the prefab
                bool success = false;
                try
                {
                    GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(sourceObject, finalPrefabPath, out success);

                    if (success && prefabAsset != null)
                    {
                        // Refresh the asset database
                        AssetDatabase.Refresh();

                        // Log the action
                        Debug.Log($"[UnityMCP] Created prefab '{System.IO.Path.GetFileNameWithoutExtension(finalPrefabPath)}' at path '{finalPrefabPath}' from GameObject '{sourceObject.name}'");

                        var data = new
                        {
                            prefabPath = finalPrefabPath,
                            prefabName = System.IO.Path.GetFileNameWithoutExtension(finalPrefabPath),
                            sourceGameObjectName = sourceObject.name,
                            sourceInstanceId = sourceObject.GetInstanceID()
                        };

                            tcs.SetResult(CreateSuccessResponse($"Successfully created prefab '{System.IO.Path.GetFileNameWithoutExtension(finalPrefabPath)}' at path '{finalPrefabPath}'", data));
                        }
                        else
                        {
                            tcs.SetResult(CreateErrorResponse($"Failed to create prefab at path '{finalPrefabPath}'. Unity PrefabUtility.SaveAsPrefabAsset returned false.", "creation_failed"));
                        }
                    }
                    catch (Exception e)
                    {
                        tcs.SetResult(CreateErrorResponse($"Exception occurred while creating prefab: {e.Message}", "creation_exception"));
                    }
                }
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to create prefab: {e.Message}", "prefab_error"));
                }
            };
        }
    }
}