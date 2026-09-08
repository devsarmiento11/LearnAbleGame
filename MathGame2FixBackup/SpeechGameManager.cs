using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using SpeechToTextNamespace;
using System;
using System.Text.RegularExpressions;

public class SpeechGameManager : MonoBehaviour, ISpeechToTextListener
{
    // =========================================================
    // MISSING NUMBER DATA
    // =========================================================

    [Serializable]
    public class MissingNumber
    {
        [Tooltip("Correct missing number. Example: 7")]
        public int correctNumber;

        [Tooltip("UI Image placed over the blank.")]
        public Image numberImage;

        [Tooltip("Sprite of the correct number.")]
        public Sprite numberSprite;

        [HideInInspector]
        public bool solved;
    }


    [Header("MISSING NUMBERS")]
    [Tooltip("Level 1 = Size 1, Level 5 = Size 4, etc.")]
    public MissingNumber[] missingNumbers;


    [Header("UI")]
    public TMP_Text result;


    [Header("SCORE SCENES")]
    public string scoreSceneName = "ScoreScene";
    public string failedScoreSceneName = "ScoreSceneFailed";


    private bool isListening = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        SpeechToText.Initialize("en-US");

        if (missingNumbers != null)
        {
            foreach (MissingNumber item in missingNumbers)
            {
                item.solved = false;

                if (item.numberImage != null)
                {
                    item.numberImage.gameObject.SetActive(false);
                }
            }
        }

