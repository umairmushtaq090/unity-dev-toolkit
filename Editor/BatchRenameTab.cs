using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevToolkit.Editor
{
    /// <summary>
    /// Renames selected GameObjects (Hierarchy) or assets (Project window) in one go,
    /// with prefix / suffix / find-replace / numbering and a live preview.
    /// </summary>
    [Serializable]
    public class BatchRenameTab
    {
        [SerializeField] bool useBaseName;
        [SerializeField] string baseName = "Item";
        [SerializeField] string find = "";
        [SerializeField] string replace = "";
        [SerializeField] string prefix = "";
        [SerializeField] string suffix = "";
        [SerializeField] bool numbering;
        [SerializeField] string separator = "_";
        [SerializeField] int startNumber = 1;
        [SerializeField] int padding = 2;
        Vector2 scroll;

        public void OnGUI()
        {
            var targets = GetTargets(out bool areAssets);

            EditorGUILayout.HelpBox(
                "Select GameObjects in the Hierarchy or assets in the Project window, set the options, check the preview, then click Rename.",
                MessageType.Info);

            useBaseName = EditorGUILayout.ToggleLeft("Replace whole name with base name", useBaseName);
            using (new EditorGUI.DisabledScope(!useBaseName))
                baseName = EditorGUILayout.TextField("Base name", baseName);

            find = EditorGUILayout.TextField("Find", find);
            replace = EditorGUILayout.TextField("Replace with", replace);
            prefix = EditorGUILayout.TextField("Prefix", prefix);
            suffix = EditorGUILayout.TextField("Suffix", suffix);

            numbering = EditorGUILayout.ToggleLeft("Add numbering", numbering);
            using (new EditorGUI.DisabledScope(!numbering))
            {
                separator = EditorGUILayout.TextField("Separator", separator);
                startNumber = EditorGUILayout.IntField("Start at", startNumber);
                padding = EditorGUILayout.IntSlider("Digits", padding, 1, 6);
            }

            EditorGUILayout.Space(6);
            string kind = areAssets ? "asset(s)" : "GameObject(s)";
            EditorGUILayout.LabelField($"Preview — {targets.Count} {kind} selected", EditorStyles.boldLabel);

            if (targets.Count == 0)
            {
                EditorGUILayout.HelpBox("Nothing selected.", MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(80));
            for (int i = 0; i < targets.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(targets[i].name, GUILayout.MinWidth(100));
                    GUILayout.Label("→", GUILayout.Width(18));
                    EditorGUILayout.LabelField(BuildName(targets[i].name, i), EditorStyles.boldLabel, GUILayout.MinWidth(100));
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button($"Rename {targets.Count} {kind}", GUILayout.Height(26)))
            {
                Apply(targets, areAssets);
                GUIUtility.ExitGUI();
            }
        }

        string BuildName(string original, int index)
        {
            string name = useBaseName && !string.IsNullOrEmpty(baseName) ? baseName : original;

            if (!string.IsNullOrEmpty(find))
                name = name.Replace(find, replace ?? "");

            name = prefix + name + suffix;

            if (numbering)
                name += separator + (startNumber + index).ToString().PadLeft(padding, '0');

            return name;
        }

        void Apply(List<Object> targets, bool areAssets)
        {
            if (areAssets)
            {
                int errors = 0;
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        string path = AssetDatabase.GetAssetPath(targets[i]);
                        string error = AssetDatabase.RenameAsset(path, BuildName(targets[i].name, i));
                        if (!string.IsNullOrEmpty(error))
                        {
                            errors++;
                            Debug.LogError($"[Dev Toolkit] Could not rename '{path}': {error}");
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                }
                Debug.Log($"[Dev Toolkit] Renamed {targets.Count - errors} asset(s).");
            }
            else
            {
                // Compute all names first so numbering is stable, then apply with Undo support.
                var newNames = targets.Select((t, i) => BuildName(t.name, i)).ToList();
                Undo.RecordObjects(targets.ToArray(), "Batch Rename");
                for (int i = 0; i < targets.Count; i++)
                    targets[i].name = newNames[i];
                Debug.Log($"[Dev Toolkit] Renamed {targets.Count} GameObject(s). Use Ctrl/Cmd+Z to undo.");
            }
        }

        /// <summary>Scene objects take priority; otherwise use selected project assets.</summary>
        static List<Object> GetTargets(out bool areAssets)
        {
            var sceneObjects = Selection.gameObjects
                .Where(go => !EditorUtility.IsPersistent(go))
                .ToList();

            if (sceneObjects.Count > 0)
            {
                areAssets = false;
                sceneObjects.Sort(CompareHierarchyOrder);
                return sceneObjects.Cast<Object>().ToList();
            }

            areAssets = true;
            return Selection.objects
                .Where(o => o != null && AssetDatabase.Contains(o) && AssetDatabase.IsMainAsset(o)
                            && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(o)))
                .OrderBy(o => o.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Sorts GameObjects in the order they appear in the Hierarchy window.</summary>
        static int CompareHierarchyOrder(GameObject a, GameObject b)
        {
            int sceneCmp = a.scene.handle.CompareTo(b.scene.handle);
            if (sceneCmp != 0) return sceneCmp;

            var pa = SiblingPath(a.transform);
            var pb = SiblingPath(b.transform);
            for (int i = 0; i < Mathf.Min(pa.Count, pb.Count); i++)
            {
                int c = pa[i].CompareTo(pb[i]);
                if (c != 0) return c;
            }
            return pa.Count.CompareTo(pb.Count);
        }

        static List<int> SiblingPath(Transform t)
        {
            var path = new List<int>();
            while (t != null)
            {
                path.Insert(0, t.GetSiblingIndex());
                t = t.parent;
            }
            return path;
        }
    }
}
