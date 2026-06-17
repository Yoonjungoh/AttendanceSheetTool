using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 출석부 진행도 관리 + 체크인 + 노출 필터링을 담당.
/// 이벤트 활성 판정은 EventManager에 위임하고, 보상 지급은 RewardManager에 위임한다
/// (출석/이벤트/보상의 책임 분리).
/// </summary>
public class AttendanceManager
{
    const string SaveKey = "Attendance_Progress_v1";
    const string HistorySaveKey = "Attendance_History_v1";

    /// <summary>
    /// 출석 진행도가 바뀔 때 발행된다. 매니저는 "상태가 바뀌었다"만 알리고,
    /// 그 정보로 무엇을 갱신할지는 구독하는 UI가 각자 판단한다(책임 분리).
    /// </summary>
    public event Action<AttendanceSheetType> OnAttendanceChanged;

    readonly Dictionary<AttendanceSheetType, AttendanceEventAdapter> _adapters =
        new Dictionary<AttendanceSheetType, AttendanceEventAdapter>();
    readonly Dictionary<AttendanceSheetType, AttendanceProgressData> _progress =
        new Dictionary<AttendanceSheetType, AttendanceProgressData>();
    AttendanceHistoryData _history = new AttendanceHistoryData();

    public void Init()
    {
        _adapters.Clear();
        foreach (AttendanceMetaData meta in Managers.SpecData.GetAllAttendance())
            _adapters[meta.AttendanceSheetType] = new AttendanceEventAdapter(meta);

        Managers.Event.RegisterProvider(() => _adapters.Values.Cast<IPeriodEvent>().ToList());

        AttendanceProgressList saved = Managers.Save.Load<AttendanceProgressList>(SaveKey);
        _progress.Clear();
        if (saved != null)
        {
            foreach (AttendanceProgressData data in saved.Items)
                _progress[data.SheetType] = data;
        }

        _history = Managers.Save.Load<AttendanceHistoryData>(HistorySaveKey) ?? new AttendanceHistoryData();
    }

    /// <summary>
    /// 체크인 이력 전체 또는 특정 출석부만 필터링해서 반환한다. CS 검증/디버그 툴에서 사용.
    /// 총 출석 횟수 같은 집계값은 이 리스트에서 파생시키면 되므로 별도 카운터를 두지 않는다.
    /// </summary>
    public List<AttendanceCheckInRecord> GetHistory(AttendanceSheetType? filter = null)
    {
        if (filter == null)
            return _history.Records;

        return _history.Records.Where(r => r.SheetType == filter.Value).ToList();
    }

    /// <summary>모든 출석 진행도/이력을 삭제하고 깨끗한 상태로 다시 초기화한다(치트 툴용).</summary>
    public void ResetAll()
    {
        Managers.Save.Delete(SaveKey);
        Managers.Save.Delete(HistorySaveKey);
        _progress.Clear();
        _history = new AttendanceHistoryData();
    }

    /// <summary>
    /// 다음 체크인에서 받게 될 Day(보상 인덱스)를 미리 계산한다. CheckIn과 완전히 같은 공식을
    /// 써야 하므로(상시 출석부는 TotalDays 주기로 순환), UI는 이 메서드만 물어보고 직접
    /// 계산하지 않는다 — 계산이 두 곳에 따로 있으면 사이클이 한 번 돈 뒤부터 서로 어긋난다.
    /// </summary>
    public int GetNextDayIndex(AttendanceSheetType sheetType)
    {
        if (!_adapters.TryGetValue(sheetType, out AttendanceEventAdapter adapter))
            return 0;

        AttendanceProgressData progress = GetProgress(sheetType);
        return ComputeDayIndex(adapter.Meta, progress.CheckInCount + 1);
    }

    static int ComputeDayIndex(AttendanceMetaData meta, int checkInCount)
    {
        return meta.AttendanceType == AttendanceType.DAILY
            ? ((checkInCount - 1) % meta.TotalDays) + 1   // 상시 출석부: TotalDays 주기로 순환
            : checkInCount;                                // 기간 한정 출석부: 1회성 누적
    }

    /// <summary>현재 시각 기준으로 노출해야 하는 출석부 목록을 반환한다.</summary>
    public List<AttendanceMetaData> GetVisibleSheets()
    {
        HashSet<int> activeIds = Managers.Event.GetActiveEvents().Select(e => e.Id).ToHashSet();

        return _adapters.Values
            .Where(a => activeIds.Contains(a.Id))
            .Select(a => a.Meta)
            .ToList();
    }

    /// <summary>Config 점검 스위치 기준으로 해당 출석부가 실제로 진입 가능한 상태인지 판정한다.</summary>
    public bool IsConfigOpen(AttendanceSheetType sheetType)
    {
        string key = $"IS_ATTENDANCE_SHEET_{sheetType}_OPEN";
        if (!Enum.TryParse(key, true, out ConfigType configType))
            return true; // Config에 스위치가 정의되지 않은 출석부는 기본 노출

        return Managers.Config.GetBool(configType);
    }

    /// <summary>
    /// 현재 사이클(상시 출석부) 또는 전체 기간(기간 한정 출석부) 기준으로 수령 완료한 일수.
    /// ClaimedDays는 상시 출석부의 경우 사이클이 바뀔 때마다 초기화되므로 그대로 사용 가능.
    /// </summary>
    public int GetClaimedDayCount(AttendanceSheetType sheetType) => GetProgress(sheetType).ClaimedDays.Count;

    public AttendanceProgressData GetProgress(AttendanceSheetType sheetType)
    {
        if (!_progress.TryGetValue(sheetType, out AttendanceProgressData data))
        {
            data = new AttendanceProgressData { SheetType = sheetType };
            _progress[sheetType] = data;
        }
        return data;
    }

