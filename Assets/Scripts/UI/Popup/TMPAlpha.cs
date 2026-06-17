using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TMPAlpha : MonoBehaviour
{
    // 알파값을 시간에 따라 보간해서 텍스트를 서서히 사라지게 함
    [SerializeField]
    private float lerpTime;
    private TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    public void FadeOut()
    {
        AlphaLerpAsync(1, 0).Forget();
    }
    private async UniTask AlphaLerpAsync(float start, float end)
    {
        float currentTime = 0.0f;
        float percent = 0.0f;

        while (percent < 1.0f)
        {
            currentTime += Time.deltaTime;
            percent = currentTime / lerpTime;

            Color color = text.color;
            color.a = Mathf.Lerp(start, end, percent);
            text.color = color;

            await UniTask.Yield();
        }
    }
}
