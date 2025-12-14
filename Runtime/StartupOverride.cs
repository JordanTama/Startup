#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JordanTama.Startup
{
    public static class StartupOverride
    {
        private const string KEY = "CustomToolbar/TargetState";
        public const string DEFAULT_STATE = "";

        private static bool used;
        
        public static string TargetState
        {
            get
            {
#if UNITY_EDITOR
                return EditorPrefs.GetString(KEY, DEFAULT_STATE);
#else
                return DEFAULT_STATE;
#endif
            }

            set
            {
#if UNITY_EDITOR
                EditorPrefs.SetString(KEY, value);
#endif
            }
        }

        public static string UseState()
        {
            string state = used ? DEFAULT_STATE : TargetState;
            used = true;
            return state;
        }
    }
}