using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for updating or creating a GameObject in Unity based on GameLovers MCP architecture
    /// </summary>
    public class UpdateGameObjectTool : McpToolBase
    {
        public UpdateGameObjectTool()
        {
            Name = "update_gameobject";
            Description = "Updates or creates a GameObject and its properties (name, tag, layer, active state, static state) based on instance ID or object path.";
            IsAsync = true; // Execute on main thread
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters from JObject
                int? instanceId = parameters["instanceId"]?.ToObject<int?>();
                string objectPath = parameters["objectPath"]?.ToObject<string>();
                string objectName = parameters["name"]?.ToObject<string>(); // Legacy support
                JObject gameObjectData = parameters["gameObjectData"] as JObject ?? parameters;

                // Support both direct parameters and nested gameObjectData
                string newName = gameObjectData["name"]?.ToObject<string>();
                string newTag = gameObjectData["tag"]?.ToObject<string>();
                int? newLayer = gameObjectData["layer"]?.ToObject<int?>();
                bool? newIsActiveSelf = gameObjectData["isActiveSelf"]?.ToObject<bool?>();
                bool? newIsStatic = gameObjectData["isStatic"]?.ToObject<bool?>();

                GameObject targetGameObject = null;
                string identifierInfo = "";

                // Identify or create the GameObject by instanceId or objectPath
                if (instanceId.HasValue)
                {
                    targetGameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    identifierInfo = $"instance ID {instanceId.Value}";
                }
                else if (!string.IsNullOrEmpty(objectPath))
                {
                    // Will create the GameObject if it doesn't exist using our hierarchy creator
                    targetGameObject = GameObjectHierarchyCreator.FindOrCreateHierarchicalGameObject(objectPath);
                    identifierInfo = $"path '{objectPath}'";
                }
                else if (!string.IsNullOrEmpty(objectName))
                {
                    // Legacy support - try to find by name
                    targetGameObject = GameObject.Find(objectName);
                    identifierInfo = $"name '{objectName}'";
                }
                else
                {
                    tcs.SetResult(CreateErrorResponse("Either 'instanceId', 'objectPath', or 'name' must be provided.", "validation_error"));
                }

                // Check if we could not identify or create the GameObject
                if (targetGameObject == null && !tcs.Task.IsCompleted)
                {
                    tcs.SetResult(CreateErrorResponse($"Target GameObject could not be identified or created using {identifierInfo}.", "not_found"));
                }

                if (!tcs.Task.IsCompleted)
                {
                    // Record for undo in Unity Editor
                    Undo.RecordObject(targetGameObject, "Update GameObject Properties");
                bool propertiesUpdated = false;
                string originalNameForLog = targetGameObject.name;

                // Update name if provided and different
                if (!string.IsNullOrEmpty(newName) && targetGameObject.name != newName)
                {
                    targetGameObject.name = newName;
                    propertiesUpdated = true;
                }

                // Update tag if provided and different, warn if tag doesn't exist
                if (!string.IsNullOrEmpty(newTag))
                {
                    bool tagExists = Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, t => t.Equals(newTag));
                    if (!tagExists)
                    {
                        Debug.LogWarning($"UpdateGameObjectTool: Tag '{newTag}' does not exist for GameObject '{originalNameForLog}'. Tag not changed. Please create the tag in Unity's Tag Manager.");
                    }
                    else if (!targetGameObject.CompareTag(newTag))
                    {
                        targetGameObject.tag = newTag;
                        propertiesUpdated = true;
                    }
                }

                // Update layer if provided and valid
                if (newLayer.HasValue)
                {
                    if (newLayer.Value < 0 || newLayer.Value > 31)
                    {
                        Debug.LogWarning($"UpdateGameObjectTool: Invalid layer value {newLayer.Value} for GameObject '{originalNameForLog}'. Layer must be between 0 and 31. Layer not changed.");
                    }
                    else if (targetGameObject.layer != newLayer.Value)
                    {
                        targetGameObject.layer = newLayer.Value;
                        propertiesUpdated = true;
                    }
                }

                // Update active state if provided and different
                if (newIsActiveSelf.HasValue && targetGameObject.activeSelf != newIsActiveSelf.Value)
                {
                    targetGameObject.SetActive(newIsActiveSelf.Value);
                    propertiesUpdated = true;
                }

                // Update static state if provided and different
                if (newIsStatic.HasValue && targetGameObject.isStatic != newIsStatic.Value)
                {
                    targetGameObject.isStatic = newIsStatic.Value;
                    propertiesUpdated = true;
                }

                // Apply transform properties if provided
                if (parameters.ContainsKey("position") || parameters.ContainsKey("rotation") || parameters.ContainsKey("scale"))
                {
                    ApplyTransformProperties(targetGameObject, parameters);
                    propertiesUpdated = true;
                }

                // Mark as dirty if any property was changed
                if (propertiesUpdated)
                {
                    EditorUtility.SetDirty(targetGameObject);
                }

                // Return success response
                var data = new
                {
                    instanceId = targetGameObject.GetInstanceID(),
                    name = targetGameObject.name,
                    path = GameObjectHierarchyCreator.GetGameObjectPath(targetGameObject),
                    position = new { x = targetGameObject.transform.position.x, y = targetGameObject.transform.position.y, z = targetGameObject.transform.position.z },
                    rotation = new { x = targetGameObject.transform.rotation.eulerAngles.x, y = targetGameObject.transform.rotation.eulerAngles.y, z = targetGameObject.transform.rotation.eulerAngles.z },
                    scale = new { x = targetGameObject.transform.localScale.x, y = targetGameObject.transform.localScale.y, z = targetGameObject.transform.localScale.z }
                };

                string message = propertiesUpdated
                    ? $"GameObject '{targetGameObject.name}' (identified by {identifierInfo}) updated successfully."
                    : $"No properties were changed for GameObject '{targetGameObject.name}' (identified by {identifierInfo}).";

                    tcs.SetResult(CreateSuccessResponse(message, data));
                }
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to update GameObject: {e.Message}", "update_error"));
                }
            };
        }

        private void ApplyTransformProperties(GameObject go, JObject parameters)
        {
            // Set position if provided
            if (parameters["position"] != null)
            {
                var pos = parameters["position"];
                var position = new Vector3(
                    pos["x"]?.ToObject<float>() ?? go.transform.position.x,
                    pos["y"]?.ToObject<float>() ?? go.transform.position.y,
                    pos["z"]?.ToObject<float>() ?? go.transform.position.z
                );
                go.transform.position = position;
            }

            // Set rotation if provided
            if (parameters["rotation"] != null)
            {
                var rot = parameters["rotation"];
                var rotation = Quaternion.Euler(
                    rot["x"]?.ToObject<float>() ?? go.transform.rotation.eulerAngles.x,
                    rot["y"]?.ToObject<float>() ?? go.transform.rotation.eulerAngles.y,
                    rot["z"]?.ToObject<float>() ?? go.transform.rotation.eulerAngles.z
                );
                go.transform.rotation = rotation;
            }

            // Set scale if provided
            if (parameters["scale"] != null)
            {
                var scl = parameters["scale"];
                var scale = new Vector3(
                    scl["x"]?.ToObject<float>() ?? go.transform.localScale.x,
                    scl["y"]?.ToObject<float>() ?? go.transform.localScale.y,
                    scl["z"]?.ToObject<float>() ?? go.transform.localScale.z
                );
                go.transform.localScale = scale;
            }
        }
    }
}