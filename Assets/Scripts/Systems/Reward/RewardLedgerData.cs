using System;
using System.Collections.Generic;

/// <summary>
/// RewardManager 저장용 DTO. JsonUtility는 Dictionary를 직렬화할 수 없으므로
/// Key/Value를 평행 List로 평탄화해서 저장한다.
/// </summary>
[Serializable]
public class RewardLedgerData
{
    public List<ItemType> Types = new List<ItemType>();
    public List<long> Values = new List<long>();
}
