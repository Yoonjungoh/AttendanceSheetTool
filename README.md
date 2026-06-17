# 기간제 이벤트 / 출석 / 보상 시스템

## 1. 프로젝트 정보

- Unity 버전: `2022.3.62f2` (LTS)
- 실행 방법:
  1. Unity Hub에서 `2022.3.62f2`로 프로젝트를 엽니다.
  2. `Assets/Scenes/SampleScene.unity`를 열고 Play합니다.
  3. 부팅 시 `Managers`가 Google Sheet에서 스펙 데이터를 다운로드(Item/Attendence/AttendanceReward/Config)한 뒤 `UI_Lobby`를 띄웁니다. "출석부" 버튼 → `UI_Attendance` 팝업에서 현재 노출 대상인 출석부만 보이고, 각 항목을 클릭하면 `UI_AttendanceDetail`에서 Day별 보상과 출석 버튼을 확인할 수 있습니다.
  4. 스펙 데이터 컬럼 구조를 바꾸려면 `Tools > Spec Data Generator`(에디터 메뉴)에서 재생성합니다 (변경하면서 테스트 해보시면 좋을 것 같습니다).
  스펙 시트 주소: https://docs.google.com/spreadsheets/d/1tKxQN-2wySfsCBaI9szU_itPk7Ob_MWZs4R0lKhvs-Y/edit?gid=17208319#gid=17208319

## 2. 시스템 설명

### 2.1 전체 구조

```
[Google Sheet]
   │ (gviz JSON, GvizParser)
   ▼
SpecDataManager / ConfigManager  ──(스펙 데이터, 운영 On/Off 플래그)
   │
   ▼
RewardManager ◄────────────┐
   ▲                       │ Grant(type, count)
   │ GetActiveEvents()     │
EventManager ◄── RegisterProvider ── AttendanceManager
   (범용 기간 판정)              (출석 진행도, 체크인)
   │                            │
   │                            ▼
   │                      SaveManager (PlayerPrefs + JsonUtility)
   ▼
UI_Lobby → UI_Attendance(목록) → UI_AttendanceDetail(상세, 출석 버튼)
   AttendanceSubItem            AttendanceRewardSubItem
                                   │
                                   ▼
                               ImageManager (ItemType → 아이콘 캐싱)
```

핵심 분리 원칙:
- 이벤트(기간 판정)와 출석(콘텐츠)을 분리했습니다. `EventManager`는 `IPeriodEvent`만 알고, `AttendanceManager`는 자신의 스펙 데이터를 `IPeriodEvent`로 변환(`AttendanceEventAdapter`)해서 등록할 뿐입니다. 그래서 `EventManager`는 출석이라는 개념을 전혀 모릅니다.
- 보상은 타입에 무관한 수치 원장(`RewardManager`)입니다. `ItemType`이 무엇이든 "더해주고 저장한다"는 동작만 하므로, 보상 종류가 늘어나도 코드가 늘어나지 않습니다.
- 저장은 전부 `SaveManager` 뒤에 숨겼습니다. 도메인 매니저들은 PlayerPrefs라는 사실을 모릅니다(`Save<T>/Load<T>`만 사용).
- UI는 표시 + 콜백 전달만 합니다. `AttendanceSubItem`/`AttendanceRewardSubItem`은 출석/보상 로직을 직접 호출하지 않고, 클릭 시 상위 팝업(`UI_Attendance`, `UI_AttendanceDetail`)이 매니저를 호출합니다.

### 2.2 주요 클래스 역할

