using System;

/// <summary>
/// 기간제 노출 여부를 판정할 수 있는 모든 콘텐츠(출석 이벤트, 향후 미션/시즌 이벤트 등)가
/// 구현하는 공통 인터페이스. EventManager는 이 인터페이스로만 콘텐츠를 다루므로
/// 새 이벤트 타입을 추가해도 EventManager 코드는 수정할 필요가 없다.
/// </summary>
public interface IPeriodEvent
{
    int Id { get; }
    string NameKey { get; }
    EventActiveState GetState(DateTime now);
}
