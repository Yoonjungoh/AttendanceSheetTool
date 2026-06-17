using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Reward 안에서 보상 한 칸을 표시한다.
/// 프리팹 계층:
/// Reward_SubItem > Bg(Image), RewardImage(Image), RewardAmountText(TMP)
/// </summary>
public class Reward_SubItem : UI_Base
{
    enum Images
    {
        RewardImage,
    }

    enum Texts
    {
        RewardAmountText,
    }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    public void SetInfo(ItemType type, int count)
    {
        Sprite icon = Managers.Image.GetIcon(type);
        if (icon != null)
            GetImage((int)Images.RewardImage).sprite = icon;

        GetTextMeshProUGUI((int)Texts.RewardAmountText).text = $"x{count}";
    }
}
