using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using SpeechToTextNamespace;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Tooltip("UI Image positioned on the blank.")]
        public Image numberImage;

        [Tooltip("Sprite that appears when answered correctly.")]
        public Sprite numberSprite;

        [HideInInspector]
        public bool solved = false;
    }


    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("MISSING NUMBERS")]
    [Tooltip("Level 1 = Size 1. Level 5 can be Size 4.")]
    public MissingNumber[] missingNumbers;


    [Header("UI - OPTIONAL")]
    [Tooltip("Displays Listening, Correct, Try Again, speech errors, etc.")]
    public TMP_Text result;


    [Header("SPEECH SETTINGS")]
    [Tooltip("If enabled, saying several answers in one recording can reveal several numbers.")]
    public bool allowMultipleAnswersPerSpeech = false;


    [Header("SCORE SCENES")]
    public string scoreSceneName = "ScoreScene";
    public string failedScoreSceneName = "ScoreSceneFailed";


    [Header("EDITOR TEST")]
    [Tooltip("Used only to test without an Android microphone.")]
    public string testTranscript = "seven";


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private bool microphoneHeld = false;
    private bool isListening = false;
    private bool permissionPending = false;
    private bool awaitingResult = false;
    private bool activityFinished = false;
    private bool speechAvailable = false;

    private readonly List<MissingNumber> answerHistory =
        new List<MissingNumber>();


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ScoreManager.BeginActivity();

        bool initialized = SpeechToText.Initialize("en-US");

        speechAvailable =
            initialized &&
            SpeechToText.IsServiceAvailable();

        Debug.Log(
            "[SpeechGame] Initialized = " +
            initialized +
            " | Service Available = " +
            speechAvailable
        );

        ResetAnswers();

        if (speechAvailable)
        {
            SetResult("");
        }
        else
        {
#if UNITY_EDITOR
            // The native speech service isn't expected to work
            // normally inside the Unity Editor.
            SetResult("");
#else
            SetResult("Speech recognition unavailable.");
#endif
        }
    }


    // =========================================================
    // POINTER DOWN
    // HOLD MICROPHONE
    // =========================================================

    public void StartListening()
    {
        if (activityFinished)
            return;

        if (!isActiveAndEnabled)
            return;

        microphoneHeld = true;

        Debug.Log("[SpeechGame] Microphone pressed.");

        if (AllNumbersSolved())
        {
            SetResult("All Correct!");
            return;
        }


#if UNITY_EDITOR

        // Don't try native Android speech recognition
        // while running inside Unity Editor.
        SetResult("Editor Mode - use Test Spoken Answer");
        return;

#else

        // Check speech service again.
        if (!speechAvailable)
        {
            speechAvailable =
                SpeechToText.IsServiceAvailable();

            if (!speechAvailable)
            {
                SetResult("Speech service unavailable.");

                Debug.LogError(
                    "[SpeechGame] Speech service unavailable."
                );

                return;
            }
        }


        // Prevent overlapping recognition sessions.
        if (permissionPending)
        {
            Debug.Log(
                "[SpeechGame] Waiting for microphone permission."
            );

            return;
        }

        if (awaitingResult)
        {
            Debug.Log(
                "[SpeechGame] Waiting for previous speech result."
            );

            return;
        }

        if (SpeechToText.IsBusy())
        {
            Debug.Log(
                "[SpeechGame] Speech recognizer already busy."
            );

            return;
        }


        // =====================================================
        // MICROPHONE PERMISSION
        // =====================================================

        if (!SpeechToText.CheckPermission())
        {
            permissionPending = true;

            SetResult("Allow microphone permission...");

            SpeechToText.RequestPermissionAsync(
                (permission) =>
                {
                    if (this == null)
                        return;

                    permissionPending = false;

                    if (!isActiveAndEnabled)
                        return;

                    if (activityFinished)
                        return;


                    if (permission ==
                        SpeechToText.Permission.Granted)
                    {
                        Debug.Log(
                            "[SpeechGame] Microphone permission granted."
                        );

                        if (microphoneHeld)
                        {
                            BeginSpeechRecognition();
                        }
                        else
                        {
                            SetResult(
                                "Hold the microphone and say the number."
                            );
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[SpeechGame] Microphone permission denied."
                        );

                        SetResult(
                            "Microphone Permission Required"
                        );
                    }
                }
            );

            return;
        }


        BeginSpeechRecognition();

#endif
    }


    // =========================================================
    // START NATIVE SPEECH RECOGNITION
    // =========================================================

    private void BeginSpeechRecognition()
    {
#if UNITY_EDITOR
        return;
#else

        if (!microphoneHeld)
            return;

        if (activityFinished)
            return;

        if (!isActiveAndEnabled)
            return;

        if (SpeechToText.IsBusy())
            return;


        awaitingResult = true;

        bool started =
            SpeechToText.Start(this);


        if (started)
        {
            isListening = true;

            SetResult("Listening...");

            Debug.Log(
                "[SpeechGame] Recognition started."
            );
        }
        else
        {
            awaitingResult = false;
            isListening = false;

            SetResult("Could not start microphone");

            Debug.LogError(
                "[SpeechGame] SpeechToText.Start returned FALSE."
            );
        }

#endif
    }


    // =========================================================
    // POINTER UP
    // RELEASE MICROPHONE
    // =========================================================

    public void StopListening()
    {
        microphoneHeld = false;

        Debug.Log("[SpeechGame] Microphone released.");


#if UNITY_EDITOR
        return;
#else

        if (!isListening)
            return;


        if (SpeechToText.IsBusy())
        {
            // Stops recording but still requests the
            // final transcript from the recognizer.
            SpeechToText.ForceStop();

            Debug.Log(
                "[SpeechGame] ForceStop called. Waiting for transcript."
            );
        }

        isListening = false;

#endif
    }


    // =========================================================
    // FINAL SPEECH RESULT
    // =========================================================

    public void OnResultReceived(
        string spokenText,
        int? errorCode
    )
    {
        isListening = false;
        awaitingResult = false;


        if (this == null)
            return;

        if (!isActiveAndEnabled)
            return;

        if (activityFinished)
            return;


        string safeText =
            spokenText ?? "";


        Debug.Log(
            "[SpeechGame] FINAL SPEECH = [" +
            safeText +
            "]"
        );


        Debug.Log(
            "[SpeechGame] ERROR CODE = " +
            (
                errorCode.HasValue
                    ? errorCode.Value.ToString()
                    : "NONE"
            )
        );


        // =====================================================
        // SPEECH ERROR
        // =====================================================

        if (errorCode.HasValue)
        {
            int code =
                errorCode.Value;


            // Android recognizer permission issue.
            if (code == 9)
            {
                SetResult(
                    "Microphone permission error"
                );

                Debug.LogError(
                    "[SpeechGame] Error 9: speech recognizer permission problem."
                );

                return;
            }


            // No useful speech / timeout.
            if (code == 6)
            {
                SetResult(
                    "Say the number again"
                );

                Debug.LogWarning(
                    "[SpeechGame] Error 6: speech timeout/no speech."
                );

                return;
            }


            SetResult(
                "Speech Error: " + code
            );

            return;
        }


        // =====================================================
        // EMPTY SPEECH
        // =====================================================

        if (string.IsNullOrWhiteSpace(safeText))
        {
            SetResult(
                "I didn't hear you"
            );

            Debug.LogWarning(
                "[SpeechGame] Empty speech result."
            );

            return;
        }


        string cleanedText =
            safeText
                .ToLower()
                .Trim();


        Debug.Log(
            "[SpeechGame] Android heard: " +
            cleanedText
        );


        // Temporarily show exactly what Android heard.
        SetResult(
            "Heard: " + cleanedText
        );


        CheckAnswer(cleanedText);
    }


    // =========================================================
    // PARTIAL SPEECH
    // =========================================================

    public void OnPartialResultReceived(
        string partialResult
    )
    {
        if (string.IsNullOrWhiteSpace(partialResult))
            return;


        Debug.Log(
            "[SpeechGame] PARTIAL = [" +
            partialResult +
            "]"
        );


        if (result != null)
        {
            result.text =
                "Hearing: " +
                partialResult;
        }
    }


    // =========================================================
    // CHECK ANSWER
    // =========================================================

    private void CheckAnswer(
        string spokenText
    )
    {
        if (missingNumbers == null ||
            missingNumbers.Length == 0)
        {
            Debug.LogError(
                "[SpeechGame] No missing numbers assigned."
            );

            return;
        }


        Debug.Log(
            "[SpeechGame] Checking answer: " +
            spokenText
        );


        int foundAnswers = 0;


        foreach (MissingNumber item
                 in missingNumbers)
        {
            if (item == null)
                continue;

            if (item.solved)
                continue;


            Debug.Log(
                "[SpeechGame] Looking for number " +
                item.correctNumber +
                " (" +
                NumberToWord(item.correctNumber) +
                ")"
            );


            if (ContainsNumber(
                spokenText,
                item.correctNumber
            ))
            {
                SolveNumber(item);

                foundAnswers++;


                // For your normal activity:
                // one spoken number = one revealed answer.
                if (!allowMultipleAnswersPerSpeech)
                    break;
            }
        }


        if (foundAnswers > 0)
        {
            if (AllNumbersSolved())
            {
                SetResult("All Correct!");
            }
            else
            {
                SetResult("Correct!");
            }
        }
        else
        {
            SetResult(
                "Try Again"
            );

            Debug.LogWarning(
                "[SpeechGame] No missing number matched speech."
            );
        }
    }


    // =========================================================
    // REVEAL ANSWER
    // =========================================================

    private void SolveNumber(
        MissingNumber item
    )
    {
        if (item == null)
            return;

        if (item.solved)
            return;


        item.solved = true;


        if (item.numberImage != null)
        {
            if (item.numberSprite != null)
            {
                item.numberImage.sprite =
                    item.numberSprite;
            }
            else
            {
                Debug.LogWarning(
                    "[SpeechGame] Sprite missing for number " +
                    item.correctNumber
                );
            }


            item.numberImage
                .gameObject
                .SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[SpeechGame] Number Image missing for " +
                item.correctNumber
            );
        }


        answerHistory.Add(item);


        Debug.Log(
            "[SpeechGame] CORRECT! Revealed number " +
            item.correctNumber
        );
    }


    // =========================================================
    // NUMBER MATCHING
    // =========================================================

    private bool ContainsNumber(
        string spokenText,
        int number
    )
    {
        string numberWord =
            NumberToWord(number);


        bool wordFound =
            Regex.IsMatch(
                spokenText,
                @"\b" +
                Regex.Escape(numberWord) +
                @"\b",
                RegexOptions.IgnoreCase
            );


        bool digitFound =
            Regex.IsMatch(
                spokenText,
                @"\b" +
                number.ToString() +
                @"\b"
            );


        Debug.Log(
            "[SpeechGame] Number " +
            number +
            " | Word match = " +
            wordFound +
            " | Digit match = " +
            digitFound
        );


        return wordFound ||
               digitFound;
    }


    // =========================================================
    // ALL NUMBERS SOLVED?
    // =========================================================

    private bool AllNumbersSolved()
    {
        if (missingNumbers == null ||
            missingNumbers.Length == 0)
        {
            return false;
        }


        foreach (MissingNumber item
                 in missingNumbers)
        {
            if (item == null)
                return false;

            if (!item.solved)
                return false;
        }


        return true;
    }


    // =========================================================
    // COUNT CORRECT ANSWERS
    // =========================================================

    private int GetCorrectAnswers()
    {
        if (missingNumbers == null)
            return 0;


        int correct = 0;


        foreach (MissingNumber item
                 in missingNumbers)
        {
            if (item != null &&
                item.solved)
            {
                correct++;
            }
        }


        return correct;
    }


    // =========================================================
    // UNDO LAST ANSWER
    // =========================================================

    public void UndoLastAnswer()
    {
        if (activityFinished)
            return;


        if (answerHistory.Count == 0)
        {
            Debug.Log(
                "[SpeechGame] Nothing to undo."
            );

            return;
        }


        int index =
            answerHistory.Count - 1;


        MissingNumber item =
            answerHistory[index];


        answerHistory.RemoveAt(index);


        if (item != null)
        {
            item.solved = false;


            if (item.numberImage != null)
            {
                item.numberImage
                    .gameObject
                    .SetActive(false);
            }


            Debug.Log(
                "[SpeechGame] Undid number " +
                item.correctNumber
            );
        }


        SetResult("");
    }


    // =========================================================
    // RESET ALL ANSWERS
    // =========================================================

    public void ResetAnswers()
    {
        answerHistory.Clear();


        if (missingNumbers == null)
            return;


        foreach (MissingNumber item
                 in missingNumbers)
        {
            if (item == null)
                continue;


            item.solved = false;


            if (item.numberImage != null)
            {
                item.numberImage
                    .gameObject
                    .SetActive(false);
            }
        }


        SetResult("");


        Debug.Log(
            "[SpeechGame] Answers reset."
        );
    }


    // =========================================================
    // DONE + SCORE
    // =========================================================

    public void Done()
    {
        if (activityFinished)
            return;


        if (awaitingResult)
        {
            StopListening();

            SetResult(
                "Please wait for your answer"
            );

            return;
        }


        if (missingNumbers == null ||
            missingNumbers.Length == 0)
        {
            Debug.LogError(
                "[SpeechGame] No missing numbers assigned."
            );

            return;
        }


        activityFinished = true;
        microphoneHeld = false;


        int correctAnswers =
            GetCorrectAnswers();


        int totalQuestions =
            missingNumbers.Length;


        ScoreManager.CorrectLines =
            correctAnswers;


        ScoreManager.CurrentScore =
            Mathf.RoundToInt(
                (
                    correctAnswers /
                    (float)totalQuestions
                )
                * 100f
            );


        Debug.Log(
            "[SpeechGame] Correct Answers = " +
            correctAnswers
        );


        Debug.Log(
            "[SpeechGame] Total Questions = " +
            totalQuestions
        );


        Debug.Log(
            "[SpeechGame] Final Score = " +
            ScoreManager.CurrentScore
        );


        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager
                .GetActiveScene()
                .name
        );


        PlayerPrefs.Save();


        ScoreManager.RecordActivity(
            SceneManager
                .GetActiveScene()
                .name,
            totalQuestions
        );


        if (ScoreManager.CurrentScore >= 50)
        {
            SceneManager.LoadScene(
                scoreSceneName
            );
        }
        else
        {
            SceneManager.LoadScene(
                failedScoreSceneName
            );
        }
    }


    // =========================================================
    // NUMBER WORD
    // =========================================================

    private string NumberToWord(
        int number
    )
    {
        switch (number)
        {
            case 0:
                return "zero";

            case 1:
                return "one";

            case 2:
                return "two";

            case 3:
                return "three";

            case 4:
                return "four";

            case 5:
                return "five";

            case 6:
                return "six";

            case 7:
                return "seven";

            case 8:
                return "eight";

            case 9:
                return "nine";

            case 10:
                return "ten";

            case 11:
                return "eleven";

            case 12:
                return "twelve";

            case 13:
                return "thirteen";

            case 14:
                return "fourteen";

            case 15:
                return "fifteen";

            case 16:
                return "sixteen";

            case 17:
                return "seventeen";

            case 18:
                return "eighteen";

            case 19:
                return "nineteen";

            case 20:
                return "twenty";

            default:
                return number.ToString();
        }
    }


    // =========================================================
    // RESULT TEXT
    // =========================================================

    private void SetResult(
        string message
    )
    {
        if (result != null)
        {
            result.text =
                message;
        }
    }


    // =========================================================
    // REQUIRED PLUGIN CALLBACKS
    // =========================================================

    public void OnReadyForSpeech()
    {
        Debug.Log(
            "[SpeechGame] READY FOR SPEECH"
        );

        SetResult(
            "Listening... Say the number"
        );
    }


    public void OnBeginningOfSpeech()
    {
        Debug.Log(
            "[SpeechGame] SPEECH STARTED"
        );

        SetResult(
            "Listening..."
        );
    }


    public void OnVoiceLevelChanged(
        float level
    )
    {
        // Required by plugin.
    }


    // =========================================================
    // EDITOR TEST
    // =========================================================

    public void TestSpokenAnswer()
    {
#if UNITY_EDITOR

        if (string.IsNullOrWhiteSpace(testTranscript))
        {
            Debug.LogWarning(
                "[SpeechGame] Test transcript is empty."
            );

            return;
        }


        Debug.Log(
            "[SpeechGame] EDITOR TEST = " +
            testTranscript
        );


        CheckAnswer(
            testTranscript
                .ToLower()
                .Trim()
        );

#endif
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDisable()
    {
        microphoneHeld = false;


        bool shouldCancel =
            awaitingResult ||
            isListening;


        awaitingResult = false;
        isListening = false;


#if !UNITY_EDITOR

        if (shouldCancel &&
            SpeechToText.IsBusy())
        {
            SpeechToText.Cancel();
        }

#endif
    }
}


// =============================================================
// CUSTOM INSPECTOR FOR EDITOR TEST BUTTON
// =============================================================

#if UNITY_EDITOR

[CustomEditor(typeof(SpeechGameManager))]
public class SpeechGameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


        SpeechGameManager manager =
            (SpeechGameManager)target;


        EditorGUILayout.Space(10);


        EditorGUILayout.HelpBox(
            "Android uses microphone recognition. " +
            "In Unity Editor, type a transcript below " +
            "and press Test Spoken Answer.",
            MessageType.Info
        );


        manager.testTranscript =
            EditorGUILayout.TextField(
                "Test Transcript",
                manager.testTranscript
            );


        if (GUILayout.Button(
            "Test Spoken Answer"
        ))
        {
            if (Application.isPlaying)
            {
                manager.TestSpokenAnswer();
            }
            else
            {
                Debug.LogWarning(
                    "Enter Play Mode before testing the spoken answer."
                );
            }
        }


        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }
}

#endif