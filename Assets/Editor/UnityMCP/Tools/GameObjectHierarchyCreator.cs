using System;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Tools
{
    /// <summary>
    /// Utility class for creating GameObject hierarchies based on path
    /// Ported from GameLovers MCP Unity implementation
    /// </summary>
    public static class GameObjectHierarchyCreator
    {
        /// <summary>
        /// Find or create a GameObject at the specified hierarchical path
        /// </summary>
        /// <param name="path">Path like "Canvas/LoginPanel/UsernameInput"</param>
        /// <returns>The found or created GameObject</returns>
        public static GameObject FindOrCreateHierarchicalGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("GameObject path cannot be null or empty.", nameof(path));
            }

            path = path.Trim('/');
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("GameObject path cannot consist only of slashes.", nameof(path));
            }

            string[] parts = path.Split('/');
            GameObject currentParent = null;
            GameObject foundOrCreatedObject = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string name = parts[i];
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"Invalid path: empty segment at part {i + 1} in path '{path}'. Ensure segments are not empty.");
                }

                Transform childTransform;
                if (currentParent == null)
                {
                    // Search for root GameObject
                    GameObject rootObj = GameObject.Find(name);
                    childTransform = rootObj?.transform;
                }
                else
                {
                    // Search for child GameObject
                    childTransform = currentParent.transform.Find(name);
                }

                if (childTransform == null)
                {
                    // Create new GameObject
                    GameObject newObj = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(newObj, $"Create {name}");

                    if (currentParent != null)
                    {
                        newObj.transform.SetParent(currentParent.transform, false);
                    }

                    foundOrCreatedObject = newObj;
                    currentParent = newObj;
                }
                else
                {
                    // Use existing GameObject
                    foundOrCreatedObject = childTransform.gameObject;
                    currentParent = foundOrCreatedObject;
                }
            }

            if (foundOrCreatedObject == null)
            {
                throw new InvalidOperationException($"Failed to find or create GameObject for path '{path}'. This indicates an unexpected state.");
            }

            return foundOrCreatedObject;
        }

        /// <summary>
        /// Get the hierarchy path of a GameObject
        /// </summary>
        /// <param name="obj">The GameObject to get the path for</param>
        /// <returns>Hierarchy path as string</returns>
        public static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return null;
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path.TrimStart('/');
        }
    }
}