#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// [에디터 전용] 출석/보상/이벤트 데이터를 다루는 치트 툴.
/// 런타임 싱글톤(Managers)에 의존하므로 Play 모드에서만 동작한다.
/// </summary>
public class CheatToolWindow : EditorWindow
{
    [MenuItem("Tools/Cheat Tool")]
    static void Open()
    {
        GetWindow<CheatToolWindow>("Cheat Tool");
    }

    void OnGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서만 사용할 수 있습니다.", MessageType.Info);
        }

        GUI.enabled = EditorApplication.isPlaying;

        if (GUILayout.Button("전체 데이터 삭제"))
        {
            Managers.Attendance.ResetAll();
            Managers.Reward.ResetAll();
            Managers.Event.ResetAll();
            Debug.Log("[Cheat] 모든 저장 데이터를 삭제했습니다.");
        }

        if (GUILayout.Button("하루 경과(+1일)"))
        {
            GameTime.AddDebugDays(1);
            Debug.Log($"[Cheat] 디버그 시간 +1일 적용. 현재 GameTime.Now = {GameTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        if (GUILayout.Button("출석 이력 로그 출력"))
        {
            foreach (AttendanceCheckInRecord record in Managers.Attendance.GetHistory())
                Debug.Log($"[Cheat] {record.CheckInDate} {record.SheetType} Day{record.Day} -> {record.RewardType} x{record.RewardCount}");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("저장소 전환 데모 (서버/PlayFab·Firebase 연동 대비)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("저장소: Local(PlayerPrefs)"))
        {
            Managers.Save.SetStore(new PlayerPrefsSaveStore());
            Debug.Log("[Cheat] 저장소를 PlayerPrefsSaveStore로 전환했습니다.");
        }
        if (GUILayout.Button("저장소: Mock 서버"))
        {
            Managers.Save.SetStore(new MockRemoteSaveStore());
            Debug.Log("[Cheat] 저장소를 MockRemoteSaveStore로 전환했습니다. 이후 Save/Load 호출 로그를 확인하세요.");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("미션 이벤트 데모", EditorStyles.boldLabel);
        if (GUILayout.Button("미션 진행 상태 로그 출력"))
        {
            foreach (MissionMetaData mission in Managers.Mission.GetAllMissions())
            {
                int progress = Managers.Attendance.GetHistory().Count;
                bool claimed = Managers.Mission.IsClaimed(mission.Id);
                Debug.Log($"[Cheat] Mission#{mission.Id} {mission.NameKey} - 진행 {progress}/{mission.RequiredAttendanceCount}, 수령여부={claimed}");
            }
        }
        if (GUILayout.Button("미션 보상 수령 시도"))
        {
            foreach (MissionMetaData mission in Managers.Mission.GetAllMissions())
            {
                bool success = Managers.Mission.ClaimReward(mission.Id) != null;
                Debug.Log($"[Cheat] Mission#{mission.Id} 보상 수령 {(success ? "성공" : "실패(미달성/이미 수령/기간 아님)")}");
            }
        }

        GUI.enabled = true;
    }
}
#endif
