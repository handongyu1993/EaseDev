using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for getting GameObject information based on GameLovers MCP architecture
    /// </summary>
    public class GetGameObjectTool : McpToolBase
    {
        public GetGameObjectTool()
        {
            Name = "get_gameobject";
            Description = "Gets GameObject information by name, instance ID, or object path";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                string objectName = parameters["name"]?.ToObject<string>();
                string objectPath = parameters["objectPath"]?.ToObject<string>();
                int? instanceId = parameters["instanceId"]?.ToObject<int?>();

                GameObject targetGameObject = null;
                string identifierInfo = "";

                // Find the GameObject by different methods
                if (instanceId.HasValue)
                {
                    targetGameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                    identifierInfo = $"instance ID {instanceId.Value}";
                }
                else if (!string.IsNullOrEmpty(objectPath))
                {
                    // Try to find by hierarchical path first
                    try
                    {
                        targetGameObject = GameObjectHierarchyCreator.FindOrCreateHierarchicalGameObject(objectPath);
                        identifierInfo = $"path '{objectPath}'";
                    }
                    catch
                    {
                        // If path creation fails, try simple find
                        targetGameObject = GameObject.Find(objectPath.Split('/')[^1]);
                        identifierInfo = $"path '{objectPath}' (found by name)";
                    }
                }
                else if (!string.IsNullOrEmpty(objectName))
                {
                    targetGameObject = GameObject.Find(objectName);
                    identifierInfo = $"name '{objectName}'";
                }
                else
                {
                    tcs.SetResult(CreateErrorResponse("Either 'name', 'objectPath', or 'instanceId' must be provided.", "validation_error"));
                }

                // Check if GameObject was found
                if (targetGameObject == null && !tcs.Task.IsCompleted)
                {
                    tcs.SetResult(CreateErrorResponse($"GameObject not found using {identifierInfo}.", "not_found"));
                }

                if (!tcs.Task.IsCompleted)
                {
                    // Collect GameObject information
                    var gameObjectInfo = new
                {
                    name = targetGameObject.name,
                    instanceId = targetGameObject.GetInstanceID(),
                    path = GameObjectHierarchyCreator.GetGameObjectPath(targetGameObject),
                    tag = targetGameObject.tag,
                    layer = targetGameObject.layer,
                    layerName = LayerMask.LayerToName(targetGameObject.layer),
                    isActive = targetGameObject.activeSelf,
                    isActiveInHierarchy = targetGameObject.activeInHierarchy,
                    isStatic = targetGameObject.isStatic,
                    transform = new
                    {
                        position = new { x = targetGameObject.transform.position.x, y = targetGameObject.transform.position.y, z = targetGameObject.transform.position.z },
                        rotation = new { x = targetGameObject.transform.rotation.eulerAngles.x, y = targetGameObject.transform.rotation.eulerAngles.y, z = targetGameObject.transform.rotation.eulerAngles.z },
                        scale = new { x = targetGameObject.transform.localScale.x, y = targetGameObject.transform.localScale.y, z = targetGameObject.transform.localScale.z }
                    },
                    components = GetComponentsInfo(targetGameObject),
                    childCount = targetGameObject.transform.childCount,
                    hasParent = targetGameObject.transform.parent != null,
                    parentName = targetGameObject.transform.parent?.name
                };

                    tcs.SetResult(CreateSuccessResponse($"GameObject '{targetGameObject.name}' found successfully.", gameObjectInfo));
                }
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to get GameObject: {e.Message}", "get_error"));
                }
            };
        }

        private object[] GetComponentsInfo(GameObject go)
        {
            var components = go.GetComponents<Component>();
            var componentInfos = new object[components.Length];

            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null)
                {
                    componentInfos[i] = new
                    {
                        type = component.GetType().Name,
                        fullType = component.GetType().FullName,
                        enabled = component is Behaviour behaviour ? behaviour.enabled : true
                    };
                }
                else
                {
                    componentInfos[i] = new
                    {
                        type = "Missing Script",
                        fullType = "Missing Script",
                        enabled = false
                    };
                }
            }

            return componentInfos;
        }
    }
}