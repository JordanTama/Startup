using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using Logger = Logging.Logger;

namespace JordanTama.Startup.Editor.Editor
{
    [InitializeOnLoad]
    public class CustomToolbar
    {
        // private const string STARTUP_NAME = "Packages/com.protocol.startup/Scenes/Startup.unity";
        
        private static readonly string[] Options;
        private static string targetState;

        private static string DropdownLabel => string.IsNullOrEmpty(StartupOverride.TargetState)
            ? "Default"
            : StartupOverride.TargetState.Split("/")[^1];
    
        static CustomToolbar()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
            
            // Use reflection to get all toolbar states
            var states = new HashSet<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                HandleAssembly(assembly);

            Options = states.ToArray();
            return;

            void HandleAssembly(Assembly assembly)
            {
                var types = assembly.GetTypes().Where(t =>
                    typeof(ICustomToolbarStates).IsAssignableFrom(t) && !t.IsAbstract &&
                    t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in types)
                    HandleType(type);
            }

            void HandleType(Type type)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as ICustomToolbarStates;
                    if (instance?.States == null) 
                        return;
                    
                    foreach (string state in instance.States)
                        states.Add(state);
                }
                catch (Exception ex)
                {
                    Logger.Warning(typeof(CustomToolbar), $"Failed to instantiate {type.FullName}: {ex.Message}");
                }
            }
        }

        private static void OnToolbarGUI()
        {
            GUI.enabled = !Application.isPlaying;
            
            // Push everything to the right
            GUILayout.FlexibleSpace();

            // Play Button
            var buttonRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(35));
            var buttonTexture = EditorGUIUtility.IconContent("Animation.Play").image;
            var buttonContent = new GUIContent(buttonTexture, "Load into selected state");
        
            if (GUI.Button(buttonRect, buttonContent, EditorStyles.toolbarButton))
                Play();
            
            // Return button
            ReturnButton();

            // Dropdown
            var dropdownRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(100));
            bool dropdown = EditorGUI.DropdownButton(dropdownRect, new GUIContent(DropdownLabel), FocusType.Passive,
                EditorStyles.toolbarPopup);
            
            if (!dropdown)
                return;

            // Dropdown menu
            var menu = CreateMenu();
            menu.DropDown(dropdownRect);
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

        private static void ReturnButton()
        {
            string path = EditorPrefs.GetString("PreviousScene");
            bool hasValidValue = !string.IsNullOrEmpty(path) && !SceneManager.GetActiveScene().path.Equals(path);

            bool enabled = GUI.enabled;
            GUI.enabled &= hasValidValue;
            
            var returnRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(35));
            var returnTexture = EditorGUIUtility.IconContent("d_RotateTool").image;
            string toolTip = hasValidValue ? $"Return to {EditorPrefs.GetString("PreviousScene", "")}" : "";
            var returnContent = new GUIContent(returnTexture, toolTip);
            
            if (GUI.Button(returnRect, returnContent, EditorStyles.toolbarButton))
            {
                var scene = EditorSceneManager.OpenScene(path);
                if (!scene.IsValid())
                    EditorPrefs.DeleteKey("PreviousScene");
            }

            GUI.enabled = enabled;
        }

        private static GenericMenu CreateMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Default"), string.IsNullOrEmpty(StartupOverride.TargetState),
                () => { StartupOverride.TargetState = StartupOverride.DEFAULT_STATE; });
            menu.AddSeparator("");
        
            foreach (string state in Options)
                AddState(state, menu);
        
            return menu;
        }

        private static void AddState(string state, GenericMenu menu)
        {
            var content = new GUIContent(state);
            menu.AddItem(content, StartupOverride.TargetState.Equals(state), Callback);
            return;
            
            void Callback() => StartupOverride.TargetState = state;
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
