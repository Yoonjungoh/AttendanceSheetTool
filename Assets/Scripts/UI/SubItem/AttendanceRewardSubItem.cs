using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출석부 상세의 Day 한 칸. 표시만 담당한다.
/// 더미 프리팹 계층:
/// AttendanceRewardSubItem > DayText(TMP), RewardText(TMP), RewardIcon(Image), ClaimedMark(GameObject), LockedMark(GameObject)
/// </summary>
public class AttendanceRewardSubItem : UI_Base
{
    enum Texts
    {
        DayText,
        RewardText,
    }

    enum Images
    {
        RewardIcon,
    }

    enum GameObjects
    {
        ClaimedMark,
        LockedMark,
    }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));
    }

    public void SetInfo(AttendanceRewardMetaData meta, bool isClaimed, bool isToday, bool isLocked)
    {
        GetTextMeshProUGUI((int)Texts.DayText).text = $"{meta.Day}일차";
        GetTextMeshProUGUI((int)Texts.RewardText).text = $"{meta.RewardType} x{meta.RewardCount}";

        Sprite icon = Managers.Image.GetIcon(meta.RewardType);
        if (icon != null)
            Get<Image>((int)Images.RewardIcon).sprite = icon;

        GetObject((int)GameObjects.ClaimedMark).SetActive(isClaimed);
        GetObject((int)GameObjects.LockedMark).SetActive(isLocked && !isClaimed);
    }
}
