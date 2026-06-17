using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 게임 전역에서 "현재 시각"을 가져오는 단일 진입점.
/// 출석/이벤트 판정 코드는 DateTime.Now를 직접 쓰지 않고 항상 이 클래스를 통해야 한다.
/// 글로벌 서비스를 가정해 UTC를 기준으로 날짜 경계를 잡는다(리전별 로컬 자정 차이로
/// 판정이 어긋나거나 기기 시간대를 바꿔 출석을 반복하는 것을 방지).
/// 치트 툴의 "하루 경과 시뮬레이션"이 디버그 오프셋만 더해서 전체 판정 로직을 일관되게
/// 흔들 수 있고, 추후 서버 시간 동기화가 필요해지면 이 클래스 내부만 교체하면 된다.
/// </summary>
public static class GameTime
{
    static TimeSpan _debugOffset = TimeSpan.Zero;
    static string _lastKnownDate;
    static bool _initialized;

    public static DateTime Now => DateTime.UtcNow + _debugOffset;

    /// <summary>날짜 경계를 넘었을 때(자동 롤오버 또는 치트 툴의 시간 조작) 발행된다.</summary>
    public static event Action OnTimeChanged;

    /// <summary>
    /// 앱 시작 시 1회 호출. 다음 UTC 자정에 자동으로 OnTimeChanged를 발행하는 감시 루프를 시작한다.
    /// 매 프레임/매초 폴링 대신 "다음 경계까지 1회성 예약"이라 비용이 거의 없다.
    /// </summary>
    public static void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        _lastKnownDate = Now.ToString("yyyyMMdd");
        ScheduleRolloverLoopAsync().Forget();
    }

    static async UniTaskVoid ScheduleRolloverLoopAsync()
    {
        while (true)
        {
            DateTime now = Now;
            DateTime nextMidnight = now.Date.AddDays(1);
            TimeSpan delay = nextMidnight - now;

            await UniTask.Delay(delay, ignoreTimeScale: true);
            CheckRollover();
        }
    }

    /// <summary>
    /// 현재 날짜가 마지막으로 관측한 날짜와 다르면 OnTimeChanged를 발행한다.
    /// 포그라운드 복귀 시(OnApplicationFocus) 호출해서, 백그라운드 중 OS가 타이머를
    /// 지연시켜 자동 롤오버를 놓친 경우를 보정하는 안전망으로도 쓰인다.
    /// </summary>
    public static void CheckRollover()
    {
        string today = Now.ToString("yyyyMMdd");
        if (today == _lastKnownDate)
            return;

        _lastKnownDate = today;
        OnTimeChanged?.Invoke();
    }

    public static void AddDebugDays(int days)
    {
        _debugOffset += TimeSpan.FromDays(days);
        CheckRollover();
    }

    public static void ResetDebugOffset()
    {
        _debugOffset = TimeSpan.Zero;
        CheckRollover();
    }
}
