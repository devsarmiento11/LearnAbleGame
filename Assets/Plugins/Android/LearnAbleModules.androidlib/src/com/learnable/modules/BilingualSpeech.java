package com.learnable.modules;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import java.util.ArrayList;
import java.util.Arrays;

public final class BilingualSpeech implements RecognitionListener {
    public interface Callback {
        void onTranscript(String transcript, int error);
        void onPartial(String transcript);
    }

    private final Activity activity;
    private final Callback callback;
    private SpeechRecognizer recognizer;

    public BilingualSpeech(Activity activity, Callback callback) {
        this.activity = activity;
        this.callback = callback;
    }

    public void start(String primaryLanguage) {
        activity.runOnUiThread(() -> {
            try {
                recognizer = SpeechRecognizer.createSpeechRecognizer(activity);
                recognizer.setRecognitionListener(this);

                Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,
                        RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, primaryLanguage);
                intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
                ArrayList<String> languages = new ArrayList<>(Arrays.asList("en-US", "fil-PH"));
                intent.putExtra(RecognizerIntent.EXTRA_ENABLE_LANGUAGE_DETECTION, true);
                intent.putStringArrayListExtra(
                        RecognizerIntent.EXTRA_LANGUAGE_DETECTION_ALLOWED_LANGUAGES, languages);
                intent.putExtra(RecognizerIntent.EXTRA_ENABLE_LANGUAGE_SWITCH,
                        RecognizerIntent.LANGUAGE_SWITCH_BALANCED);
                intent.putStringArrayListExtra(
                        RecognizerIntent.EXTRA_LANGUAGE_SWITCH_ALLOWED_LANGUAGES, languages);
                recognizer.startListening(intent);
            } catch (RuntimeException error) {
                destroyRecognizer();
                callback.onTranscript("", SpeechRecognizer.ERROR_CLIENT);
            }
        });
    }

    public void stop() {
        activity.runOnUiThread(() -> {
            if (recognizer != null) {
                recognizer.stopListening();
            }
        });
    }

    public void cancel() {
        activity.runOnUiThread(this::destroyRecognizer);
    }

    private void destroyRecognizer() {
        if (recognizer != null) {
            SpeechRecognizer activeRecognizer = recognizer;
            recognizer = null;
            activeRecognizer.destroy();
        }
    }

    private static String firstTranscript(Bundle results) {
        ArrayList<String> transcripts = results.getStringArrayList(
                SpeechRecognizer.RESULTS_RECOGNITION);
        return transcripts == null || transcripts.isEmpty() ? "" : transcripts.get(0);
    }

    @Override public void onResults(Bundle results) {
        if (recognizer != null) {
            destroyRecognizer();
            callback.onTranscript(firstTranscript(results), 0);
        }
    }

    @Override public void onError(int error) {
        if (recognizer != null) {
            destroyRecognizer();
            callback.onTranscript("", error);
        }
    }

    @Override public void onPartialResults(Bundle results) {
        if (recognizer != null) {
            callback.onPartial(firstTranscript(results));
        }
    }

    @Override public void onReadyForSpeech(Bundle params) {}
    @Override public void onBeginningOfSpeech() {}
    @Override public void onRmsChanged(float rms) {}
    @Override public void onBufferReceived(byte[] buffer) {}
    @Override public void onEndOfSpeech() {}
    @Override public void onEvent(int eventType, Bundle params) {}
}
