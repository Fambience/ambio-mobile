using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [System.Serializable]
    public class ScreenEntry
    {
        public UIScreenType screenType;
        public GameObject screenObject;
    }

    [Header("Screen Registry")]
    public List<ScreenEntry> screenEntries;

    private Dictionary<UIScreenType, GameObject> screenMap = new();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        foreach (var entry in screenEntries)
        {
            if (!screenMap.ContainsKey(entry.screenType))
            {
                screenMap.Add(entry.screenType, entry.screenObject);
            }
        }
    }

    public void OpenScreen(UIScreenType screenToOpen)
    {
        foreach (var kvp in screenMap)
        {
            kvp.Value.SetActive(kvp.Key == screenToOpen);
        }
    }

    public void TransitionScreens(UIScreenType current, UIScreenType next)
    {
        if (screenMap.ContainsKey(current)) screenMap[current].SetActive(false);
        if (screenMap.ContainsKey(next)) screenMap[next].SetActive(true);
    }

    public GameObject GetScreen(UIScreenType type)
    {
        screenMap.TryGetValue(type, out var screen);
        return screen;
    }
}