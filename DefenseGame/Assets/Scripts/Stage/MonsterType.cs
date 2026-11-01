namespace DefenseGame
{
    /// <summary>
    /// 몬스터의 공격 성향입니다. 구글 시트 Monster 시트의 Type 열에서 이름으로 씁니다(예: Swarm).
    /// </summary>
    /// <remarks>
    /// 멤버 이름이나 값을 바꾸면 게임 데이터를 다시 가져와야 합니다(SchemaHash에 enum 구성이 들어갑니다).
    /// </remarks>
    public enum MonsterType
    {
        /// <summary>평범한 속도와 체력</summary>
        Normal = 0,

        /// <summary>약하지만 떼로 몰려옴</summary>
        Swarm = 1,

        /// <summary>빠르게 돌진함</summary>
        Fast = 2,

        /// <summary>느리지만 단단함</summary>
        Tank = 3,

        /// <summary>멀리서 공격함</summary>
        Ranged = 4,

        /// <summary>스테이지 마지막에 등장하는 우두머리</summary>
        Boss = 5,
    }
}
