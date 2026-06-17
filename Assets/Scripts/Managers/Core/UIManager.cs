using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    int _order = 10;

    List<UI_Popup> _popups = new List<UI_Popup>();
    UI_Scene _sceneUI = null;

    public GameObject Root
    {
        get
        {
			GameObject root = GameObject.Find("@UI_Root");
			if (root == null)
				root = new GameObject { name = "@UI_Root" };
            return root;
		}
    }

    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = 0;
        }
    }

	public T MakeWorldSpaceUI<T>(Transform parent = null, string name = null) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/WorldSpace/{name}");
		if (parent != null)
			go.transform.SetParent(parent);

        Canvas canvas = go.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

		T worldSpaceUI = Util.GetOrAddComponent<T>(go);
		if (!worldSpaceUI.IsInitialized)
		{
			worldSpaceUI.Init();
			worldSpaceUI.IsInitialized = true;
		}

		return worldSpaceUI;
	}

	public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");
		if (parent != null)
			go.transform.SetParent(parent);

		T subItem = Util.GetOrAddComponent<T>(go);
		subItem.Init();
		subItem.IsInitialized = true;

		return subItem;
	}

	public T ShowSceneUI<T>(string name = null) where T : UI_Scene
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
		T sceneUI = Util.GetOrAddComponent<T>(go);
        _sceneUI = sceneUI;

		go.transform.SetParent(Root.transform);

		sceneUI.Init();
		sceneUI.IsInitialized = true;

		return sceneUI;
	}

	public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");
        T popup = Util.GetOrAddComponent<T>(go);
        _popups.Add(popup);

        go.transform.SetParent(Root.transform);

        popup.Init();
        popup.IsInitialized = true;

		return popup;
    }

    public void ClosePopupUI(UI_Popup popup)
    {
        if (!_popups.Remove(popup))
            return;

        Managers.Resource.Destroy(popup.gameObject);
    }

    public void ClosePopupUI()
    {
        if (_popups.Count == 0)
            return;

        UI_Popup popup = _popups[_popups.Count - 1];
        ClosePopupUI(popup);
    }

    public void CloseAllPopupUI()
    {
        while (_popups.Count > 0)
            ClosePopupUI();
    }

    public void Clear()
    {
        CloseAllPopupUI();
        _sceneUI = null;
    }
}