        if (result != null)
        {
            result.text = "";
        }
    }


    // =========================================================
    // PRESS DOWN MICROPHONE
    // =========================================================

    public void StartListening()
    {
        if (AllNumbersSolved())
        {
            if (result != null)
                result.text = "All Correct!";

            return;
        }

        if (SpeechToText.IsBusy())
            return;

        if (!SpeechToText.CheckPermission())
        {
            SpeechToText.RequestPermissionAsync((permission) =>
            {
                if (permission == SpeechToText.Permission.Granted)
                {
                    BeginSpeechRecognition();
                }
                else
                {
                    if (result != null)
                        result.text = "Microphone Permission Required";
                }
            });

            return;
        }

        BeginSpeechRecognition();
    }


    private void BeginSpeechRecognition()
    {
        if (SpeechToText.IsBusy())
            return;

        bool started = SpeechToText.Start(this);

        if (started)
        {
            isListening = true;

            if (result != null)
                result.text = "Listening...";
        }
        else
        {
            isListening = false;

            if (result != null)
                result.text = "Try Again";
        }
    }


    // =========================================================
    // RELEASE MICROPHONE
    // =========================================================

    public void StopListening()
    {
        if (!isListening)
            return;

        if (SpeechToText.IsBusy())
        {
            SpeechToText.ForceStop();
        }

        isListening = false;
    }


    // =========================================================
    // FINAL SPEECH RESULT
    // =========================================================

    public void OnResultReceived(string spokenText, int? errorCode)
    {
        isListening = false;

        Debug.Log("Speech Result: " + spokenText);

        if (errorCode != null)
        {
            if (result != null)
                result.text = "Try Again";

            Debug.Log("Speech Error Code: " + errorCode);
            return;
        }

        if (string.IsNullOrWhiteSpace(spokenText))
        {
            if (result != null)
                result.text = "Try Again";

            return;
        }

        CheckAnswer(spokenText.ToLower().Trim());
    }


    // =========================================================
    // CHECK ANSWER
    // =========================================================

    private void CheckAnswer(string spokenText)
    {
        Debug.Log("Student said: " + spokenText);

        if (missingNumbers == null || missingNumbers.Length == 0)
        {
            Debug.LogError("No missing numbers assigned.");
            return;
        }

        foreach (MissingNumber item in missingNumbers)
        {
            if (item.solved)
                continue;

            if (ContainsNumber(spokenText, item.correctNumber))
            {
                SolveNumber(item);
                return;
            }
        }

        if (result != null)
        {
            result.text = "Try Again";
        }
    }


    // =========================================================
    // SOLVE NUMBER
    // =========================================================

    private void SolveNumber(MissingNumber item)
    {
        if (item == null || item.solved)
            return;

        item.solved = true;

        if (item.numberImage != null)
        {
            item.numberImage.sprite = item.numberSprite;
            item.numberImage.gameObject.SetActive(true);
        }

        if (AllNumbersSolved())
        {
            if (result != null)
                result.text = "All Correct!";
        }
        else
        {
            if (result != null)
                result.text = "Correct!";
        }

        Debug.Log("Correct Number: " + item.correctNumber);
    }


    // =========================================================
    // NUMBER DETECTION
    // =========================================================

    private bool ContainsNumber(string spokenText, int number)
    {
        string numberWord = NumberToWord(number);

        bool wordFound = Regex.IsMatch(
            spokenText,
            @"\b" + Regex.Escape(numberWord) + @"\b",
            RegexOptions.IgnoreCase
        );

        bool digitFound = Regex.IsMatch(
            spokenText,
            @"\b" + number.ToString() + @"\b"
        );

        return wordFound || digitFound;
    }


    // =========================================================
    // ALL NUMBERS SOLVED?
    // =========================================================

    private bool AllNumbersSolved()
    {
        if (missingNumbers == null || missingNumbers.Length == 0)
            return false;

        foreach (MissingNumber item in missingNumbers)
        {
            if (item == null || !item.solved)
                return false;
        }

        return true;
    }


    // =========================================================
    // CORRECT ANSWER COUNT
    // =========================================================

    private int GetCorrectAnswers()
    {
        if (missingNumbers == null)
            return 0;

        int correct = 0;

        foreach (MissingNumber item in missingNumbers)
        {
            if (item != null && item.solved)
            {
                correct++;
            }
        }

        return correct;
    }


    // =========================================================
    // DONE BUTTON
    // =========================================================

    public void Done()
    {
        if (missingNumbers == null || missingNumbers.Length == 0)
        {
            Debug.LogError("No missing numbers assigned!");
            return;
        }

        int correctAnswers = GetCorrectAnswers();
        int totalQuestions = missingNumbers.Length;

        ScoreManager.CorrectLines = correctAnswers;

        ScoreManager.CurrentScore =
            Mathf.RoundToInt(
                (correctAnswers / (float)totalQuestions) * 100f
            );

        Debug.Log("Correct Answers: " + correctAnswers);
        Debug.Log("Total Questions: " + totalQuestions);
        Debug.Log("Final Score: " + ScoreManager.CurrentScore);


        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();


        ScoreManager.RecordActivity(
            SceneManager.GetActiveScene().name,
            totalQuestions
        );


        if (ScoreManager.CurrentScore >= 50)
        {
            SceneManager.LoadScene(scoreSceneName);
        }
        else
        {
            SceneManager.LoadScene(failedScoreSceneName);
        }
    }


    // =========================================================
    // NUMBER TO WORD
    // =========================================================

    private string NumberToWord(int number)
    {
        switch (number)
        {
            case 0: return "zero";
            case 1: return "one";
            case 2: return "two";
            case 3: return "three";
            case 4: return "four";
            case 5: return "five";
            case 6: return "six";
            case 7: return "seven";
            case 8: return "eight";
            case 9: return "nine";
            case 10: return "ten";
            case 11: return "eleven";
            case 12: return "twelve";
            case 13: return "thirteen";
            case 14: return "fourteen";
            case 15: return "fifteen";
            case 16: return "sixteen";
            case 17: return "seventeen";
            case 18: return "eighteen";
            case 19: return "nineteen";
            case 20: return "twenty";

            default:
                return number.ToString();
        }
    }


    // =========================================================
    // SPEECH PLUGIN CALLBACKS
    // =========================================================

    public void OnReadyForSpeech()
    {
        Debug.Log("Ready for speech.");
    }

    public void OnBeginningOfSpeech()
    {
        Debug.Log("Student started speaking.");
    }

    public void OnVoiceLevelChanged(float level)
    {
    }

    public void OnPartialResultReceived(string partialResult)
    {
        Debug.Log("Partial Result: " + partialResult);
    }
}