    public bool CanCheckInToday(AttendanceSheetType sheetType)
    {
        AttendanceProgressData progress = GetProgress(sheetType);
        string today = GameTime.Now.ToString("yyyyMMdd");
        if (progress.LastCheckInDate == today)
        {
            Debug.LogWarning($"[AttendanceManager] {sheetType}: 날짜 비교로 막힘 - " +
                $"LastCheckInDate={progress.LastCheckInDate}, today={today}, GameTime.Now={GameTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            return false;
        }

        if (_adapters.TryGetValue(sheetType, out AttendanceEventAdapter adapter)
            && adapter.Meta.AttendanceType == AttendanceType.EVENT_DAILY
            && progress.CheckInCount >= adapter.Meta.TotalDays)
        {
            Debug.LogWarning($"[AttendanceManager] {sheetType}: EVENT_DAILY TotalDays 도달로 막힘 - " +
                $"CheckInCount={progress.CheckInCount}, TotalDays={adapter.Meta.TotalDays}");
            return false; // 기간 한정 출석부는 TotalDays까지만 1회성으로 진행
        }

        return true;
    }

    /// <summary>
    /// 체크인 + 보상 지급을 한 번에 처리한다. 성공 시 지급된 보상 정보를 반환하고,
    /// 이미 출석했거나 진행 불가 상태면 null을 반환한다. 검증을 모두 통과하기 전까지는
    /// progress를 변경하지 않는다(보상 데이터 누락 등으로 실패해도 상태가 깨지지 않도록).
    /// </summary>
    public List<AttendanceRewardMetaData> CheckIn(AttendanceSheetType sheetType)
    {
        if (!CanCheckInToday(sheetType))
            return null; // 막힌 구체적인 이유는 CanCheckInToday 내부에서 이미 로그됨

        if (!_adapters.TryGetValue(sheetType, out AttendanceEventAdapter adapter))
        {
            Debug.LogWarning($"[AttendanceManager] {sheetType}: _adapters에 해당 출석부가 없습니다. (Init 시점에 등록되지 않았거나 sheetType 불일치)");
            return null;
        }

        AttendanceProgressData progress = GetProgress(sheetType);
        int nextCheckInCount = progress.CheckInCount + 1;

        // UI(UI_AttendanceDetail)도 GetNextDayIndex()로 같은 공식을 쓴다 — 계산이 두 곳에
        // 따로 있으면 상시 출석부 사이클이 한 번 돈 뒤부터 서로 어긋난다.
        int dayIndex = ComputeDayIndex(adapter.Meta, nextCheckInCount);

        // 상시 출석부는 새 사이클이 시작될 때(dayIndex가 1로 되돌아올 때) 이전 사이클의
        // 수령 기록을 초기화해야 한다. 그렇지 않으면 2번째 사이클의 1일차가 1번째 사이클에서
        // 이미 수령한 Day로 취급되어 막혀버린다.
        bool isNewCycle = adapter.Meta.AttendanceType == AttendanceType.DAILY && dayIndex == 1;

        if (!isNewCycle && progress.ClaimedDays.Contains(dayIndex))
        {
            Debug.LogWarning($"[AttendanceManager] {sheetType}: Day {dayIndex}는 이미 수령한 보상입니다. ClaimedDays=[{string.Join(",", progress.ClaimedDays)}]");
            return null; // 이미 수령한 Day - 중복 수령 방지 (날짜 체크와 별개의 추가 방어)
        }

        List<AttendanceRewardMetaData> rewards = Managers.SpecData.GetAllAttendanceReward()
            .Where(r => r.AttendanceSheetType == sheetType && r.Day == dayIndex)
            .ToList();

        if (rewards.Count == 0)
        {
            Debug.LogWarning($"[AttendanceManager] {sheetType}: Day {dayIndex}에 해당하는 보상 데이터가 없습니다(스펙 데이터 확인 필요). TotalDays={adapter.Meta.TotalDays}");
            return null; // 보상 데이터 누락 - 진행 상태를 변경하지 않고 그대로 반환
        }

        if (isNewCycle)
            progress.ClaimedDays.Clear();

        progress.CheckInCount = nextCheckInCount;
        progress.ClaimedDays.Add(dayIndex);
        progress.LastCheckInDate = GameTime.Now.ToString("yyyyMMdd");
        Save();

        foreach (AttendanceRewardMetaData reward in rewards)
            AddHistory(sheetType, dayIndex, reward, progress.LastCheckInDate);

        // Day당 보상이 여러 종류일 수 있으므로(예: Day7 = 아이템 2종) 한 번에 모아 지급해야
        // 팝업도 보상 종류별로 따로 뜨지 않고 한 팝업에 전부 모여서 표시된다.
        Managers.Reward.Grant(rewards.Select(r => (r.RewardType, r.RewardCount)).ToList());
        OnAttendanceChanged?.Invoke(sheetType);
        return rewards;
    }

    void AddHistory(AttendanceSheetType sheetType, int day, AttendanceRewardMetaData reward, string checkInDate)
    {
        _history.Records.Add(new AttendanceCheckInRecord
        {
            CheckInDate = checkInDate,
            SheetType = sheetType,
            Day = day,
            RewardType = reward.RewardType,
            RewardCount = reward.RewardCount,
        });
        Managers.Save.Save(HistorySaveKey, _history);
    }

    void Save()
    {
        AttendanceProgressList data = new AttendanceProgressList { Items = _progress.Values.ToList() };
        Managers.Save.Save(SaveKey, data);
    }
}
