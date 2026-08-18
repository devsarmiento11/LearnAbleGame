using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpeechToTextNamespace;

public class SpeechGameManager : MonoBehaviour, ISpeechToTextListener
{
    public Image numberImage;

    public Sprite sixSprite;
    public Sprite sevenSprite;

    public TMP_Text result;

    void Start()
    {
        SpeechToText.Initialize();
    }

    public void StartListening()
    {
        if (!SpeechToText.CheckPermission())
        {
            SpeechToText.RequestPermissionAsync();
            return;
        }

        SpeechToText.Start(this);
    }

    public void OnResultReceived(string spokenText, int? errorCode)
    {
        if (errorCode != null)
        {
            result.text = "Try Again";
            return;
        }

        CheckAnswer(spokenText.ToLower());
    }

    void CheckAnswer(string word)
    {
        Debug.Log(word);

        if (word.Contains("six"))
        {
            numberImage.sprite = sixSprite;
            numberImage.gameObject.SetActive(true);
            result.text = "Correct!";
        }
        else if (word.Contains("seven"))
        {
            numberImage.sprite = sevenSprite;
            numberImage.gameObject.SetActive(true);
            result.text = "Correct!";
        }
        else
        {
            result.text = "Try Again";
        }
    }

    // Required by the plugin
    public void OnReadyForSpeech() { }
    public void OnBeginningOfSpeech() { }
    public void OnVoiceLevelChanged(float level) { }
    public void OnPartialResultReceived(string partialResult) { }
}