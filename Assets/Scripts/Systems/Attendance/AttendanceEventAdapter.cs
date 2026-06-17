using System;

/// <summary>
/// AttendanceMetaData(스펙 데이터)를 IPeriodEvent로 변환하는 어댑터.
/// AttendanceType.DAILY는 상시 노출(AlwaysOn), EVENT_DAILY는 Start~End 기간에만 Active.
/// </summary>
public class AttendanceEventAdapter : IPeriodEvent
{
    readonly AttendanceMetaData _meta;

    public AttendanceEventAdapter(AttendanceMetaData meta)
    {
        _meta = meta;
    }

    public int Id => _meta.Id;
    public string NameKey => _meta.NameKey;
    public AttendanceSheetType SheetType => _meta.AttendanceSheetType;
    public AttendanceMetaData Meta => _meta;

    public EventActiveState GetState(DateTime now)
    {
        if (_meta.AttendanceType == AttendanceType.DAILY)
            return EventActiveState.AlwaysOn;

        if (now < _meta.StartDateTime)
            return EventActiveState.NotStarted;

        if (now > _meta.EndDateTime)
            return EventActiveState.Ended;
            
        return EventActiveState.Active;
    }
}
