using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevToolkit.Editor
{
    /// <summary>
    /// Finds every asset in Assets/ (scenes, prefabs, materials, ScriptableObjects...) that directly uses a given asset.
    /// Handy before deleting or replacing something.
    /// </summary>
    [Serializable]
    public class FindReferencesTab
    {
        [SerializeField] Object target;
        [SerializeField] List<string> results = new List<string>();
        [SerializeField] string searchedPath;
        Vector2 scroll;

        public void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Pick any asset to see which scenes, prefabs, materials and other assets reference it. Useful before deleting an asset.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                target = EditorGUILayout.ObjectField("Asset", target, typeof(Object), false);
                if (GUILayout.Button("Use Selection", GUILayout.Width(100)) && Selection.activeObject != null
                    && AssetDatabase.Contains(Selection.activeObject))
                {
                    target = Selection.activeObject;
                }
            }

            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Find References", GUILayout.Height(26)))
                    Find();
            }

            if (string.IsNullOrEmpty(searchedPath)) return;

            EditorGUILayout.Space(4);
            if (results.Count == 0)
            {
                EditorGUILayout.HelpBox($"No assets in Assets/ reference:\n{searchedPath}\n\n(It may still be loaded from code via Resources or Addressables.)", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"{results.Count} asset(s) reference {System.IO.Path.GetFileName(searchedPath)}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var path in results)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var icon = AssetDatabase.GetCachedIcon(path);
                    GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));
                    EditorGUILayout.LabelField(path);
                    if (GUILayout.Button("Ping", GUILayout.Width(45)))
                    {
                        var obj = AssetDatabase.LoadMainAssetAtPath(path);
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Find()
        {
            results.Clear();
            searchedPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(searchedPath))
            {
                Debug.LogWarning("[Dev Toolkit] Please pick an asset from the Project window.");
                return;
            }

            var candidates = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)
                            && p != searchedPath
                            && !AssetDatabase.IsValidFolder(p))
                .ToArray();

            try
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (i % 25 == 0 &&
                        EditorUtility.DisplayCancelableProgressBar("Finding references", candidates[i], (float)i / candidates.Length))
                        break;

                    var deps = AssetDatabase.GetDependencies(candidates[i], false);
                    if (Array.IndexOf(deps, searchedPath) >= 0)
                        results.Add(candidates[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
