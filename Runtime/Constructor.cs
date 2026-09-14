using Cysharp.Threading.Tasks;
using JordanTama.StateMachine;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace JordanTama.Startup
{
    public static class Constructor
    {
        private static Machine _machine;

        private static Machine Machine
        {
            get
            {
                _machine ??= Locator.Get<Machine>();
                return _machine;
            }
        }

        [ConstructStateMachine(ignoreInTests: true), Preserve]
        private static void Construct(StateConstructor rootState)
        {
            var startupState = new StateConstructor(Constants.STARTUP_STATE_NAME, onEnterAsync: OnEnterAsync);
            rootState.AddState(startupState);

            Machine.Initialized += () => Machine.ChangeState(Constants.STARTUP_STATE_NAME).Forget();
        }

        private static async UniTask OnEnterAsync(string from)
        {
            await SceneManager.LoadSceneAsync(Constants.STARTUP_SCENE_NAME).ToUniTask();
            
            // Wait a frame so that anything in the startup scene can update once
            await UniTask.NextFrame();
            await LoadEntryPoint();
        }

        private static async UniTask LoadEntryPoint()
        {
            string overrideState = StartupOverride.UseState();
            
            if (string.IsNullOrEmpty(overrideState))
            {
                var info = Machine.GetStateInfo(Machine.CurrentStateId);
                if (info.Children.Length == 0)
                {
                    Debug.LogError($"No states were registered as children of {Constants.STARTUP_STATE_NAME}");
                    return;
                }

                overrideState = info.Children[0];
            }
            
            await Machine.ChangeState(overrideState);
        }
    }
}