using System.Collections.Generic;
using Nyonyo.Framework.Logging;
using UnityEngine;

namespace DefenseGame.Data
{
    /// <summary>
    /// 자동 생성된 <see cref="GameData"/>에 덧붙이는 부분입니다. 테이블끼리의 참조를 연결하고 시트 실수를 검사합니다.
    /// </summary>
    public sealed partial class GameData
    {
        protected override void OnLoaded()
        {
            var errors = new List<string>();
            var wavesBySet = new Dictionary<int, List<WaveData>>();
            foreach (var wave in Wave.All)
            {
                wave.Validate(Monster, errors);
                if (!wavesBySet.TryGetValue(wave.Set, out var list))
                {
                    list = new List<WaveData>();
                    wavesBySet.Add(wave.Set, list);
                }
                list.Add(wave);
            }

            foreach (var list in wavesBySet.Values)
                list.Sort((a, b) => a.Wave.CompareTo(b.Wave));

            foreach (var stage in Stage.All)
            {
                if (!wavesBySet.TryGetValue(stage.WaveSet, out var waves))
                    waves = new List<WaveData>();
                stage.LinkWaves(waves, Monster, errors);
                Log.Info($"[DefenseGame] Stage {stage.Index} {stage.Name}: 웨이브 {stage.Waves.Count}개, {stage.TotalDuration / 60f:0.0}분");
            }

            // 시트 실수 하나로 게임이 멈추지 않도록, 문제가 있는 부분은 빼고 에러만 남깁니다.
            foreach (var error in errors)
                Debug.LogError($"[DefenseGame] 게임 데이터 오류: {error}");
        }
    }
}
