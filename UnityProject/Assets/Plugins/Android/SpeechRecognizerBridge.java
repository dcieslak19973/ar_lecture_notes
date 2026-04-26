package com.arlecture.speech;

import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;

/**
 * Wraps Android SpeechRecognizer with continuous listening.
 * Automatically restarts after each utterance so recording stays live.
 */
public class SpeechRecognizerBridge {

    private SpeechRecognizer recognizer;
    private final ISpeechCallback callback;
    private volatile boolean listening = false;

    public SpeechRecognizerBridge(ISpeechCallback callback) {
        this.callback = callback;
    }

    public void startListening() {
        listening = true;
        UnityPlayer.currentActivity.runOnUiThread(this::createAndStart);
    }

    public void stopListening() {
        listening = false;
        UnityPlayer.currentActivity.runOnUiThread(() -> {
            if (recognizer != null) {
                recognizer.stopListening();
                recognizer.destroy();
                recognizer = null;
            }
        });
    }

    private void createAndStart() {
        if (!listening) return;

        Context ctx = UnityPlayer.currentActivity;
        if (recognizer != null) {
            recognizer.destroy();
        }
        recognizer = SpeechRecognizer.createSpeechRecognizer(ctx);
        recognizer.setRecognitionListener(new RecognitionListener() {
            @Override public void onReadyForSpeech(Bundle params) {}
            @Override public void onBeginningOfSpeech() {}
            @Override public void onRmsChanged(float rmsdB) {}
            @Override public void onBufferReceived(byte[] buffer) {}
            @Override public void onEndOfSpeech() {}

            @Override
            public void onError(int error) {
                callback.onError(error);
                // Restart on no-match or timeout to keep listening continuously
                if (listening && (error == SpeechRecognizer.ERROR_NO_MATCH
                        || error == SpeechRecognizer.ERROR_SPEECH_TIMEOUT
                        || error == SpeechRecognizer.ERROR_RECOGNIZER_BUSY)) {
                    createAndStart();
                }
            }

            @Override
            public void onResults(Bundle results) {
                ArrayList<String> matches =
                        results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
                float[] scores =
                        results.getFloatArray(SpeechRecognizer.CONFIDENCE_SCORES);
                if (matches != null && !matches.isEmpty()) {
                    float confidence = (scores != null && scores.length > 0) ? scores[0] : 1.0f;
                    callback.onFinalResult(matches.get(0), confidence);
                }
                // Restart to keep recording continuously
                if (listening) createAndStart();
            }

            @Override
            public void onPartialResults(Bundle partialResults) {
                ArrayList<String> partial =
                        partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
                if (partial != null && !partial.isEmpty()) {
                    callback.onPartialResult(partial.get(0));
                }
            }

            @Override public void onEvent(int eventType, Bundle params) {}
        });

        Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,
                RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
        intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
        recognizer.startListening(intent);
    }
}
