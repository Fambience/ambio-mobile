using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class UploadProgressController : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement root;
    private VisualElement progressBarFill;
    private TextElement percentageText;
    private TextElement uploadTitle;
    private Image uploadIcon;
    
    [Header("Progress Settings")]
    [Range(0f, 100f)]
    public float currentProgress = 0f;
    public float animationSpeed = 2f;
    public bool autoSimulateProgress = true;
    public float simulationSpeed = 1f;
    
    [Header("Test Settings")]
    public bool simulateError = false;
    public float errorAtPercentage = 75f;
    
    private float targetProgress = 0f;
    private bool isUploading = false;
    private UploadState currentState = UploadState.Ready;
    
    public enum UploadState
    {
        Ready,
        Uploading,
        Completed,
        Error
    }

    void Start()
    {
        InitializeUI();
        
        // Start auto simulation if enabled
        if (autoSimulateProgress)
        {
            StartUpload();
        }
    }

    void InitializeUI()
    {
        // Get the UI Document component
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }

        root = uiDocument.rootVisualElement;
        
        // Get references to UI elements
        progressBarFill = root.Q<VisualElement>("progressBarFill");
        percentageText = root.Q<TextElement>("percentage");
        uploadTitle = root.Q<TextElement>("uploadTitle");
        uploadIcon = root.Q<Image>("uploadIcon");

        if (progressBarFill == null || percentageText == null || uploadTitle == null || uploadIcon == null)
        {
            Debug.LogError("Could not find required UI elements!");
            return;
        }

        // Initialize to ready state
        SetUploadState(UploadState.Ready);
    }

    void Update()
    {
        // Smoothly animate progress bar (except in error state)
        if (currentState != UploadState.Error && Mathf.Abs(currentProgress - targetProgress) > 0.1f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, animationSpeed * Time.deltaTime);
            UpdateProgressUI();
        }

        // Auto simulate progress if enabled
        if (autoSimulateProgress && isUploading && targetProgress < 100f && currentState == UploadState.Uploading)
        {
            float increment = simulationSpeed * Time.deltaTime;
            
            // Slow down as we approach 100%
            if (targetProgress > 80f)
                increment *= 0.3f;
            else if (targetProgress > 60f)
                increment *= 0.6f;
                
            SetProgress(targetProgress + increment);
            
            // Check for simulated error
            if (simulateError && targetProgress >= errorAtPercentage)
            {
                TriggerError();
                return;
            }
            
            // Complete upload when reaching 100%
            if (targetProgress >= 100f)
            {
                CompleteUpload();
            }
        }
    }

    public void SetProgress(float progress)
    {
        if (currentState == UploadState.Error) return; // Don't update progress if in error state
        
        targetProgress = Mathf.Clamp(progress, 0f, 100f);
    }

    public void StartUpload()
    {
        isUploading = true;
        SetUploadState(UploadState.Uploading);
        SetProgress(0f);
    }

    public void CompleteUpload()
    {
        isUploading = false;
        autoSimulateProgress = false;
        SetUploadState(UploadState.Completed);
        SetProgress(100f);
        
        // Optional: Hide UI after completion
        StartCoroutine(HideUIAfterDelay(3f));
    }

    public void TriggerError()
    {
        isUploading = false;
        autoSimulateProgress = false;
        SetUploadState(UploadState.Error);
        // Don't change progress - keep it where it failed
    }

    public void CancelUpload()
    {
        isUploading = false;
        autoSimulateProgress = false;
        SetUploadState(UploadState.Ready);
        SetProgress(0f);
    }

    private void SetUploadState(UploadState newState)
    {
        currentState = newState;
        
        // Remove existing CSS classes
        progressBarFill.RemoveFromClassList("completed");
        progressBarFill.RemoveFromClassList("error");
        
        switch (newState)
        {
            case UploadState.Ready:
                uploadTitle.text = "Ready to Upload";
                uploadIcon.image = Resources.Load<Texture2D>("uploading");
                percentageText.style.display = DisplayStyle.Flex;
                break;
                
            case UploadState.Uploading:
                uploadTitle.text = "Uploading...";
                uploadIcon.image = Resources.Load<Texture2D>("uploading");
                percentageText.style.display = DisplayStyle.Flex;
                // Progress bar is blue by default (no additional class needed)
                break;
                
            case UploadState.Completed:
                uploadTitle.text = "Uploaded";
                uploadIcon.image = Resources.Load<Texture2D>("uploaded");
                percentageText.style.display = DisplayStyle.Flex;
                progressBarFill.AddToClassList("completed");
                break;
                
            case UploadState.Error:
                uploadTitle.text = "Failed to upload";
                uploadIcon.image = Resources.Load<Texture2D>("uploadingError");
                percentageText.style.display = DisplayStyle.None; // Hide percentage
                progressBarFill.AddToClassList("error");
                break;
        }
        
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (progressBarFill != null)
        {
            // Update progress bar width
            progressBarFill.style.width = Length.Percent(currentProgress);
        }

        if (percentageText != null && currentState != UploadState.Error)
        {
            // Update percentage text (only if not in error state)
            percentageText.text = $"{currentProgress:F0}%";
        }
    }

    private IEnumerator HideUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
        }
    }

    // Public methods for external control
    public void ResetProgress()
    {
        SetProgress(0f);
        isUploading = false;
        SetUploadState(UploadState.Ready);
    }

    public void SetUploadSpeed(float speed)
    {
        simulationSpeed = Mathf.Max(0.1f, speed);
    }

    public bool IsUploading()
    {
        return isUploading;
    }

    public float GetCurrentProgress()
    {
        return currentProgress;
    }

    public UploadState GetCurrentState()
    {
        return currentState;
    }

    // Method to manually trigger error for testing
    public void SimulateError()
    {
        TriggerError();
    }

    // Example of how to use this script with actual file upload
    public void StartRealUpload(System.Action<float> onProgressUpdate, System.Action onComplete, System.Action onError = null)
    {
        StartCoroutine(SimulateRealUpload(onProgressUpdate, onComplete, onError));
    }

    private IEnumerator SimulateRealUpload(System.Action<float> onProgressUpdate, System.Action onComplete, System.Action onError)
    {
        StartUpload();
        
        float progress = 0f;
        while (progress < 100f && currentState == UploadState.Uploading)
        {
            // Simulate upload progress (replace with actual upload logic)
            progress += Random.Range(1f, 5f);
            progress = Mathf.Min(progress, 100f);
            
            // Simulate random error chance (5% chance per update)
            if (Random.Range(0f, 100f) < 5f && progress > 20f)
            {
                TriggerError();
                onError?.Invoke();
                yield break;
            }
            
            SetProgress(progress);
            onProgressUpdate?.Invoke(progress);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        if (currentState == UploadState.Uploading)
        {
            CompleteUpload();
            onComplete?.Invoke();
        }
    }

    // Method to retry upload after error
    public void RetryUpload()
    {
        if (currentState == UploadState.Error)
        {
            ResetProgress();
            StartUpload();
        }
    }
}