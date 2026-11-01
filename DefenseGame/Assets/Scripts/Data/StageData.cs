using System.Collections.Generic;

namespace DefenseGame.Data
{
    /// <summary>
    /// 자동 생성된 <see cref="StageData"/>에 덧붙이는 부분입니다.
    /// </summary>
    public sealed partial class StageData
    {
        private WaveData[] _waves = System.Array.Empty<WaveData>();
        private MonsterData[] _monsterRows = System.Array.Empty<MonsterData>();

        /// <summary><see cref="WaveSet"/>에 속한 웨이브를 웨이브 번호 순서로 담은 목록입니다.</summary>
        public IReadOnlyList<WaveData> Waves => _waves;

        /// <summary>이 스테이지의 웨이브에 나오는 몬스터입니다. 처음 나오는 순서대로, 겹치지 않게 담습니다.</summary>
        public IReadOnlyList<MonsterData> MonsterRows => _monsterRows;

        /// <summary>모든 웨이브 길이의 합(초)입니다.</summary>
        public float TotalDuration { get; private set; }

        internal void LinkWaves(List<WaveData> waves, MonsterTable monsters, List<string> errors)
        {
            _waves = waves.ToArray();

            var rows = new List<MonsterData>();
            var types = new HashSet<MonsterType>();
            var duration = 0f;
            foreach (var wave in _waves)
            {
                duration += wave.Duration;
                foreach (var id in wave.Monster)
                {
                    if (!monsters.TryGet(id, out var monster) || rows.Contains(monster))
                        continue;
                    rows.Add(monster);
                    types.Add(monster.Type);
                }
            }
            _monsterRows = rows.ToArray();
            TotalDuration = duration;

            if (_waves.Length == 0)
                errors.Add($"Stage {Index}: WaveSet {WaveSet}에 해당하는 웨이브가 Wave 시트에 없습니다.");

            // 로비에 보여 주는 유형이 실제 웨이브에 나오지 않으면 플레이어를 속이게 되므로 알립니다.
            foreach (var type in MonsterTypes)
            {
                if (!types.Contains(type))
                    errors.Add($"Stage {Index}: 미리보기 유형 {type}가 WaveSet {WaveSet}의 웨이브에 나오지 않습니다.");
            }
        }
    }
}
