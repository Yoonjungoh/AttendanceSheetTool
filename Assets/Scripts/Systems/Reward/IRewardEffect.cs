/// <summary>
/// 특정 ItemType에 한해 수치 누적 외 추가 처리(이펙트/사운드 등)가 필요할 때만 구현한다.
/// 등록되지 않은 ItemType은 RewardManager의 기본 수치 누적 로직만 동작한다.
/// </summary>
public interface IRewardEffect
{
    void OnGrant(ItemType type, int count);
}
