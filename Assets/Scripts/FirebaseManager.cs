using UnityEngine;
using Firebase;

public class FirebaseManager : MonoBehaviour
{
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("🔥 Firebase is connected successfully!");
            }
            else
            {
                Debug.LogError("Firebase dependencies not available: " + dependencyStatus);
            }
        });
    }
}