using Cysharp.Threading.Tasks;
using JordanTama.CoreServices;
using JordanTama.StateMachine;
using Services;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using SettingsService = Protocol.Settings.SettingsService;

namespace JordanTama.Startup
{
    public static class Constructor
    {
        private static Machine machine;

        private static Machine Machine
        {
            get
            {
                machine ??= Locator.Get<Machine>();
                return machine;
            }
        }

        [ConstructStateMachine(ignoreInTests: true), Preserve]
        private static void Construct(StateConstructor rootState)
        {
            var startupState = new StateConstructor(Constants.STARTUP_STATE_NAME, onEnterAsync: OnEnterAsync);
            rootState.AddState(startupState);

            Machine.OnStateChangeComplete += OnStateChangeComplete;
        }

        private static async UniTask OnEnterAsync(string from)
        {
            await Locator.RegisterMonoBehaviour<FocusService>();
            await Locator.RegisterMonoBehaviour<CursorService>();
            await Locator.RegisterAsync(new SettingsService());
            
            await SceneManager.LoadSceneAsync(Constants.STARTUP_SCENE_NAME).ToUniTask();
            
            // Wait a frame so that anything in the startup scene can update once
            await UniTask.NextFrame();
            LoadEntryPoint();
        }

        private static void OnStateChangeComplete(string from, string to)
        {
            if (to != JordanTama.StateMachine.Constants.ROOT_STATE_NAME)
                return;

            Machine.OnStateChangeComplete -= OnStateChangeComplete;
            UniTask.Void(async () =>
            {
                await Machine.ChangeStateAsync(Constants.STARTUP_STATE_NAME);
            });
        }

        private static void LoadEntryPoint()
        {
            string overrideState = StartupOverride.UseState();
            
            if (string.IsNullOrEmpty(overrideState))
            {
                var info = Machine.GetStateInfo(Machine.CurrentStateId);
                if (info.Children.Length == 0)
                {
                    Logging.Logger.Error(nameof(Constructor),
                        $"No states were registered as children of {Constants.STARTUP_STATE_NAME}");
                    return;
                }

                overrideState = info.Children[0];
            }
            
            var stateInfo = Machine.GetStateInfo(overrideState);
            if (stateInfo.IsAsyncState)
                Machine.ChangeStateAsync(overrideState).Forget();
            else
                Machine.ChangeState(overrideState);
        }
    }
}