| 클래스 | 역할 |
|---|---|
| `Managers` | 전체 매니저 DI 컨테이너 + 부팅 초기화(`InitAsync`, async UniTask) |
| `SpecDataManager` / `ConfigManager` | Google Sheet 스펙 데이터 / 운영 Config 다운로드·조회 |
| `SaveManager` | JsonUtility 기반 직렬화 + `ISaveStore` 위임 범용 저장/복원 (기본 매체: PlayerPrefs) |
| `IPeriodEvent` / `EventManager` | 기간제 노출 판정의 범용 계층. 콘텐츠별 매니저가 `RegisterProvider`로 자신의 이벤트 목록을 등록 |
| `AttendanceEventAdapter` | `AttendenceMetaData` → `IPeriodEvent` 변환 어댑터 |
| `AttendanceManager` | 출석 진행도 관리, 노출 필터링(Config + 기간), 체크인 처리 |
| `RewardManager` | `ItemType` 키 범용 보상 원장(Grant/GetBalance) |
| `ImageManager` | `ItemType` → 아이콘 `Sprite` 조회 + 캐싱 |
| `UI_Lobby` | 부팅 후 첫 화면, 출석부 진입점 |
| `UI_Attendance` / `AttendanceSubItem` | 노출 대상 출석부 목록 / 한 줄 표시 |
| `UI_AttendanceDetail` / `AttendanceRewardSubItem` | 특정 출석부의 Day별 보상 + 출석 버튼 / Day 한 칸 표시 |
| `UI_Reward` / `Reward_SubItem` | 보상 지급 직후 뜨는 팝업, 지급된 보상 목록을 한 번에 모아서 표시 |
| `GameTime` | "현재 시각"의 단일 진입점. 출석/이벤트 판정은 `DateTime.Now`를 직접 쓰지 않고 항상 이걸 통합니다 |
| `AttendanceCheckInRecord` / `GetHistory()` | 체크인 1건의 이력 레코드(언제/어떤 출석부/며칠차/무슨 보상) — CS 검증용 |
| `CheatToolWindow` (Editor 전용) | `Tools > Cheat Tool` 메뉴. 데이터 전체 삭제, 하루 경과 시뮬레이션, 이력 로그 출력, 저장소 전환/미션 데모 |
| `ISaveStore` / `PlayerPrefsSaveStore` / `MockRemoteSaveStore` | 저장 매체 추상화. `SaveManager`는 JSON 직렬화만 책임지고 실제 read/write는 이 인터페이스에 위임 |
| `MissionEventAdapter` / `MissionManager` | "미션 이벤트가 추가된다면?" 질문에 대한 실제 동작 증거. `AttendanceManager`와 동일한 패턴(어댑터 등록 + `RewardManager.Grant` 위임) |

### 2.3 데이터 모델 (Google Sheet, 코드 생성기로 자동 생성됨)

- `Attendance` 시트 → `AttendanceMetaData`: 출석부 1종(Id, AttendanceSheetType, AttendanceType, NameKey, TotalDays, Start/End).
  - `AttendanceType.DAILY`: 상시 출석부. Start/End 무시, `TotalDays` 길이로 보상 인덱스가 순환(1→…→TotalDays→1…)합니다.
  - `AttendanceType.EVENT_DAILY`: 기간 한정 출석부. Start~End 안에서만 노출, 1일차~TotalDays일차까지 1회성(순환 없음). `EVENT_DAILY`라는 이름은 추후 `EVENT_CONTINUOUS`(연속 출석) 등이 추가될 수 있어 포괄적으로 명명한 것이며, 이번 프로젝트에서는 `EVENT_DAILY`만 구현했습니다.
- `AttendanceReward` 시트 → `AttendanceRewardMetaData`: 출석부의 Day별 보상(AttendanceSheetType, Day, RewardType, RewardCount).
- `Config` 시트 → `ConfigType.IS_ATTENDANCE_SHEET_{SHEETTYPE}_OPEN`: 장애 대응용 킬스위치입니다. 라이브 중 특정 출석부에서 버그가 발견되면 코드 재배포 없이 이 값을 `false`로 바꿔 즉시 차단할 수 있습니다(단순 "운영 On/Off 토글"이 아니라 사고 대응 수단). 코드 생성기가 Config 시트 변경 시 자동 재생성하므로, 출석부가 100개 이상으로 늘어나도 사람이 enum을 직접 추가할 필요 없습니다(재생성만 하면 됩니다).

