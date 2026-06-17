using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특정 출석부의 Day별 보상 목록 + 출석 버튼을 보여주는 상세 팝업.
/// 더미 프리팹 계층:
/// UI_AttendanceDetail(Canvas) > TitleText(TMP), CloseButton(Button), CheckInButton(Button), Content(레이아웃 그룹)
/// </summary>
public class UI_AttendanceDetail : UI_Popup
{
    enum Texts
    {
        TitleText,
    }

    enum Buttons
    {
        CheckInButton,
        CloseButton,
    }

    enum GameObjects
    {
        Content,
    }

    AttendanceSheetType _sheetType;

    public override void Init()
    {
        base.Init();
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(GameObjects));

        GetButton((int)Buttons.CloseButton).onClick.AddListener(ClosePopupUI);
        GetButton((int)Buttons.CheckInButton).onClick.AddListener(OnClickCheckIn);

        Managers.Attendance.OnAttendanceChanged -= OnAttendanceChanged;
        Managers.Attendance.OnAttendanceChanged += OnAttendanceChanged;

        GameTime.OnTimeChanged -= OnTimeChanged;
        GameTime.OnTimeChanged += OnTimeChanged;
    }

    void OnDestroy()
    {
        Managers.Attendance.OnAttendanceChanged -= OnAttendanceChanged;
        GameTime.OnTimeChanged -= OnTimeChanged;
    }

    void OnAttendanceChanged(AttendanceSheetType sheetType)
    {
        if (sheetType == _sheetType)
            Refresh();
    }

    void OnTimeChanged()
    {
        Refresh();
    }

    public void SetSheetType(AttendanceSheetType sheetType)
    {
        _sheetType = sheetType;
        Refresh();
    }

    void OnClickCheckIn()
    {
        // 체크인 성공/실패와 무관하게 화면 갱신은 OnAttendanceChanged 구독 핸들러가 담당한다.
        // 여기서는 호출 결과에 대한 사용자 안내(실패 사유 로그)만 처리한다.
        List<AttendanceRewardMetaData> rewards = Managers.Attendance.CheckIn(_sheetType);
        if (rewards == null || rewards.Count == 0)
            Debug.Log("[UI_AttendanceDetail] 이미 오늘 출석했거나 더 이상 진행할 수 없는 출석부입니다.");
        else
            Debug.Log($"[UI_AttendanceDetail] 보상 지급: {string.Join(", ", rewards.Select(r => $"{r.RewardType} x{r.RewardCount}"))}");
    }

    void Refresh()
    {
        // GetAttendance(AttendanceSheetType)는 내부적으로 Id 컬럼을 enum 정수값으로 오해해 조회하는
        // 기존 자동생성 코드의 시맨틱 불일치가 있어, 여기서는 AttendanceSheetType 필드로 직접 필터링한다.
        AttendanceMetaData meta = Managers.SpecData.GetAllAttendance()
            .FirstOrDefault(m => m.AttendanceSheetType == _sheetType);
        if (meta == null)
            return;

        GetTextMeshProUGUI((int)Texts.TitleText).text = meta.NameKey;
        GetButton((int)Buttons.CheckInButton).interactable = Managers.Attendance.CanCheckInToday(_sheetType);

        AttendanceProgressData progress = Managers.Attendance.GetProgress(_sheetType);

        Transform content = GetObject((int)GameObjects.Content).transform;
        for (int i = content.childCount - 1; i >= 0; i--)
            Managers.Resource.Destroy(content.GetChild(i).gameObject);

        List<AttendanceRewardMetaData> rewards = Managers.SpecData.GetAllAttendanceReward()
            .Where(r => r.AttendanceSheetType == _sheetType)
            .OrderBy(r => r.Day)
            .ToList();

        // "오늘 차례"가 몇 일차인지는 AttendanceManager.CheckIn과 완전히 같은 공식을 써야 하므로
        // 여기서 따로 계산하지 않고 매니저에게 그대로 물어본다(계산이 두 곳에 있으면 상시 출석부
        // 사이클이 한 번 돈 뒤부터 서로 어긋난다).
        int nextDayIndex = Managers.Attendance.GetNextDayIndex(_sheetType);

        foreach (AttendanceRewardMetaData reward in rewards)
        {
            bool isClaimed = progress.ClaimedDays.Contains(reward.Day);
            bool isToday = !isClaimed && reward.Day == nextDayIndex;
            bool isLocked = !isClaimed && !isToday;

            AttendanceRewardSubItem item = Managers.UI.MakeSubItem<AttendanceRewardSubItem>(content);
            item.SetInfo(reward, isClaimed, isToday, isLocked);
        }
    }
}
