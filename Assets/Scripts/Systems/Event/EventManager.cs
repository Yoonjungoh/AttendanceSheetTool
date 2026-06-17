using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 콘텐츠별 매니저(AttendanceManager 등)가 등록한 IPeriodEvent 제공자를 취합해서
/// 통합 조회 API만 제공하는 범용 계층. 새 콘텐츠(미션 이벤트 등) 추가 시
/// RegisterProvider 한 줄만 추가하면 되고 이 클래스 자체는 수정하지 않는다.
///
/// 활성 여부는 항상 호출 시점의 DateTime.Now로 재계산한다. 저장된 상태값을
/// 그대로 신뢰하지 않는 이유: 기기 시간 조작이나 오프라인 중 시간 경과에
/// 취약해지기 때문이다(README 한계 섹션 참고). 저장은 "최근에 본 상태" 캐시
/// 용도로만 사용한다.
/// </summary>
[Serializable]
public class EventStateCache
{
    public int EventId;
    public string LastKnownState;
}

[Serializable]
public class EventStateCacheList
{
    public List<EventStateCache> Items = new List<EventStateCache>();
}

public class EventManager
{
    const string SaveKey = "Event_StateCache_v1";

    readonly List<Func<List<IPeriodEvent>>> _providers = new List<Func<List<IPeriodEvent>>>();
    Dictionary<int, EventStateCache> _stateCache = new Dictionary<int, EventStateCache>();

    public void Init()
    {
        EventStateCacheList saved = Managers.Save.Load<EventStateCacheList>(SaveKey);
        _stateCache = saved != null
            ? saved.Items.ToDictionary(x => x.EventId, x => x)
            : new Dictionary<int, EventStateCache>();
    }

    /// <summary>콘텐츠별 매니저가 자신의 IPeriodEvent 목록 제공자를 등록한다.</summary>
    public void RegisterProvider(Func<List<IPeriodEvent>> provider)
    {
        _providers.Add(provider);
    }

    public List<IPeriodEvent> GetAllEvents()
    {
        List<IPeriodEvent> result = new List<IPeriodEvent>();
        foreach (var provider in _providers)
            result.AddRange(provider());
        return result;
    }

    public List<IPeriodEvent> GetActiveEvents()
    {
        DateTime now = GameTime.Now;
        List<IPeriodEvent> result = new List<IPeriodEvent>();

        foreach (IPeriodEvent ev in GetAllEvents())
        {
            EventActiveState state = ev.GetState(now);
            CacheState(ev.Id, state);

            if (state == EventActiveState.Active || state == EventActiveState.AlwaysOn)
                result.Add(ev);
        }

        Save();
        return result;
    }

    void CacheState(int eventId, EventActiveState state)
    {
        if (!_stateCache.TryGetValue(eventId, out EventStateCache cache))
        {
            cache = new EventStateCache { EventId = eventId };
            _stateCache[eventId] = cache;
        }
        cache.LastKnownState = state.ToString();
    }

    void Save()
    {
        EventStateCacheList data = new EventStateCacheList { Items = _stateCache.Values.ToList() };
        Managers.Save.Save(SaveKey, data);
    }

    /// <summary>저장된 이벤트 상태 캐시를 삭제하고 초기화한다(치트 툴용).</summary>
    public void ResetAll()
    {
        Managers.Save.Delete(SaveKey);
        _stateCache.Clear();
    }
}