### 2.4 CS 검증용 출석 이력 + 치트 툴

- `AttendanceManager`는 체크인할 때마다 단순 누적 카운트만 늘리는 게 아니라, `AttendanceCheckInRecord`(언제/어떤 출석부/며칠차/무슨 보상) 1건을 `_history` 리스트에 추가하고 `Attendance_History_v1` 키로 저장합니다. "총 출석 횟수"처럼 흔히 카운터로 따로 저장하는 값은 이 리스트에서 파생(`GetHistory().Count`)시키므로 별도 카운터를 중복 저장하지 않습니다. CS 측에서 "그때 보상을 못 받았다" 같은 개별 사건을 검증하려면 단순 숫자보다 이런 이력 로그가 훨씬 유용하다고 판단했습니다.
- 치트 툴은 빌드에 포함되지 않는 Unity 에디터 메뉴(`Tools > Cheat Tool`, `CheatToolWindow`)로 제공합니다. 런타임 싱글톤(`Managers`)에 의존하므로 Play 모드에서만 동작하며, Play 모드가 아닐 때는 버튼이 비활성화됩니다.
  - 전체 데이터 삭제: `AttendanceManager`/`RewardManager`/`EventManager`의 `ResetAll()`만 호출합니다. 치트 툴은 PlayerPrefs 키 같은 세부사항을 모르고, 각 매니저가 "내 저장 데이터를 지우고 초기화"하는 책임을 스스로 집니다.
  - 하루 경과 시뮬레이션: 저장된 날짜 문자열(`LastCheckInDate`)을 직접 조작하지 않습니다. 대신 `GameTime.AddDebugDays(1)`로 "현재 시각" 자체를 하루 미루면, 출석 가능 여부 판정과 이벤트 기간 판정이 전부 일관되게 그 시점 기준으로 재계산됩니다. 이렇게 하면 EVENT_DAILY 출석부의 기간 만료 시뮬레이션도 같은 메커니즘으로 자연히 커버됩니다. `GameTime.OnTimeChanged` 이벤트를 `UI_Attendance`/`UI_AttendanceDetail`이 구독하고 있어서, 에디터에서 시간을 미뤄도 떠 있는 화면이 즉시 갱신됩니다.

## 3. 새로운 기능 추가 방법

### 새로운 출석 이벤트 추가 (예: "8주년 기념 14일 출석")
1. Google Sheet `Attendance` 탭에 행 추가 (`AttendanceSheetType=ANNIVERSARY_14`, `AttendanceType=EVENT_DAILY`, Start/End/TotalDays 지정).
2. `AttendanceReward` 탭에 해당 `AttendanceSheetType`의 Day 1~14 보상 행 추가.
3. `Config` 탭에 `IS_ATTENDANCE_SHEET_ANNIVERSARY_14_OPEN` 행 추가(true).
4. `Tools > Spec Data Generator`로 코드 재생성(`SheetEnums.cs`, `MetaData.cs` 등 자동 갱신).
5. C# 코드 수정이 필요하지 않습니다. `AttendanceManager.Init()`이 `GetAllAttendance()`를 순회하며 자동으로 어댑터를 만들고 `EventManager`에 등록하기 때문입니다.

### 새로운 보상 타입 추가 (예: "시즌 포인트")
1. Google Sheet에서 `ItemType` enum 정의에 `SEASON_POINT` 추가 → 코드 재생성.
2. 더미 아이콘 `Assets/Resources/Prefabs/Icons/Reward/SEASON_POINT.png` 배치.
3. `RewardManager`/`ImageManager` 코드 수정이 필요하지 않습니다. 둘 다 `ItemType`에 무관하게 동작하는 범용 원장/캐시이기 때문입니다.

