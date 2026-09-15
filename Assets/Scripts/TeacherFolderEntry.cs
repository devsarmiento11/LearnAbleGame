using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class TeacherFolderEntry : MonoBehaviour
{
    public Button button;
    public TMP_Text folderLabel;

    public void Setup(string folderName, UnityAction openFolder)
    {
        if (button == null) button = GetComponent<Button>();
        if (folderLabel == null) folderLabel = GetComponentInChildren<TMP_Text>(true);
        folderLabel.richText = false; folderLabel.text = folderName;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(openFolder);
    }
}
