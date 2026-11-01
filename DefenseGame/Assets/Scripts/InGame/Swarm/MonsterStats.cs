using DefenseGame.Data;
using UnityEngine;

namespace DefenseGame.InGame.Swarm
{
    /// <summary>
    /// 잡에서 쓰는 몬스터 능력치입니다. 생성 코드의 <see cref="MonsterData"/>는 string이 들어 있는 class라 잡에 넘길 수 없어서, 스테이지 시작 때 이 구조체로 옮깁니다.
    /// </summary>
    public struct MonsterStats
    {
        public float Speed;
        public float MaxHp;
        public float Radius;
        public Color Color;
        public int SortingOrder;
        public bool IsBoss;

        /// <param name="hpScale">스테이지 체력 배율입니다. 웨이브 체력 배율은 웨이브 생성을 붙일 때 곱합니다.</param>
        public static MonsterStats From(MonsterData data, float hpScale)
        {
            return new MonsterStats
            {
                Speed = data.Speed,
                MaxHp = data.Hp * hpScale,
                Radius = RadiusOf(data.Type),
                Color = data.Type.ToColor(),
                // 큰 몬스터가 작은 몬스터를 가리지 않도록 작은 쪽을 위에 그립니다.
                SortingOrder = data.Type == MonsterType.Boss ? 0 : data.Type == MonsterType.Tank ? 1 : 2,
                IsBoss = data.Type == MonsterType.Boss,
            };
        }

        // 몸 크기(월드 반지름)는 아직 시트에 없어서 유형으로 정합니다. 필요하면 Monster 시트에 열을 추가합니다.
        private static float RadiusOf(MonsterType type)
        {
            if (type == MonsterType.Swarm)
                return 0.14f;
            if (type == MonsterType.Fast)
                return 0.17f;
            if (type == MonsterType.Tank)
                return 0.28f;
            if (type == MonsterType.Boss)
                return 0.7f;
            return 0.2f;
        }
    }
}
