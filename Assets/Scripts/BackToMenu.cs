using UnityEngine;

public class BackToMenu : MonoBehaviour
{
    public void GoBack()
    {
        SceneReturnHistory.GoBack("MathScene");
    }
}
