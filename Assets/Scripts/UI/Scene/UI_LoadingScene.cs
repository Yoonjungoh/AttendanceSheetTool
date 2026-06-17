using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_LoadingScene : UI_Scene
{
    private static UI_LoadingScene instance;
    public static UI_LoadingScene Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<UI_LoadingScene>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    instance = CreateLoadingScene();
                }
            }
            return instance;
        }
    }
    private static UI_LoadingScene CreateLoadingScene()
    {
        return Managers.Resource.Instantiate("LoadingScene").GetComponent<UI_LoadingScene>();
    }
    void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Slider sceneLoadSlider;

    [SerializeField]
    private TextMeshProUGUI loadingText;

    private string loadSceneName;
    public void LoadScene(string sceneName)
    {
        gameObject.SetActive(true);
        loadingText.color = new Color32(0, 0, 0, 255);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        loadSceneName = sceneName;
        LoadSceneProcessAsync().Forget();
    }
    async UniTask LoadSceneProcessAsync()
    {
        sceneLoadSlider.value = 0f;
        await FadeAsync(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(loadSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        while (op.isDone == false)
        {
            await UniTask.Yield();
            if (op.progress < 0.9f)
            {
                sceneLoadSlider.value = op.progress;
            }
            else
            {
                timer += Time.unscaledDeltaTime;
                sceneLoadSlider.value = Mathf.Lerp(0.9f, 1f, timer);
                if (sceneLoadSlider.value >= 1f)
                {
                    op.allowSceneActivation = true;
                    return;
                }
            }
        }
    }
    private async UniTask FadeAsync(bool isFadeIn)
    {
        float timer = 0f;
        while (timer <= 1f)
        {
            await UniTask.Yield();
            timer += Time.unscaledDeltaTime * 3f;
            canvasGroup.alpha = isFadeIn ? Mathf.Lerp(0f, 1f, timer) : Mathf.Lerp(1f, 0f, timer);
        }
        if (isFadeIn == false)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {

        // 씬 로딩 완료
        if (scene.name == loadSceneName)
        {
            FadeAsync(false).Forget();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
