using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("Screens")]
    public VisualTreeAsset completeProfileScreen;
    public VisualTreeAsset budgetScreen;
    public VisualTreeAsset familyScreen;
   
    private string userName = "Vansh";
    private readonly string[] locations = { "New York", "Los Angeles", "Chicago", "Houston", "San Francisco" };

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        ShowCompleteProfile();
    }

    public void ShowCompleteProfile()
    {
        uiDocument.visualTreeAsset = completeProfileScreen;
        var root = uiDocument.rootVisualElement;

        var screen = new CompleteProfileController();
        screen.Initialize(root, this);
    }

    public void ShowBudgetScreen()
    {
        uiDocument.visualTreeAsset = budgetScreen;
        var root = uiDocument.rootVisualElement;

        var screen = new BudgeScreenController();
        screen.Initialize(root, this);
    }

    public void ShowFamilyScreen()
    {
        uiDocument.visualTreeAsset = familyScreen;
        var root = uiDocument.rootVisualElement;
    }
}