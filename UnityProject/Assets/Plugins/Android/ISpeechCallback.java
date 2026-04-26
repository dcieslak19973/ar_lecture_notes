package com.arlecture.speech;

/**
 * Callback interface bridged to C# via AndroidJavaProxy.
 */
public interface ISpeechCallback {
    void onPartialResult(String text);
    void onFinalResult(String text, float confidence);
    void onError(int errorCode);
}
