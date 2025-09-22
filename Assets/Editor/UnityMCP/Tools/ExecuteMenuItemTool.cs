using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Tool for executing Unity menu items based on GameLovers MCP architecture
    /// </summary>
    public class ExecuteMenuItemTool : McpToolBase
    {
        public ExecuteMenuItemTool()
        {
            Name = "execute_menu_item";
            Description = "Executes a Unity menu item by path (e.g. 'GameObject/Create Empty', 'File/New Scene')";
            IsAsync = true;
        }

        public override void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Extract menu path parameter
                    string menuPath = parameters["menuPath"]?.ToObject<string>();

                    if (string.IsNullOrEmpty(menuPath))
                    {
                        tcs.SetResult(CreateErrorResponse("Parameter 'menuPath' is required.", "validation_error"));
                        return;
                    }

                    // Validate menu item exists
                    if (!Menu.GetEnabled(menuPath))
                    {
                        // Check if menu exists but is disabled
                        try
                        {
                            EditorApplication.ExecuteMenuItem(menuPath);
                            // If we get here, menu exists but might be disabled
                        }
                        catch (Exception ex)
                        {
                            tcs.SetResult(CreateErrorResponse($"Menu item '{menuPath}' does not exist or is not available. Error: {ex.Message}", "menu_not_found"));
                            return;
                        }
                    }

                    // Execute the menu item
                    try
                    {
                        bool success = EditorApplication.ExecuteMenuItem(menuPath);

                        if (success)
                        {
                            Debug.Log($"[UnityMCP] Successfully executed menu item: {menuPath}");

                            var data = new
                            {
                                menuPath = menuPath,
                                executed = true,
                                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                            };

                            tcs.SetResult(CreateSuccessResponse($"Successfully executed menu item '{menuPath}'", data));
                        }
                        else
                        {
                            tcs.SetResult(CreateErrorResponse($"Failed to execute menu item '{menuPath}'. The menu item returned false.", "execution_failed"));
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.SetResult(CreateErrorResponse($"Exception occurred while executing menu item '{menuPath}': {ex.Message}", "execution_exception"));
                    }
                }
                catch (Exception e)
                {
                    tcs.SetResult(CreateErrorResponse($"Failed to execute menu item: {e.Message}", "menu_error"));
                }
            };
        }
    }
}