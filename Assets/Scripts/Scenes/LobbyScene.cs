using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyScene : BaseScene
{
    protected override void Init()
    {
        Managers.Init();
    }

    private async UniTaskVoid Awake()
    {
        Init();
        await LoadLobbyAsync();
    }

    private async UniTask LoadLobbyAsync()
    {
        if (Managers.IsDataReady)
        {
            Managers.UI.ShowSceneUI<UI_Lobby>();
            return;
        }

        UI_ToastPopup loadingToast = Managers.UI.ShowPopupUI<UI_ToastPopup>();
        loadingToast.SetMessage("Server Data Loading...");

        await Managers.InitDataAsync();

        if (loadingToast != null)
            loadingToast.ClosePopupUI();

        Managers.UI.ShowSceneUI<UI_Lobby>();
    }
}
