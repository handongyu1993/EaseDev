using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for creating UI elements in Unity using GameLovers MCP architecture
    /// Properly creates UI components with correct hierarchies and components
    /// </summary>
    public class CreateUIElementTool : McpToolBase
    {
        public CreateUIElementTool()
        {
            Name = "create_ui_element";
            Description = "Creates UI elements like Canvas, Button, Text, Image, InputField etc. in the Unity scene with proper components";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                    string elementType = parameters["elementType"]?.ToObject<string>() ?? "Canvas";
                    string elementName = parameters["name"]?.ToObject<string>() ?? elementType;
                    string parentPath = parameters["parentPath"]?.ToObject<string>();
                    string text = parameters["text"]?.ToObject<string>();

                    GameObject createdElement = null;
                    GameObject parentObject = null;

                    // Find parent if specified
                    if (!string.IsNullOrEmpty(parentPath))
                    {
                        parentObject = GameObjectHierarchyCreator.FindOrCreateHierarchicalGameObject(parentPath);
                    }

                    // Create UI element based on type
                    switch (elementType.ToLower())
                    {
                        case "canvas":
                            createdElement = CreateCanvas(elementName);
                            break;
                        case "button":
                            createdElement = CreateButton(elementName, parentObject, text);
                            break;
                        case "text":
                            createdElement = CreateText(elementName, parentObject, text);
                            break;
                        case "image":
                            createdElement = CreateImage(elementName, parentObject);
                            break;
                        case "panel":
                            createdElement = CreatePanel(elementName, parentObject);
                            break;
                        case "inputfield":
                            createdElement = CreateInputField(elementName, parentObject, text);
                            break;
                        case "scrollview":
                            createdElement = CreateScrollView(elementName, parentObject);
                            break;
                        default:
                            tcs.SetResult(CreateErrorResponse($"Unsupported UI element type: {elementType}. Supported types: Canvas, Button, Text, Image, Panel, InputField, ScrollView", "unsupported_element"));
                            return;
                    }

                    if (createdElement == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"Failed to create UI element of type '{elementType}'", "creation_failed"));
                        return;
                    }

                    // Apply position and size if specified
                    ApplyRectTransformProperties(createdElement, parameters);

                    // Mark as dirty for saving
                    EditorUtility.SetDirty(createdElement);

                    // Log the action
                    Debug.Log($"[UnityMCP] Created UI element '{createdElement.name}' of type '{elementType}' with proper components");

                    var data = new
                    {
                        elementName = createdElement.name,
                        elementType = elementType,
                        instanceId = createdElement.GetInstanceID(),
                        hasRectTransform = createdElement.GetComponent<RectTransform>() != null,
                        parentName = createdElement.transform.parent?.name,
                        path = GameObjectHierarchyCreator.GetGameObjectPath(createdElement),
                        components = GetComponentList(createdElement)
                    };

                    tcs.SetResult(CreateSuccessResponse($"Successfully created UI element '{createdElement.name}' of type '{elementType}' with components", data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to create UI element: {e.Message}", "ui_error"));
                }
            };
        }

        private GameObject CreateCanvas(string name)
        {
            // Create Canvas with proper components
            GameObject canvasGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(canvasGO, $"Create {name}");

            // Add Canvas component
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            // Add CanvasScaler component
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            // Add GraphicRaycaster component
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create EventSystem if it doesn't exist
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            return canvasGO;
        }

        private GameObject CreateButton(string name, GameObject parent, string text)
        {
            // Ensure we have a canvas
            Canvas canvas = EnsureCanvas(parent);

            // Create Button GameObject
            GameObject buttonGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(buttonGO, $"Create {name}");

            // Add RectTransform and set parent
            var rectTransform = buttonGO.AddComponent<RectTransform>();
            if (parent != null)
            {
                buttonGO.transform.SetParent(parent.transform, false);
            }
            else if (canvas != null)
            {
                buttonGO.transform.SetParent(canvas.transform, false);
            }

            // Set default button size and position
            rectTransform.sizeDelta = new Vector2(160f, 30f);
            rectTransform.anchoredPosition = Vector2.zero;

            // Add Image component for button background
            var image = buttonGO.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            // Add Button component
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // Create Text child for button text
            GameObject textGO = new GameObject("Text");
            Undo.RegisterCreatedObjectUndo(textGO, "Create Button Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = !string.IsNullOrEmpty(text) ? text : "Button";
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleCenter;

            return buttonGO;
        }

        private GameObject CreateText(string name, GameObject parent, string text)
        {
            Canvas canvas = EnsureCanvas(parent);

            GameObject textGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(textGO, $"Create {name}");

            var rectTransform = textGO.AddComponent<RectTransform>();
            if (parent != null)
            {
                textGO.transform.SetParent(parent.transform, false);
            }
            else if (canvas != null)
            {
                textGO.transform.SetParent(canvas.transform, false);
            }

            rectTransform.sizeDelta = new Vector2(160f, 30f);

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = !string.IsNullOrEmpty(text) ? text : "New Text";
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleLeft;

            return textGO;
        }

        private GameObject CreateImage(string name, GameObject parent)
        {
            Canvas canvas = EnsureCanvas(parent);

            GameObject imageGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(imageGO, $"Create {name}");

            var rectTransform = imageGO.AddComponent<RectTransform>();
            if (parent != null)
            {
                imageGO.transform.SetParent(parent.transform, false);
            }
            else if (canvas != null)
            {
                imageGO.transform.SetParent(canvas.transform, false);
            }

            rectTransform.sizeDelta = new Vector2(100f, 100f);

            var image = imageGO.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.color = Color.white;

            return imageGO;
        }

        private GameObject CreatePanel(string name, GameObject parent)
        {
            Canvas canvas = EnsureCanvas(parent);

            GameObject panelGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(panelGO, $"Create {name}");

            var rectTransform = panelGO.AddComponent<RectTransform>();
            if (parent != null)
            {
                panelGO.transform.SetParent(parent.transform, false);
            }
            else if (canvas != null)
            {
                panelGO.transform.SetParent(canvas.transform, false);
            }

            // Full screen panel by default
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var image = panelGO.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.color = new Color(1f, 1f, 1f, 0.392f);

            return panelGO;
        }

        private GameObject CreateInputField(string name, GameObject parent, string placeholder)
        {
            Canvas canvas = EnsureCanvas(parent);

            GameObject inputFieldGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(inputFieldGO, $"Create {name}");

            var rectTransform = inputFieldGO.AddComponent<RectTransform>();
            if (parent != null)
            {
                inputFieldGO.transform.SetParent(parent.transform, false);
            }
            else if (canvas != null)
            {
                inputFieldGO.transform.SetParent(canvas.transform, false);
            }

            rectTransform.sizeDelta = new Vector2(160f, 30f);

            var image = inputFieldGO.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var inputField = inputFieldGO.AddComponent<InputField>();

            // Create Placeholder text
            GameObject placeholderGO = new GameObject("Placeholder");
            Undo.RegisterCreatedObjectUndo(placeholderGO, "Create Placeholder");
            placeholderGO.transform.SetParent(inputFieldGO.transform, false);

            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -7);

            var placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.text = !string.IsNullOrEmpty(placeholder) ? placeholder : "Enter text...";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            placeholderText.alignment = TextAnchor.MiddleLeft;

            // Create Text component for actual text
            GameObject textGO = new GameObject("Text");
            Undo.RegisterCreatedObjectUndo(textGO, "Create Input Text");
            textGO.transform.SetParent(inputFieldGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -7);

            var inputText = textGO.AddComponent<Text>();
            inputText.text = "";
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 14;
            inputText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            inputText.alignment = TextAnchor.MiddleLeft;

            // Setup InputField
            inputField.targetGraphic = image;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;

            return inputFieldGO;
        }

        private GameObject CreateScrollView(string name, GameObject parent)
        {
            Canvas canvas = EnsureCanvas(parent);

            GameObject scrollViewGO = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(scrollViewGO, $"Create {name}");

            var rectTransform = scrollViewGO.AddComponent<RectTransform>();
            if (parent != null)
            {
                scrollViewGO.transform.SetParent(parent.transform, false);
            }
            else if (canvas != null)
            {
                scrollViewGO.transform.SetParent(canvas.transform, false);
            }

            rectTransform.sizeDelta = new Vector2(200f, 200f);

            var image = scrollViewGO.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.color = Color.white;

            var scrollRect = scrollViewGO.AddComponent<ScrollRect>();

            // Create Viewport
            GameObject viewportGO = new GameObject("Viewport");
            Undo.RegisterCreatedObjectUndo(viewportGO, "Create Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);

            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");

            // Create Content
            GameObject contentGO = new GameObject("Content");
            Undo.RegisterCreatedObjectUndo(contentGO, "Create Content");
            contentGO.transform.SetParent(viewportGO.transform, false);

            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);
            contentRect.anchoredPosition = Vector2.zero;

            // Setup ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return scrollViewGO;
        }

        private Canvas EnsureCanvas(GameObject parent)
        {
            Canvas canvas = parent?.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    // Create a canvas if none exists
                    var canvasGO = CreateCanvas("Canvas");
                    canvas = canvasGO.GetComponent<Canvas>();
                }
            }
            return canvas;
        }

        private void ApplyRectTransformProperties(GameObject element, JObject parameters)
        {
            var rectTransform = element.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Apply position if specified
            if (parameters["position"] != null)
            {
                var pos = parameters["position"];
                rectTransform.anchoredPosition = new Vector2(
                    pos["x"]?.ToObject<float>() ?? rectTransform.anchoredPosition.x,
                    pos["y"]?.ToObject<float>() ?? rectTransform.anchoredPosition.y
                );
            }

            // Apply size if specified
            if (parameters["size"] != null)
            {
                var size = parameters["size"];
                rectTransform.sizeDelta = new Vector2(
                    size["width"]?.ToObject<float>() ?? rectTransform.sizeDelta.x,
                    size["height"]?.ToObject<float>() ?? rectTransform.sizeDelta.y
                );
            }
        }

        private string[] GetComponentList(GameObject obj)
        {
            var components = obj.GetComponents<Component>();
            var componentNames = new string[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                componentNames[i] = components[i].GetType().Name;
            }
            return componentNames;
        }
    }
}