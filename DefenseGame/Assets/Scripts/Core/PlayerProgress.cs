using System;
using Nyonyo.Framework;
using Nyonyo.Framework.Save;

namespace DefenseGame
{
    /// <summary>
    /// 클리어한 스테이지와 재화를 관리하고 저장합니다.
    /// </summary>
    public sealed class PlayerProgress : IInitializable
    {
        private const string SaveKey = "progress";

        private readonly SaveService _save;
        private PlayerProgressData _data = new();

        public PlayerProgress(SaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public static PlayerProgress Instance => Services.Get<PlayerProgress>();

        /// <summary>재화가 바뀌면 호출됩니다.</summary>
        public event Action CurrencyChanged;

        public int ClearedStageIndex => _data.clearedStageIndex;

        public int Gold => _data.gold;

        public int Gem => _data.gem;

        public void Initialize()
        {
            _data = _save.Load(SaveKey, new PlayerProgressData());
        }

        public void MarkStageCleared(int stageIndex)
        {
            _data.clearedStageIndex = stageIndex;
            Save();
        }

        public void AddGold(int amount)
        {
            _data.gold += amount;
            Save();
            CurrencyChanged?.Invoke();
        }

        private void Save() => _save.Save(SaveKey, _data);
    }
}
