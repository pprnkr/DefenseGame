using DefenseGame.InGame.Swarm;
using Nyonyo.Framework;
using Nyonyo.Framework.UI;
using UnityEngine;

namespace DefenseGame.InGame
{
    /// <summary>
    /// 게임 씬 진입점입니다. 로비에서 고른 스테이지(<see cref="StageService.CurrentStage"/>)로 전투를 준비합니다.
    /// </summary>
    /// <remarks>지금은 몬스터 대량 처리 부하 테스트(<see cref="MonsterSwarm"/>)만 돌립니다. 뒤로가기(ESC)로 로비에 돌아갑니다.</remarks>
    public sealed class InGameScene : MonoBehaviour
    {
        [SerializeField] private MonsterSwarm _swarm;

        private async Awaitable Start()
        {
            await Bootstrapper.WaitUntilReadyAsync(destroyCancellationToken);

            var stage = StageService.Instance.CurrentStage;
            if (stage == null)
            {
                // 에디터에서 이 씬을 바로 Play한 경우입니다.
                stage = StageService.Instance.Stages[0];
                Debug.LogWarning($"[DefenseGame] 로비를 거치지 않아 첫 스테이지({stage.Name})로 시작합니다.");
            }

            Debug.Log($"[DefenseGame] STAGE {stage.Index} {stage.Name} 시작 (수심 {stage.Depth}m, 웨이브 {stage.Waves.Count}개, {stage.TotalDuration / 60f:0.0}분)");
            PopupManager.Instance.BackKeyUnhandled += ReturnToLobby;
            _swarm.Begin(stage);
        }

        private void OnDestroy()
        {
            if (Services.TryGet<PopupManager>(out var popups))
                popups.BackKeyUnhandled -= ReturnToLobby;
        }

        private void ReturnToLobby() => StageService.Instance.ReturnToLobby();
    }
}
