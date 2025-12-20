using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using Logger = Logging.Logger;

namespace JordanTama.Startup.Editor
{
    public class StatesDropdown
    {
        private const string ELEMENT_PATH = "JordanTama/Select Override State";
        
        private static string DropdownLabel => string.IsNullOrEmpty(StartupOverride.TargetState)
            ? "Default"
            : StartupOverride.TargetState.Split("/")[^1];
    
        [MainToolbarElement(ELEMENT_PATH, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -100)]
        public static MainToolbarElement CreateDropdown()
        {
            var content = new MainToolbarContent(DropdownLabel);
            var dropdown = new MainToolbarDropdown(content, ShowDropdown);
            return dropdown;
        }

        private static void SelectState(string state)
        {
            StartupOverride.TargetState = state;
            MainToolbar.Refresh(ELEMENT_PATH);
        }

        private static void ShowDropdown(Rect rect)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Default"), string.IsNullOrEmpty(StartupOverride.TargetState),
                () => SelectState(StartupOverride.DEFAULT_STATE));
            menu.AddSeparator("");
            
            // Use reflection to get all toolbar states
            var states = new HashSet<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                HandleAssembly(assembly);

            foreach (string state in states)
            {
                var content = new GUIContent(state);
                menu.AddItem(content, StartupOverride.TargetState.Equals(state), () => SelectState(state));
            }
            
            menu.DropDown(rect);
            return;

            void HandleAssembly(Assembly assembly)
            {
                var types = assembly.GetTypes().Where(t =>
                    typeof(IToolbarStates).IsAssignableFrom(t) && !t.IsAbstract &&
                    t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in types)
                    HandleType(type);
            }

            void HandleType(Type type)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as IToolbarStates;
                    if (instance?.States == null) 
                        return;
                    
                    foreach (string state in instance.States)
                        states.Add(state);
                }
                catch (Exception ex)
                {
                    Logger.Warning(typeof(StatesDropdown), $"Failed to instantiate {type.FullName}: {ex.Message}");
                }
            }
        }
    }
}