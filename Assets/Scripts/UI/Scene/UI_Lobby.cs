using UnityEngine.UI;

/// <summary>
/// 데이터 로딩 완료 후 보여지는 로비 화면. 출석부 진입점만 제공한다.
/// 더미 프리팹 계층: UI_Lobby(Canvas) > AttendanceButton(Button) > Text(TMP)
/// </summary>
public class UI_Lobby : UI_Scene
{
    enum Buttons
    {
        AttendanceButton,
    }

    public override void Init()
    {
        base.Init();
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.AttendanceButton).onClick.AddListener(OnClickAttendance);
    }

    void OnClickAttendance()
    {
        Managers.UI.ShowPopupUI<UI_Attendance>();
    }
}
