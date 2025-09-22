using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMCP.Utils;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for creating GameObjects in Unity based on GameLovers MCP architecture
    /// </summary>
    public class CreateGameObjectTool : McpToolBase
    {
        public CreateGameObjectTool()
        {
            Name = "unity.create_gameobject";
            Description = "Creates a GameObject in Unity with optional primitive type, position, rotation, and scale";
            IsAsync = true; // Execute asynchronously to handle main thread requirements
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            // Execute on Unity's main thread using EditorApplication.delayCall
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                string objectName = parameters["name"]?.ToObject<string>() ?? "GameObject";
                string objectPath = parameters["objectPath"]?.ToObject<string>();
                string primitiveType = parameters["primitiveType"]?.ToObject<string>();
                string parentName = parameters["parent"]?.ToObject<string>();

                GameObject go;

                // Create GameObject - prefer objectPath over direct creation
                if (!string.IsNullOrEmpty(objectPath))
                {
                    // Use hierarchical creation like GameLovers
                    go = GameObjectHierarchyCreator.FindOrCreateHierarchicalGameObject(objectPath);
                }
                else
                {
                    // Create primitive or empty GameObject
                    if (!string.IsNullOrEmpty(primitiveType))
                    {
                        if (System.Enum.TryParse<PrimitiveType>(primitiveType, true, out var type))
                        {
                            go = GameObject.CreatePrimitive(type);
                            go.name = objectName;
                        }
                        else
                        {
                            go = new GameObject(objectName);
                        }
                    }
                    else
                    {
                        go = new GameObject(objectName);
                    }

                    // Set parent if provided
                    if (!string.IsNullOrEmpty(parentName))
                    {
                        var parent = GameObject.Find(parentName);
                        if (parent != null)
                        {
                            go.transform.SetParent(parent.transform, false);
                        }
                    }
                }

                // Apply transform properties
                ApplyTransformProperties(go, parameters);

                // Register for undo
                Undo.RegisterCreatedObjectUndo(go, $"Create GameObject '{go.name}'");

                // Mark scene as dirty
                EditorUtility.SetDirty(go);

                var data = new
                {
                    objectName = go.name,
                    instanceId = go.GetInstanceID(),
                    path = GameObjectHierarchyCreator.GetGameObjectPath(go),
                    position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                    rotation = new { x = go.transform.rotation.eulerAngles.x, y = go.transform.rotation.eulerAngles.y, z = go.transform.rotation.eulerAngles.z },
                    scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z }
                };

                    tcs.SetResult(CreateSuccessResponse($"GameObject '{go.name}' created successfully", data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to create GameObject: {e.Message}", "creation_error"));
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
                    pos["x"]?.ToObject<float>() ?? 0f,
                    pos["y"]?.ToObject<float>() ?? 0f,
                    pos["z"]?.ToObject<float>() ?? 0f
                );
                go.transform.position = position;
            }

            // Set rotation if provided
            if (parameters["rotation"] != null)
            {
                var rot = parameters["rotation"];
                var rotation = Quaternion.Euler(
                    rot["x"]?.ToObject<float>() ?? 0f,
                    rot["y"]?.ToObject<float>() ?? 0f,
                    rot["z"]?.ToObject<float>() ?? 0f
                );
                go.transform.rotation = rotation;
            }

            // Set scale if provided
            if (parameters["scale"] != null)
            {
                var scl = parameters["scale"];
                var scale = new Vector3(
                    scl["x"]?.ToObject<float>() ?? 1f,
                    scl["y"]?.ToObject<float>() ?? 1f,
                    scl["z"]?.ToObject<float>() ?? 1f
                );
                go.transform.localScale = scale;
            }
        }
    }
}