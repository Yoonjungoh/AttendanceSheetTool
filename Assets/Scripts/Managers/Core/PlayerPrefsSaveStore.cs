using UnityEngine;

/// <summary>
/// 로컬(PlayerPrefs) 저장소. SaveManager의 기본 ISaveStore 구현체.
/// </summary>
public class PlayerPrefsSaveStore : ISaveStore
{
    public void Save(string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public string Load(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }

    public bool HasKey(string key) => PlayerPrefs.HasKey(key);

    public void Delete(string key) => PlayerPrefs.DeleteKey(key);
}
