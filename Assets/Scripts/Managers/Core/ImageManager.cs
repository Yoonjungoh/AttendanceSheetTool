using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemType 키로 보상 아이콘 Sprite를 조회하고 캐싱한다.
/// Assets/Resources/Prefabs/Icons/Reward/{ItemType}.png 형태로 더미 이미지를 배치하면
/// Managers.Resource.Load 경로 규칙(Resources.Load("Prefabs/" + path))과 자동으로 맞는다.
/// 새 보상 타입 추가 시 이미지 파일만 추가하면 되고 이 클래스는 수정할 필요가 없다.
/// </summary>
public class ImageManager
{
    const string IconPathFormat = "Icons/Reward/{0}";

    readonly Dictionary<ItemType, Sprite> _cache = new Dictionary<ItemType, Sprite>();

    public Sprite GetIcon(ItemType type)
    {
        if (_cache.TryGetValue(type, out Sprite cached))
            return cached;

        string path = string.Format(IconPathFormat, type);
        Sprite sprite = Managers.Resource.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"[ImageManager] 아이콘을 찾지 못했습니다: {path}");
            return null;
        }

        _cache[type] = sprite;
        return sprite;
    }
}
