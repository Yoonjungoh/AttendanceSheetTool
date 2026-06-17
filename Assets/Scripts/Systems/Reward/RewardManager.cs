using System.Collections.Generic;

/// <summary>
/// ItemType을 키로 하는 범용 수치 보상 원장(ledger).
/// 새 보상 타입(시즌 포인트, 캐릭터 조각 등)이 추가돼도 ItemType enum에 값만 추가하면
/// 이 클래스는 수정할 필요가 없다 — Grant/GetBalance가 타입에 무관하게 동작하기 때문.
/// 특정 타입에 한해 수치 누적 외의 처리가 필요하면 IRewardEffect를 등록해서 확장한다.
/// </summary>
public class RewardManager
{
    const string SaveKey = "Reward_Ledger_v1";

    readonly Dictionary<ItemType, long> _balances = new Dictionary<ItemType, long>();
    readonly Dictionary<ItemType, IRewardEffect> _effects = new Dictionary<ItemType, IRewardEffect>();

    public void Init()
    {
        _balances.Clear();
        RewardLedgerData data = Managers.Save.Load<RewardLedgerData>(SaveKey);
        if (data != null)
        {
            for (int i = 0; i < data.Types.Count && i < data.Values.Count; i++)
                _balances[data.Types[i]] = data.Values[i];
        }
    }

    /// <summary>특정 ItemType에 한해 추가 처리(이펙트 등)가 필요할 때 등록한다.</summary>
    public void RegisterEffect(ItemType type, IRewardEffect effect)
    {
        _effects[type] = effect;
    }

    public void Grant(ItemType type, int count)
    {
        if (count == 0)
            return;

        _balances.TryGetValue(type, out long current);
        _balances[type] = current + count;
        Save();

        if (_effects.TryGetValue(type, out IRewardEffect effect))
            effect.OnGrant(type, count);
    }

    public long GetBalance(ItemType type)
    {
        _balances.TryGetValue(type, out long value);
        return value;
    }

    void Save()
    {
        RewardLedgerData data = new RewardLedgerData();
        foreach (var kv in _balances)
        {
            data.Types.Add(kv.Key);
            data.Values.Add(kv.Value);
        }
        Managers.Save.Save(SaveKey, data);
    }

    /// <summary>저장된 보상 잔액을 삭제하고 초기화한다(치트 툴용).</summary>
    public void ResetAll()
    {
        Managers.Save.Delete(SaveKey);
        _balances.Clear();
    }

    /// <summary>보상을 받을 때마다 UI_Reward 팝업으로 목록을 보여준다.</summary>
    public void ShowRewardPopup(List<(ItemType type, int count)> rewards)
    {
        UI_Reward rewardUI = Managers.UI.ShowPopupUI<UI_Reward>();
        rewardUI.SetData(rewards);
    }

    /// <summary>여러 보상을 한 번에 지급하고, 팝업 1개에 전부 모아서 보여준다.</summary>
    public void Grant(List<(ItemType type, int count)> rewards)
    {
        foreach (var r in rewards)
            Grant(r.type, r.count);

        ShowRewardPopup(rewards);
    }
}
