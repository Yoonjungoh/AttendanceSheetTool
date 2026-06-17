using System;
using Cysharp.Threading.Tasks;
using TMPro;

public class UI_ToastPopup : UI_Popup
{
    enum Texts
    {
        ToastPopupText,
    }

    public override void Init()
    {
        base.Init();
        Bind<TextMeshProUGUI>(typeof(Texts));
    }

    public void SetMessage(string message)
    {
        GetTextMeshProUGUI((int)Texts.ToastPopupText).text = message;
    }

    public async UniTaskVoid ShowAndAutoClose(string message, float duration = 1.5f)
    {
        SetMessage(message);
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());
        ClosePopupUI();
    }
}
