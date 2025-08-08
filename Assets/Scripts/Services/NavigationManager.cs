using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public enum NavScreen
{
    Home,
    Explore,
    Create,
    Profile
}

public static class NavigationManager
{
    private static NavigationBarController navigationController;

    private static VisualElement navigationBar;
    private static Image homeIcon;
    private static Image exploreIcon;
    private static Image createIcon;
    private static Image profileIcon;

    private static NavScreen currentScreen = NavScreen.Home;
    private static bool isVisible = false;
    private static VisualElement navigationContainer;

    public static void Initialize(NavigationBarController controller)
    {
        navigationController = controller;
        controller.StartCoroutine(InitializeAfterFrame());
    }

    private static IEnumerator InitializeAfterFrame()
    {
        yield return null; // Wait one frame
        yield return new WaitForEndOfFrame(); // Wait for UI to fully render
        
        var root = navigationController.GetComponent<UIDocument>().rootVisualElement;
        
        Debug.Log("=== Navigation Initialization ===");
        Debug.Log("Root element children count: " + root.childCount);
        
        // Find navigation container
        navigationContainer = root.Q<VisualElement>("navigationContainer");
        
        if (navigationContainer == null)
        {
            Debug.LogError("navigationContainer not found! Available root children:");
            for (int i = 0; i < root.childCount; i++)
            {
                Debug.Log($"  {i}: {root[i].name} (type: {root[i].GetType().Name})");
            }
            yield break;
        }

        Debug.Log("navigationContainer found!");
        
        // Find navigation bar
        navigationBar = navigationContainer.Q<VisualElement>("navigationBar");
        if (navigationBar == null)
        {
            Debug.LogError("navigationBar not found in navigationContainer!");
            yield break;
        }

        Debug.Log("navigationBar found!");

        // Find navigation bar background (where icons are)
        var navigationBarBg = navigationBar.Q<VisualElement>("navigationBarBg");
        if (navigationBarBg == null)
        {
            Debug.LogError("navigationBarBg not found!");
            yield break;
        }

        Debug.Log("navigationBarBg found!");

        // Find all icons
        homeIcon = navigationBarBg.Q<Image>("home");
        exploreIcon = navigationBarBg.Q<Image>("explore");
        createIcon = navigationBarBg.Q<Image>("create");
        profileIcon = navigationBarBg.Q<Image>("profile");

        // Debug icon finding
        Debug.Log($"Icons found - Home: {homeIcon != null}, Explore: {exploreIcon != null}, Create: {createIcon != null}, Profile: {profileIcon != null}");

        if (homeIcon == null || exploreIcon == null || createIcon == null || profileIcon == null)
        {
            Debug.LogError("Some icons not found! navigationBarBg children:");
            for (int i = 0; i < navigationBarBg.childCount; i++)
            {
                var child = navigationBarBg[i];
                Debug.Log($"  {i}: {child.name} (type: {child.GetType().Name})");
            }
        }

        // Hide by default - will be shown when explicitly called
        navigationContainer.style.visibility = Visibility.Hidden;
        navigationContainer.style.opacity = 0f;
        
        // Set initial state as hidden
        isVisible = false;
        
        // Prepare icons but don't show yet
        UpdateSelectedIcon(NavScreen.Home);

        Debug.Log("NavigationManager initialized successfully!");
    }

    public static void ToggleNavigationBar(bool show)
    {
        if (navigationContainer == null)
        {
            Debug.LogError("navigationContainer is null!");
            return;
        }

        // Use visibility instead of display to preserve your CSS layout
        navigationContainer.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
        navigationContainer.style.opacity = show ? 1f : 0f;
        isVisible = show;

        Debug.Log($"Navigation bar toggled: {(show ? "shown" : "hidden")}");
    }

    public static void UpdateSelectedIcon(NavScreen screen)
    {
        if (homeIcon == null || exploreIcon == null || createIcon == null || profileIcon == null)
        {
            Debug.LogWarning("Some icons are null, cannot update selection");
            return;
        }

        currentScreen = screen;

        UpdateIconStyle(homeIcon, screen == NavScreen.Home, "home-icon-selected", "home-icon-unselected");
        UpdateIconStyle(exploreIcon, screen == NavScreen.Explore, "explore-icon-selected", "explore-icon-unselected");
        UpdateIconStyle(createIcon, screen == NavScreen.Create, "create-icon-selected", "create-icon-unselected");
        UpdateIconStyle(profileIcon, screen == NavScreen.Profile, "profile-icon-selected", "profile-icon-unselected");

        Debug.Log($"Navigation icons updated for screen: {screen}");
    }

    public static bool IsNavigationBarVisible() => isVisible;
    public static NavScreen GetCurrentScreen() => currentScreen;

    private static void UpdateIconStyle(Image icon, bool selected, string selectedClass, string unselectedClass)
    {
        if (icon == null) return;
        
        // Clear all classes first
        icon.ClearClassList();
        
        // Add the appropriate class
        string classToAdd = selected ? selectedClass : unselectedClass;
        icon.AddToClassList(classToAdd);
        
        Debug.Log($"Icon {icon.name} updated with class: {classToAdd}");
    }

    // Force show method for debugging
    public static void ForceShowNavigationBar()
    {
        if (navigationContainer != null)
        {
            navigationContainer.style.visibility = Visibility.Visible;
            navigationContainer.style.opacity = 1f;
            isVisible = true;
            Debug.Log("Navigation bar force shown");
        }
        else
        {
            Debug.LogError("Cannot force show - navigationContainer is null");
        }
    }
}