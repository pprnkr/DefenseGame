using System.Collections.Generic;

namespace DefenseGame.Data
{
    /// <summary>
    /// 자동 생성된 <see cref="WaveData"/>에 덧붙이는 부분입니다.
    /// </summary>
    /// <remarks>
    /// 한 웨이브는 출현 묶음 여러 개로 이루어집니다. 묶음 i는 Monster[i]를 Delay[i]초 뒤부터 Interval[i]초 간격으로 Count[i]마리 만듭니다.
    /// </remarks>
    public sealed partial class WaveData
    {
        /// <summary>출현 묶음 수입니다.</summary>
        public int GroupCount => Monster.Length;

        // 시트의 배열은 빈 칸을 건너뛰므로, 한 묶음에서 칸 하나만 비워도 묶음끼리 짝이 어긋납니다. 그런 실수를 잡습니다.
        internal void Validate(MonsterTable monsters, List<string> errors)
        {
            if (Count.Length != Monster.Length || Interval.Length != Monster.Length || Delay.Length != Monster.Length)
            {
                errors.Add($"Wave {Id}: 출현 묶음의 칸 수가 다릅니다(Monster {Monster.Length}, Count {Count.Length}, Interval {Interval.Length}, Delay {Delay.Length}). " +
                           "묶음마다 네 칸을 모두 채우거나 모두 비우세요(0도 채워야 합니다).");
            }

            foreach (var id in Monster)
            {
                if (!monsters.Contains(id))
                    errors.Add($"Wave {Id}: 몬스터 {id}가 Monster 시트에 없습니다.");
            }
        }
    }
}
