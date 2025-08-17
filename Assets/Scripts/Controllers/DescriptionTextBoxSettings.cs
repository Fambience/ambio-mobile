using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

[System.Serializable]
public class DescriptionTextBoxSettings
{
    [Header("Text Settings")]
    public string placeholderText = "Describe your design...";
    public int fontSize = 35;
    public Color textColor = new Color(0.2f, 0.2f, 0.2f); // #333333
    public Color placeholderColor = new Color(0.6f, 0.6f, 0.6f);
    
    [Header("Layout Settings")]
    public int paddingLeft = 20;
    public int paddingRight = 20;
    public int paddingTop = 30;
    public int paddingBottom = 20;
    
    [Header("Wrapping Settings")]
    public float characterWidth = 20f; // Estimated character width for wrapping
    public bool preserveEnterKey = true;
    
    [Header("Character Limit")]
    public int maxCharacters = 150; // Maximum character limit
}

[UxmlElement]
public partial class DescriptionTextBox : VisualElement
{
    [UxmlAttribute("placeholder-text")]
    public string placeholderText { get; set; } = "Describe your Design...";
    
    [UxmlAttribute("font-size")]
    public int fontSize { get; set; } = 35;
    
    [UxmlAttribute("padding-left")]
    public int paddingLeft { get; set; } = 20;
    
    [UxmlAttribute("padding-right")]
    public int paddingRight { get; set; } = 20;
    
    [UxmlAttribute("padding-top")]
    public int paddingTop { get; set; } = 30;
    
    [UxmlAttribute("padding-bottom")]
    public int paddingBottom { get; set; } = 20;
    
    [UxmlAttribute("character-width")]
    public float characterWidth { get; set; } = 20f;
    
    [UxmlAttribute("preserve-enter")]
    public bool preserveEnterKey { get; set; } = true;
    
    [UxmlAttribute("max-characters")]
    public int maxCharacters { get; set; } = 150;
    
    private TextField textField;
    private Label placeholderLabel;
    private Label characterCountLabel;
    private DescriptionTextBoxSettings settings;
    private bool isInitialized = false;
    private bool isFocused = false;
    private bool isUpdatingText = false;
    
    // Events
    public event Action<string> onValueChanged;
    public event Action onFocusIn;
    public event Action onFocusOut;
    public event Action onCharacterLimitReached;
    
    // Properties
    public string value
    {
        get => textField?.value ?? "";
        set
        {
            if (textField != null && !isUpdatingText)
            {
                // Enforce character limit
                string limitedValue = EnforceCharacterLimit(value);
                textField.value = limitedValue;
                UpdatePlaceholderVisibility();
                UpdateCharacterCount();
            }
        }
    }
    
    public string text => value; // Alias for convenience
    
    public int charactersRemaining => settings != null ? settings.maxCharacters - (textField?.value?.Length ?? 0) : 0;
    public int currentCharacterCount => textField?.value?.Length ?? 0;
    
    public DescriptionTextBox() : this(null) 
    {
        // Schedule initialization for next frame to ensure UXML attributes are set
        schedule.Execute(() => {
            if (!isInitialized)
            {
                Initialize();
            }
        });
    }
    
    public DescriptionTextBox(DescriptionTextBoxSettings settings)
    {
        this.settings = settings ?? CreateSettingsFromAttributes();
        schedule.Execute(() => Initialize());
    }
    
    private DescriptionTextBoxSettings CreateSettingsFromAttributes()
    {
        return new DescriptionTextBoxSettings
        {
            placeholderText = this.placeholderText,
            fontSize = this.fontSize,
            paddingLeft = this.paddingLeft,
            paddingRight = this.paddingRight,
            paddingTop = this.paddingTop,
            paddingBottom = this.paddingBottom,
            characterWidth = this.characterWidth,
            preserveEnterKey = this.preserveEnterKey,
            maxCharacters = this.maxCharacters
        };
    }
    
    private string EnforceCharacterLimit(string text)
    {
        if (string.IsNullOrEmpty(text) || settings == null)
            return text;
            
        if (text.Length > settings.maxCharacters)
        {
            string truncated = text.Substring(0, settings.maxCharacters);
            onCharacterLimitReached?.Invoke();
            return truncated;
        }
        
        return text;
    }
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        // Create settings from UXML attributes if not provided
        if (settings == null)
            settings = CreateSettingsFromAttributes();
        
