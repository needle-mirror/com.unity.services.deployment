using System.Collections.Generic;
using System.ComponentModel;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
#if UNITY_6000_3_OR_NEWER
using UnityEngine;
#endif

namespace Unity.Services.Deployment.Editor.Interface.UI.Inspector
{
    /// <summary>
    /// Lookup table for <see cref="UnityEditor.Editor"/> instances that need to be redrawn based on a <see cref="IDeploymentItem"/> paths.<br />
    /// The class registers to the <see cref="IDeploymentItem.PropertyChanged"/> event to trigger a redraw of the editor when the status of a deployment item changes.
    /// </summary>
    class DeploymentItemEditorLookup
    {
#if UNITY_6000_3_OR_NEWER
        readonly Dictionary<string, HashSet<EntityId>> m_EditorLookup = new();
#else
        readonly Dictionary<string, HashSet<int>> m_EditorLookup = new();
#endif
        /// <summary>
        /// Reverse lookup from a deployment item to the path it was registered under.
        /// This is needed to find the correct editor entry when an item's path changes.
        /// </summary>
        readonly Dictionary<IDeploymentItem, string> m_ItemToRegisteredPath = new();

        readonly DeploymentItemsLookup m_DeploymentItemsLookup;

        public DeploymentItemEditorLookup(DeploymentItemsLookup deploymentItemsLookup)
        {
            m_DeploymentItemsLookup = deploymentItemsLookup;
        }

        /// <summary>
        /// Registers the given <see cref="UnityEditor.Editor"/> for redraw when the status of any <see cref="IDeploymentItem"/> at the specified file paths changes.<br />
        /// </summary>
        /// <param name="editor">The editor instance to be redrawn on <see cref="DeploymentItem"/> changes.</param>
        /// <param name="filePaths">The paths currently displayed by the editor.</param>
        public void RegisterEditorForRedraw(UnityEditor.Editor editor, HashSet<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!m_EditorLookup.TryGetValue(filePath, out var editors))
                {
#if UNITY_6000_3_OR_NEWER
                    editors = new HashSet<EntityId>();
#else
                    editors = new HashSet<int>();
#endif
                    m_EditorLookup.Add(filePath, editors);

                    if (m_DeploymentItemsLookup.TryGetDeploymentItemsAtPath(filePath, out var items))
                    {
                        foreach (var item in items)
                        {
                            if (item != null)
                            {
                                item.PropertyChanged += TriggerEditorRedraw;
                                m_ItemToRegisteredPath[item] = filePath;
                            }
                        }
                    }
                }

#if UNITY_6000_3_OR_NEWER
                editors.Add(editor.GetEntityId());
#else
                editors.Add(editor.GetInstanceID());
#endif
            }
        }

        /// <summary>
        /// Cleans up the editor lookup table by removing any editors that are no longer valid instances
        /// and unregister the <see cref="IDeploymentItem.PropertyChanged"/> event
        /// for paths that no longer have any editors drawing them.
        /// </summary>
        public void CleanupEditors()
        {
            var keysToRemove = new List<string>();
            foreach (var (path, editorIds) in m_EditorLookup)
            {
                // If the path is not a valid deployment item anymore, it can be safely removed.
                if (!m_DeploymentItemsLookup.TryGetDeploymentItemsAtPath(path, out var deploymentItems))
                {
                    RemoveItemsRegisteredAtPath(path);
                    keysToRemove.Add(path);
                    continue;
                }

                // Remove any editor that is no longer valid.
#if UNITY_6000_3_OR_NEWER
                editorIds.RemoveWhere(editorId => EditorUtility.EntityIdToObject(editorId) == null);
#else
                editorIds.RemoveWhere(editorId => EditorUtility.InstanceIDToObject(editorId) == null);
#endif

                // If there are no more editors for this path, we can remove it and unregister its callback.
                var hasRemainingEditors = editorIds.Count > 0;
                if (!hasRemainingEditors)
                {
                    foreach (var deploymentItem in deploymentItems)
                    {
                        if (deploymentItem != null)
                        {
                            deploymentItem.PropertyChanged -= TriggerEditorRedraw;
                            m_ItemToRegisteredPath.Remove(deploymentItem);
                        }
                    }

                    keysToRemove.Add(path);
                }
            }

            foreach (var key in keysToRemove)
            {
                m_EditorLookup.Remove(key);
            }
        }

        /// <summary>
        /// Triggers an Editor redraw for all editors registered to the <see cref="IDeploymentItem"/>.
        /// </summary>
        /// <remarks>
        /// This method is registered to the <see cref="IDeploymentItem.PropertyChanged"/> event.<br/>
        /// When the <see cref="IDeploymentItem.Path"/> property changes, the editor lookup may still
        /// reference the old path. In that case, we fall back to the previously registered path
        /// to find and repaint the correct editors, then migrate the registration to the new path.
        /// </remarks>
        void TriggerEditorRedraw(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not IDeploymentItem deploymentItem)
                return;
            var currentPath = deploymentItem.Path;
            var hasCurrentPath = !string.IsNullOrEmpty(currentPath);
            var isPathChange = e.PropertyName == nameof(IDeploymentItem.Path);
