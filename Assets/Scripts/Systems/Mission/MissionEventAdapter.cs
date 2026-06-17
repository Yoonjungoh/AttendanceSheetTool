using System;

/// <summary>
/// MissionMetaData를 IPeriodEvent로 변환하는 어댑터. AttendanceEventAdapter와 동일한 패턴이다.
/// EventManager는 이 클래스의 존재를 모르고 IPeriodEvent로만 다루므로, 미션이라는
/// 새 콘텐츠 타입이 추가되어도 EventManager 코드는 무수정이다.
/// </summary>
public class MissionEventAdapter : IPeriodEvent
{
    readonly MissionMetaData _meta;

    public MissionEventAdapter(MissionMetaData meta)
    {
        _meta = meta;
    }

    public int Id => _meta.Id;
    public string NameKey => _meta.NameKey;
    public MissionMetaData Meta => _meta;

    public EventActiveState GetState(DateTime now)
    {
        if (now < _meta.StartDateTime)
            return EventActiveState.NotStarted;

        if (now > _meta.EndDateTime)
            return EventActiveState.Ended;

        return EventActiveState.Active;
    }
}
