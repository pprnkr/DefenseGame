using System;
using System.Collections.Generic;
using DefenseGame.Data;
using Nyonyo.Framework;
using Nyonyo.Framework.Scenes;

namespace DefenseGame
{
    /// <summary>
    /// 스테이지 목록과 해금 상태를 알려 주고, 고른 스테이지로 게임 씬을 엽니다.
    /// </summary>
    /// <remarks>
    /// 스테이지 데이터는 게임 시작 때 <see cref="GameData"/>가 bytes에서 모두 읽어 둡니다.
    /// <see cref="Launch"/>는 고른 스테이지를 <see cref="CurrentStage"/>에 담아 게임 씬으로 넘깁니다.
    /// </remarks>
    public sealed class StageService
    {
        private readonly GameData _data;
        private readonly PlayerProgress _progress;
        private readonly ISceneService _scenes;

        public StageService(GameData data, PlayerProgress progress, ISceneService scenes)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        }

        public static StageService Instance => Services.Get<StageService>();

        /// <summary>시트 순서대로 담긴 모든 스테이지입니다.</summary>
        public IReadOnlyList<StageData> Stages => _data.Stage.All;

        /// <summary>게임 씬에서 진행할 스테이지입니다. 로비에서 시작하지 않았으면 null입니다.</summary>
        public StageData CurrentStage { get; private set; }

        /// <summary>
        /// 플레이할 수 있는 가장 뒤 스테이지의 <see cref="Stages"/> 순번입니다. 마지막으로 클리어한 스테이지의 다음 스테이지입니다.
        /// </summary>
        public int HighestUnlockedIndex
        {
            get
            {
                var stages = Stages;
                for (var i = 0; i < stages.Count; i++)
                {
                    if (stages[i].Index == _progress.ClearedStageIndex)
                        return Math.Min(i + 1, stages.Count - 1);
                }
                return 0;
            }
        }

        /// <summary>스테이지를 시작합니다. 이미 씬을 바꾸는 중이면 무시하고 false를 돌려줍니다.</summary>
        public bool Launch(StageData stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            if (_scenes.IsLoading)
                return false;

            CurrentStage = stage;
            _scenes.Load(SceneNames.InGame);
            return true;
        }

        public void ReturnToLobby()
        {
            if (_scenes.IsLoading)
                return;

            CurrentStage = null;
            _scenes.Load(SceneNames.Lobby);
        }
    }
}