#if UNITY_6000_3_OR_NEWER
    HashSet<EntityId> editorList = null;
#else
    HashSet<int> editorList = null;
#endif

            // Try the current path first.
            var foundAtCurrentPath = hasCurrentPath
                && m_EditorLookup.TryGetValue(currentPath, out editorList);
            if (foundAtCurrentPath)
            {
                RepaintEditors(editorList);
                if (isPathChange)
                    m_ItemToRegisteredPath[deploymentItem] = currentPath;
                return;
            }

            // Current path not found in lookup -- the item may have been moved.
            // Fall back to the path the item was originally registered under.
            var hasRegisteredPath = m_ItemToRegisteredPath.TryGetValue(deploymentItem, out var registeredPath)
                && !string.IsNullOrEmpty(registeredPath);
            var foundAtRegisteredPath = hasRegisteredPath
                && m_EditorLookup.TryGetValue(registeredPath, out editorList);
            if (foundAtRegisteredPath)
            {
                RepaintEditors(editorList);
                CopyEditorRegistrations(deploymentItem, registeredPath, currentPath, editorList);
            }
        }

        /// <summary>
        /// Copies editor registrations from the old path to the new path so that subsequent
        /// property changes (e.g. VariantTags) find the correct editors.<br/>
        /// The old path key is intentionally kept: other items sharing the same old path
        /// may still need it. <see cref="CleanupEditors"/> will remove it once it becomes stale.
        /// </summary>
#if UNITY_6000_3_OR_NEWER
        void CopyEditorRegistrations(IDeploymentItem item, string oldPath, string newPath, HashSet<EntityId> sourceEditors)
#else
        void CopyEditorRegistrations(IDeploymentItem item, string oldPath, string newPath, HashSet<int> sourceEditors)
#endif
        {
            if (string.IsNullOrEmpty(newPath) || newPath == oldPath)
                return;

            if (!m_EditorLookup.TryGetValue(newPath, out var targetEditors))
            {
#if UNITY_6000_3_OR_NEWER
                targetEditors = new HashSet<EntityId>(sourceEditors);
#else
                targetEditors = new HashSet<int>(sourceEditors);
#endif
                m_EditorLookup[newPath] = targetEditors;
            }
            else
            {
                foreach (var id in sourceEditors)
                    targetEditors.Add(id);
            }

            m_ItemToRegisteredPath[item] = newPath;
        }

        /// <summary>
        /// Removes all <see cref="m_ItemToRegisteredPath"/> entries pointing to the given path
        /// and unsubscribes their <see cref="IDeploymentItem.PropertyChanged"/> handler.
        /// </summary>
        void RemoveItemsRegisteredAtPath(string path)
        {
            List<IDeploymentItem> itemsToRemove = null;
            foreach (var kvp in m_ItemToRegisteredPath)
            {
                if (kvp.Value == path)
                {
                    kvp.Key.PropertyChanged -= TriggerEditorRedraw;
                    itemsToRemove ??= new List<IDeploymentItem>();
                    itemsToRemove.Add(kvp.Key);
                }
            }

            if (itemsToRemove != null)
            {
                foreach (var item in itemsToRemove)
                    m_ItemToRegisteredPath.Remove(item);
            }
        }

#if UNITY_6000_3_OR_NEWER
        static void RepaintEditors(HashSet<EntityId> editorList)
#else
        static void RepaintEditors(HashSet<int> editorList)
#endif
        {
            foreach (var editorId in editorList)
            {
#if UNITY_6000_3_OR_NEWER
                var editorObject = EditorUtility.EntityIdToObject(editorId) as UnityEditor.Editor;
#else
                var editorObject = EditorUtility.InstanceIDToObject(editorId) as UnityEditor.Editor;
#endif
                if (editorObject != null)
                    editorObject.Repaint();
            }
        }
    }
}
