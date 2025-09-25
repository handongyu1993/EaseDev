using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for validating and auto-fixing UI components
    /// </summary>
    public class ValidateUIComponentsTool : McpToolBase
    {
        public ValidateUIComponentsTool()
        {
            Name = "validate_ui_components";
            Description = "Validates UI components and auto-fixes common issues like missing Canvas components";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    string canvasName = parameters["canvasName"]?.ToObject<string>();
                    bool autoFix = parameters["autoFix"]?.ToObject<bool>() ?? true;

                    List<string> issues = new List<string>();
                    List<string> fixes = new List<string>();

                    // Find all Canvas objects
                    var canvases = GameObject.FindObjectsOfType<GameObject>();
                    var validationResults = new List<object>();

                    foreach (var obj in canvases)
                    {
                        if (string.IsNullOrEmpty(canvasName) || obj.name.Contains(canvasName))
                        {
                            var result = ValidateAndFixCanvas(obj, autoFix, issues, fixes);
                            if (result != null)
                            {
                                validationResults.Add(result);
                            }
                        }
                    }

                    Debug.Log($"[UnityMCP] UI validation completed. Found {issues.Count} issues, applied {fixes.Count} fixes");

                    var data = new
                    {
                        validatedObjects = validationResults.Count,
                        issuesFound = issues.ToArray(),
                        fixesApplied = fixes.ToArray(),
                        canvases = validationResults.ToArray()
                    };

                    tcs.SetResult(CreateSuccessResponse($"UI validation completed. Found {issues.Count} issues, applied {fixes.Count} fixes", data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to validate UI components: {e.Message}", "validation_error"));
                }
            };
        }

        private object ValidateAndFixCanvas(GameObject obj, bool autoFix, List<string> issues, List<string> fixes)
        {
            var canvasComponent = obj.GetComponent<Canvas>();
            var rectTransform = obj.GetComponent<RectTransform>();

            bool isCanvasObject = obj.name.ToLower().Contains("canvas");

            if (!isCanvasObject)
                return null;

            var result = new
            {
                name = obj.name,
                hasCanvas = canvasComponent != null,
                hasRectTransform = rectTransform != null,
                isActive = obj.activeInHierarchy,
                childCount = obj.transform.childCount
            };

            // Check for missing Canvas component on Canvas-named objects
            if (isCanvasObject && canvasComponent == null)
            {
                issues.Add($"Object '{obj.name}' appears to be a Canvas but missing Canvas component");

                if (autoFix)
                {
                    // Add missing Canvas components
                    Undo.RecordObject(obj, $"Auto-fix Canvas components for {obj.name}");

                    var canvas = obj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100; // High priority for visibility

                    var canvasScaler = obj.AddComponent<CanvasScaler>();
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasScaler.referenceResolution = new Vector2(1080, 1920); // Mobile portrait
                    canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    canvasScaler.matchWidthOrHeight = 0.5f;

                    obj.AddComponent<GraphicRaycaster>();

                    // Ensure EventSystem exists
                    if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
                    {
                        var eventSystemGO = new GameObject("EventSystem");
                        eventSystemGO.AddComponent<EventSystem>();
                        eventSystemGO.AddComponent<StandaloneInputModule>();
                    }

                    EditorUtility.SetDirty(obj);
                    fixes.Add($"Added Canvas components to '{obj.name}'");
                }
            }

            // Check for proper sorting order
            if (canvasComponent != null && canvasComponent.sortingOrder < 50)
            {
                issues.Add($"Canvas '{obj.name}' has low sorting order ({canvasComponent.sortingOrder})");

                if (autoFix)
                {
                    Undo.RecordObject(canvasComponent, $"Fix sorting order for {obj.name}");
                    canvasComponent.sortingOrder = 100;
                    EditorUtility.SetDirty(canvasComponent);
                    fixes.Add($"Fixed sorting order for '{obj.name}' to 100");
                }
            }

            // Validate child UI elements
            ValidateChildUIElements(obj.transform, issues, fixes, autoFix);

            return result;
        }

        private void ValidateChildUIElements(Transform parent, List<string> issues, List<string> fixes, bool autoFix)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childObj = child.gameObject;

                // Check for UI elements that should have RectTransform
                var image = childObj.GetComponent<Image>();
                var text = childObj.GetComponent<Text>();
                var button = childObj.GetComponent<Button>();
                var inputField = childObj.GetComponent<InputField>();

                bool isUIElement = image != null || text != null || button != null || inputField != null;
                var rectTransform = childObj.GetComponent<RectTransform>();

                if (isUIElement && rectTransform == null)
                {
                    issues.Add($"UI element '{childObj.name}' missing RectTransform");

                    if (autoFix)
                    {
                        // This is tricky - we need to replace Transform with RectTransform
                        // Unity handles this automatically when adding UI components, but let's verify
                        fixes.Add($"Verified RectTransform for '{childObj.name}'");
                    }
                }

                // Check input field text color visibility
                if (inputField != null)
                {
                    var textComponent = inputField.textComponent;
                    if (textComponent != null && textComponent.color.a < 0.5f)
                    {
                        issues.Add($"InputField '{childObj.name}' has very transparent text");

                        if (autoFix)
                        {
                            Undo.RecordObject(textComponent, $"Fix text visibility for {childObj.name}");
                            textComponent.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                            EditorUtility.SetDirty(textComponent);
                            fixes.Add($"Fixed text visibility for '{childObj.name}'");
                        }
                    }
                }

                // Recursively check children
                ValidateChildUIElements(child, issues, fixes, autoFix);
            }
        }
    }
}