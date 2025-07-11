using UnityEngine;
using UnityEngine.UIElements;

public class ProfileScreenController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset postCardTemplate;
    
    private VisualElement root;
    private VisualElement contentContainer;
    private Button designsTab;
    private Button savedTab;
    private Button aboutTab;
    
    private VisualElement designsTabButton;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        
        // Get tab buttons
        designsTab = root.Q<Button>("designsTab");
        savedTab = root.Q<Button>("savedTab");
        aboutTab = root.Q<Button>("aboutTab");
        
        // Get tab button containers for styling
        designsTabButton = designsTab.parent;
        savedTabButton = savedTab.parent;
        aboutTabButton = aboutTab.parent;
        
        // Create content container after scroll view
        var scrollView = root.Q<ScrollView>("scroll-container");
        contentContainer = new VisualElement();
        contentContainer.name = "tabContent";
        contentContainer.AddToClassList("tab-content");
        scrollView.Add(contentContainer);
        
        // Set up event listeners
        designsTab.clicked += () => ShowDesignsTab();
        savedTab.clicked += () => ShowSavedTab();
        aboutTab.clicked += () => ShowAboutTab();
        
        // Show designs tab by default
        ShowDesignsTab();
    }
    
    private void ShowDesignsTab()
    {
        // Update tab styling
        RemoveSelectedFromAllTabs();
        designsTabButton.AddToClassList("selected");
        
        // Clear content and show designs
        contentContainer.Clear();
        LoadDesignCards();
    }
    
    private void ShowSavedTab()
    {
        // Update tab styling
        RemoveSelectedFromAllTabs();
        savedTabButton.AddToClassList("selected");
        
        // Clear content and show saved items
        contentContainer.Clear();
        LoadSavedCards();
    }
    
    private void ShowAboutTab()
    {
        // Update tab styling
        RemoveSelectedFromAllTabs();
        aboutTabButton.AddToClassList("selected");
        
        // Clear content and show about section
        contentContainer.Clear();
    }
    
    private void RemoveSelectedFromAllTabs()
    {
        designsTabButton.RemoveFromClassList("selected");
        savedTabButton.RemoveFromClassList("selected");
        aboutTabButton.RemoveFromClassList("selected");
    }
    
    private void LoadDesignCards()
    {
        // Create multiple post cards for designs
        for (int i = 0; i < 3; i++)
        {
            var postCard = CreatePostCard(
                $"Design {i + 1}",
                "This contemporary living room features a warm, minimalist design with a calming neutral color palette..."
            );
            contentContainer.Add(postCard);
        }
    }
    
    private void LoadSavedCards()
    {
        // Create saved post cards
        for (int i = 0; i < 2; i++)
        {
            var postCard = CreatePostCard(
                $"Saved Design {i + 1}",
                "This saved design showcases beautiful interior styling with modern furniture and elegant decor..."
            );
            contentContainer.Add(postCard);
        }
    }
    
    private VisualElement CreatePostCard(string title, string description)
    {
        var card = new VisualElement();
        card.AddToClassList("card");
        
        // Card Header
        var cardHeader = new VisualElement();
        cardHeader.AddToClassList("card-header");
        
        var userImage = new Image();
        userImage.name = "userImage";
        userImage.AddToClassList("user-image");
        
        var userName = new TextElement();
        userName.name = "userName";
        userName.text = "Krishna Yadav";
        userName.AddToClassList("user-name");
        
        cardHeader.Add(userImage);
        cardHeader.Add(userName);
        
        // Card Body
        var cardBody = new VisualElement();
        cardBody.AddToClassList("card-body");
        
        var descriptionElement = new TextElement();
        descriptionElement.name = "description";
        descriptionElement.text = description;
        descriptionElement.AddToClassList("description");
        
        var cardImage = new Image();
        cardImage.name = "card-image";
        cardImage.AddToClassList("card-image");
        
        cardBody.Add(descriptionElement);
        cardBody.Add(cardImage);
        
        // Card Footer
        var cardFooter = new VisualElement();
        cardFooter.AddToClassList("card-footer");
        
        var innerCardFooter = new VisualElement();
        innerCardFooter.AddToClassList("inner-card-footer");
        
        var favouriteIcon = new Image();
        favouriteIcon.name = "favourite";
        favouriteIcon.AddToClassList("footer-icon");
        favouriteIcon.AddToClassList("like");
        
        var commentSection = new VisualElement();
        commentSection.name = "commentSection";
        commentSection.AddToClassList("comment-section");
        
        var commentArea = new TextElement();
        commentArea.name = "commentArea";
        commentArea.text = "Add your comments...";
        commentArea.AddToClassList("comment-area");
        
        commentSection.Add(commentArea);
        
        var shareIcon = new Image();
        shareIcon.name = "share";
        shareIcon.AddToClassList("footer-icon");
        shareIcon.AddToClassList("share");
        
        var bookmarkIcon = new Image();
        bookmarkIcon.name = "bookmark";
        bookmarkIcon.AddToClassList("footer-icon");
        bookmarkIcon.AddToClassList("bookmark");
        
        innerCardFooter.Add(favouriteIcon);
        innerCardFooter.Add(commentSection);
        innerCardFooter.Add(shareIcon);
        innerCardFooter.Add(bookmarkIcon);
        
        cardFooter.Add(innerCardFooter);
        
        // Assemble card
        card.Add(cardHeader);
        card.Add(cardBody);
        card.Add(cardFooter);
        
        return card;
    }
}