using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class ChatMessageInputController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // Configuration
    [SerializeField] private float minHeight = 150f;
    [SerializeField] private float maxHeight = 450f;
    [SerializeField] private float animationSpeed = 10f;

    private TextField messageInput;
    private VisualElement messageInputSection;
    private Label measurementLabel;

    private float currentHeight;
    private float targetHeight;
    private bool isAnimating = false;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        // Get references to UI elements
        messageInput = root.Q<TextField>("message-input");
        messageInputSection = root.Q<VisualElement>("message-input-section");

        if (messageInput == null || messageInputSection == null)
        {
            Debug.LogError("ChatMessageInputController: Required UI elements not found!");
            return;
        }

        // Create a hidden label for text measurement
        CreateMeasurementLabel();

        // Set initial height
        currentHeight = minHeight;
        targetHeight = minHeight;
        messageInput.style.height = minHeight;

        // Register callback for text changes
        messageInput.RegisterValueChangedCallback(OnTextChanged);

        Debug.Log("ChatMessageInputController initialized");
    }

    private void OnDisable()
    {
        if (messageInput != null)
        {
            messageInput.UnregisterValueChangedCallback(OnTextChanged);
        }
    }

    private void CreateMeasurementLabel()
    {
        // Create a hidden label that matches the input field's styling
        // This is used to measure the actual height needed for the text
        measurementLabel = new Label();
        measurementLabel.style.position = Position.Absolute;
        measurementLabel.style.visibility = Visibility.Hidden;
        measurementLabel.style.whiteSpace = WhiteSpace.Normal;
        measurementLabel.style.fontSize = 50; // Match the input font size
        measurementLabel.style.paddingLeft = 20;
        measurementLabel.style.paddingRight = 20;
        measurementLabel.style.paddingTop = 10;
        measurementLabel.style.paddingBottom = 10;

        // Add to root but make it invisible
        uiDocument.rootVisualElement.Add(measurementLabel);
    }

    private void OnTextChanged(ChangeEvent<string> evt)
    {
        CalculateRequiredHeight(evt.newValue);
    }

    private void CalculateRequiredHeight(string text)
    {
        if (measurementLabel == null || messageInput == null)
            return;

        // If text is empty, set to minimum height
        if (string.IsNullOrEmpty(text))
        {
            targetHeight = minHeight;
            StartHeightAnimation();
            return;
        }

        // Set the measurement label's text and width to match the input field
        measurementLabel.text = text;
        measurementLabel.style.width = messageInput.resolvedStyle.width;

        // Wait a frame for layout to update, then measure
        StartCoroutine(MeasureTextHeight());
    }

    private IEnumerator MeasureTextHeight()
    {
        // Wait for layout to update
        yield return null;

        if (measurementLabel == null)
            yield break;

        // Get the measured height
        float measuredHeight = measurementLabel.resolvedStyle.height;

        // Add some padding for comfort
        float requiredHeight = measuredHeight + 40f;

        // Clamp between min and max
        targetHeight = Mathf.Clamp(requiredHeight, minHeight, maxHeight);

        // Start animation
        StartHeightAnimation();
    }

    private void StartHeightAnimation()
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateHeight());
        }
    }

    private IEnumerator AnimateHeight()
    {
        isAnimating = true;

        while (Mathf.Abs(currentHeight - targetHeight) > 0.5f)
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * animationSpeed);

            // Update the input field height
            messageInput.style.height = currentHeight;

            // Also update the parent container to expand upward
            UpdateParentContainerHeight();

            yield return null;
        }

        // Set final height
        currentHeight = targetHeight;
        messageInput.style.height = currentHeight;
        UpdateParentContainerHeight();

        isAnimating = false;
    }

    private void UpdateParentContainerHeight()
    {
        if (messageInputSection == null)
            return;

        // Calculate the total height needed for the input section
        // Base padding (50px top + 50px bottom = 100px) + current input height
        float sectionHeight = 100f + currentHeight;

        // Update the section height
        messageInputSection.style.height = sectionHeight;
    }

    // Public method to clear the input (useful when sending a message)
    public void ClearInput()
    {
        if (messageInput != null)
        {
            messageInput.value = "";
            targetHeight = minHeight;
            currentHeight = minHeight;
            messageInput.style.height = minHeight;
            UpdateParentContainerHeight();
        }
    }

    // Public method to get the current text
    public string GetText()
    {
        return messageInput?.value ?? "";
    }

    // Public method to set focus on the input
    public void FocusInput()
    {
        messageInput?.Focus();
    }
}