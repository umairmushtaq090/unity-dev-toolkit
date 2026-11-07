using UnityEditor;
using UnityEngine;

namespace DevToolkit.Editor
{
    /// <summary>
    /// Main window. Open it from Tools > Dev Toolkit.
    /// </summary>
    public class DevToolkitWindow : EditorWindow
    {
        static readonly string[] Tabs = { "Missing Scripts", "Batch Rename", "Find References" };

        [SerializeField] int selectedTab;
        [SerializeField] MissingScriptsTab missingScripts = new MissingScriptsTab();
        [SerializeField] BatchRenameTab batchRename = new BatchRenameTab();
        [SerializeField] FindReferencesTab findReferences = new FindReferencesTab();

        [MenuItem("Tools/Dev Toolkit", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<DevToolkitWindow>("Dev Toolkit");
            window.minSize = new Vector2(420, 320);
            window.Show();
        }

        void OnSelectionChange() => Repaint();

        void OnGUI()
        {
            EditorGUILayout.Space(4);
            selectedTab = GUILayout.Toolbar(selectedTab, Tabs, GUILayout.Height(24));
            EditorGUILayout.Space(6);

            switch (selectedTab)
            {
                case 0: missingScripts.OnGUI(); break;
                case 1: batchRename.OnGUI(); break;
                case 2: findReferences.OnGUI(); break;
            }
        }
    }
}
