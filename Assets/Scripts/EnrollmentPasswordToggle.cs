using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Adds only an eye inside the existing field; the field's artwork and outer rect stay untouched.
public sealed class EnrollmentPasswordToggle : MonoBehaviour
{
    TMP_InputField input;
    Button button;
    LoginPasswordEye eye;
    bool visible;

    public static EnrollmentPasswordToggle Attach(TMP_InputField field)
    {
        var toggle = field.GetComponent<EnrollmentPasswordToggle>();
        if (toggle != null) return toggle;
        toggle = field.gameObject.AddComponent<EnrollmentPasswordToggle>();
        toggle.Initialize(field);
        return toggle;
    }

    void Initialize(TMP_InputField field)
    {
        input = field;
        var hitArea = new GameObject("PasswordVisibility", typeof(RectTransform), typeof(Image), typeof(Button));
        hitArea.transform.SetParent(field.transform, false);
        var rect = (RectTransform)hitArea.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, .5f);
        rect.pivot = new Vector2(1, .5f);
        rect.sizeDelta = new Vector2(34, 32);
        // The authored field sprite has transparent padding: its visible border is
        // seven canvas units above the input's RectTransform center.
        rect.anchoredPosition3D = new Vector3(-5, 7, 0);
        hitArea.GetComponent<Image>().color = Color.clear;

        var icon = new GameObject("Eye", typeof(RectTransform), typeof(CanvasRenderer), typeof(LoginPasswordEye));
        icon.transform.SetParent(hitArea.transform, false);
        eye = icon.GetComponent<LoginPasswordEye>();
        var eyeRect = eye.rectTransform;
        eyeRect.anchorMin = eyeRect.anchorMax = new Vector2(.5f, .5f);
        eyeRect.pivot = new Vector2(.5f, .5f);
        eyeRect.sizeDelta = new Vector2(22, 22);
        eyeRect.anchoredPosition3D = Vector3.zero;
        eye.raycastTarget = false;
        button = hitArea.GetComponent<Button>();
        button.targetGraphic = eye;
        var colors = button.colors;
        colors.normalColor = new Color(.48f, .20f, .035f);
        colors.highlightedColor = new Color(.70f, .35f, .045f);
        colors.selectedColor = colors.normalColor;
        colors.pressedColor = new Color(.34f, .12f, .02f);
        colors.disabledColor = new Color(.48f, .20f, .035f, .4f);
        button.colors = colors;
        button.onClick.AddListener(() => {
            if (input.interactable) SetVisible(!visible);
        });
        // Long passwords scroll within the text area instead of running underneath the eye.
        field.textViewport.offsetMax = new Vector2(-42, field.textViewport.offsetMax.y);
        SetVisible(false);
    }

    public void SetInteractable(bool value)
    {
        if (button == null) return;
        button.interactable = value;
        if (!value) SetVisible(false);
    }

    void SetVisible(bool value)
    {
        visible = value;
        input.inputType = value ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
        input.ForceLabelUpdate(); eye.SetVisible(value);
        button.gameObject.name = value ? "Hide password" : "Show password";
    }
    void OnDisable() { if (input != null) SetVisible(false); }
    void OnApplicationFocus(bool focused) { if (!focused && input != null) SetVisible(false); }
}
