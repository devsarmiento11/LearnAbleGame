using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class StudentSceneUI
{
    public static T At<T>(Transform root, string path) where T : Component
    {
        var child = root.Find(path);
        if (child == null || !child.TryGetComponent<T>(out var component)) throw new InvalidOperationException("Missing student scene control: " + path);
        return component;
    }
    public static Button Bind(Transform root, string path, UnityAction action)
    {
        var button = At<Button>(root, path);
        // Replace serialized navigation so selection/validation always happens before leaving.
        button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(action); return button;
    }
    public static void Text(Transform root, string path, string text)
    {
        var label = At<TMP_Text>(root, path); label.richText = false; label.text = string.IsNullOrWhiteSpace(text) ? "—" : text;
    }
}
