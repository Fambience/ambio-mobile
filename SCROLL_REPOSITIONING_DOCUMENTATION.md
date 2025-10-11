# Scroll Repositioning Implementation Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Components](#core-components)
4. [Implementation Details](#implementation-details)
5. [Flow Diagrams](#flow-diagrams)
6. [Code Examples](#code-examples)
7. [Key Considerations](#key-considerations)
8. [Testing & Validation](#testing--validation)

---

## Overview

### Purpose
The scroll repositioning feature maintains the user's scroll position when navigating between screens in the Ambio mobile application. This enhances user experience by allowing users to return to exactly where they left off when switching between Home and Explore screens.

### Benefits
- **Improved UX**: Users don't lose their place when navigating between screens
- **Reduced frustration**: No need to scroll back to previous position manually
- **Seamless navigation**: Creates a more native app-like experience
- **State persistence**: Works in conjunction with data caching for optimal performance

---

## Architecture

### System Design

The scroll repositioning system is built on three layers:

```
┌─────────────────────────────────────────┐
│         UI Controllers Layer            │
│  (HomeUIController, ExploreScreenController)  │
│   - Capture scroll events               │
│   - Restore scroll positions            │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Screen State Manager (Singleton)   │
│   - Manages all screen states           │
│   - Provides scroll position API        │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│         ScreenState (Data Model)        │
│   - Stores scroll position per screen   │
│   - Stores other screen metadata        │
└─────────────────────────────────────────┘
```

---

## Core Components

### 1. ScreenState.cs

**Location**: `Assets/Scripts/Services/ScreenState.cs`

**Purpose**: Data model that holds the state information for a single screen.

**Key Properties**:
```csharp
public Vector2 scrollPosition;      // Current scroll offset
public bool isInitialized;          // Whether screen has loaded data
public DateTime lastFetchTime;      // Last data fetch timestamp
public string selectedTag;          // Currently selected tag
public int selectedTagId;           // ID of selected tag
public FilterData filterData;       // Applied filters
```

**Key Methods**:
- `ShouldRefresh()`: Determines if cached data is expired
- `MarkAsInitialized()`: Marks screen as loaded with data
- `Invalidate()`: Resets screen state for fresh load

---

### 2. ScreenStateManager.cs

**Location**: `Assets/Scripts/Services/ScreenStateManager.cs`

**Purpose**: Singleton manager that coordinates all screen states across the application.

**Scroll Position API** (Lines 86-109):

```csharp
// Save scroll position for a screen
public void SaveScrollPosition(string screenName, Vector2 scrollPosition)

// Retrieve saved scroll position
public Vector2 GetScrollPosition(string screenName)

// Reset scroll position to zero
public void ResetScrollPosition(string screenName)
```

**Implementation Details**:
- Maintains a `Dictionary<string, ScreenState>` for all screens
- Each screen identified by unique string key ("Home", "Explore", etc.)
- Automatically creates ScreenState on first access
- Provides debug logging for all operations

---

### 3. HomeUIController.cs

**Location**: `Assets/Scripts/Controllers/HomeUIController.cs`

**Scroll Capture** (Lines 567-581):
```csharp
private void OnDisable()
{
    // Save scroll position when leaving screen
    if (container != null)
    {
        Vector2 currentScrollPos = container.scrollOffset;
        ScreenStateManager.Instance.SaveScrollPosition("Home", currentScrollPos);
        Debug.Log($"[HomeUIController] Saved scroll position: {currentScrollPos}");

        // Cleanup event listeners
        container.UnregisterCallback<WheelEvent>(OnScroll);
        container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }
}
```

**Scroll Restoration** (Lines 352-364):
```csharp
public void RestoreScrollPosition()
{
    Vector2 savedScrollPos = ScreenStateManager.Instance.GetScrollPosition("Home");
    if (container != null && savedScrollPos != Vector2.zero)
    {
        // Delay scroll restoration to ensure UI is fully rendered
        container.schedule.Execute(() =>
        {
            container.scrollOffset = savedScrollPos;
            Debug.Log($"[HomeUIController] Restored scroll position: {savedScrollPos}");
        }).ExecuteLater(100); // 100ms delay
    }
}
```

---

### 4. HomeScreenController.cs

**Location**: `Assets/Scripts/Controllers/HomeScreenController.cs`

**Smart UI Building** (Lines 102-127):

The controller decides whether to build UI gradually (fresh load) or immediately (cached load):

```csharp
private void BuildInitialUI()
{
    uiController.ClearContent();
    // Reset indices
    cardsDisplayed = 0;
    currentHomePostIndex = 0;
    currentExplorePostIndex = 0;
    currentDesignerIndex = 0;
    isShowingHomeFeed = dataHandler.HomePosts.Count > 0;

    // Check if we're using cached data
    bool usingCache = ScreenStateManager.Instance.IsScreenInitialized("Home") &&
                     !ScreenStateManager.Instance.ShouldLoadData("Home", forceRefresh: false);

    if (usingCache)
    {
        Debug.Log("[HomeScreen] Using cached data - building all cards immediately");
        BuildAllCardsImmediately(); // Builds all cards at once, then restores scroll
    }
    else
    {
        Debug.Log("[HomeScreen] Fresh load - building cards gradually");
        StartCoroutine(BuildUIGradually()); // Progressive loading, no scroll restore
    }
}
```

**Immediate Build with Scroll Restore** (Lines 303-348):
```csharp
private void BuildAllCardsImmediately()
{
    Debug.Log("[HomeScreen] Building all cards immediately from cache");

    // Build all posts and designer sections at once
    while (true)
    {
        // Add designer sections every 5 cards
        if (cardsDisplayed > 0 && cardsDisplayed % 5 == 0 &&
            currentDesignerIndex < dataHandler.TrendingDesigners.Count)
        {
            // Add trending designers section
        }

        Post postToShow = GetNextPost();
        if (postToShow != null)
        {
            VisualElement verticalCard = uiController.CreateVerticalCard(postToShow);
            uiController.AddToContainer(verticalCard);
            cardsDisplayed++;
        }
        else
        {
            break; // No more posts
        }
    }

    Debug.Log($"[HomeScreen] Built {cardsDisplayed} cards immediately");

    // Restore scroll position AFTER all cards are built
    uiController.RestoreScrollPosition();
}
```

---

### 5. ExploreScreenController.cs

**Location**: `Assets/Scripts/Controllers/ExploreScreenController.cs`

**OnEnable Logic** (Lines 47-69):
```csharp
void OnEnable()
{
    baseURL = baseScript.baseURL;
    authToken = AuthTokenManager.GetToken();

    if (isSearchScreenActive)
    {
        HideSearchScreen();
    }
    StartCoroutine(ShowNavigationAfterDelay());

    // Check if we should reload or use cached data
    if (ScreenStateManager.Instance.ShouldLoadData("Explore"))
    {
        Debug.Log("[ExploreScreen] Loading initial data (not cached or expired)");
        Invoke("InitializeExploreScreen", 0.1f);
    }
    else
    {
        Debug.Log("[ExploreScreen] Using cached data");
        Invoke("RestoreExploreScreenFromCache", 0.1f);
    }
}
```

**Cache Restoration** (Lines 107-147):
```csharp
void RestoreExploreScreenFromCache()
{
    uiDocument = GetComponent<UIDocument>();
    Debug.Log("Restoring Explore Screen from cache...");

    InitializeUI();
    SetupPullToRefresh();
    InitializeFilterPopup();

    // Restore cached posts
    List<PostData> cachedPosts = DataCache.Instance.GetCachedPostDataList("Explore_CurrentPosts");
    if (cachedPosts != null)
    {
        allPosts = cachedPosts;
        Debug.Log($"[ExploreScreen] Restored {allPosts.Count} posts from cache");
    }
    else
    {
        LoadTrendingPosts(); // Fallback if cache missing
    }

    CreateScrollableContent();

    // Restore scroll position with delay
    Vector2 savedScrollPos = ScreenStateManager.Instance.GetScrollPosition("Explore");
    if (mainScrollView != null && savedScrollPos != Vector2.zero)
    {
        mainScrollView.schedule.Execute(() =>
        {
            mainScrollView.scrollOffset = savedScrollPos;
            Debug.Log($"[ExploreScreen] Restored scroll position: {savedScrollPos}");
        }).ExecuteLater(100); // 100ms delay
    }
}
```

**Save on Exit** (Lines 909-918):
```csharp
void OnDisable()
{
    // Save scroll position when leaving the screen
    if (mainScrollView != null)
    {
        Vector2 currentScrollPos = mainScrollView.scrollOffset;
        ScreenStateManager.Instance.SaveScrollPosition("Explore", currentScrollPos);
        Debug.Log($"[ExploreScreen] Saved scroll position: {currentScrollPos}");
    }
}
```

---

## Flow Diagrams

### Complete User Journey Flow

```
┌─────────────────────────────────────────────────────┐
│ User on Home Screen (scrolled down to position Y)  │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ User navigates to Explore Screen                    │
│ → OnDisable() triggered on HomeUIController         │
│ → Saves scrollOffset to ScreenStateManager          │
│   ScreenStateManager.SaveScrollPosition("Home", Y)  │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ User views Explore Screen                           │
│ (scrolls, interacts with content)                   │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ User navigates back to Home Screen                  │
│ → OnEnable() triggered on HomeScreenController      │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│ Check: ShouldLoadData("Home")?                      │
└────────┬────────────────────────────────┬───────────┘
         │                                │
    YES (expired/not cached)         NO (cached & fresh)
         │                                │
         ▼                                ▼
┌────────────────────┐          ┌─────────────────────┐
│ Load fresh data    │          │ Use cached data     │
│ Build UI gradually │          │ Build ALL cards     │
│ NO scroll restore  │          │ immediately         │
└────────────────────┘          └──────────┬──────────┘
                                           │
                                           ▼
                                ┌─────────────────────┐
                                │ RestoreScrollPosition()│
                                │ Wait 100ms           │
                                │ Apply scrollOffset=Y │
                                └─────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────┐
│ User sees Home Screen at EXACT same position (Y)    │
└─────────────────────────────────────────────────────┘
```

### State Management Flow

```
Screen Lifecycle:

FIRST VISIT:
OnEnable → ShouldLoadData=true → LoadData → BuildUI (gradual) → MarkInitialized
                                                                      ↓
                                                              scrollPosition = 0

NAVIGATING AWAY:
OnDisable → SaveScrollPosition(currentOffset) → ScreenState updated
                                                      ↓
                                              scrollPosition = Y

RETURNING (within cache time):
OnEnable → ShouldLoadData=false → UseCachedData → BuildUI (immediate) → RestoreScroll
                                                                              ↓
                                                                      scrollOffset = Y

RETURNING (after cache expired):
OnEnable → ShouldLoadData=true → LoadData → BuildUI (gradual) → MarkInitialized
                                                                      ↓
                                                              scrollPosition = 0 (reset)
```

---

## Code Examples

### Example 1: Implementing Scroll Repositioning for a New Screen

```csharp
public class MyNewScreenController : MonoBehaviour
{
    private ScrollView mainScrollView;
    private const string SCREEN_NAME = "MyScreen";

    void OnEnable()
    {
        // Check if should load or use cache
        if (ScreenStateManager.Instance.ShouldLoadData(SCREEN_NAME))
        {
            LoadFreshData();
        }
        else
        {
            RestoreFromCache();
        }
    }

    void RestoreFromCache()
    {
        // 1. Setup UI
        InitializeUI();

        // 2. Load cached data
        var cachedData = DataCache.Instance.GetCachedData(SCREEN_NAME);
        BuildUI(cachedData);

        // 3. Restore scroll position with delay
        Vector2 savedScrollPos = ScreenStateManager.Instance.GetScrollPosition(SCREEN_NAME);
        if (mainScrollView != null && savedScrollPos != Vector2.zero)
        {
            mainScrollView.schedule.Execute(() =>
            {
                mainScrollView.scrollOffset = savedScrollPos;
                Debug.Log($"[MyScreen] Restored scroll: {savedScrollPos}");
            }).ExecuteLater(100);
        }
    }

    void OnDisable()
    {
        // Save scroll position when leaving
        if (mainScrollView != null)
        {
            Vector2 currentPos = mainScrollView.scrollOffset;
            ScreenStateManager.Instance.SaveScrollPosition(SCREEN_NAME, currentPos);
            Debug.Log($"[MyScreen] Saved scroll: {currentPos}");
        }
    }
}
```

### Example 2: Manual Scroll Reset

```csharp
// Reset scroll position (e.g., after pull-to-refresh)
public void ResetToTop()
{
    ScreenStateManager.Instance.ResetScrollPosition("Home");
    if (container != null)
    {
        container.scrollOffset = Vector2.zero;
    }
}
```

### Example 3: Conditional Scroll Restoration

```csharp
// Only restore scroll if user was scrolled past certain threshold
public void RestoreScrollPosition()
{
    Vector2 savedScrollPos = ScreenStateManager.Instance.GetScrollPosition("Home");

    // Only restore if user was scrolled down significantly
    if (container != null && savedScrollPos.y > 100f)
    {
        container.schedule.Execute(() =>
        {
            container.scrollOffset = savedScrollPos;
            Debug.Log($"Restored significant scroll: {savedScrollPos}");
        }).ExecuteLater(100);
    }
}
```

---

## Key Considerations

### 1. **Timing is Critical**

**Why 100ms delay?**
- UI elements need time to render and calculate their sizes
- ScrollView needs to know its content height before accepting scrollOffset
- Too short: scroll restoration fails silently
- Too long: visible "jump" effect

**Recommendation**: Use `schedule.Execute().ExecuteLater(100)` for most cases

### 2. **Build Strategy Matters**

| Build Type | When Used | Scroll Restore | Performance |
|------------|-----------|----------------|-------------|
| **Gradual** | Fresh data load | ❌ No | Better (progressive) |
| **Immediate** | Cached data | ✅ Yes | Acceptable (all at once) |

**Why different strategies?**
- **Gradual**: For fresh loads, we don't have a meaningful scroll position yet
- **Immediate**: For cached loads, we need all content rendered to restore exact position

### 3. **Cache Integration**

Scroll repositioning works hand-in-hand with data caching:

```csharp
bool usingCache = ScreenStateManager.Instance.IsScreenInitialized("Home") &&
                  !ScreenStateManager.Instance.ShouldLoadData("Home");
```

- If data is cached → Use immediate build + restore scroll
- If data is fresh → Use gradual build + no restore (position = 0)

### 4. **Memory Management**

```csharp
void OnDisable()
{
    // ALWAYS cleanup
    container.UnregisterCallback<WheelEvent>(OnScroll);
    container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
    container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
    container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
}
```

Prevents memory leaks from event listeners.

### 5. **Pull-to-Refresh Behavior**

When user triggers refresh:
```csharp
private void HandleRefresh()
{
    // Invalidate cache AND screen state
    ScreenStateManager.Instance.InvalidateScreen("Home");
    DataCache.Instance.InvalidateScreen("Home");

    // This will reset scroll position to top on next load
    StartCoroutine(RefreshContent());
}
```

**Result**: After refresh, user starts from top (expected behavior)

### 6. **Vector2 vs float**

```csharp
public Vector2 scrollPosition; // Stores both X and Y
```

- Most screens only use Y (vertical scroll)
- X component reserved for horizontal scroll (if needed)
- Compare with `Vector2.zero` to check if position was saved

---

## Testing & Validation

### Manual Test Cases

#### Test 1: Basic Scroll Restoration
1. Open Home screen
2. Scroll down to middle of feed
3. Navigate to Explore screen
4. Navigate back to Home screen
5. **Expected**: Home screen shows at same scroll position

#### Test 2: Multiple Navigations
1. Scroll down on Home screen (position A)
2. Go to Explore, scroll down (position B)
3. Go back to Home
4. **Expected**: Home at position A
5. Go back to Explore
6. **Expected**: Explore at position B

#### Test 3: Cache Expiration
1. Scroll down on Home screen
2. Wait 6+ minutes (cache expires - default 5min)
3. Navigate away and back to Home
4. **Expected**: Fresh load, scroll at top (position reset)

#### Test 4: Pull-to-Refresh
1. Scroll down on Home screen
2. Pull to refresh
3. **Expected**: Scroll resets to top after refresh completes

#### Test 5: App Backgrounding
1. Scroll down on Home screen
2. Background the app
3. Return to app
4. **Expected**: Scroll position maintained (depends on Unity lifecycle)

### Debug Logging

All components include comprehensive logging:

```
[ScreenStateManager] Saved scroll position for Home: (0, 450.5)
[HomeUIController] Saved scroll position: (0, 450.5)
[HomeScreen] Using cached data - building all cards immediately
[HomeScreen] Built 25 cards immediately
[HomeUIController] Restored scroll position: (0, 450.5)
```

Use Unity Console filter: `scroll` to track all scroll-related events

### Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Scroll jumps to top | Delay too short | Increase ExecuteLater to 150-200ms |
| Scroll position wrong | Content changed | Rebuild all content before restore |
| No restoration | Vector2.zero | Check SaveScrollPosition is called |
| Memory leak | Events not unregistered | Add cleanup in OnDisable |

---

## Performance Metrics

### Memory Impact
- **ScreenState per screen**: ~100 bytes
- **Dictionary overhead**: Minimal (< 5 screens typically)
- **Total**: < 1KB for entire system

### Performance Impact
- **Save operation**: O(1) - Dictionary lookup
- **Restore operation**: O(1) - Dictionary lookup + single scroll assignment
- **100ms delay**: Imperceptible to users

### Scalability
- Supports unlimited screens
- Each screen state is independent
- No cross-screen dependencies

---

## Future Enhancements

### Potential Improvements

1. **Persistent Storage**
   - Save scroll positions to PlayerPrefs
   - Survive app restarts

2. **Smart Scroll Hints**
   - Visual indicator showing restored position
   - "Return to top" quick action

3. **Scroll Analytics**
   - Track average scroll depth
   - Identify most-viewed content

4. **Advanced Caching**
   - Save scroll position per filter/tag combination
   - Context-aware restoration

---

## Summary

The scroll repositioning implementation provides seamless navigation by:
- ✅ Saving scroll position when leaving a screen
- ✅ Restoring position when returning (if using cached data)
- ✅ Integrating with data caching system
- ✅ Handling edge cases (refresh, expiration, etc.)
- ✅ Minimal performance overhead
- ✅ Clean, maintainable architecture

**Key Takeaway**: The system enhances UX by making the app feel more responsive and context-aware, remembering where users were in their browsing session.

---

## References

### File Locations
- `Assets/Scripts/Services/ScreenState.cs` - Data model
- `Assets/Scripts/Services/ScreenStateManager.cs` - State manager
- `Assets/Scripts/Controllers/HomeUIController.cs` - Home screen UI
- `Assets/Scripts/Controllers/HomeScreenController.cs` - Home screen logic
- `Assets/Scripts/Controllers/ExploreScreenController.cs` - Explore screen

### Related Systems
- Data Caching (`DataCache.cs`)
- State Management (`ScreenStateManager.cs`)
- Navigation (`NavigationManager.cs`)

---

**Document Version**: 1.0
**Last Updated**: 2025-10-08
**Author**: Development Team
**Status**: Active Implementation