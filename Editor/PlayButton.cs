using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JordanTama.Startup.Editor.com.jordantama.startup.Editor
{
    public class PlayButton
    {
        private const string ELEMENT_PATH = "JordanTama/Play Override State";
        
        [MainToolbarElement(ELEMENT_PATH, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -102)]
        public static MainToolbarElement CreateButton()
        {
            var icon = EditorGUIUtility.IconContent("Animation.Play").image as Texture2D;
            var content = new MainToolbarContent(icon);
            return new MainToolbarButton(content, Play);
        }
        
        private static void Play()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) 
                return;
            
            string activeScenePath = SceneManager.GetActiveScene().path;
            
            string scenePath = GetStartupScenePath();
            InsertAtFirstBuildIndex(scenePath);
            
            if (!activeScenePath.Equals(scenePath))
                EditorPrefs.SetString("PreviousScene", activeScenePath);
            
            EditorSceneManager.OpenScene(scenePath);
            
            EditorApplication.EnterPlaymode();
        }
        
        private static string GetStartupScenePath()
        {
            var existingScene = EditorBuildSettings.scenes.FirstOrDefault(s =>
                Path.GetFileNameWithoutExtension(s.path) == Constants.STARTUP_SCENE_NAME && File.Exists(s.path));
            
            if (existingScene != null)
                return existingScene.path;

            string scenePath;
            string[] guids = AssetDatabase.FindAssets($"t:Scene {Constants.STARTUP_SCENE_NAME}");
            
            // Scene already exists but isn't in build settings
            if (guids.Length > 0)
            {
                scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            else
            {
                scenePath = $"Assets/Scenes/{Constants.STARTUP_SCENE_NAME}.unity";
                string directory = Path.GetDirectoryName(scenePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                var newScene =
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                    
                EditorSceneManager.SaveScene(newScene, scenePath);
            }
                
            // Add the new scene to the build settings
            var originalScenes = EditorBuildSettings.scenes.ToList();
            if (originalScenes.Any(s => s.path == scenePath))
                return scenePath;

            var newSceneEntry = new EditorBuildSettingsScene(scenePath, true);
            originalScenes.Add(newSceneEntry);

            EditorBuildSettings.scenes = originalScenes.ToArray();
            return scenePath;
        }

        private static void InsertAtFirstBuildIndex(string scenePath)
        {
            var sceneList = EditorBuildSettings.scenes.ToList();
            int sceneIndex = sceneList.FindIndex(s => s.path == scenePath);
            if (sceneIndex == 0)
                return;
            
            var scene = sceneList[sceneIndex];
            sceneList.RemoveAt(sceneIndex);
            sceneList.Insert(0, scene);
            EditorBuildSettings.scenes = sceneList.ToArray();
        }
    }
}