using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for adding components to GameObjects based on GameLovers MCP architecture
    /// </summary>
    public class AddComponentTool : McpToolBase
    {
        public AddComponentTool()
        {
            Name = "add_component";
            Description = "Adds a component to a GameObject by instance ID or name";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                    string componentTypeName = parameters["componentType"]?.ToObject<string>();
                    string gameObjectName = parameters["gameObjectName"]?.ToObject<string>();
                    int? instanceId = parameters["instanceId"]?.ToObject<int?>();

                    if (string.IsNullOrEmpty(componentTypeName))
                    {
                        tcs.SetResult(CreateErrorResponse("Parameter 'componentType' is required.", "validation_error"));
                        return;
                    }

                    // Find target GameObject
                    GameObject targetObject = null;
                    string identifierInfo = "";

                    if (instanceId.HasValue)
                    {
                        targetObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                        identifierInfo = $"instance ID {instanceId.Value}";
                    }
                    else if (!string.IsNullOrEmpty(gameObjectName))
                    {
                        targetObject = GameObject.Find(gameObjectName);
                        identifierInfo = $"name '{gameObjectName}'";
                    }
                    else
                    {
                        tcs.SetResult(CreateErrorResponse("Either 'gameObjectName' or 'instanceId' must be provided.", "validation_error"));
                        return;
                    }

                    if (targetObject == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"GameObject not found using {identifierInfo}.", "not_found"));
                        return;
                    }

                    // Find component type
                    Type componentType = FindComponentType(componentTypeName);
                    if (componentType == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"Component type '{componentTypeName}' not found.", "type_not_found"));
                        return;
                    }

                    // Check if component already exists (for single-instance components)
                    if (targetObject.GetComponent(componentType) != null)
                    {
                        // Some components allow multiple instances
                        var allowMultiple = componentType.GetCustomAttribute<System.ComponentModel.EditorBrowsableAttribute>();
                        if (componentType == typeof(Transform) || componentType == typeof(RectTransform))
                        {
                            tcs.SetResult(CreateErrorResponse($"GameObject '{targetObject.name}' already has a {componentTypeName} component, and only one is allowed.", "component_exists"));
                            return;
                        }
                    }

                    // Add component
                    Component newComponent;
                    try
                    {
                        Undo.RecordObject(targetObject, $"Add {componentTypeName}");
                        newComponent = targetObject.AddComponent(componentType);
                        EditorUtility.SetDirty(targetObject);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetResult(CreateErrorResponse($"Failed to add component '{componentTypeName}' to GameObject '{targetObject.name}': {ex.Message}", "add_component_failed"));
                        return;
                    }

                    if (newComponent == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"Failed to add component '{componentTypeName}' to GameObject '{targetObject.name}'", "add_component_failed"));
                        return;
                    }

                    // Configure component with provided parameters if available
                    var componentData = parameters["componentData"] as JObject;
                    if (componentData != null)
                    {
                        ConfigureComponent(newComponent, componentData);
                    }

                    Debug.Log($"[UnityMCP] Added component '{componentTypeName}' to GameObject '{targetObject.name}'");

                    var data = new
                    {
                        gameObjectName = targetObject.name,
                        gameObjectInstanceId = targetObject.GetInstanceID(),
                        componentType = componentType.Name,
                        componentInstanceId = newComponent.GetInstanceID(),
                        componentEnabled = newComponent is Behaviour behaviour ? behaviour.enabled : true
                    };

                    tcs.SetResult(CreateSuccessResponse($"Successfully added component '{componentTypeName}' to GameObject '{targetObject.name}'", data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to add component: {e.Message}", "component_error"));
                }
            };
        }

        private Type FindComponentType(string typeName)
        {
            // Common Unity component mappings
            var commonTypes = new System.Collections.Generic.Dictionary<string, string>
            {
                { "rigidbody", "UnityEngine.Rigidbody" },
                { "collider", "UnityEngine.BoxCollider" },
                { "boxcollider", "UnityEngine.BoxCollider" },
                { "spherecollider", "UnityEngine.SphereCollider" },
                { "capsulecollider", "UnityEngine.CapsuleCollider" },
                { "meshcollider", "UnityEngine.MeshCollider" },
                { "light", "UnityEngine.Light" },
                { "camera", "UnityEngine.Camera" },
                { "audiosource", "UnityEngine.AudioSource" },
                { "audiolistener", "UnityEngine.AudioListener" },
                { "animator", "UnityEngine.Animator" },
                { "animation", "UnityEngine.Animation" },
                { "particlesystem", "UnityEngine.ParticleSystem" },
                { "canvas", "UnityEngine.Canvas" },
                { "canvasgroup", "UnityEngine.CanvasGroup" },
                { "graphicraycaster", "UnityEngine.UI.GraphicRaycaster" },
                { "button", "UnityEngine.UI.Button" },
                { "image", "UnityEngine.UI.Image" },
                { "text", "UnityEngine.UI.Text" },
                { "inputfield", "UnityEngine.UI.InputField" },
                { "scrollrect", "UnityEngine.UI.ScrollRect" },
                { "slider", "UnityEngine.UI.Slider" },
                { "toggle", "UnityEngine.UI.Toggle" },
                { "dropdown", "UnityEngine.UI.Dropdown" }
            };

            // Check common types first
            string lowerTypeName = typeName.ToLower().Replace(" ", "");
            if (commonTypes.ContainsKey(lowerTypeName))
            {
                typeName = commonTypes[lowerTypeName];
            }

            // Try to find the type
            Type type = Type.GetType(typeName);
            if (type != null) return type;

            // Try with UnityEngine namespace
            type = Type.GetType($"UnityEngine.{typeName}");
            if (type != null) return type;

            // Try with UnityEngine.UI namespace
            type = Type.GetType($"UnityEngine.UI.{typeName}");
            if (type != null) return type;

            // Search all loaded assemblies
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }

        private void ConfigureComponent(Component component, JObject componentData)
        {
            try
            {
                Type componentType = component.GetType();

                foreach (var property in componentData.Properties())
                {
                    string propertyName = property.Name;
                    var propertyValue = property.Value;

                    // Find the property or field
                    PropertyInfo propInfo = componentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    FieldInfo fieldInfo = null;

                    if (propInfo == null)
                    {
                        fieldInfo = componentType.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    }

                    if (propInfo != null && propInfo.CanWrite)
                    {
                        object value = ConvertValue(propertyValue, propInfo.PropertyType);
                        if (value != null)
                        {
                            propInfo.SetValue(component, value);
                        }
                    }
                    else if (fieldInfo != null && !fieldInfo.IsInitOnly)
                    {
                        object value = ConvertValue(propertyValue, fieldInfo.FieldType);
                        if (value != null)
                        {
                            fieldInfo.SetValue(component, value);
                        }
                    }
                }

                EditorUtility.SetDirty(component);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityMCP] Failed to configure component: {ex.Message}");
            }
        }

        private object ConvertValue(JToken token, Type targetType)
        {
            try
            {
                if (targetType == typeof(bool)) return token.ToObject<bool>();
                if (targetType == typeof(int)) return token.ToObject<int>();
                if (targetType == typeof(float)) return token.ToObject<float>();
                if (targetType == typeof(string)) return token.ToObject<string>();
                if (targetType == typeof(Vector3))
                {
                    var obj = token.ToObject<JObject>();
                    return new Vector3(
                        obj["x"]?.ToObject<float>() ?? 0f,
                        obj["y"]?.ToObject<float>() ?? 0f,
                        obj["z"]?.ToObject<float>() ?? 0f
                    );
                }
                if (targetType == typeof(Color))
                {
                    var obj = token.ToObject<JObject>();
                    return new Color(
                        obj["r"]?.ToObject<float>() ?? 0f,
                        obj["g"]?.ToObject<float>() ?? 0f,
                        obj["b"]?.ToObject<float>() ?? 0f,
                        obj["a"]?.ToObject<float>() ?? 1f
                    );
                }

                return token.ToObject(targetType);
            }
            catch
            {
                return null;
            }
        }
    }
}