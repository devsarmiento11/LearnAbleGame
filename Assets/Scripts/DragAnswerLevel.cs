using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DragAnswerLevel : MonoBehaviour
{
    public DragAnswerChoice[] choices;
    public DragAnswerZone[] dropZones;
    public GameTimer timer;
    [Range(0, 100)] public int passingScore = 50;
    public string scoreSceneName = "ScoreScene";
    public string failedScoreSceneName = "ScoreSceneFailed";
    public bool IsFinished { get; private set; }
    readonly Stack<DragAnswerZone[]> history = new Stack<DragAnswerZone[]>();
    bool moving;

    void Awake()
    {
        foreach (var choice in choices) if (choice != null) choice.manager = this;
        foreach (var zone in dropZones) if (zone != null) zone.manager = this;
        if (timer != null) timer.Expired = TimeUp;
    }

    void Start()
    {
        ScoreManager.BeginActivity();
        ScoreManager.TotalLines = dropZones.Where(z => z != null).Distinct().Count();
    }

    internal bool BeginMove()
    {
        if (moving || IsFinished) return false;
        history.Push(choices.Select(c => c != null ? c.Zone : null).ToArray());
        moving = true;
        return true;
    }

    internal void EndMove() { moving = false; }

    public void UndoLastMove()
    {
        if (IsFinished || moving || history.Count == 0) return;
        var previous = history.Pop();
        foreach (var choice in choices) if (choice != null) choice.ReturnHome();
        for (int i = 0; i < choices.Length; i++)
            if (choices[i] != null && previous[i] != null) choices[i].Accept(previous[i]);
    }

    public void Done() { if (!moving) Finish(false); }
    void TimeUp() { Finish(true); }

    void Finish(bool timedOut)
    {
        if (IsFinished) return;
        var zones = dropZones.Where(z => z != null).Distinct().ToArray();
        if (zones.Length == 0) { Debug.LogError("Assign at least one answer zone.", this); return; }
        IsFinished = true;
        if (timer != null) timer.StopTimer();
        int correct = zones.Count(z => z.IsCorrect);
        ScoreManager.TotalLines = zones.Length;
        ScoreManager.CorrectLines = correct;
        ScoreManager.CurrentScore = Mathf.RoundToInt(100f * correct / zones.Length);
        string scene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("PreviousGameScene", scene);
        PlayerPrefs.Save();
        ScoreManager.RecordActivity(scene, zones.Length);
        SceneManager.LoadScene(!timedOut && ScoreManager.CurrentScore >= passingScore
            ? scoreSceneName : failedScoreSceneName);
    }
}
