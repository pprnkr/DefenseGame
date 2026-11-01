using Nyonyo.Framework;
using Nyonyo.Framework.UI;
using UnityEngine;

namespace DefenseGame.Lobby
{
    /// <summary>
    /// 로비 씬 진입점입니다. 서비스 준비(게임 데이터 로드 포함)가 끝나면 로비 화면을 열고, 씬을 떠날 때 닫습니다.
    /// </summary>
    /// <remarks>
    /// 로비 화면 프리팹은 Close On Scene Change를 꺼 두었습니다. 씬 전환으로 들어올 때 Start가 SceneLoaded 이벤트보다 먼저 불려서,
    /// 켜 두면 방금 연 화면이 그 이벤트에 닫히기 때문입니다. 대신 이 오브젝트가 파괴될 때(씬을 떠날 때) 직접 닫습니다.
    /// </remarks>
    public sealed class LobbyScene : MonoBehaviour
    {
        private LobbyScreen _screen;

        private async Awaitable Start()
        {
            await Bootstrapper.WaitUntilReadyAsync(destroyCancellationToken);

            // WebGL은 Addressables 동기 로드가 안 되므로 OpenAsync로 엽니다.
            _screen = await PopupManager.Instance.OpenAsync<LobbyScreen>(cancellationToken: destroyCancellationToken);
        }

        private void OnDestroy()
        {
            // 게임 종료 때는 서비스가 먼저 정리되므로 팝업이 이미 없을 수 있습니다.
            if (_screen != null && !_screen.IsClosed)
                _screen.Close();
        }
    }
}
