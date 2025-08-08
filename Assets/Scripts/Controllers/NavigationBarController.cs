using UnityEngine;
using UnityEngine.UIElements;

public class NavigationBarController : MonoBehaviour
{
    public UIDocument uiDocument;

    void Start()
    {
        Debug.Log("NavigationBarController Start()");

        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is not assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("RootVisualElement is null.");
            return;
        }

        NavigationManager.Initialize(this);
        RegisterEvents(root);
    }

    private void RegisterEvents(VisualElement root)
    {
        RegisterClick(root, "home", () => {
            Debug.Log("Home icon clicked");
            UIManager.Instance.OpenScreen(UIScreenType.Home);
        });
        
        // Uncomment when you have these screen types
        // RegisterClick(root, "explore", () => UIManager.Instance.OpenScreen(UIScreenType.Explore));
        // RegisterClick(root, "create", () => UIManager.Instance.OpenScreen(UIScreenType.Create));
        // RegisterClick(root, "profile", () => UIManager.Instance.OpenScreen(UIScreenType.Profile));
    }

    private void RegisterClick(VisualElement root, string name, System.Action callback)
    {
        var icon = root.Q<Image>(name);
        if (icon != null)
        {
            icon.pickingMode = PickingMode.Position;
            icon.RegisterCallback<ClickEvent>(_ => {
                Debug.Log($"Icon {name} clicked");
                callback();
            });
            Debug.Log($"Registered click event for {name}");
        }
        else
        {
            Debug.LogWarning($"Could not find icon with name: {name}");
        }
    }

    // Debug method - right-click in inspector and select this
    [ContextMenu("Debug Navigation Bar")]
    public void DebugNavigationBar()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is null");
            return;
        }

        var root = uiDocument.rootVisualElement;
        
        Debug.Log("=== NAVIGATION BAR DEBUG ===");
        Debug.Log($"Root children count: {root.childCount}");
        
        var navContainer = root.Q<VisualElement>("navigationContainer");
        if (navContainer != null)
        {
            Debug.Log("✓ Navigation container found!");
            Debug.Log($"  Visibility: {navContainer.style.visibility}");
            Debug.Log($"  Opacity: {navContainer.style.opacity}");
            Debug.Log($"  Display: {navContainer.style.display}");
            Debug.Log($"  Position: {navContainer.resolvedStyle.position}");
            Debug.Log($"  Bottom: {navContainer.resolvedStyle.bottom}");
            Debug.Log($"  Height: {navContainer.resolvedStyle.height}");
            Debug.Log($"  Width: {navContainer.resolvedStyle.width}");
            
            var navBar = navContainer.Q<VisualElement>("navigationBar");
            if (navBar != null)
            {
                Debug.Log("✓ Navigation bar found!");
                Debug.Log($"  Background color: {navBar.resolvedStyle.backgroundColor}");
                Debug.Log($"  Children: {navBar.childCount}");
                
                var navBg = navBar.Q<VisualElement>("navigationBarBg");
                if (navBg != null)
                {
                    Debug.Log("✓ Navigation bar bg found!");
                    Debug.Log($"  Children: {navBg.childCount}");
                    
                    // Check each icon
                    CheckIcon(navBg, "home");
                    CheckIcon(navBg, "explore");
                    CheckIcon(navBg, "create");
                    CheckIcon(navBg, "profile");
                }
                else
                {
                    Debug.LogError("✗ navigationBarBg not found!");
                }
            }
            else
            {
                Debug.LogError("✗ navigationBar not found!");
            }
        }
        else
        {
            Debug.LogError("✗ navigationContainer not found!");
            
            // List all root children
            Debug.Log("Available root children:");
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root[i];
                Debug.Log($"  {i}: '{child.name}' (type: {child.GetType().Name})");
            }
        }
    }

    private void CheckIcon(VisualElement parent, string iconName)
    {
        var icon = parent.Q<Image>(iconName);
        if (icon != null)
        {
            var classes = new System.Collections.Generic.List<string>();
            foreach(var className in icon.GetClasses())
            {
                classes.Add(className);
            }
            Debug.Log($"  ✓ Icon '{iconName}' found - Classes: [{string.Join(", ", classes)}]");
            Debug.Log($"    Size: {icon.resolvedStyle.width}x{icon.resolvedStyle.height}");
        }
        else
        {
            Debug.LogError($"  ✗ Icon '{iconName}' NOT found!");
        }
    }

    void OnDestroy()
    {
        // Cleanup if needed
    }
}