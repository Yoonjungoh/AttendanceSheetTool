using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출석부 목록 팝업. 현재 노출돼야 하는 출석부(AttendanceManager.GetVisibleSheets)만큼
/// AttendanceSubItem을 생성한다.
/// 더미 프리팹 계층: UI_Attendance(Canvas) > CloseButton(Button), Content(레이아웃 그룹)
/// </summary>
public class UI_Attendance : UI_Popup
{
    enum GameObjects
    {
        Content,
    }

    enum Buttons
    {
        CloseButton,
    }

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.CloseButton).onClick.AddListener(ClosePopupUI);

        Managers.Attendance.OnAttendanceChanged -= OnAttendanceChanged;
        Managers.Attendance.OnAttendanceChanged += OnAttendanceChanged;

        GameTime.OnTimeChanged -= OnTimeChanged;
        GameTime.OnTimeChanged += OnTimeChanged;

        Refresh();
    }

    void OnDestroy()
    {
        Managers.Attendance.OnAttendanceChanged -= OnAttendanceChanged;
        GameTime.OnTimeChanged -= OnTimeChanged;
    }

    void OnAttendanceChanged(AttendanceSheetType sheetType)
    {
        Refresh();
    }

    void OnTimeChanged()
    {
        Refresh();
    }

    void Refresh()
    {
        Transform content = GetObject((int)GameObjects.Content).transform;
        for (int i = content.childCount - 1; i >= 0; i--)
            Managers.Resource.Destroy(content.GetChild(i).gameObject);

        List<AttendanceMetaData> sheets = Managers.Attendance.GetVisibleSheets();
        foreach (AttendanceMetaData meta in sheets)
        {
            AttendanceSubItem item = Managers.UI.MakeSubItem<AttendanceSubItem>(content);
            item.SetInfo(meta, Managers.Attendance.GetProgress(meta.AttendanceSheetType), OnClickSheet);
        }
    }

    void OnClickSheet(AttendanceSheetType sheetType)
    {
        if (!Managers.Attendance.IsConfigOpen(sheetType))
        {
            UI_ToastPopup toast = Managers.UI.ShowPopupUI<UI_ToastPopup>();
            toast.ShowAndAutoClose("점검 중입니다.");
            return;
        }

        UI_AttendanceDetail detail = Managers.UI.ShowPopupUI<UI_AttendanceDetail>();
        detail.SetSheetType(sheetType);
    }
}