        // Setup container styles
        style.position = Position.Relative;
        style.overflow = Overflow.Hidden;
        
        // Create placeholder label (behind the text field)
        placeholderLabel = new Label();
        placeholderLabel.name = "placeholder-label";
        placeholderLabel.text = settings.placeholderText;
        placeholderLabel.style.position = Position.Absolute;
        placeholderLabel.style.top = 0;
        placeholderLabel.style.left = 0;
        placeholderLabel.style.right = 0;
        placeholderLabel.style.bottom = 0;
        placeholderLabel.style.paddingLeft = settings.paddingLeft;
        placeholderLabel.style.paddingRight = settings.paddingRight;
        placeholderLabel.style.paddingTop = settings.paddingTop;
        placeholderLabel.style.paddingBottom = settings.paddingBottom;
        placeholderLabel.style.fontSize = settings.fontSize;
        placeholderLabel.style.color = settings.placeholderColor;
        placeholderLabel.style.whiteSpace = WhiteSpace.Normal;
        placeholderLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        placeholderLabel.pickingMode = PickingMode.Ignore;
        placeholderLabel.style.display = DisplayStyle.Flex;
        
        // Create the main text field
        textField = new TextField();
        textField.name = "main-textfield";
        textField.multiline = true;
        textField.style.position = Position.Absolute;
        textField.style.top = 0;
        textField.style.left = 0;
        textField.style.right = 0;
        textField.style.bottom = 0;
        textField.style.paddingLeft = settings.paddingLeft;
        textField.style.paddingRight = settings.paddingRight;
        textField.style.paddingTop = settings.paddingTop;
        textField.style.paddingBottom = settings.paddingBottom;
        textField.style.fontSize = settings.fontSize;
        textField.style.color = settings.textColor;
        textField.style.backgroundColor = new StyleColor(Color.clear);
        textField.style.unityTextAlign = TextAnchor.UpperLeft;
        textField.style.whiteSpace = WhiteSpace.Normal;
        
        // Create character count label
        characterCountLabel = new Label();
        characterCountLabel.name = "character-count-label";
        characterCountLabel.style.position = Position.Absolute;
        characterCountLabel.style.bottom = 5;
        characterCountLabel.style.right = 10;
        characterCountLabel.style.fontSize = settings.fontSize - 10; // Smaller font
        characterCountLabel.style.color = new Color(0.5f, 0.5f, 0.5f); // Gray color
        characterCountLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        characterCountLabel.pickingMode = PickingMode.Ignore;
        
        // Remove all borders from the text field
        textField.style.borderTopWidth = 0;
        textField.style.borderBottomWidth = 0;
        textField.style.borderLeftWidth = 0;
        textField.style.borderRightWidth = 0;
        
        // Schedule to remove borders and styling from child elements
        schedule.Execute(() => {
            var textFieldContainer = textField.Q(className: "unity-text-field");
            if (textFieldContainer != null)
            {
                textFieldContainer.style.borderTopWidth = 0;
                textFieldContainer.style.borderBottomWidth = 0;
                textFieldContainer.style.borderLeftWidth = 0;
                textFieldContainer.style.borderRightWidth = 0;
                textFieldContainer.style.backgroundColor = new StyleColor(Color.clear);
                textFieldContainer.style.paddingTop = 0;
                textFieldContainer.style.paddingBottom = 0;
                textFieldContainer.style.paddingLeft = 0;
                textFieldContainer.style.paddingRight = 0;
                textFieldContainer.style.marginTop = 0;
                textFieldContainer.style.marginBottom = 0;
                textFieldContainer.style.marginLeft = 0;
                textFieldContainer.style.marginRight = 0;
            }
    
            var textInput = textField.Q(className: "unity-text-field__input");
            if (textInput != null)
            {
                textInput.style.backgroundColor = new StyleColor(Color.clear);
                textInput.style.borderTopWidth = 0;
                textInput.style.borderBottomWidth = 0;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
        
                // CHANGE THESE LINES - Add padding instead of setting to 0
                textInput.style.paddingTop = 20;    // Add some top padding
                textInput.style.paddingBottom = 10; // Add some bottom padding
                textInput.style.paddingLeft = 10;   // Add some left padding for cursor
                textInput.style.paddingRight = 10;  // Add some right padding
        
                textInput.style.marginTop = 0;
                textInput.style.marginBottom = 0;
                textInput.style.marginLeft = 0;
                textInput.style.marginRight = 0;
                textInput.style.unityTextAlign = TextAnchor.UpperLeft;
                textInput.style.whiteSpace = WhiteSpace.Normal;
            }
        });
        
