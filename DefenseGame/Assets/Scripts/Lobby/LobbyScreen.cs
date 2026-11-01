using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Tweening;
using Nyonyo.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Lobby
{
    /// <summary>
    /// 로비 첫 화면입니다. 상단 재화, 가운데 탭 내용, 하단 탭 버튼(상점 | 전투 | 강화)으로 이루어집니다.
    /// </summary>
    [Popup(Path = "Assets/Prefabs/UI/Lobby/LobbyScreen.prefab", LoadType = LoadType.Addressables)]
    public class LobbyScreen : PopupBase
    {
        #region Auto Generated UI
        #pragma warning disable CS0169, CS0649
        [SerializeField] private TextMeshProUGUI TXT_Gold;
        [SerializeField] private TextMeshProUGUI TXT_Gem;
        [SerializeField] private ShopTabView UI_ShopTab;
        [SerializeField] private BattleTabView UI_BattleTab;
        [SerializeField] private UpgradeTabView UI_UpgradeTab;
        [SerializeField] private Button BTN_Shop;
        [SerializeField] private Button BTN_Battle;
        [SerializeField] private Button BTN_Upgrade;
        #pragma warning restore CS0169, CS0649
        #endregion

        private static readonly Color s_tabNormalColor = new Color32(0x0E, 0x2A, 0x47, 0xFF);
        private static readonly Color s_tabSelectedColor = new Color32(0x1F, 0x8F, 0xB8, 0xFF);
        private const float TabSelectedScale = 1.08f;
        private const float TabTweenSeconds = 0.15f;

        private LobbyTab _currentTab;
        private bool _hasTab;

        protected override void PostOpen()
        {
            PlayerProgress.Instance.CurrencyChanged += RefreshCurrency;
            RefreshCurrency();
            SelectTab(LobbyTab.Battle);
        }

        protected override void PrevClose()
        {
            PlayerProgress.Instance.CurrencyChanged -= RefreshCurrency;
        }

        protected override void OnButtonClick(Button button)
        {
            if (button == BTN_Shop)
            {
                SelectTab(LobbyTab.Shop);
            }
            else if (button == BTN_Battle)
            {
                SelectTab(LobbyTab.Battle);
            }
            else if (button == BTN_Upgrade)
            {
                SelectTab(LobbyTab.Upgrade);
            }
            else
            {
                base.OnButtonClick(button);
            }
        }

        private void SelectTab(LobbyTab tab)
        {
            if (_hasTab && _currentTab == tab)
                return;

            _hasTab = true;
            _currentTab = tab;

            ShowTab(UI_ShopTab, BTN_Shop, tab == LobbyTab.Shop);
            ShowTab(UI_BattleTab, BTN_Battle, tab == LobbyTab.Battle);
            ShowTab(UI_UpgradeTab, BTN_Upgrade, tab == LobbyTab.Upgrade);
        }

        private void ShowTab(UIView view, Button button, bool selected)
        {
            if (selected)
                view.Show();
            else
                view.Hide();

            button.image.DOKill();
            button.image.DOColor(selected ? s_tabSelectedColor : s_tabNormalColor, TabTweenSeconds);
            button.transform.DOKill();
            button.transform.DOScale(selected ? TabSelectedScale : 1f, TabTweenSeconds).SetEase(Ease.OutBack);
        }

        private void RefreshCurrency()
        {
            TXT_Gold.text = PlayerProgress.Instance.Gold.ToString("N0");
            TXT_Gem.text = PlayerProgress.Instance.Gem.ToString("N0");
        }
    }
}
