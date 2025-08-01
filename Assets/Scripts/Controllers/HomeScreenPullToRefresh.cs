using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class HomeScreenPullToRefresh : MonoBehaviour
{
    [Header("Pull to Refresh Settings")]
    public float pullThreshold = 100f;
    public float refreshIndicatorSize = 50f;
    public float refreshDuration = 2f;
    
    private ScrollView container;
    private VisualElement refreshIndicator;
    private VisualElement refreshContainer;
    private Image refreshIcon;
    private bool isRefreshing = false;
    private bool isPulling = false;
    private float pullDistance = 0f;

    public System.Action OnRefreshRequested;

    public void Initialize(ScrollView scrollView)
    {
        container = scrollView;
        SetupPullToRefresh();
    }
    
    private void SetupPullToRefresh()
    {
        container.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        
        // Create refresh indicator container
        refreshContainer = new VisualElement();
        refreshContainer.style.height = 0;
        refreshContainer.style.flexDirection = FlexDirection.Row;
        refreshContainer.style.justifyContent = Justify.Center;
        refreshContainer.style.alignItems = Align.Center;
        refreshContainer.style.overflow = Overflow.Hidden;
        refreshContainer.style.marginTop = 0;
        
        // Create refresh indicator
        refreshIndicator = new VisualElement();
        refreshIndicator.style.width = refreshIndicatorSize;
        refreshIndicator.style.height = refreshIndicatorSize;
        refreshIndicator.style.justifyContent = Justify.Center;
        refreshIndicator.style.alignItems = Align.Center;
        refreshIndicator.style.opacity = 0;
        
        // Create refresh icon
        refreshIcon = new Image();
        refreshIcon.image = Resources.Load<Texture2D>("loader");
        refreshIcon.style.width = refreshIndicatorSize;
        refreshIcon.style.height = refreshIndicatorSize;
        
        refreshIndicator.Add(refreshIcon);
        refreshContainer.Add(refreshIndicator);
        
        // Insert refresh container at the beginning
        container.Insert(0, refreshContainer);
        
        // Register scroll event
        container.RegisterCallback<WheelEvent>(OnScroll);
        
        // Register pointer events for touch/mouse
        container.RegisterCallback<PointerDownEvent>(OnPointerDown);
        container.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        container.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }
    
    private void OnScroll(WheelEvent evt)
    {
        if (container.scrollOffset.y <= 0 && evt.delta.y < 0 && !isRefreshing)
        {
            HandlePullToRefresh(-evt.delta.y * 10f);
        }
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        if (container.scrollOffset.y <= 0 && !isRefreshing)
        {
            isPulling = true;
            pullDistance = 0f;
        }
    }
    
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isPulling && container.scrollOffset.y <= 0 && !isRefreshing)
        {
            pullDistance += evt.deltaPosition.y;
            HandlePullToRefresh(pullDistance);
        }
    }
    
    private void OnPointerUp(PointerUpEvent evt)
    {
        if (isPulling)
        {
            isPulling = false;
            
            if (pullDistance >= pullThreshold && !isRefreshing)
            {
                StartRefresh();
            }
            else
            {
                ResetPullIndicator();
            }
            
            pullDistance = 0f;
        }
    }
    
    private void HandlePullToRefresh(float distance)
    {
        if (isRefreshing) return;
        
        float normalizedDistance = Mathf.Clamp(distance, 0f, pullThreshold * 1.5f);
        float progress = normalizedDistance / pullThreshold;
        refreshContainer.style.height = normalizedDistance;
        refreshIndicator.style.opacity = Mathf.Clamp01(progress);
        float rotation = progress * 180f;
        refreshIcon.transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
    
    private void StartRefresh()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        refreshContainer.style.height = refreshIndicatorSize + 20f;
        refreshIndicator.style.opacity = 1f;
        StartCoroutine(RotateRefreshIcon());
        StartCoroutine(RefreshContent());
    }
    
    private IEnumerator RotateRefreshIcon()
    {
        float rotationSpeed = 360f;
        
        while (isRefreshing)
        {
            float currentRotation = refreshIcon.transform.rotation.eulerAngles.z;
            float newRotation = currentRotation + rotationSpeed * Time.deltaTime;
            refreshIcon.transform.rotation = Quaternion.Euler(0, 0, newRotation);
            yield return null;
        }
    }
    
    private IEnumerator RefreshContent()
    {
        OnRefreshRequested?.Invoke();
        yield return new WaitForSeconds(0.5f);
        ResetPullIndicator();
        isRefreshing = false;
    }
    
    private void ResetPullIndicator()
    {
        StartCoroutine(AnimateRefreshIndicatorOut());
    }
    
    private IEnumerator AnimateRefreshIndicatorOut()
    {
        float animationDuration = 0.3f;
        float startHeight = refreshContainer.resolvedStyle.height;
        float startOpacity = refreshIndicator.resolvedStyle.opacity;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            refreshContainer.style.height = Mathf.Lerp(startHeight, 0f, easedProgress);
            refreshIndicator.style.opacity = Mathf.Lerp(startOpacity, 0f, easedProgress);
            
            yield return null;
        }
        
        refreshContainer.style.height = 0;
        refreshIndicator.style.opacity = 0;
        refreshIcon.transform.rotation = Quaternion.identity;
    }

    public bool IsRefreshing()
    {
        return isRefreshing;
    }

    private void OnDestroy()
    {
        // Unregister events to prevent memory leaks
        if (container != null)
        {
            container.UnregisterCallback<WheelEvent>(OnScroll);
            container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
    }
}