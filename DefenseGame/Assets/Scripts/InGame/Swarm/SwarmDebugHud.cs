using UnityEngine;

namespace DefenseGame.InGame.Swarm
{
    /// <summary>
    /// 부하 테스트 화면 위에 마리 수, FPS, 잡 대기 시간을 보여 주고 마리 수를 바꾸는 개발용 HUD입니다.
    /// </summary>
    /// <remarks>
    /// 개발 도구라 IMGUI로 그립니다. IMGUI 기본 폰트는 기기에 따라 한글이 없을 수 있어서 영어로 표시합니다.
    /// 터치로도 누를 수 있고, 해상도가 달라도 읽히도록 화면 높이 960 기준으로 크기를 맞춥니다.
    /// </remarks>
    public sealed class SwarmDebugHud : MonoBehaviour
    {
        private const float ReferenceHeight = 960f;
        private const float SmoothFactor = 0.05f;
        private const int Step = 250;

        [SerializeField] private MonsterSwarm _swarm;

        private float _smoothedFrameMs = 16.7f;
        private float _smoothedWaitMs;
        private GUIStyle _labelStyle;

        private void Update()
        {
            _smoothedFrameMs = Mathf.Lerp(_smoothedFrameMs, Time.unscaledDeltaTime * 1000f, SmoothFactor);
            _smoothedWaitMs = Mathf.Lerp(_smoothedWaitMs, (float)_swarm.JobWaitMilliseconds, SmoothFactor);
        }

        private void OnGUI()
        {
            var scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };

            var width = Screen.width / scale;
            GUI.Box(new Rect(8, 8, width - 16, 92), GUIContent.none);
            GUI.Label(new Rect(18, 12, width - 36, 24),
                $"Monsters {_swarm.Count}   FPS {1000f / _smoothedFrameMs:0}   Frame {_smoothedFrameMs:0.0}ms", _labelStyle);
            GUI.Label(new Rect(18, 36, width - 36, 24),
                $"Job wait {_smoothedWaitMs:0.00}ms   Kills {_swarm.Kills}", _labelStyle);

            var buttonWidth = (width - 36 - 24) / 4f;
            if (GUI.Button(new Rect(18, 64, buttonWidth, 30), $"-{Step}"))
                _swarm.SetTargetCount(_swarm.Count - Step);
            if (GUI.Button(new Rect(26 + buttonWidth, 64, buttonWidth, 30), $"+{Step}"))
                _swarm.SetTargetCount(_swarm.Count + Step);
            if (GUI.Button(new Rect(34 + buttonWidth * 2, 64, buttonWidth, 30), $"Max {_swarm.MaxCount}"))
                _swarm.SetTargetCount(_swarm.MaxCount);
            if (GUI.Button(new Rect(42 + buttonWidth * 3, 64, buttonWidth, 30), "Lobby"))
                StageService.Instance.ReturnToLobby();
        }
    }
}
