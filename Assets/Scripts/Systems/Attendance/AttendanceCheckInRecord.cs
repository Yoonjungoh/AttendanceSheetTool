using System;
using System.Collections.Generic;

/// <summary>
/// 체크인 1건의 기록. 단순 누적 카운트 대신 이 레코드들의 리스트를 저장해서,
/// CS 문의("그때 보상을 못 받았다") 같은 개별 사건 검증에 쓸 수 있게 한다.
/// 총 출석 횟수 등 집계값은 이 리스트에서 파생시키면 되므로 별도 카운터를 두지 않는다.
/// </summary>
[Serializable]
public class AttendanceCheckInRecord
{
    public string CheckInDate;   // "yyyyMMdd"
    public AttendanceSheetType SheetType;
    public int Day;
    public ItemType RewardType;
    public int RewardCount;
}

[Serializable]
public class AttendanceHistoryData
{
    public List<AttendanceCheckInRecord> Records = new List<AttendanceCheckInRecord>();
}
