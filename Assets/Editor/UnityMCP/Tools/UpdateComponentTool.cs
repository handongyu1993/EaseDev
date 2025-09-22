using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for updating or adding components to GameObjects
    /// Ported from GameLovers MCP Unity implementation
    /// </summary>
    public class UpdateComponentTool : McpToolBase
    {
        public UpdateComponentTool()
        {
            Name = "update_component";
            Description = "Updates component fields on a GameObject or adds it to the GameObject if it does not contain the component";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                    int? instanceId = parameters["instanceId"]?.ToObject<int?>();
                    string objectPath = parameters["objectPath"]?.ToObject<string>();
                    string componentName = parameters["componentName"]?.ToObject<string>();
                    JObject componentData = parameters["componentData"] as JObject;

                    // Validate parameters
                    if (!instanceId.HasValue && string.IsNullOrEmpty(objectPath))
                    {
                        tcs.SetResult(CreateErrorResponse("Either 'instanceId' or 'objectPath' must be provided", "validation_error"));
                        return;
                    }

                    if (string.IsNullOrEmpty(componentName))
                    {
                        tcs.SetResult(CreateErrorResponse("Required parameter 'componentName' not provided", "validation_error"));
                        return;
                    }

                    // Find or create GameObject
                    GameObject gameObject = null;
                    string identifier = "unknown";

                    if (instanceId.HasValue)
                    {
                        gameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                        identifier = $"ID {instanceId.Value}";
                    }
                    else if (!string.IsNullOrEmpty(objectPath))
                    {
                        // Use hierarchy creator to find or create the GameObject
                        gameObject = GameObjectHierarchyCreator.FindOrCreateHierarchicalGameObject(objectPath);
                        identifier = $"path '{objectPath}'";
                    }

                    if (gameObject == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"GameObject with {identifier} not found or could not be created", "not_found_error"));
                        return;
                    }

                    Debug.Log($"[UnityMCP] Updating component '{componentName}' on GameObject '{gameObject.name}' (found by {identifier})");

                    // Try to find the component by name
                    Component component = gameObject.GetComponent(componentName);

                    // If component not found, try to add it
                    if (component == null)
                    {
                        Type componentType = FindComponentType(componentName);
                        if (componentType == null)
                        {
                            tcs.SetResult(CreateErrorResponse($"Component type '{componentName}' not found in Unity", "component_error"));
                            return;
                        }

                        component = Undo.AddComponent(gameObject, componentType);

                        // Ensure changes are saved
                        EditorUtility.SetDirty(gameObject);
                        if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        }

                        Debug.Log($"[UnityMCP] Added component '{componentName}' to GameObject '{gameObject.name}'");
                    }

                    // Update component fields
                    if (componentData != null && componentData.Count > 0)
                    {
                        bool success = UpdateComponentData(component, componentData, out string errorMessage);
                        if (!success)
                        {
                            tcs.SetResult(CreateErrorResponse(errorMessage, "update_error"));
                            return;
                        }

                        // Ensure field changes are saved
                        EditorUtility.SetDirty(gameObject);
                        if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        }
                    }

                    tcs.SetResult(CreateSuccessResponse($"Successfully updated component '{componentName}' on GameObject '{gameObject.name}'", new {
                        gameObjectName = gameObject.name,
                        componentName = componentName,
                        instanceId = gameObject.GetInstanceID()
                    }));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to update component: {e.Message}", "component_error"));
                }
            };
        }

        /// <summary>
        /// Find a component type by name
        /// </summary>
        private Type FindComponentType(string componentName)
        {
            // First try direct match
            Type type = Type.GetType(componentName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                return type;
            }

            // Try common Unity namespaces
            string[] commonNamespaces = new string[]
            {
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.EventSystems",
                "UnityEngine.Animations",
                "UnityEngine.Rendering",
                "TMPro"
            };

            foreach (string ns in commonNamespaces)
            {
                type = Type.GetType($"{ns}.{componentName}, UnityEngine");
                if (type != null && typeof(Component).IsAssignableFrom(type))
                {
                    return type;
                }
            }

            // Try assemblies search
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.Name == componentName && typeof(Component).IsAssignableFrom(t))
                        {
                            return t;
                        }
                    }
                }
                catch (Exception)
                {
                    // Some assemblies might throw exceptions when getting types
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Update component data based on the provided JObject
        /// </summary>
        private bool UpdateComponentData(Component component, JObject componentData, out string errorMessage)
        {
            errorMessage = "";

            if (component == null || componentData == null)
            {
                errorMessage = "Component or component data is null";
                return false;
            }

            Type componentType = component.GetType();
            bool fullSuccess = true;

            // Record object for undo
            Undo.RecordObject(component, $"Update {componentType.Name} fields");

            // Process each field or property in the component data
            foreach (var property in componentData.Properties())
            {
                string fieldName = property.Name;
                JToken fieldValue = property.Value;

                // Skip null values
                if (string.IsNullOrEmpty(fieldName) || fieldValue.Type == JTokenType.Null)
                {
                    continue;
                }

                // Try to update field
                FieldInfo fieldInfo = componentType.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (fieldInfo != null)
                {
                    object value = ConvertJTokenToValue(fieldValue, fieldInfo.FieldType);
                    fieldInfo.SetValue(component, value);
                    continue;
                }

                // Try to update property if not found as a field
                PropertyInfo propertyInfo = componentType.GetProperty(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    object value = ConvertJTokenToValue(fieldValue, propertyInfo.PropertyType);
                    propertyInfo.SetValue(component, value);
                    continue;
                }

                fullSuccess = false;
                errorMessage = $"Field or Property with name '{fieldName}' not found on component '{componentType.Name}'";
                Debug.LogWarning(errorMessage);
            }

            return fullSuccess;
        }

        /// <summary>
        /// Convert a JToken to a value of the specified type
        /// </summary>
        private object ConvertJTokenToValue(JToken token, Type targetType)
        {
            if (token == null)
            {
                return null;
            }

            // Handle Unity Vector types
            if (targetType == typeof(Vector2) && token.Type == JTokenType.Object)
            {
                JObject vector = (JObject)token;
                return new Vector2(
                    vector["x"]?.ToObject<float>() ?? 0f,
                    vector["y"]?.ToObject<float>() ?? 0f
                );
            }

            if (targetType == typeof(Vector3) && token.Type == JTokenType.Object)
            {
                JObject vector = (JObject)token;
                return new Vector3(
                    vector["x"]?.ToObject<float>() ?? 0f,
                    vector["y"]?.ToObject<float>() ?? 0f,
                    vector["z"]?.ToObject<float>() ?? 0f
                );
            }

            if (targetType == typeof(Color) && token.Type == JTokenType.Object)
            {
                JObject color = (JObject)token;
                return new Color(
                    color["r"]?.ToObject<float>() ?? 0f,
                    color["g"]?.ToObject<float>() ?? 0f,
                    color["b"]?.ToObject<float>() ?? 0f,
                    color["a"]?.ToObject<float>() ?? 1f
                );
            }

            if (targetType == typeof(Rect) && token.Type == JTokenType.Object)
            {
                JObject rect = (JObject)token;
                return new Rect(
                    rect["x"]?.ToObject<float>() ?? 0f,
                    rect["y"]?.ToObject<float>() ?? 0f,
                    rect["width"]?.ToObject<float>() ?? 0f,
                    rect["height"]?.ToObject<float>() ?? 0f
                );
            }

            // Handle enum types
            if (targetType.IsEnum)
            {
                // If JToken is a string, try to parse as enum name
                if (token.Type == JTokenType.String)
                {
                    string enumName = token.ToObject<string>();
                    if (Enum.TryParse(targetType, enumName, true, out object result))
                    {
                        return result;
                    }

                    // If parsing fails, try to convert numeric value
                    if (int.TryParse(enumName, out int enumValue))
                    {
                        return Enum.ToObject(targetType, enumValue);
                    }
                }
                // If JToken is a number, convert directly to enum
                else if (token.Type == JTokenType.Integer)
                {
                    return Enum.ToObject(targetType, token.ToObject<int>());
                }
            }

            // For other types, use JToken's ToObject method
            try
            {
                return token.ToObject(targetType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityMCP] Error converting value to type {targetType.Name}: {ex.Message}");
                return null;
            }
        }
    }
}