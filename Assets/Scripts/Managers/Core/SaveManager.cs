using UnityEngine;

/// <summary>
/// 범용 저장소. 도메인 매니저(Attendance, Reward, Event 등)는 저장 포맷/매체를 직접
/// 다루지 않고 이 클래스를 통해서만 Save/Load 한다. JSON 직렬화(Wrapper/JsonUtility)
/// 책임은 여기서 그대로 갖고, 실제로 어디에 쓰고 읽을지는 ISaveStore에 위임한다.
/// 추후 서버 저장이나 PlayFab/Firebase 연동이 필요해지면 ISaveStore 구현체만
/// 새로 만들어 생성자에 넘기면 되고, 도메인 매니저 쪽 호출부는 무수정이다.
/// (데모용 구현체: PlayerPrefsSaveStore, MockRemoteSaveStore)
/// </summary>
public class SaveManager
{
    [System.Serializable]
    class Wrapper<T>
    {
        public T Value;
        public Wrapper(T value) { Value = value; }
    }

    ISaveStore _store;

    public SaveManager(ISaveStore store = null)
    {
        _store = store ?? new PlayerPrefsSaveStore();
    }

    /// <summary>저장소 구현체를 교체한다(치트 툴의 Local/Mock 서버 전환 데모용).</summary>
    public void SetStore(ISaveStore store)
    {
        _store = store ?? new PlayerPrefsSaveStore();
    }

    public void Save<T>(string key, T data)
    {
        string json = JsonUtility.ToJson(new Wrapper<T>(data));
        _store.Save(key, json);
    }

    public T Load<T>(string key, T defaultValue = default)
    {
        if (!_store.HasKey(key))
            return defaultValue;

        string json = _store.Load(key);
        if (string.IsNullOrEmpty(json))
            return defaultValue;

        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper != null ? wrapper.Value : defaultValue;
    }

    public bool HasKey(string key) => _store.HasKey(key);

    public void Delete(string key) => _store.Delete(key);
}
