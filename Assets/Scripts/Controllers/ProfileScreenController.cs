using UnityEngine;
using UnityEngine.UIElements;

public class ProfileScreenController : MonoBehaviour
{
    [Header("Card Templates")] public VisualTreeAsset postCardTemplate;
    
    private VisualElement root;
    private ScrollView scrollView;
    private VisualElement tabContentContainer;
    private VisualElement aboutContent;
    private Button designsTab;
    private Button savedTab;
    private Button aboutTab;
    
    private VisualElement designsTabButton;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        scrollView = root.Q<ScrollView>("scroll-container");
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        
        designsTab = root.Q<Button>("designsTab");
        savedTab = root.Q<Button>("savedTab");
        aboutTab = root.Q<Button>("aboutTab");
        
        designsTabButton = designsTab.parent;
        savedTabButton = savedTab.parent;
        aboutTabButton = aboutTab.parent;
        
        // Get the about content element
        aboutContent = root.Q<VisualElement>("aboutContent");
        
        // Create dynamic content container for designs and saved tabs
        tabContentContainer = new VisualElement();
        tabContentContainer.name = "tabContentContainer";
        tabContentContainer.AddToClassList("tab-content");
        scrollView.Add(tabContentContainer);
        
        designsTab.clicked += () => ShowDesignsTab();
        savedTab.clicked += () => ShowSavedTab();
        aboutTab.clicked += () => ShowAboutTab();
        
        ShowDesignsTab();
    }
    
    private void ShowDesignsTab()
    {
        RemoveSelectedFromAllTabs();
        designsTabButton.AddToClassList("selected");
        
        // Hide about content and show dynamic content
        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;
        
        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();
        LoadDesignCards();
    }
    
    private void ShowSavedTab()
    {
        RemoveSelectedFromAllTabs();
        savedTabButton.AddToClassList("selected");
        
        // Hide about content and show dynamic content
        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;
        
        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();
        LoadSavedCards();
    }
    
    private void ShowAboutTab()
    {
        RemoveSelectedFromAllTabs();
        aboutTabButton.AddToClassList("selected");
        
        // Hide dynamic content and show about content
        tabContentContainer.style.display = DisplayStyle.None;
        tabContentContainer.Clear();
        
        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.Flex;
    }
    
    private void RemoveSelectedFromAllTabs()
    {
        designsTabButton.RemoveFromClassList("selected");
        savedTabButton.RemoveFromClassList("selected");
        aboutTabButton.RemoveFromClassList("selected");
    }
    
    private void LoadDesignCards()
    {
        if (postCardTemplate == null)
        {
            Debug.LogError("Post card template is not assigned!");
            ShowEmptyState("No card template assigned");
            return;
        }
        for (int i = 0; i < 5; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();
            var userName = postCard.Q<TextElement>("userName");
            if (userName != null)
                userName.text = "Krishna Yadav";
            var description = postCard.Q<TextElement>("description");
            if (description != null)
                description.text = $"Design {i + 1}: This contemporary living room features a warm, minimalist design with a calming neutral color palette and modern furniture arrangement.";
            var userImage = postCard.Q<Image>("userImage");
            if (userImage != null)
                userImage.image = LoadImage("person");
            var cardImage = postCard.Q<Image>("card-image");
            if (cardImage != null)
                cardImage.image = LoadImage("Contemporary");   
            tabContentContainer.Add(postCard);
        }
    }
    
    private void LoadSavedCards()
    {
        if (postCardTemplate == null)
        {
            Debug.LogError("Post card template is not assigned!");
            ShowEmptyState("No card template assigned");
            return;
        }
        for (int i = 0; i < 3; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();
            var userName = postCard.Q<TextElement>("userName");
            if (userName != null)
                userName.text = "Krishna Yadav";
            var description = postCard.Q<TextElement>("description");
            if (description != null)
                description.text = $"Saved Design {i + 1}: This beautiful interior design has been saved to your collection for future inspiration and reference.";
            var userImage = postCard.Q<Image>("userImage");
            if (userImage != null)
                userImage.image = LoadImage("person");
            var cardImage = postCard.Q<Image>("card-image");
            if (cardImage != null)
                cardImage.image = LoadImage("Contemporary");
            tabContentContainer.Add(postCard);
        }
    }
    
    private void ShowEmptyState(string message)
    {
        var emptyState = new VisualElement();
        emptyState.AddToClassList("empty-state");
        var emptyText = new TextElement();
        emptyText.text = message;
        emptyText.AddToClassList("empty-state-text");
        emptyState.Add(emptyText);
        tabContentContainer.Add(emptyState);
    }
    
    private Texture2D LoadImage(string imageName)
    {
        Texture2D texture = Resources.Load<Texture2D>(imageName);
        if (texture == null)
        {
            texture = Resources.Load<Texture2D>($"Images/{imageName}");
        }
        if (texture == null)
        {
            Debug.LogWarning($"Could not load image: {imageName}");
        }
        return texture;
    }
    
    private void OnDisable()
    {
        if (designsTab != null)
            designsTab.clicked -= ShowDesignsTab;
        if (savedTab != null)
            savedTab.clicked -= ShowSavedTab;
        if (aboutTab != null)
            aboutTab.clicked -= ShowAboutTab;
    }
}