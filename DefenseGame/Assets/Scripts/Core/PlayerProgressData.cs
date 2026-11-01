using System;

namespace DefenseGame
{
    /// <summary>
    /// 저장되는 플레이어 진행 데이터입니다. JsonUtility로 저장하므로 public 필드만 씁니다.
    /// </summary>
    [Serializable]
    public sealed class PlayerProgressData
    {
        /// <summary>마지막으로 클리어한 스테이지 번호(Stage 시트의 Index)입니다. 0이면 아직 클리어한 스테이지가 없습니다.</summary>
        public int clearedStageIndex;

        public int gold;

        public int gem;
    }
}