### 새로운 이벤트 타입 추가
실제로 `Assets/Scripts/Systems/Mission/` 아래에 구현해서 동작을 확인했습니다(`Tools > Cheat Tool`의 "미션 이벤트 데모" 버튼으로 검증 가능).
1. `MissionMetaData`(스펙 — 데모라 하드코딩, 실 운영이라면 `Mission` 시트 + 코드 생성기로 자동 생성).
2. `MissionEventAdapter : IPeriodEvent` 작성(미션의 시작/종료 시각을 `IPeriodEvent`로 노출) — `AttendanceEventAdapter`와 동일한 패턴.
3. `MissionManager` 작성(미션별 진행 조건 체크는 `AttendanceManager.GetHistory().Count`를 재사용, 완료 시 `RewardManager.Grant` 호출).
4. `Managers.InitAsync()`에 `Mission.Init()` 한 줄 추가, 내부에서 `Event.RegisterProvider(...)` 호출.
5. `EventManager`, `RewardManager`, `SaveManager`, `UI_Base` 계열은 전부 수정하지 않았습니다. 새 매니저 1개만 추가됐으며, 설계 당시 예상한 확장성이 실제 구현을 통해 성립함을 확인했습니다

## 4. 설계하면서 고려한 점

- Google Sheet 기반 스펙 데이터 파이프라인(`GoogleSheetCodeGenerator`, `GvizParser`)을 직접 구현해 운영 데이터가 C# 코드에 자동으로 반영되는 구조를 만들었습니다. 출석부/보상 스키마가 요구사항(DAILY=상시 누적, EVENT_DAILY=기간제)과 정확히 대응되어, 데이터 정의와 런타임 로직을 깔끔하게 분리할 수 있었습니다.
- "기간제 이벤트"와 "출석"을 의도적으로 별도 계층(`IPeriodEvent`/`EventManager` vs `AttendanceManager`)으로 분리해서, "이벤트 종류가 늘어난다면" 상황에 구조적으로 대처할 수 있게 했습니다.
- 보상을 "특정 타입에 종속된 분기 로직"이 아니라 "타입에 무관한 수치 원장"으로 설계해서, 새 보상 타입 추가 시 C# 코드가 전혀 늘어나지 않게 했습니다. 다만 향후 비수치형 보상(예: 캐릭터 스킨 언락처럼 가산이 의미 없는 보상)이 생기면 이 가정이 깨지므로, `IRewardEffect` 훅을 남겨 예외적인 타입만 별도 처리할 수 있게 했습니다.
- 저장은 PlayerPrefs 그대로 쓰지 않고 `SaveManager` 뒤로 한 단계 숨겼습니다 — 서버 저장/PlayFab 전환 시 호출부를 건드리지 않기 위함입니다.
- 런타임 코루틴(`IEnumerator`/`StartCoroutine`)은 전부 UniTask(`async UniTask`/`UniTaskVoid`)로 통일했습니다. 에디터 전용 코드 생성기(`GoogleSheetCodeGenerator`)는 런타임 흐름과 무관해 그대로 코루틴으로 남겨뒀습니다.

## 5. 현재 구조의 한계와 개선 방향

