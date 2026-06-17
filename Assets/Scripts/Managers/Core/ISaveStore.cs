/// <summary>
/// SaveManager가 직렬화한 raw 문자열을 실제로 어디에 읽고 쓸지 결정하는 매체 계층.
/// SaveManager는 Wrapper&lt;T&gt;/JsonUtility 직렬화 책임을 그대로 갖고, 이 인터페이스의
/// 구현체만 교체하면 PlayerPrefs ↔ 서버(PlayFab/Firebase 등) ↔ Mock 서버 전환이 가능하다.
/// </summary>
public interface ISaveStore
{
    void Save(string key, string json);
    string Load(string key);
    bool HasKey(string key);
    void Delete(string key);
}
