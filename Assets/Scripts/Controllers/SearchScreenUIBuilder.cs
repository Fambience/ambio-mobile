using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

public static class SearchScreenUIBuilder
{
    private static string baseURL;
    private static string authToken;
    private static VisualElement searchResultsContainer;
    private static Coroutine searchDelayCoroutine;
    private static TextField searchField;
    private static VisualTreeAsset searchScreenTemplate;
    private static bool isPlaceholderActive = true;
    private static string placeholderText = "Search for designers...";
    private static VisualTreeAsset designerCardTemplate;

    // Loader elements
    private static VisualElement loadingIndicator;
    private static Image loadingIcon;
    private static bool isLoading = false;
    private static Coroutine loaderRotationCoroutine;
    
    public delegate void BackButtonDelegate();
    public static event BackButtonDelegate OnBackButtonClicked;
    
    public static VisualElement CreateSearchScreen(BackButtonDelegate onBackClicked = null, VisualTreeAsset template = null)
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
        OnBackButtonClicked = onBackClicked;
        searchScreenTemplate = template;
        
        VisualElement searchScreen;
        
        if (searchScreenTemplate != null)
        {
            searchScreen = searchScreenTemplate.Instantiate();
        }
        else
        {
            searchScreen = CreateSearchScreenProgrammatically();
        }
        
        searchScreen.name = "searchScreen";
        searchScreen.style.width = Length.Percent(100);
        searchScreen.style.height = Length.Percent(100);
        searchScreen.style.position = Position.Absolute;
        searchScreen.style.top = 0;
        searchScreen.style.left = 0;
        
        SetupSearchScreenComponents(searchScreen);
        
