using System;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 출석부 목록의 한 줄. 표시 + 클릭 콜백 전달만 담당하고 출석 로직은 직접 호출하지 않는다
/// (UI와 콘텐츠 로직 분리).
/// 더미 프리팹 계층: AttendanceSubItem(SelectButton: Button) > NameText / ProgressText / StateText (TMP)
/// </summary>
public class AttendanceSubItem : UI_Base
{
    enum Texts
    {
        NameText,
        ProgressText,
        StateText,
    }

    enum Buttons
    {
        SelectButton,
    }

    AttendanceSheetType _sheetType;
    Action<AttendanceSheetType> _onClick;

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.SelectButton).onClick.AddListener(() => _onClick?.Invoke(_sheetType));
    }

    public void SetInfo(AttendanceMetaData meta, AttendanceProgressData progress, Action<AttendanceSheetType> onClick)
    {
        _sheetType = meta.AttendanceSheetType;
        _onClick = onClick;

        GetTextMeshProUGUI((int)Texts.NameText).text = meta.NameKey;
        GetTextMeshProUGUI((int)Texts.ProgressText).text = $"{progress.ClaimedDays.Count}/{meta.TotalDays}";
        GetTextMeshProUGUI((int)Texts.StateText).text = GetStateText(meta, progress);
    }

    string GetStateText(AttendanceMetaData meta, AttendanceProgressData progress)
    {
        bool canCheckIn = Managers.Attendance.CanCheckInToday(meta.AttendanceSheetType);
        if (meta.AttendanceType == AttendanceType.EVENT_DAILY && progress.CheckInCount >= meta.TotalDays)
            return "완료";

        return canCheckIn ? "출석 가능" : "오늘 출석 완료";
    }
}
