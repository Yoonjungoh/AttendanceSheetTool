using System;
using System.Collections.Generic;

/// <summary>
/// 출석부 1종(AttendenceSheetType)의 진행도 저장용 DTO.
/// </summary>
[Serializable]
public class AttendanceProgressData
{
    public AttendanceSheetType SheetType;
    public int CheckInCount;
    public List<int> ClaimedDays = new List<int>();
    public string LastCheckInDate = string.Empty;
}

[Serializable]
public class AttendanceProgressList
{
    public List<AttendanceProgressData> Items = new List<AttendanceProgressData>();
}
