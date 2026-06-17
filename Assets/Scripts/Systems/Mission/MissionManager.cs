using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// "미션 이벤트가 추가된다면?" 질문에 대한 실제 동작 증거. AttendanceManager와 동일한
/// 패턴(IPeriodEvent 어댑터 등록 + RewardManager.Grant 위임)을 한 번 더 적용했을 뿐이며,
/// EventManager/RewardManager/SaveManager는 이 클래스의 존재를 모른다(전부 무수정).
/// 진행 조건은 데모로 "출석 체크인 N회 달성"을 사용하고, AttendanceManager.GetHistory()를
/// 그대로 재사용한다 — 새 미션 진행 데이터를 따로 추적하는 코드가 필요 없다.
/// </summary>
public class MissionManager
{
    const string SaveKey = "Mission_Progress_v1";

    readonly Dictionary<int, MissionEventAdapter> _adapters = new Dictionary<int, MissionEventAdapter>();
    MissionProgressData _progress = new MissionProgressData();

    public void Init()
    {
        _adapters.Clear();
        foreach (MissionMetaData meta in MissionSampleData.CreateAll())
            _adapters[meta.Id] = new MissionEventAdapter(meta);

        Managers.Event.RegisterProvider(() => _adapters.Values.Cast<IPeriodEvent>().ToList());

        _progress = Managers.Save.Load<MissionProgressData>(SaveKey) ?? new MissionProgressData();
    }

    public List<MissionMetaData> GetAllMissions() => _adapters.Values.Select(a => a.Meta).ToList();

    public bool IsClaimed(int missionId) => _progress.ClaimedMissionIds.Contains(missionId);

    /// <summary>
    /// 미션 조건(출석 N회)을 만족하고 아직 수령하지 않았으면 보상을 지급한다.
    /// 실패 시 null(미달성/이미 수령/기간 아님 등 — AttendanceManager.CheckIn과 동일한 반환 규칙).
    /// </summary>
    public MissionMetaData ClaimReward(int missionId)
    {
        if (!_adapters.TryGetValue(missionId, out MissionEventAdapter adapter))
            return null;

        if (adapter.GetState(GameTime.Now) != EventActiveState.Active)
        {
            Debug.LogWarning($"[MissionManager] Mission#{missionId}: 기간 내가 아니라 수령할 수 없습니다.");
            return null;
        }

        if (IsClaimed(missionId))
        {
            Debug.LogWarning($"[MissionManager] Mission#{missionId}: 이미 수령한 미션입니다.");
            return null;
        }

        int progress = Managers.Attendance.GetHistory().Count;
        if (progress < adapter.Meta.RequiredAttendanceCount)
        {
            Debug.LogWarning($"[MissionManager] Mission#{missionId}: 진행도 미달({progress}/{adapter.Meta.RequiredAttendanceCount}).");
            return null;
        }

        _progress.ClaimedMissionIds.Add(missionId);
        Save();

        Managers.Reward.Grant(adapter.Meta.RewardType, adapter.Meta.RewardCount);
        return adapter.Meta;
    }

    /// <summary>저장된 미션 진행 상태를 삭제하고 초기화한다(치트 툴용).</summary>
    public void ResetAll()
    {
        Managers.Save.Delete(SaveKey);
        _progress = new MissionProgressData();
    }

    void Save() => Managers.Save.Save(SaveKey, _progress);
}
