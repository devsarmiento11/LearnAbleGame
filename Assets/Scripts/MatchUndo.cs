using UnityEngine;

public class MatchUndo : MonoBehaviour
{
    public void Undo()
    {
        MatchManager.Instance.Undo();
    }
}