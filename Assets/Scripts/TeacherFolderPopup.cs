using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keep this modal above the scroll view, including its transparent raycast area.
[DisallowMultipleComponent]
public sealed class TeacherFolderPopup : MonoBehaviour
{
    public TMP_InputField nameInput;
    public Button confirmButton;
    public Button cancelButton;
    TMP_Text feedback;

    void OnEnable()
    {
        transform.SetAsLastSibling();
        EnsureFeedback();
    }

    void EnsureFeedback()
    {
        if (feedback != null || nameInput == null) return;
        var go = new GameObject("Folder feedback", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        feedback = go.AddComponent<TextMeshProUGUI>();
        feedback.font = nameInput.textComponent.font;
        feedback.fontSize = 14; feedback.color = nameInput.textComponent.color;
        feedback.alignment = TextAlignmentOptions.Center;
        feedback.richText = false; feedback.raycastTarget = false;
        var rect = feedback.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 60);
        rect.sizeDelta = new Vector2(((RectTransform)transform).rect.width - 90, 42);
    }

    public void Show()
    {
        gameObject.SetActive(true); transform.SetAsLastSibling();
        ShowMessage(""); nameInput.ActivateInputField();
    }

    public void ShowMessage(string text)
    {
        EnsureFeedback();
        if (feedback != null) feedback.text = text;
    }
}
