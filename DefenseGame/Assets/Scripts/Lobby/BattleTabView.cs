using DefenseGame.Data;
using Nyonyo.Framework.Tweening;
using Nyonyo.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Lobby
{
    /// <summary>
    /// 전투 탭입니다. 가운데 큰 스테이지 진입 버튼과, 이번 스테이지에 몰려올 몬스터 3종을 보여 줍니다.
    /// 좌우 화살표로 해금된 스테이지를 고를 수 있습니다.
    /// </summary>
    public class BattleTabView : UIView
    {
        #region Auto Generated UI
        #pragma warning disable CS0169, CS0649
        [SerializeField] private TextMeshProUGUI TXT_StageNumber;
        [SerializeField] private TextMeshProUGUI TXT_StageName;
        [SerializeField] private TextMeshProUGUI TXT_Depth;
        [SerializeField] private TextMeshProUGUI TXT_Description;
        [SerializeField] private Button BTN_PrevStage;
        [SerializeField] private Button BTN_NextStage;
        [SerializeField] private Button BTN_Start;
        [SerializeField] private MonsterPreviewView UI_Monster1;
        [SerializeField] private MonsterPreviewView UI_Monster2;
        [SerializeField] private MonsterPreviewView UI_Monster3;
        #pragma warning restore CS0169, CS0649
        #endregion

        // StageService.Stages 목록에서 고른 위치입니다(Stage 시트의 Index 값이 아님).
        private int _selected = -1;

        protected override void OnShow()
        {
            if (_selected < 0)
                _selected = StageService.Instance.HighestUnlockedIndex;

            Refresh();
            BTN_Start.transform.DOKill();
            BTN_Start.transform.localScale = Vector3.one;
            BTN_Start.transform.DOScale(1.04f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        protected override void OnHide()
        {
            BTN_Start.transform.DOKill();
        }

        protected override void OnButtonClick(Button button)
        {
            if (button == BTN_PrevStage)
            {
                MoveStage(-1);
            }
            else if (button == BTN_NextStage)
            {
                MoveStage(1);
            }
            else if (button == BTN_Start)
            {
                StageService.Instance.Launch(StageService.Instance.Stages[_selected]);
            }
            else
            {
                base.OnButtonClick(button);
            }
        }

        private void MoveStage(int delta)
        {
            var next = Mathf.Clamp(_selected + delta, 0, StageService.Instance.HighestUnlockedIndex);
            if (next == _selected)
                return;

            _selected = next;
            Refresh();
        }

        private void Refresh()
        {
            var stages = StageService.Instance.Stages;
            var stage = stages[_selected];

            TXT_StageNumber.text = $"STAGE {stage.Index}";
            TXT_StageName.text = stage.Name;
            TXT_Depth.text = $"수심 {stage.Depth:N0}m · 웨이브 {stage.Waves.Count}";
            TXT_Description.text = stage.Description;

            // 넘어갈 스테이지가 없으면 화살표를 숨깁니다(해금되지 않은 스테이지는 고를 수 없음).
            BTN_PrevStage.gameObject.SetActive(_selected > 0);
            BTN_NextStage.gameObject.SetActive(_selected < StageService.Instance.HighestUnlockedIndex);

            ShowMonster(UI_Monster1, stage, 0);
            ShowMonster(UI_Monster2, stage, 1);
            ShowMonster(UI_Monster3, stage, 2);
        }

        private void ShowMonster(MonsterPreviewView view, StageData stage, int index)
        {
            if (index < stage.MonsterTypes.Length)
            {
                view.Show();
                view.Setup(stage.MonsterTypes[index]);
            }
            else
            {
                view.Hide();
            }
        }
    }
}