        return searchScreen;
    }
    
    private static void SetupSearchScreenComponents(VisualElement searchScreen)
    {
        VisualElement container = searchScreen.Q<VisualElement>(className: "container");
        if (container != null)
        {
            VisualElement existingTopBar = container.Q<VisualElement>(className: "top-bar");
            if (existingTopBar != null)
            {
                container.Remove(existingTopBar);
            }
            
            VisualElement topBarWithSearch = CreateTopBarWithSearch();
            container.Insert(0, topBarWithSearch);
            
            CreateAndAddResultsContainer(container);
        }
    }
    
    private static VisualElement CreateTopBarWithSearch()
    {
        VisualElement topBar = new VisualElement();
        topBar.AddToClassList("top-bar");
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.alignItems = Align.Center;
        topBar.style.marginBottom = 32;
        topBar.style.marginTop = Length.Percent(10);
        topBar.style.paddingLeft = Length.Percent(1); 
        
        Button backButton = new Button();
        backButton.name = "backButton";
        backButton.AddToClassList("back-button");
        backButton.style.width = 45;
        backButton.style.height = 45;
        backButton.style.backgroundColor = Color.clear;
        backButton.style.borderLeftWidth = 0;
        backButton.style.borderRightWidth = 0;
        backButton.style.borderTopWidth = 0;
        backButton.style.borderBottomWidth = 0;
        backButton.style.fontSize = 18;
        backButton.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        backButton.style.paddingLeft = 0;
        backButton.style.paddingRight = 0;
        backButton.style.paddingTop = 0;
        backButton.style.paddingBottom = 0;
        backButton.style.justifyContent = Justify.Center;
        backButton.style.alignItems = Align.Center;
        backButton.style.flexShrink = 0; 
        
        Texture2D backIconTexture = Resources.Load<Texture2D>("back-icon");
        if (backIconTexture != null)
        {
            Image backIcon = new Image();
            backIcon.AddToClassList("back-icon");
            backIcon.style.width = 45;
            backIcon.style.height = 45;
            backIcon.image = backIconTexture;
            backIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            backIcon.style.scale = new Vector2(2.5f, 2.5f); 
            backButton.Add(backIcon);
        }
        else
        {
            Label backText = new Label("←");
            backText.style.fontSize = 24;
            backText.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            backButton.Add(backText);
        }
        
        backButton.RegisterCallback<ClickEvent>(evt => OnBackButtonClicked?.Invoke());
        
        VisualElement searchContainer = CreateSearchFieldContainer();
        searchContainer.style.flexGrow = 1; 
        searchContainer.style.marginLeft = 0; 
        
        topBar.Add(backButton);
        topBar.Add(searchContainer);
        
        return topBar;
    }
    
    private static VisualElement CreateSearchFieldContainer()
    {
        VisualElement searchWrapper = new VisualElement();
        searchWrapper.AddToClassList("search-wrapper");
        searchWrapper.style.paddingLeft = 16;
        searchWrapper.style.paddingRight = 16;
        searchWrapper.style.width = Length.Percent(70); 
        searchWrapper.style.marginLeft = Length.Percent(3);
        searchWrapper.style.marginRight = Length.Percent(3);
        
        searchField = new TextField();
        searchField.name = "searchField";
        searchField.AddToClassList("search-field");
        searchField.style.width = Length.Percent(100);
        searchField.style.height = 100;
        searchField.style.borderLeftWidth = 2;
        searchField.style.borderRightWidth = 2;
        searchField.style.borderTopWidth = 2;
        searchField.style.borderBottomWidth = 2;
        searchField.style.paddingLeft = 40;
        searchField.style.borderTopLeftRadius = 50;
        searchField.style.borderTopRightRadius = 50;
        searchField.style.borderBottomLeftRadius = 50;
        searchField.style.borderBottomRightRadius = 50;
        searchField.style.fontSize = 35;
        searchField.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        searchField.style.backgroundColor = Color.white;
        
        searchField.value = "";
        searchField.SetValueWithoutNotify("");
        
        SetupSearchFieldPlaceholder(searchField);
        searchField.RegisterValueChangedCallback(evt => OnSearchInputChanged(evt.newValue));
        
        searchWrapper.Add(searchField);
        
        return searchWrapper;
    }
    
    private static void CreateAndAddResultsContainer(VisualElement container)
    {
        ScrollView scrollView = new ScrollView();
        scrollView.name = "searchScrollView";
        scrollView.mode = ScrollViewMode.Vertical;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.style.width = Length.Percent(100);
        scrollView.style.flexGrow = 1;
        scrollView.style.marginTop = 16;

        searchResultsContainer = new VisualElement();
        searchResultsContainer.name = "searchResultsContainer";
        searchResultsContainer.style.width = Length.Percent(100);

        // Create loader indicator (same as home screen)
        CreateLoadingIndicator();

        scrollView.Add(searchResultsContainer);
        container.Add(scrollView);
    }

    private static void CreateLoadingIndicator()
    {
        loadingIndicator = new VisualElement();
        loadingIndicator.name = "loadingIndicator";
        loadingIndicator.style.width = Length.Percent(100);
        loadingIndicator.style.height = 100;
        loadingIndicator.style.alignItems = Align.Center;
        loadingIndicator.style.justifyContent = Justify.Center;
        loadingIndicator.style.marginTop = 100;
        loadingIndicator.style.display = DisplayStyle.None;
        loadingIndicator.style.opacity = 0;

        loadingIcon = new Image();
        loadingIcon.image = Resources.Load<Texture2D>("loader");
        loadingIcon.style.width = 50;
        loadingIcon.style.height = 50;

        loadingIndicator.Add(loadingIcon);
    }
    
    private static VisualElement CreateSearchScreenProgrammatically()
    {
        VisualElement searchScreen = new VisualElement();
        searchScreen.AddToClassList("container");
        searchScreen.style.flexDirection = FlexDirection.Column;
        searchScreen.style.paddingLeft = 24;
        searchScreen.style.paddingRight = 24;
        searchScreen.style.paddingTop = 24;
        searchScreen.style.paddingBottom = 24;
        searchScreen.style.backgroundColor = new Color(0.96f, 0.94f, 0.93f, 1f);
        searchScreen.style.height = Length.Percent(100);
        searchScreen.style.width = Length.Percent(100);
        searchScreen.style.justifyContent = Justify.SpaceBetween;
        
        return searchScreen;
    }
    
    private static void SetupSearchFieldPlaceholder(TextField searchField)
    {
        isPlaceholderActive = true;
        searchField.SetValueWithoutNotify(placeholderText);
        searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        
        searchField.RegisterCallback<FocusInEvent>(evt =>
        {
            if (isPlaceholderActive)
            {
                searchField.SetValueWithoutNotify("");
                searchField.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                isPlaceholderActive = false;
            }
        });
        
        searchField.RegisterCallback<FocusOutEvent>(evt =>
        {
            if (string.IsNullOrEmpty(searchField.value.Trim()))
            {
                searchField.SetValueWithoutNotify(placeholderText);
                searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                isPlaceholderActive = true;
            }
        });
        
        searchField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (isPlaceholderActive)
            {
                searchField.SetValueWithoutNotify("");
                searchField.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                isPlaceholderActive = false;
            }
        });
    }
    
    private static void OnSearchInputChanged(string searchQuery)
    {
        if (isPlaceholderActive || searchQuery == placeholderText)
        {
            return;
        }

        if (searchDelayCoroutine != null)
        {
            CoroutineRunner.Instance.StopRoutine(searchDelayCoroutine);
        }

        if (string.IsNullOrEmpty(searchQuery.Trim()))
        {
            ClearSearchResults();
            HideLoader();
            return;
        }

        // Show loader immediately when user starts typing
        ShowLoader();

        searchDelayCoroutine = CoroutineRunner.Instance.StartRoutine(SearchWithDelay(searchQuery.Trim()));
    }
    
    private static IEnumerator SearchWithDelay(string query)
    {
        yield return new WaitForSeconds(3f);

        // Clear previous results before fetching new ones
        ClearSearchResults();

        yield return PostDataGetter.SearchDesigners(
            baseURL,
            authToken,
            query,
            onSuccess: designers => UpdateSearchResults(designers),
            onError: error => ShowSearchError(error)
        );
    }
    
    private static void UpdateSearchResults(List<PostDataGetter.DesignerSearchResult> designers)
    {
        // Hide loader
        HideLoader();

        ClearSearchResults();

        if (designers == null || designers.Count == 0)
        {
            ShowNoResultsMessage();
            return;
        }

        foreach (var designer in designers)
        {
            VisualElement designerCard = CreateDesignerCard(designer);
            searchResultsContainer.Add(designerCard);
        }
    }
    
    public static void SetDesignerCardTemplate(VisualTreeAsset template)
    {
        designerCardTemplate = template;
    }

    private static VisualElement CreateDesignerCard(PostDataGetter.DesignerSearchResult designer)
    {
        if (designerCardTemplate == null)
        {
            Debug.LogError("Designer card template not set! Please call SetDesignerCardTemplate() first.");
            return new VisualElement();
        }
        
        VisualElement mainContainer = designerCardTemplate.Instantiate();
        
        Image designerIcon = mainContainer.Q<Image>("designerIcon");
        TextElement designerUID = mainContainer.Q<TextElement>("designerUID");
        
        VisualElement userBasicDetail = mainContainer.Q<VisualElement>("userBasicDetail");
        TextElement designerName = null;
        TextElement designerFollow = null;
        
        if (userBasicDetail != null)
        {
            var textElements = userBasicDetail.Query<TextElement>().ToList();
            if (textElements.Count >= 3)
            {
                designerName = textElements[0];
                designerFollow = textElements[2];
            }
        }
        
        if (designerUID != null)
        {
            designerUID.text = designer.userName ?? "Unknown";
        }
        
        if (designerName != null)
        {
            designerName.text = $"{designer.firstName} {designer.lastName}".Trim();
        }
        
        if (designerFollow != null)
        {
            designerFollow.text = FormatFollowerCount(designer.followersCount) + " Followers";
        }
        
        if (designerIcon != null)
        {
            if (!string.IsNullOrEmpty(designer.avatar))
            {
                CoroutineRunner.Instance.StartRoutine(LoadAvatarImage(designer.avatar, designerIcon));
            }
            else
            {
                designerIcon.style.backgroundImage = null;
                designerIcon.style.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            }
        }
        
        mainContainer.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log($"Designer clicked: {designer.userName}");
        });
        
        mainContainer.RegisterCallback<MouseEnterEvent>(evt =>
        {
            mainContainer.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 0.3f);
        });
        
        mainContainer.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            mainContainer.style.backgroundColor = Color.clear;
        });
        
        return mainContainer;
    }

    private static IEnumerator LoadAvatarImage(string imageUrl, Image targetImage)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                targetImage.image = texture;
                targetImage.style.backgroundImage = null;
            }
            else
            {
                targetImage.style.backgroundImage = null;
                targetImage.style.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            }
        }
    }
    
    private static string FormatFollowerCount(int count)
    {
        if (count >= 1000000)
            return (count / 1000000.0).ToString("0.0") + "M";
        else if (count >= 1000)
            return (count / 1000.0).ToString("0.0") + "K";
        else
            return count.ToString();
    }
    
    private static void ClearSearchResults()
    {
        if (searchResultsContainer != null)
        {
            searchResultsContainer.Clear();
        }
    }
    
    private static void ShowNoResultsMessage()
    {
        VisualElement noResultsContainer = new VisualElement();
        noResultsContainer.style.alignItems = Align.Center;
        noResultsContainer.style.justifyContent = Justify.Center;
        noResultsContainer.style.paddingTop = 60;
        
        Label noResultsLabel = new Label("No designers found");
        noResultsLabel.style.fontSize =35;
        noResultsLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        noResultsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        noResultsContainer.Add(noResultsLabel);
        searchResultsContainer.Add(noResultsContainer);
    }
    
    private static void ShowSearchError(string error)
    {
        // Hide loader
        HideLoader();

        VisualElement errorContainer = new VisualElement();
        errorContainer.style.alignItems = Align.Center;
        errorContainer.style.justifyContent = Justify.Center;
        errorContainer.style.paddingTop = 60;

        Label errorLabel = new Label("Search failed. Please try again.");
        errorLabel.style.fontSize = 35;
        errorLabel.style.color = Color.red;
        errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        errorContainer.Add(errorLabel);
        searchResultsContainer.Clear();
        searchResultsContainer.Add(errorContainer);

        Debug.LogError($"Search error: {error}");
    }
    
    public static void FocusSearchField()
    {
        searchField?.Focus();
    }

    private static void ShowLoader()
    {
        if (loadingIndicator == null || isLoading) return;

        isLoading = true;

        // Add loader to search results container
        if (searchResultsContainer != null && !searchResultsContainer.Contains(loadingIndicator))
        {
            searchResultsContainer.Add(loadingIndicator);
        }

        loadingIndicator.style.display = DisplayStyle.Flex;
        loadingIndicator.style.opacity = 1;

        // Start rotation animation
        if (loaderRotationCoroutine != null)
        {
            CoroutineRunner.Instance.StopRoutine(loaderRotationCoroutine);
        }
        loaderRotationCoroutine = CoroutineRunner.Instance.StartRoutine(RotateLoader());
    }

    private static void HideLoader()
    {
        if (loadingIndicator == null) return;

        isLoading = false;

        // Stop rotation animation
        if (loaderRotationCoroutine != null)
        {
            CoroutineRunner.Instance.StopRoutine(loaderRotationCoroutine);
            loaderRotationCoroutine = null;
        }

        loadingIndicator.style.display = DisplayStyle.None;
        loadingIndicator.style.opacity = 0;

        // Reset rotation
        if (loadingIcon != null)
        {
            loadingIcon.transform.rotation = Quaternion.identity;
        }
    }

    private static IEnumerator RotateLoader()
    {
        float rotationSpeed = 360f; // degrees per second

        while (isLoading)
        {
            if (loadingIcon != null)
            {
                float currentRotation = loadingIcon.transform.rotation.eulerAngles.z;
                float newRotation = currentRotation + rotationSpeed * Time.deltaTime;
                loadingIcon.transform.rotation = Quaternion.Euler(0, 0, newRotation);
            }
            yield return null;
        }
    }

    private class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SearchCoroutineRunner");
                    _instance = obj.AddComponent<CoroutineRunner>();
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public Coroutine StartRoutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
        
        public void StopRoutine(Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
    }
}
