using System;
using System.Collections.Generic;

/// <summary>
/// 미션 1종의 스펙. "출석을 N회 달성하면 보상"처럼 기간제 + 조건 달성형 콘텐츠 예시다.
/// 실제 운영이라면 Attendance/Item처럼 Google Sheet에 Mission 탭을 추가하고
/// GoogleSheetCodeGenerator로 이 클래스도 자동 생성하는 게 정석이지만, 이 구현은
/// "새 이벤트 타입 추가 시 EventManager/RewardManager가 무수정인지"를 보여주는
/// 데모 목적이라 데이터를 하드코딩했다.
/// </summary>
[Serializable]
public class MissionMetaData
{
    public int Id;
    public string NameKey;
    public DateTime StartDateTime;
    public DateTime EndDateTime;
    public int RequiredAttendanceCount;
    public ItemType RewardType;
    public int RewardCount;
}

public static class MissionSampleData
{
    /// <summary>데모용 하드코딩 미션 목록. Start/End는 GameTime 기준으로 항상 활성 상태가 되도록 넉넉히 잡았다.</summary>
    public static List<MissionMetaData> CreateAll()
    {
        DateTime start = GameTime.Now.AddDays(-1);
        DateTime end = GameTime.Now.AddDays(30);

        return new List<MissionMetaData>
        {
            new MissionMetaData
            {
                Id = 1, NameKey = "MISSION_ATTEND_3",
                StartDateTime = start, EndDateTime = end,
                RequiredAttendanceCount = 3, RewardType = ItemType.JEWEL, RewardCount = 10,
            },
            new MissionMetaData
            {
                Id = 2, NameKey = "MISSION_ATTEND_7",
                StartDateTime = start, EndDateTime = end,
                RequiredAttendanceCount = 7, RewardType = ItemType.GOLD, RewardCount = 500,
            },
        };
    }
}
