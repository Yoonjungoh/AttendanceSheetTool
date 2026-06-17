using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// "서버 저장 기능이 추가된다면?" / "PlayFab·Firebase 연동이 필요하다면?"에 대한 답을
/// 코드로 증명하기 위한 가짜 원격 저장소. 실제 네트워크 호출 대신 persistentDataPath의
/// 별도 파일에 기록하고, 호출부에는 인위적인 지연을 로그로만 남겨서 "여기가 실제로는
/// 네트워크 호출이 될 지점"임을 보여준다. SaveManager/도메인 매니저 코드는 전혀 모르고
/// ISaveStore 구현체만 이걸로 바뀐다.
/// </summary>
public class MockRemoteSaveStore : ISaveStore
{
    [Serializable]
    class Entry
    {
        public string Key;
        public string Json;
    }

    [Serializable]
    class EntryList
    {
        public List<Entry> Items = new List<Entry>();
    }

    static readonly string FilePath = Path.Combine(Application.persistentDataPath, "mock_remote_save.json");
    const int FakeLatencyMs = 150;

    readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

    public MockRemoteSaveStore()
    {
        Load();
    }

    public void Save(string key, string json)
    {
        System.Threading.Thread.Sleep(FakeLatencyMs); // 실제로는 await UnityWebRequest 등 비동기 호출이 될 지점
        _cache[key] = json;
        Flush();
        Debug.Log($"[Mock 서버] Save({key}) — {FakeLatencyMs}ms 지연 시뮬레이션 후 {FilePath}에 기록");
    }

    public string Load(string key)
    {
        System.Threading.Thread.Sleep(FakeLatencyMs);
        _cache.TryGetValue(key, out string json);
        Debug.Log($"[Mock 서버] Load({key}) — {FakeLatencyMs}ms 지연 시뮬레이션");
        return json ?? string.Empty;
    }

    public bool HasKey(string key) => _cache.ContainsKey(key);

    public void Delete(string key)
    {
        _cache.Remove(key);
        Flush();
    }

    void Load()
    {
        if (!File.Exists(FilePath))
            return;

        EntryList data = JsonUtility.FromJson<EntryList>(File.ReadAllText(FilePath));
        if (data == null)
            return;

        foreach (Entry entry in data.Items)
            _cache[entry.Key] = entry.Json;
    }

    void Flush()
    {
        EntryList data = new EntryList
        {
            Items = _cache.Select(kv => new Entry { Key = kv.Key, Json = kv.Value }).ToList(),
        };
        File.WriteAllText(FilePath, JsonUtility.ToJson(data));
    }
}
