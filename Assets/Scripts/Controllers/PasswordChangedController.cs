using UnityEngine;
using UnityEngine.UIElements;
using Services;

public class PasswordChangedController : MonoBehaviour
{
    private Button signInButton;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        signInButton = root.Q<Button>("signInButton");

        if (signInButton != null)
        {
            signInButton.clicked += () =>
            {
                UIManager.Instance.OpenScreen(UIScreenType.Login);
            };
        }
        else
        {
            Debug.LogError("Sign In button not found in UXML.");
        }
    }
}