        // Add elements - placeholder behind, textfield in front, character count on top
        Add(placeholderLabel);
        Add(textField);
        Add(characterCountLabel);
        
        // Setup events
        textField.RegisterValueChangedCallback(OnTextChanged);
        textField.RegisterCallback<FocusInEvent>(OnFocusIn);
        textField.RegisterCallback<FocusOutEvent>(OnFocusOut);
        textField.RegisterCallback<KeyDownEvent>(OnKeyDown);
        
        // Register for geometry changes
        RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        
        // Initial state - show placeholder and update character count
        UpdatePlaceholderVisibility();
        UpdateCharacterCount();
        
        isInitialized = true;
    }
    
    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        // Apply text wrapping when geometry changes
        if (!string.IsNullOrEmpty(textField?.value))
        {
            ApplyTextWrapping();
        }
    }
    
    private void OnKeyDown(KeyDownEvent evt)
    {
        // Prevent input if at character limit (except for delete/backspace)
        if (textField != null && currentCharacterCount >= settings.maxCharacters)
        {
            if (evt.keyCode != KeyCode.Backspace && evt.keyCode != KeyCode.Delete && 
                evt.keyCode != KeyCode.LeftArrow && evt.keyCode != KeyCode.RightArrow &&
                evt.keyCode != KeyCode.UpArrow && evt.keyCode != KeyCode.DownArrow &&
                evt.keyCode != KeyCode.Home && evt.keyCode != KeyCode.End)
            {
                evt.PreventDefault();
                evt.StopPropagation();
                onCharacterLimitReached?.Invoke();
                return;
            }
        }
        
        // Handle text wrapping on key input
        schedule.Execute(() => ApplyTextWrapping());
    }
    
    private void OnTextChanged(ChangeEvent<string> evt)
    {
        if (isUpdatingText) return;
        
        // Enforce character limit
        string limitedValue = EnforceCharacterLimit(evt.newValue);
        
        if (limitedValue != evt.newValue)
        {
            isUpdatingText = true;
            textField.value = limitedValue;
            isUpdatingText = false;
        }
        
        UpdatePlaceholderVisibility();
        UpdateCharacterCount();
        ApplyTextWrapping();
        onValueChanged?.Invoke(limitedValue);
    }
    
    private void OnFocusIn(FocusInEvent evt)
    {
        isFocused = true;
        UpdatePlaceholderVisibility();
        onFocusIn?.Invoke();
    }
    
    private void OnFocusOut(FocusOutEvent evt)
    {
        isFocused = false;
        UpdatePlaceholderVisibility();
        onFocusOut?.Invoke();
    }
    
    private void UpdatePlaceholderVisibility()
    {
        if (!isInitialized || placeholderLabel == null) return;
        
        bool hasText = !string.IsNullOrEmpty(textField?.value);
        
        // Show placeholder only when there's no text AND not focused
        if (!hasText && !isFocused)
        {
            placeholderLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            placeholderLabel.style.display = DisplayStyle.None;
        }
    }
    
    private void UpdateCharacterCount()
    {
        if (!isInitialized || characterCountLabel == null || settings == null) return;
        
        int currentCount = currentCharacterCount;
        int remaining = charactersRemaining;
        
        characterCountLabel.text = $"{currentCount}/{settings.maxCharacters}";
        
        // Change color based on remaining characters
        if (remaining <= 10)
        {
            characterCountLabel.style.color = new Color(0.8f, 0.2f, 0.2f); // Red when close to limit
        }
        else if (remaining <= 30)
        {
            characterCountLabel.style.color = new Color(0.8f, 0.6f, 0.2f); // Orange when getting close
        }
        else
        {
            characterCountLabel.style.color = new Color(0.5f, 0.5f, 0.5f); // Gray when plenty left
        }
    }
    
    private void ApplyTextWrapping()
    {
        if (!isInitialized || textField == null || isUpdatingText) return;
        
        string currentValue = textField.value;
        if (string.IsNullOrEmpty(currentValue)) return;
        
        string wrappedText = WrapText(currentValue);
        
        // Only update if the text actually changed
        if (textField.value != wrappedText)
        {
            isUpdatingText = true;
            
            // Store cursor position
            int cursorPos = textField.cursorIndex;
            
            // Update the text field
            textField.value = wrappedText;
            
            // Try to restore cursor position
            if (cursorPos <= wrappedText.Length)
            {
                textField.SelectRange(cursorPos, cursorPos);
            }
            
            isUpdatingText = false;
        }
    }
    
    private string WrapText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        float availableWidth = resolvedStyle.width - settings.paddingLeft - settings.paddingRight;
        
        if (availableWidth <= 0)
            availableWidth = 400; // Fallback width
        
        string[] words = text.Split(' ');
        List<string> lines = new List<string>();
        string currentLine = "";
        
        foreach (string word in words)
        {
            if (settings.preserveEnterKey && word.Contains('\n'))
            {
                string[] wordParts = word.Split('\n');
                for (int i = 0; i < wordParts.Length; i++)
                {
                    if (i == 0)
                    {
                        string testLine = string.IsNullOrEmpty(currentLine) ? wordParts[i] : currentLine + " " + wordParts[i];
                        if (ShouldWrapLine(testLine, availableWidth))
                        {
                            if (!string.IsNullOrEmpty(currentLine))
                            {
                                lines.Add(currentLine);
                                currentLine = wordParts[i];
                            }
                            else
                            {
                                lines.Add(wordParts[i]);
                                currentLine = "";
                            }
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            lines.Add(currentLine);
                        }
                        currentLine = wordParts[i];
                    }
                    
                    if (i < wordParts.Length - 1)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            lines.Add(currentLine);
                        }
                        currentLine = "";
                    }
                }
                continue;
            }
            
            string testLine2 = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            
            if (ShouldWrapLine(testLine2, availableWidth))
            {
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    lines.Add(word);
                    currentLine = "";
                }
            }
            else
            {
                currentLine = testLine2;
            }
        }
        
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }
        
        return string.Join("\n", lines);
    }
    
    private bool ShouldWrapLine(string line, float availableWidth)
    {
        float estimatedWidth = line.Length * settings.characterWidth;
        return estimatedWidth > availableWidth;
    }
    
    // Public methods
    public void Focus()
    {
        textField?.Focus();
    }
    
    public void ClearText()
    {
        value = "";
    }
    
    public void UpdateSettings(DescriptionTextBoxSettings newSettings)
    {
        settings = newSettings;
        
        if (isInitialized)
        {
            // Update text field styles
            textField.style.paddingLeft = settings.paddingLeft;
            textField.style.paddingRight = settings.paddingRight;
            textField.style.paddingTop = settings.paddingTop;
            textField.style.paddingBottom = settings.paddingBottom;
            textField.style.fontSize = settings.fontSize;
            textField.style.color = settings.textColor;
            
            // Update placeholder label styles
            placeholderLabel.style.paddingLeft = settings.paddingLeft;
            placeholderLabel.style.paddingRight = settings.paddingRight;
            placeholderLabel.style.paddingTop = settings.paddingTop;
            placeholderLabel.style.paddingBottom = settings.paddingBottom;
            placeholderLabel.style.fontSize = settings.fontSize;
            placeholderLabel.style.color = settings.placeholderColor;
            placeholderLabel.text = settings.placeholderText;
            
            // Update character count label
            characterCountLabel.style.fontSize = settings.fontSize - 10;
            
            // Refresh display
            UpdatePlaceholderVisibility();
            UpdateCharacterCount();
            if (!string.IsNullOrEmpty(value))
            {
                ApplyTextWrapping();
            }
        }
    }
    
    // Add this method to manually trigger initialization if needed
    public void ForceInitialize()
    {
        isInitialized = false;
        Initialize();
    }
}