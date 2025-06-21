using UnityEngine;
using UnityEngine.UIElements;

public class HousematesController : MonoBehaviour
{
    private readonly string[] options = {
        "I live alone",
        "with partner",
        "with family",
        "with roommates",
        "with pets"
    };

    public void Init(VisualElement root)
    {
        var group = root.Q<VisualElement>("checkboxGroup");
        foreach (var label in options)
        {
            var toggle = new Toggle(label)
            {
                name = label.Replace(" ", "_").ToLower()
            };
            toggle.AddToClassList("toggle");
            group.Add(toggle);
        }

        var completeBtn = root.Q<Button>("completeButton");
        if (completeBtn != null)
        {
            completeBtn.clicked += () =>
            {
                Debug.Log("Completed. Selected options:");
                foreach (var child in group.Children())
                {
                    if (child is Toggle toggle && toggle.value)
                        Debug.Log(toggle.text);
                }
            };
        }
    }
}