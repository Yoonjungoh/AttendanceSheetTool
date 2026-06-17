using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 지급 시 보여주는 팝업.
/// 프리팹 계층:
/// RewardPanel > BackgroundButton, RewardInfoPanel > RewardText, Divider, RewardScrollView > Viewport > RewardItemContent, ConfirmButton, CloseButton
/// </summary>
public class UI_Reward : UI_Popup
{
    enum GameObjects
    {
        RewardItemContent,
    }

    enum Buttons
    {
        ConfirmButton,
        CloseButton,
        BackgroundButton,
    }

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.ConfirmButton).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.CloseButton).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.BackgroundButton).onClick.AddListener(ClosePopupUI);
    }

    public void SetData(List<(ItemType type, int count)> rewards)
    {
        Transform content = GetObject((int)GameObjects.RewardItemContent).transform;
        for (int i = content.childCount - 1; i >= 0; i--)
            Managers.Resource.Destroy(content.GetChild(i).gameObject);

        foreach (var reward in rewards)
        {
            Reward_SubItem item = Managers.UI.MakeSubItem<Reward_SubItem>(content);
            item.SetInfo(reward.type, reward.count);
        }
    }
}
