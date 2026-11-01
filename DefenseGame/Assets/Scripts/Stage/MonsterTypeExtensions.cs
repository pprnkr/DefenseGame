using UnityEngine;

namespace DefenseGame
{
    /// <summary>
    /// <see cref="MonsterType"/>을 화면에 보여 줄 글자와 색으로 바꿉니다.
    /// </summary>
    public static class MonsterTypeExtensions
    {
        public static string ToLabel(this MonsterType type)
        {
            if (type == MonsterType.Swarm)
                return "무리";
            if (type == MonsterType.Fast)
                return "돌격";
            if (type == MonsterType.Tank)
                return "중장갑";
            if (type == MonsterType.Ranged)
                return "원거리";
            if (type == MonsterType.Boss)
                return "보스";
            return "일반";
        }

        /// <summary>로비 미리보기 배지에 붙이는 짧은 특징입니다. 배지가 작아서 세 글자 안으로 씁니다.</summary>
        public static string ToTrait(this MonsterType type)
        {
            if (type == MonsterType.Swarm)
                return "다수";
            if (type == MonsterType.Fast)
                return "빠름";
            if (type == MonsterType.Tank)
                return "단단함";
            if (type == MonsterType.Ranged)
                return "사격";
            if (type == MonsterType.Boss)
                return "강력";
            return "기본";
        }

        public static Color ToColor(this MonsterType type)
        {
            if (type == MonsterType.Swarm)
                return new Color32(0x4F, 0xD1, 0xC5, 0xFF);
            if (type == MonsterType.Fast)
                return new Color32(0xFF, 0x9F, 0x43, 0xFF);
            if (type == MonsterType.Tank)
                return new Color32(0x8E, 0x9A, 0xAF, 0xFF);
            if (type == MonsterType.Ranged)
                return new Color32(0xB3, 0x8B, 0xFF, 0xFF);
            if (type == MonsterType.Boss)
                return new Color32(0xFF, 0x5C, 0x7A, 0xFF);
            return new Color32(0x6F, 0xB7, 0xFF, 0xFF);
        }
    }
}
