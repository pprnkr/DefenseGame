using Nyonyo.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame.Lobby
{
    /// <summary>
    /// 전투 탭에서 몰려올 몬스터 유형 하나를 간략하게 보여 줍니다(아이콘, 유형 이름, 특징).
    /// </summary>
    /// <remarks>
    /// 아이콘 그림이 아직 없어서, 유형 색으로 칠한 원에 유형 이름 첫 글자를 씁니다.
    /// </remarks>
    public class MonsterPreviewView : UIView
    {
        #region Auto Generated UI
        #pragma warning disable CS0169, CS0649
        [SerializeField] private Image IMG_Icon;
        [SerializeField] private TextMeshProUGUI TXT_Initial;
        [SerializeField] private TextMeshProUGUI TXT_Name;
        [SerializeField] private Image IMG_TypeBadge;
        [SerializeField] private TextMeshProUGUI TXT_Type;
        #pragma warning restore CS0169, CS0649
        #endregion

        public void Setup(MonsterType type)
        {
            var color = type.ToColor();
            var label = type.ToLabel();
            IMG_Icon.color = color;
            TXT_Initial.text = label.Substring(0, 1);
            TXT_Name.text = label;
            IMG_TypeBadge.color = color;
            TXT_Type.text = type.ToTrait();
        }
    }
}
