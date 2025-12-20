using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JordanTama.Startup.Editor.com.jordantama.startup.Editor
{
    public class ReturnButton
    {
        private const string ELEMENT_PATH = "JordanTama/Return to Previous Scene";

        public ReturnButton()
        {
            SceneManager.activeSceneChanged += (_, _) => { MainToolbar.Refresh(ELEMENT_PATH); };
            // EditorApplication.playModeStateChanged += _ => { MainToolbar.Refresh(ELEMENT_PATH); };
        }
        
        [MainToolbarElement(ELEMENT_PATH, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -101)]
        public static MainToolbarElement CreateButton()
        {
            string path = EditorPrefs.GetString("PreviousScene");
            bool hasValidValue = !string.IsNullOrEmpty(path) && !SceneManager.GetActiveScene().path.Equals(path);
            string toolTip = hasValidValue ? $"Return to {EditorPrefs.GetString("PreviousScene", "")}" : "";
            
            var icon = EditorGUIUtility.IconContent("d_RotateTool").image as Texture2D;
            var content = new MainToolbarContent(icon, toolTip);
            var button = new MainToolbarButton(content, Return)
            {
                enabled = hasValidValue && !Application.isPlaying && !EditorApplication.isPlaying,
            };
            
            return button;
        }

        private static void Return()
        {
            string path = EditorPrefs.GetString("PreviousScene");
            var scene = EditorSceneManager.OpenScene(path);
            if (!scene.IsValid())
                EditorPrefs.DeleteKey("PreviousScene");
        }
    }
}