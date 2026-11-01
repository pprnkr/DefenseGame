using Unity.Mathematics;

namespace DefenseGame.InGame.Swarm
{
    /// <summary>
    /// 몬스터를 칸으로 나눠 찾는 균일 격자입니다. 이웃 몬스터와 포탑 사거리 안 몬스터를 전부 훑지 않고 근처 칸만 봅니다.
    /// </summary>
    /// <remarks>
    /// 칸마다 몬스터 목록은 <see cref="BuildGridJob"/>이 카운팅 정렬로 만듭니다.
    /// 칸 c의 몬스터는 SortedIndices[CellStarts[c] .. CellStarts[c + 1]) 입니다.
    /// 격자 밖 위치는 가장자리 칸으로 모읍니다.
    /// </remarks>
    public readonly struct SwarmGrid
    {
        public readonly float2 Origin;
        public readonly float CellSize;
        public readonly int2 Size;

        public SwarmGrid(float2 min, float2 max, float cellSize)
        {
            Origin = min;
            CellSize = cellSize;
            Size = math.max((int2)math.ceil((max - min) / cellSize), 1);
        }

        public int CellCount => Size.x * Size.y;

        public int2 CellOf(float2 position) => math.clamp((int2)math.floor((position - Origin) / CellSize), 0, Size - 1);

        public int IndexOf(int2 cell) => cell.y * Size.x + cell.x;

        public int IndexOf(float2 position) => IndexOf(CellOf(position));
    }
}
