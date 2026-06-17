using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers s_instance;
    static Managers Instance { get { Init(); return s_instance; } }

    DataManager _data = new DataManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    SpecDataManager _specData = new SpecDataManager();
    ConfigManager _config = new ConfigManager();
    UIManager _ui = new UIManager();
    URLManager _url = new URLManager();
    SaveManager _save = new SaveManager();
    ImageManager _image = new ImageManager();
    RewardManager _reward = new RewardManager();
    EventManager _event = new EventManager();
    AttendanceManager _attendance = new AttendanceManager();
    MissionManager _mission = new MissionManager();

    public static DataManager Data { get { return Instance._data; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static SpecDataManager SpecData { get { return Instance._specData; } }
    public static ConfigManager Config { get { return Instance._config; } }
    public static UIManager UI { get { return Instance._ui; } }
    public static URLManager URL { get { return Instance._url; } }
    public static SaveManager Save { get { return Instance._save; } }
    public static ImageManager Image { get { return Instance._image; } }
    public static RewardManager Reward { get { return Instance._reward; } }
    public static EventManager Event { get { return Instance._event; } }
    public static AttendanceManager Attendance { get { return Instance._attendance; } }
    public static MissionManager Mission { get { return Instance._mission; } }

    private void Start()
    {
        Init();
    }

    public static void Init()
    {
        if (s_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();

            s_instance._sound.Init();
            s_instance._resource.Init();
            GameTime.Init();
        }
    }

    public static void Clear()
    {
        Sound.Clear();
        UI.Clear();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            GameTime.CheckRollover();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
            GameTime.CheckRollover();
    }

    // ══════════════════════════════════════════════════════════
    // 전체 데이터 초기화 (SpecData → Config 순차 실행)
    // ══════════════════════════════════════════════════════════
    public static bool IsDataReady { get; private set; }
    public static bool IsDataLoading { get; private set; }

    public static UniTask InitDataAsync()
    {
        return Instance.InitAsync();
    }

    public async UniTask InitAsync()
    {
        if (IsDataReady)
            return;

        if (IsDataLoading)
        {
            while (IsDataLoading)
                await UniTask.Yield();
            return;
        }

        IsDataLoading = true;

        await SpecData.DownloadDataSheetAsync();
        await Config.DownloadConfigAsync();

        Reward.Init();
        Event.Init();
        Attendance.Init();
        Mission.Init();

        OnAllDataReady();

        IsDataLoading = false;
    }

    void OnAllDataReady()
    {
        IsDataReady = true;
        Debug.Log("[Managers] 모든 데이터 준비 완료. 게임 시작 가능.");
    }
}
