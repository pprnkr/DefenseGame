using DefenseGame.Data;
using Nyonyo.Framework;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Debugging;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Save;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.UI;
using UnityEngine;

namespace DefenseGame
{
    /// <summary>
    /// 게임 서비스를 등록합니다. Resources/NyonyoFrameworkSettings의 Installers 목록에 들어 있습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "DefenseGame/Game Installer", fileName = "DefenseGameInstaller")]
    public sealed class DefenseGameInstaller : GameInstaller
    {
        [SerializeField] private UISettings _uiSettings;

        public override void Install(ServiceRegistry services)
        {
            // 다른 서비스가 쓰는 기반 서비스를 먼저 등록합니다(정리는 역순이라 가장 나중에 정리됨).
            var events = new EventBus();
            services.Register<IEventBus>(events);

            var assets = new AssetLoader();
            services.Register<IAssetLoader>(assets);

            var save = new SaveService();
            services.Register(save);

            var data = new GameData(assets);
            services.Register(data);

            var progress = new PlayerProgress(save);
            services.Register(progress);

            var scenes = new SceneService(new FadeTransition(Bootstrapper.Root, new Color(0.01f, 0.04f, 0.1f)), events) { MinimumLoadingSeconds = 0.3f };
            services.Register<ISceneService>(scenes);
            services.Register(new PopupManager(_uiSettings, Bootstrapper.Root, events, assets));
            services.Register(new StageService(data, progress, scenes));
            services.Register(new DebugConsole(Bootstrapper.Root));
        }
    }
}