- 이벤트 상태 캐시의 신뢰 범위: `EventManager`는 활성 여부를 항상 `DateTime.Now`로 재계산하고, 저장된 `EventStateCache`는 참고용일 뿐 판정에 쓰지 않습니다. 이는 기기 시간 조작에는 안전하지만, 반대로 서버 시간 동기화가 없으므로 클라이언트 시간 조작으로 기간 종료 출석부에 계속 접근하는 것은 막지 못합니다. 실제 서비스라면 서버 시간 기준 검증이 필요합니다.
- `SaveManager`는 `ISaveStore` 인터페이스로 저장 매체를 추상화해뒀습니다(`PlayerPrefsSaveStore`가 기본, `MockRemoteSaveStore`는 실제 네트워크 없이 지연·로그만 흉내 낸 데모 구현체). `Tools > Cheat Tool`에서 둘을 런타임에 전환할 수 있고, 전환해도 `AttendanceManager`/`RewardManager`/`EventManager` 호출부는 무수정입니다. 다만 `MockRemoteSaveStore`는 동기 API(`Thread.Sleep`)로 지연을 흉내 낸 것이라, 실제 PlayFab/Firebase는 비동기 API이므로 `ISaveStore`도 결국 `async`로 바꿔야 한다는 한계는 남아 있습니다.
- `ConfigType`이 출석부마다 `IS_ATTENDANCE_SHEET_{TYPE}_OPEN` 1:1 매핑이라, 출석부가 매우 많아지면 Config 시트 자체가 길어집니다. `Attendence` 시트에 `IsOpen` 컬럼을 직접 두는 방식이 더 단순할 수 있으나, Config 운영 패턴(실시간 On/Off, 코드 재배포 없이 변경)을 보여드리기 위해 현재 구조를 유지했습니다. 또한 연관 없는 단순 데이터 타입도 Config에 추가 가능합니다.
- `RewardManager`는 수치 가산만 가능한 보상을 가정합니다. 캐릭터 조각처럼 합성/소비가 필요한 보상은 결국 같은 패턴(타입별 수량 누적)으로 표현되지만, "장비 1개 지급" 같은 비-수치 보상은 별도 인벤토리 시스템이 필요하며 현재 범위 밖입니다.
- AttendanceManager의 Day 판정: 상시 출석부의 "사이클 진행도(ClaimedDays)"가 사이클 전환 시점에 초기화되는데, 이는 클라이언트에서만 검증되는 로직이라 서버 권위가 없는 구조입니다. 실제 라이브 서비스라면 출석 체크인 자체를 서버 API 호출로 처리하고 클라이언트는 결과만 반영해야 합니다.
- `AttendanceManager`는 상태 변경 시 `OnAttendanceChanged` 이벤트(순수 C# `event Action<T>`)만 발행하고, UI(`UI_Attendance`, `UI_AttendanceDetail`)는 이를 구독해서 스스로 갱신합니다 — 매니저가 UI를 직접 갱신하지 않도록 한 옵저버 패턴입니다. `RewardManager`/`EventManager`도 동일한 패턴(`OnRewardChanged` 등)으로 확장 가능하지만, 현재 그 값을 실시간으로 보여주는 화면이 없어 이번 범위에는 넣지 않았습니다. 예시로는 Reward를 얻음으로써 갱신되는 재화 UI가 있을 것 같습니다.
- `GameTime`의 디버그 시간 오프셋은 메모리에만 있고 저장하지 않습니다(앱 재시작 시 초기화). 의도된 동작이지만, 반대로 말하면 "하루 경과 시뮬레이션"은 영구적인 시간 조작이 아니라 현재 세션 한정 테스트 도구라는 한계가 있습니다. 실제 서비스라면 이 클래스 자체가 서버 시간 동기화 로직으로 교체될 자리입니다.
- `GameTime.Now`는 `DateTime.UtcNow` 기준이고, 앱이 켜진 상태로 UTC 자정을 넘기면 다음 자정 시점에 자동으로 `OnTimeChanged`가 발행되어 출석 UI가 갱신됩니다(매 프레임/매초 폴링이 아니라 "다음 경계까지 1회성 예약" 방식 — `GameTime.ScheduleRolloverLoopAsync`). 다만 이 예약은 OS가 백그라운드 앱의 타이머를 지연시키면 늦게 발화할 수 있어, `Managers.OnApplicationFocus`/`OnApplicationPause`에서 포그라운드 복귀 시 `GameTime.CheckRollover()`를 한 번 더 호출해 보정합니다.
- `AttendanceCheckInRecord` 이력 리스트는 별도 정리(pruning) 없이 계속 쌓입니다. PlayerPrefs 기반 프로토타입 범위에서는 문제 없지만, 실제 서비스에서는 서버 DB에 쌓고 오래된 기록은 별도 보관소로 옮기는 정책이 필요합니다.

## 6. 글로벌 서비스 설계 고려사항

- 날짜 경계를 기기 로컬 시간이 아닌 UTC로 고정했습니다. 로컬 자정을 기준으로 삼으면 리전마다 "하루"가 시작되는 실제 시각이 달라져 서버와 클라이언트의 판정이 어긋날 수 있고, 클라이언트가 시스템 시간대를 바꿔 자정을 더 자주 맞이하게 만들어 출석을 반복 수령하는 부정 사용 경로도 생깁니다. `GameTime.Now`를 `DateTime.UtcNow` 기준으로 통일해서 `AttendanceManager`/`EventManager`가 동일한 절대 기준으로 "오늘"을 판정하게 했습니다.
- 판정은 UTC, 표시는 로컬이 원칙입니다. 이번 범위의 UI는 "Day N 수령 가능/완료"만 보여주면 충분해 별도 텍스트가 없지만, 추후 "다음 출석까지 남은 시간" 같은 카운트다운을 추가한다면 `GameTime.Now`(UTC) 기준 남은 시간을 계산한 뒤 사용자의 로컬 시간대로 표시값만 변환해야 합니다 — 판정 기준 자체를 로컬로 바꾸면 안 됩니다.
- 앱이 켜져 있는 동안 자정을 넘기는 경우를 자동으로 처리해야 한다는 점도 글로벌 운영에서 중요합니다. 동시 접속자가 여러 시간대에 분산돼 있으면 "어떤 유저는 자정에 맞춰 재접속하고 어떤 유저는 계속 접속해 있는" 상황이 항상 발생합니다. 매 프레임/매초 폴링은 동시접속자 수와 무관하게 클라이언트 1대당 일정 비용이 들어 비효율적이므로, "다음 UTC 자정까지 남은 시간만큼 1회성 대기 후 콜백, 콜백 후 재예약"하는 방식(`GameTime.ScheduleRolloverLoopAsync`)을 택했습니다. 여기에 백그라운드 복귀 시 1회 보정 체크(`OnApplicationFocus`/`OnApplicationPause`)를 더해, OS의 백그라운드 타이머 지연으로 콜백이 늦게 발화하는 경우까지 안전하게 커버합니다.
- 다만 이 판정은 여전히 클라이언트 시간(`DateTime.UtcNow`) 기반이라, 기기 시간 자체를 조작하면 우회할 수 있다는 한계는 남아 있습니다(섹션 5의 "서버 시간 동기화" 항목과 동일한 한계). 실 서비스라면 체크인 가능 여부 자체를 서버 API가 UTC 기준으로 판정하고 클라이언트는 결과만 반영해야 합니다.
- 라이브 운영 중 장애 대응도 코드 재배포 없이 가능하게 설계했습니다. 특정 출석부에서 버그가 발견되면 운영자가 Google Sheet의 `Config` 시트에서 해당 `IS_ATTENDANCE_SHEET_{TYPE}_OPEN` 값을 `false`로 바꾸기만 하면 됩니다. 클라이언트는 다음 부팅 시 `ConfigManager.DownloadConfigAsync()`로 최신 값을 받아오므로, 별도 클라이언트 빌드/배포 없이 문제 콘텐츠만 즉시 비활성화할 수 있습니다. 단, 이미 실행 중인 클라이언트(앱을 끄지 않은 유저)에는 즉시 반영되지 않으므로, 실시간 차단이 꼭 필요하다면 Config를 주기적으로 재다운로드하거나 서버 푸시로 전환해야 한다는 한계는 남아 있습니다.

## 7. 작업 시간

| 항목 | 시간 |
|---|---|
| 설계 | 1.4h |
| 이벤트 시스템 구현 | 1.6h |
| 출석 시스템 구현 | 2.8h |
| 보상/이미지 시스템 구현 | 1.2h |
| UI | 3.0h |
| 확장 시나리오 대응(ISaveStore/Mock 저장소/미션 이벤트) | 3.0h |
| README | 1.4h |
| 총 작업 시간 | 약 14.4시간 |

## 8. AI 사용 내역

- 사용한 AI 도구: Claude (Claude Code)
- 사용 범위: 전체 시스템 구조(IPeriodEvent 계층 분리, ISaveStore 추상화, GameTime 단일 진입점, Config 킬스위치 방식 등)는 직접 설계·결정한 뒤 AI에 C# 스크립트 작성을 지시하는 방식으로 진행했습니다. 코드 생성 후에는 라인 단위로 로직을 검토·수정했으며, 추가 예정일 수 있는 `ISaveStore`/`MockRemoteSaveStore`/미션 이벤트 구현도 동일한 방식으로 진행했습니다. 

AI 프롬프팅은 기존 코드베이스 컨텍스트를 담은 MD 파일 3개를 참조 문서로 제공하는 방식으로 진행했습니다.
에이전트를 활용할 때, 아키텍트(Architect), 프로그래머(Programmer), QA로 역할을 엄격히 분리한 마크다운 문서를 운용했습니다.
한 페르소나가 코드를 작성하면 다른 관점을 가진 페르소나가 이를 다시 리뷰하고 약점을 찌르도록 유도하여, AI 자체적으로 1차적인 검증과 방어 로직을 구축하게 만드는 고도화된 구조를 실무에 적용하고 있습니다. 실제로 프로그래머가 로직을 짠 후, QA가 검증하다가 버그가 생기면 다시 프로그래머에게 검증을 요구해서 버그가 없는 상태로 사용자에게 도달합니다.
- 검증 방법: AI가 생성한 C# 스크립트는 먼저 라인 단위로 코드를 검토하며 설계 의도와의 일치 여부를 확인했습니다. 
의도하지 않은 패턴 이탈이나 로직 오류가 발견된 부분은 직접 수정했습니다. 이후 에디터에서 아래 흐름을 직접 실행해 최종 동작을 확인했습니다.
  1. 부팅 → Google Sheet 데이터 다운로드 완료 확인 (Item/Attendance/AttendanceReward/Config 4개 시트)
  2. `UI_Attendance` 팝업 — 기간 내 EVENT_DAILY 출석부만 노출되고, 기간 외 출석부는 표시되지 않는 것 확인
  3. `UI_AttendanceDetail` — 각 Day의 보상 목록, 오늘 출석 가능 Day 강조, 이미 수령한 Day 비활성화 확인
  4. 출석 버튼 클릭 → `UI_Reward` 팝업에 보상 종류·수량 정상 표시 확인
  5. 같은 Day 재수령 시도 → 버튼 비활성화로 중복 수령이 방지되는 것 확인
  6. 에디터 재실행 후 체크인 이력, 보상 잔액, 출석 진행 상태가 그대로 복원되는 것 확인
  7. `Tools > Cheat Tool`의 "하루 경과" 버튼으로 GameTime 오프셋을 +1일 → 출석 가능 여부 재판정 및 UI 즉시 갱신 확인
  8. "하루 경과" 반복으로 EVENT_DAILY 출석부의 TotalDays 도달 시 체크인 불가 처리 확인
  9. DAILY 출석부에서 TotalDays 사이클 완료 후 ClaimedDays 초기화 및 1일차 재진행 가능 여부 확인
  10. `Tools > Cheat Tool`의 저장소 전환(Local ↔ Mock 서버) 버튼 클릭 → 전환 후에도 매니저 호출부 무수정으로 동작하는 것 확인
  11. `Tools > Cheat Tool`의 미션 진행/보상 수령 버튼으로 MissionManager 코드 컴파일 및 동작 확인
  12. Google Sheet `Attendance` 시트의 START/END 날짜를 수정하고 재실행하여 기간 판정이 시트 데이터 기준으로 동적으로 변경되는 것 확인
