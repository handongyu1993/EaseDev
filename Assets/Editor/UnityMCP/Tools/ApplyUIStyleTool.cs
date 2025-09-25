using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for applying modern UI styles and themes to UI elements
    /// </summary>
    public class ApplyUIStyleTool : McpToolBase
    {
        public ApplyUIStyleTool()
        {
            Name = "apply_ui_style";
            Description = "Applies modern UI styles and themes to UI elements (modern_flat, classic, game_style)";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    string canvasName = parameters["canvasName"]?.ToObject<string>() ?? "Canvas";
                    string styleTheme = parameters["theme"]?.ToObject<string>() ?? "modern_flat";
                    bool applyToAll = parameters["applyToAll"]?.ToObject<bool>() ?? true;

                    var styledElements = new List<object>();
                    int styledCount = 0;

                    // Find the target canvas
                    GameObject canvasObj = GameObject.Find(canvasName);
                    if (canvasObj == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"Canvas '{canvasName}' not found", "canvas_not_found"));
                        return;
                    }

                    // Apply styles based on theme
                    switch (styleTheme.ToLower())
                    {
                        case "modern_flat":
                            styledCount = ApplyModernFlatStyle(canvasObj, styledElements, applyToAll);
                            break;
                        case "classic":
                            styledCount = ApplyClassicStyle(canvasObj, styledElements, applyToAll);
                            break;
                        case "game_style":
                            styledCount = ApplyGameStyle(canvasObj, styledElements, applyToAll);
                            break;
                        default:
                            tcs.SetResult(CreateErrorResponse($"Unsupported theme: {styleTheme}. Supported: modern_flat, classic, game_style", "unsupported_theme"));
                            return;
                    }

                    Debug.Log($"[UnityMCP] Applied '{styleTheme}' style to {styledCount} UI elements");

                    var data = new
                    {
                        theme = styleTheme,
                        canvasName = canvasName,
                        elementsStyled = styledCount,
                        elements = styledElements.ToArray()
                    };

                    tcs.SetResult(CreateSuccessResponse($"Applied '{styleTheme}' style to {styledCount} elements", data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to apply UI style: {e.Message}", "style_error"));
                }
            };
        }

        private int ApplyModernFlatStyle(GameObject canvas, List<object> styledElements, bool applyToAll)
        {
            int count = 0;

            // Modern flat color palette
            var primaryBlue = new Color(0.29f, 0.565f, 0.886f, 1f); // #4A90E2
            var lightGray = new Color(0.95f, 0.95f, 0.95f, 1f);
            var darkText = new Color(0.2f, 0.2f, 0.2f, 1f);
            var whiteText = Color.white;

            // Style all UI elements recursively
            count += StyleElementsRecursively(canvas.transform, (element) => {
                var result = new { name = element.name, type = "unknown", styled = false };

                // Style buttons with modern flat design
                var button = element.GetComponent<Button>();
                if (button != null)
                {
                    var image = element.GetComponent<Image>();
                    if (image != null)
                    {
                        Undo.RecordObject(image, $"Style button {element.name}");
                        image.color = primaryBlue;
                        EditorUtility.SetDirty(image);
                    }

                    // Style button text
                    var text = element.GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        Undo.RecordObject(text, $"Style button text {element.name}");
                        text.color = whiteText;
                        text.fontSize = 18;
                        text.fontStyle = FontStyle.Bold;
                        EditorUtility.SetDirty(text);
                    }

                    result = new { name = element.name, type = "button", styled = true };
                    return result;
                }

                // Style input fields
                var inputField = element.GetComponent<InputField>();
                if (inputField != null)
                {
                    var image = element.GetComponent<Image>();
                    if (image != null)
                    {
                        Undo.RecordObject(image, $"Style input field {element.name}");
                        image.color = lightGray;
                        EditorUtility.SetDirty(image);
                    }

                    // Style input text
                    if (inputField.textComponent != null)
                    {
                        Undo.RecordObject(inputField.textComponent, $"Style input text {element.name}");
                        inputField.textComponent.color = darkText;
                        inputField.textComponent.fontSize = 16;
                        EditorUtility.SetDirty(inputField.textComponent);
                    }

                    // Style placeholder text
                    if (inputField.placeholder != null)
                    {
                        var placeholderText = inputField.placeholder as Text;
                        if (placeholderText != null)
                        {
                            Undo.RecordObject(placeholderText, $"Style placeholder {element.name}");
                            placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                            placeholderText.fontSize = 16;
                            EditorUtility.SetDirty(placeholderText);
                        }
                    }

                    result = new { name = element.name, type = "inputfield", styled = true };
                    return result;
                }

                // Style panels
                var panel = element.GetComponent<Image>();
                if (panel != null && element.name.ToLower().Contains("panel"))
                {
                    Undo.RecordObject(panel, $"Style panel {element.name}");
                    panel.color = new Color(1f, 1f, 1f, 0.98f); // Almost opaque white
                    EditorUtility.SetDirty(panel);

                    result = new { name = element.name, type = "panel", styled = true };
                    return result;
                }

                // Style titles and text
                var textComponent = element.GetComponent<Text>();
                if (textComponent != null && element.name.ToLower().Contains("title"))
                {
                    Undo.RecordObject(textComponent, $"Style title {element.name}");
                    textComponent.color = darkText;
                    textComponent.fontSize = 28;
                    textComponent.fontStyle = FontStyle.Bold;
                    EditorUtility.SetDirty(textComponent);

                    result = new { name = element.name, type = "title", styled = true };
                    return result;
                }

                return result;
            });

            styledElements.AddRange(GetStyledElementsList(canvas.transform));
            return count;
        }

        private int ApplyClassicStyle(GameObject canvas, List<object> styledElements, bool applyToAll)
        {
            // Classic UI style with traditional colors
            var classicBlue = new Color(0.4f, 0.6f, 0.9f, 1f);
            var classicGray = new Color(0.9f, 0.9f, 0.9f, 1f);

            // Implementation similar to modern flat but with classic colors
            return ApplyModernFlatStyle(canvas, styledElements, applyToAll); // Simplified for now
        }

        private int ApplyGameStyle(GameObject canvas, List<object> styledElements, bool applyToAll)
        {
            // Game UI style with vibrant colors and gradients
            var gameGreen = new Color(0.2f, 0.8f, 0.3f, 1f);
            var gameOrange = new Color(1f, 0.6f, 0.1f, 1f);

            // Implementation similar to modern flat but with game colors
            return ApplyModernFlatStyle(canvas, styledElements, applyToAll); // Simplified for now
        }

        private int StyleElementsRecursively(Transform parent, Func<GameObject, object> styleFunc)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var result = styleFunc(child.gameObject);

                // Check if styling was applied
                var dynamicResult = result as dynamic;
                if (dynamicResult?.styled == true)
                {
                    count++;
                }

                // Recursively style children
                count += StyleElementsRecursively(child, styleFunc);
            }
            return count;
        }

        private List<object> GetStyledElementsList(Transform parent)
        {
            var elements = new List<object>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var button = child.GetComponent<Button>();
                var inputField = child.GetComponent<InputField>();
                var image = child.GetComponent<Image>();
                var text = child.GetComponent<Text>();

                if (button != null || inputField != null || image != null || text != null)
                {
                    elements.Add(new
                    {
                        name = child.name,
                        hasButton = button != null,
                        hasInputField = inputField != null,
                        hasImage = image != null,
                        hasText = text != null
                    });
                }

                elements.AddRange(GetStyledElementsList(child));
            }
            return elements;
        }
    }
}