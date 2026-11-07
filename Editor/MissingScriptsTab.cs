using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DevToolkit.Editor
{
    /// <summary>
    /// Finds and removes "Missing (Mono Script)" components in open scenes and project prefabs.
    /// </summary>
    [Serializable]
    public class MissingScriptsTab
    {
        [Serializable]
        class Entry
        {
            public string label;
            public string prefabPath;     // set for prefab assets
            public GameObject sceneObject; // set for scene objects
            public int count;
        }

        [SerializeField] bool includeOpenScenes = true;
        [SerializeField] bool includePrefabs = true;
        [SerializeField] List<Entry> results = new List<Entry>();
        [SerializeField] bool hasScanned;
        Vector2 scroll;

        public void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Scans for components whose script is missing (\"Missing (Mono Script)\") and lets you remove them safely.",
                MessageType.Info);

            includeOpenScenes = EditorGUILayout.ToggleLeft("Open scenes (including inactive objects)", includeOpenScenes);
            includePrefabs = EditorGUILayout.ToggleLeft("All prefabs in Assets/", includePrefabs);

            if (GUILayout.Button("Scan", GUILayout.Height(26)))
                Scan();

            if (!hasScanned) return;

            EditorGUILayout.Space(4);
            int total = 0;
            foreach (var e in results) total += e.count;

            if (results.Count == 0)
            {
                EditorGUILayout.HelpBox("No missing scripts found. Nice and clean!", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"Found {total} missing script(s) on {results.Count} object(s)/prefab(s)", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < results.Count; i++)
            {
                var e = results[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    string icon = e.prefabPath != null && e.prefabPath.Length > 0 ? "[Prefab] " : "[Scene] ";
                    EditorGUILayout.LabelField(icon + e.label, GUILayout.MinWidth(150));
                    GUILayout.Label($"x{e.count}", GUILayout.Width(35));

                    if (GUILayout.Button("Select", GUILayout.Width(55)))
                        SelectEntry(e);

                    if (GUILayout.Button("Fix", GUILayout.Width(40)))
                    {
                        FixEntry(e);
                        results.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Remove ALL missing scripts", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Remove missing scripts",
                        $"Remove {total} missing script component(s)?\n\nScene changes can be undone. Prefab changes are saved to disk.",
                        "Remove", "Cancel"))
                {
                    int removed = 0;
                    foreach (var e in results) removed += FixEntry(e);
                    results.Clear();
                    Debug.Log($"[Dev Toolkit] Removed {removed} missing script component(s).");
                    GUIUtility.ExitGUI();
                }
            }
        }

        void Scan()
        {
            results.Clear();
            hasScanned = true;

            try
            {
                if (includeOpenScenes)
                {
                    for (int s = 0; s < SceneManager.sceneCount; s++)
                    {
                        var scene = SceneManager.GetSceneAt(s);
                        if (!scene.isLoaded) continue;

                        foreach (var root in scene.GetRootGameObjects())
                        foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        {
                            int c = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                            if (c > 0)
                                results.Add(new Entry { label = $"{scene.name}: {GetHierarchyPath(t)}", sceneObject = t.gameObject, count = c });
                        }
                    }
                }

                if (includePrefabs)
                {
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                        if (EditorUtility.DisplayCancelableProgressBar("Scanning prefabs", path, (float)i / guids.Length))
                            break;

                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) continue;

                        int c = 0;
                        foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                            c += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);

                        if (c > 0)
                            results.Add(new Entry { label = path, prefabPath = path, count = c });
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void SelectEntry(Entry e)
        {
            Object target = e.sceneObject != null
                ? (Object)e.sceneObject
                : AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);

            if (target == null)
            {
                Debug.LogWarning("[Dev Toolkit] Object no longer exists. Please scan again.");
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        static int FixEntry(Entry e)
        {
            try
            {
                if (e.sceneObject != null)
                {
                    Undo.RegisterCompleteObjectUndo(e.sceneObject, "Remove Missing Scripts");
                    int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(e.sceneObject);
                    if (n > 0) EditorSceneManager.MarkSceneDirty(e.sceneObject.scene);
                    return n;
                }

                if (!string.IsNullOrEmpty(e.prefabPath))
                    return FixPrefab(e.prefabPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Dev Toolkit] Could not fix '{e.label}': {ex.Message}");
            }
            return 0;
        }

        static int FixPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            int removed = 0;
            try
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

                if (removed > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return removed;
        }

        static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
