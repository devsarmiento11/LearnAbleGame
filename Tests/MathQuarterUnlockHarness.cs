// Minimal Unity substitute for testing the production scripts without touching real PlayerPrefs.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace UnityEngine {
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
    public class Object {}
    public class Transform { public void SetAsLastSibling() {} public bool IsChildOf(Transform t) { return false; } }
    public class GameObject : Object {
        public bool activeInHierarchy; public Transform transform = new Transform();
        public void SetActive(bool active) { activeInHierarchy = active; }
        public T GetComponentInParent<T>() where T : class { return null; }
        public T[] GetComponentsInChildren<T>() { return new T[0]; }
    }
    public class MonoBehaviour {
        public T GetComponentInParent<T>() where T : class { return null; }
    }
    public class Canvas { public T[] GetComponentsInChildren<T>(bool inactive) { return new T[0]; } }
    public class Sprite : Object {}
    public static class Debug { public static void Log(string s) {} }
    public static class PlayerPrefs {
        private static readonly Dictionary<string,int> values = new Dictionary<string,int>();
        public static int GetInt(string key, int fallback) { return values.TryGetValue(key, out var value) ? value : fallback; }
        public static void SetInt(string key, int value) { values[key] = value; }
        public static void DeleteKey(string key) { values.Remove(key); }
        public static void Save() {}
    }
}
namespace UnityEngine.UI {
    public class Selectable {
        public bool interactable = true; public string name = "Cancel";
        public GameObject gameObject = new GameObject(); public Transform transform = new Transform();
        public bool IsInteractable() { return interactable; }
    }
    public class Button : Selectable {}
    public class Image { public Sprite sprite; }
}
namespace UnityEngine.EventSystems {
    public class EventSystem {
        public static EventSystem current; public GameObject currentSelectedGameObject;
        public void SetSelectedGameObject(GameObject obj) { currentSelectedGameObject = obj; }
    }
}
public class EnglishLevelFinishIndicators { public bool IsComplete(int level) { return false; } }
public static class MathQuarterUnlockHarness {
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run() {
        string[] ids = { "Math_LetsCount", "Math_ComparingNumbers", "Math_LetsCompareNumbers",
            "Math_MatchTheCoinsToTheirValues", "Math_MatchTheMoneyToTheirValues", "Math_HowMuchWillYouPay?" };
        foreach (string id in ids) {
            var manager = new UnlockLevelManager { activityId = id, confirmationPopup = new GameObject(),
                levelButtons = new UnlockLevelManager.LevelButtonData[5] };
            for (int level = 1; level <= 5; level++) manager.levelButtons[level-1] = new UnlockLevelManager.LevelButtonData {
                levelNumber = level, buttonImage = new Image(), lockedSprite = new Sprite(), unlockedSprite = new Sprite() };
            manager.LockLevels2To5();
            Check(LevelUnlockStorage.IsUnlocked(id, 1), "Level 1 must remain unlocked");
            foreach (int invalid in new[] { -1, 0, 1, 6 }) { manager.RequestUnlock(invalid); Check(!manager.confirmationPopup.activeInHierarchy, "Invalid request opened popup"); }
            for (int level = 2; level <= 5; level++) {
                var data = manager.levelButtons[level-1];
                Check(data.buttonImage.sprite == data.lockedSprite, "Locked sprite not refreshed");
                manager.ConfirmUnlock(); Check(!LevelUnlockStorage.IsUnlocked(id, level), "Confirmed without request");
                manager.RequestUnlock(level); Check(manager.confirmationPopup.activeInHierarchy, "Popup did not open");
                manager.CancelUnlock(); manager.ConfirmUnlock();
                Check(!LevelUnlockStorage.IsUnlocked(id, level), "Cancel persisted unlock");
                manager.RequestUnlock(level); manager.RequestUnlock(level == 5 ? 2 : level+1); manager.ConfirmUnlock();
                Check(LevelUnlockStorage.IsUnlocked(id, level), "Confirm failed to persist selected level");
                Check(!manager.confirmationPopup.activeInHierarchy, "Confirm left popup open");
                Check(data.buttonImage.sprite == data.unlockedSprite, "Unlocked sprite not refreshed");
                for (int later = level+1; later <= 5; later++) Check(!LevelUnlockStorage.IsUnlocked(id, later), "Repeated click changed selection");
                manager.RequestUnlock(level); Check(!manager.confirmationPopup.activeInHierarchy, "Already unlocked request reopened popup");
                var button = new Button(); var icon = new GameObject();
                var gate = new LevelLockController { activityId = id, levelNumber = level, lockObject = icon, buttonsToLock = new[] { button } };
                gate.RefreshLockState(); Check(button.interactable && !icon.activeInHierarchy, "Unlocked gameplay button blocked");
                LevelUnlockStorage.LockLevel(id, level); gate.RefreshLockState();
                Check(!button.interactable && icon.activeInHierarchy, "Locked gameplay button enabled");
                LevelUnlockStorage.UnlockLevel(id, level);
            }
            manager.RequestUnlock(2); manager.LockLevels2To5(); manager.ConfirmUnlock();
            for (int level = 2; level <= 5; level++) Check(!LevelUnlockStorage.IsUnlocked(id, level), "Relock failed");
            Check(LevelUnlockStorage.IsUnlocked(id, 1), "Relock locked level 1");
        }
        LevelUnlockStorage.UnlockLevel(ids[0], 3);
        Check(!LevelUnlockStorage.IsUnlocked(ids[1], 3), "Activity unlock leaked to another activity");
        LevelUnlockStorage.LockLevel(ids[0], 3);
    }
}
