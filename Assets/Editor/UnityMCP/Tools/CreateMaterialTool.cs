using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for creating and managing materials in Unity based on GameLovers MCP architecture
    /// </summary>
    public class CreateMaterialTool : McpToolBase
    {
        public CreateMaterialTool()
        {
            Name = "create_material";
            Description = "Creates materials with specified properties and optionally applies them to GameObjects";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract parameters
                    string materialName = parameters["materialName"]?.ToObject<string>() ?? "NewMaterial";
                    string materialPath = parameters["materialPath"]?.ToObject<string>();
                    string shaderName = parameters["shaderName"]?.ToObject<string>() ?? "Universal Render Pipeline/Lit";

                    // Color parameters
                    var colorParam = parameters["color"];
                    Color materialColor = Color.white;
                    if (colorParam != null)
                    {
                        materialColor = new Color(
                            colorParam["r"]?.ToObject<float>() ?? 1f,
                            colorParam["g"]?.ToObject<float>() ?? 1f,
                            colorParam["b"]?.ToObject<float>() ?? 1f,
                            colorParam["a"]?.ToObject<float>() ?? 1f
                        );
                    }

                    // Other material properties
                    float metallic = parameters["metallic"]?.ToObject<float>() ?? 0f;
                    float smoothness = parameters["smoothness"]?.ToObject<float>() ?? 0.5f;
                    float emission = parameters["emission"]?.ToObject<float>() ?? 0f;

                    // GameObject to apply material to
                    string targetObjectName = parameters["targetObjectName"]?.ToObject<string>();
                    int? targetInstanceId = parameters["targetInstanceId"]?.ToObject<int?>();

                    // Create material
                    Material material = new Material(Shader.Find(shaderName));
                    if (material.shader == null)
                    {
                        tcs.SetResult(CreateErrorResponse($"Shader '{shaderName}' not found. Using default shader.", "shader_not_found"));
                        material = new Material(Shader.Find("Standard"));
                    }

                    material.name = materialName;

                    // Set material properties
                    if (material.HasProperty("_BaseColor") || material.HasProperty("_Color"))
                    {
                        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                        material.SetColor(colorProperty, materialColor);
                    }

                    if (material.HasProperty("_Metallic"))
                    {
                        material.SetFloat("_Metallic", metallic);
                    }

                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", smoothness);
                    }

                    if (material.HasProperty("_EmissionColor") && emission > 0)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", materialColor * emission);
                    }

                    // Determine save path
                    string finalPath = "";
                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        finalPath = materialPath;
                        if (!finalPath.EndsWith(".mat"))
                        {
                            finalPath += ".mat";
                        }
                        if (!finalPath.StartsWith("Assets/"))
                        {
                            finalPath = $"Assets/Materials/{finalPath}";
                        }
                    }
                    else
                    {
                        finalPath = $"Assets/Materials/{materialName}.mat";
                    }

                    // Ensure directory exists
                    string directory = System.IO.Path.GetDirectoryName(finalPath);
                    if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                    {
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

                    // Save material as asset
                    AssetDatabase.CreateAsset(material, finalPath);
                    AssetDatabase.Refresh();

                    // Apply material to target object if specified
                    GameObject targetObject = null;
                    if (targetInstanceId.HasValue)
                    {
                        targetObject = EditorUtility.InstanceIDToObject(targetInstanceId.Value) as GameObject;
                    }
                    else if (!string.IsNullOrEmpty(targetObjectName))
                    {
                        targetObject = GameObject.Find(targetObjectName);
                    }

                    if (targetObject != null)
                    {
                        Renderer renderer = targetObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            Undo.RecordObject(renderer, "Apply Material");
                            renderer.material = material;
                            EditorUtility.SetDirty(renderer);
                        }
                        else
                        {
                            Debug.LogWarning($"[UnityMCP] Target object '{targetObject.name}' has no Renderer component to apply material to.");
                        }
                    }

                    Debug.Log($"[UnityMCP] Created material '{materialName}' at path '{finalPath}'");

                    var data = new
                    {
                        materialName = material.name,
                        materialPath = finalPath,
                        shaderName = material.shader.name,
                        color = new { r = materialColor.r, g = materialColor.g, b = materialColor.b, a = materialColor.a },
                        metallic = metallic,
                        smoothness = smoothness,
                        emission = emission,
                        appliedToObject = targetObject?.name
                    };

                    tcs.SetResult(CreateSuccessResponse($"Successfully created material '{materialName}'" + (targetObject != null ? $" and applied to '{targetObject.name}'" : ""), data));
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to create material: {e.Message}", "material_error"));
                }
            };
        }
    }
}