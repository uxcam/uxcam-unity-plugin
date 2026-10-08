package com.uxcam.unity;

import android.app.Activity;
import android.graphics.Rect;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.view.View;

import androidx.annotation.Keep;

import com.uxcam.OnVerificationListener;
import com.uxcam.UXCam;
import com.uxcam.datamodel.UXConfig;
import com.uxcam.screenshot.model.UXCamBlur;
import com.uxcam.screenshot.model.UXCamOccludeAllTextFields;
import com.uxcam.screenshot.model.UXCamOcclusion;
import com.uxcam.screenshot.model.UXCamOverlay;
import com.uxcam.screenshot.screenshotTaker.OcclusionReadyCallback;
import com.uxcam.screenshot.screenshotTaker.OcclusionRectRequestListener;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import org.json.JSONTokener;

import java.lang.ref.WeakReference;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

// Calls arrive on the Unity thread, not the UI thread. Start runs on the UI thread, and calls made while it is
// pending queue behind it to keep their order.
@Keep
public final class UXCamUnityBridge {

    private static final String TAG = "UXCamUnity";
    private static final Handler MAIN = new Handler(Looper.getMainLooper());

    private static volatile boolean sStartPending;
    private static volatile WeakReference<Activity> sActivity = new WeakReference<>(null);

    private static volatile float[] sOcclusionRects = new float[0];
    private static boolean sOcclusionListenerAttached;

    @Keep
    public interface VerificationCallback {
        void onResult(boolean verified, String error);
    }

    private UXCamUnityBridge() {
    }

    @Keep
    public static void startWithConfiguration(final Activity activity, final String configJson, final String pluginType,
                                              final String pluginVersion, final VerificationCallback callback) {
        sActivity = new WeakReference<>(activity);
        sStartPending = true;
        MAIN.post(new Runnable() {
            @Override
            public void run() {
                try {
                    JSONObject json = new JSONObject(configJson);
                    UXConfig config = buildConfig(json);
                    // Must precede start so the session is attributed to the Unity plugin.
                    UXCam.pluginType(pluginType, pluginVersion);
                    // Before start, so opting in doesn't restart a running session.
                    if (json.optBoolean("optIntoVideoRecording", false)) UXCam.optIntoVideoRecording();
                    if (callback != null) UXCam.addVerificationListener(new OneShotVerificationListener(callback));
                    UXCam.startWithConfigurationCrossPlatform(activity, config);
                } catch (Throwable t) {
                    Log.e(TAG, "startWithConfiguration failed", t);
                    if (callback != null) callback.onResult(false, String.valueOf(t.getMessage()));
                } finally {
                    sStartPending = false;
                }
            }
        });
    }

    @Keep
    public static void startNewSession() {
        dispatch(new Runnable() { @Override public void run() { UXCam.startNewSession(); } });
    }

    @Keep
    public static void stopSessionAndUploadData() {
        dispatch(new Runnable() { @Override public void run() { UXCam.stopSessionAndUploadData(); } });
    }

    @Keep
    public static void cancelCurrentSession() {
        dispatch(new Runnable() { @Override public void run() { UXCam.cancelCurrentSession(); } });
    }

    @Keep
    public static boolean isRecording() {
        try {
            return UXCam.isRecording();
        } catch (Throwable t) {
            Log.e(TAG, "isRecording failed", t);
            return false;
        }
    }

    @Keep
    public static void pauseScreenRecording() {
        dispatch(new Runnable() { @Override public void run() { UXCam.pauseScreenRecording(); } });
    }

    @Keep
    public static void resumeScreenRecording() {
        dispatch(new Runnable() { @Override public void run() { UXCam.resumeScreenRecording(); } });
    }

    @Keep
    @SuppressWarnings("deprecation")
    public static void allowShortBreakForAnotherApp(final boolean continueSession) {
        dispatch(new Runnable() { @Override public void run() { UXCam.allowShortBreakForAnotherApp(continueSession); } });
    }

    @Keep
    public static void optInOverall() {
        dispatch(new Runnable() { @Override public void run() { UXCam.optInOverall(); } });
    }

    @Keep
    public static void optOutOverall() {
        dispatch(new Runnable() { @Override public void run() { UXCam.optOutOverall(); } });
    }

    @Keep
    public static boolean optInOverallStatus() {
        try {
            return UXCam.optInOverallStatus();
        } catch (Throwable t) {
            Log.e(TAG, "optInOverallStatus failed", t);
            return false;
        }
    }

    @Keep
    public static void optIntoVideoRecording() {
        dispatch(new Runnable() { @Override public void run() { UXCam.optIntoVideoRecording(); } });
    }

    @Keep
    public static void optOutOfVideoRecording() {
        dispatch(new Runnable() { @Override public void run() { UXCam.optOutOfVideoRecording(); } });
    }

    @Keep
    public static boolean optInVideoRecordingStatus() {
        try {
            return UXCam.optInVideoRecordingStatus();
        } catch (Throwable t) {
            Log.e(TAG, "optInVideoRecordingStatus failed", t);
            return false;
        }
    }

    @Keep
    public static void tagScreenName(final String screenName) {
        dispatch(new Runnable() { @Override public void run() { UXCam.tagScreenName(screenName); } });
    }

    @Keep
    public static void addScreenNameToIgnore(final String screenName) {
        dispatch(new Runnable() { @Override public void run() { UXCam.addScreenNameToIgnore(screenName); } });
    }

    @Keep
    public static void removeScreenNameToIgnore(final String screenName) {
        dispatch(new Runnable() { @Override public void run() { UXCam.removeScreenNameToIgnore(screenName); } });
    }

    @Keep
    public static void removeAllScreenNamesToIgnore() {
        dispatch(new Runnable() { @Override public void run() { UXCam.removeAllScreenNamesToIgnore(); } });
    }

    @Keep
    public static void setUserIdentity(final String userIdentity) {
        dispatch(new Runnable() { @Override public void run() { UXCam.setUserIdentity(userIdentity); } });
    }

    @Keep
    public static void setUserProperty(final String key, final String jsonValue) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                Object value = parseScalar(jsonValue);
                if (value instanceof Boolean) UXCam.setUserProperty(key, (boolean) (Boolean) value);
                else if (value instanceof Integer) UXCam.setUserProperty(key, (int) (Integer) value);
                else if (value instanceof Float) UXCam.setUserProperty(key, (float) (Float) value);
                else if (value != null) UXCam.setUserProperty(key, value.toString());
            }
        });
    }

    // The non-boolean overloads are marked @Deprecated in the SDK without a replacement; they remain the only API.
    @Keep
    @SuppressWarnings("deprecation")
    public static void setSessionProperty(final String key, final String jsonValue) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                Object value = parseScalar(jsonValue);
                if (value instanceof Boolean) UXCam.setSessionProperty(key, (boolean) (Boolean) value);
                else if (value instanceof Integer) UXCam.setSessionProperty(key, (int) (Integer) value);
                else if (value instanceof Float) UXCam.setSessionProperty(key, (float) (Float) value);
                else if (value != null) UXCam.setSessionProperty(key, value.toString());
            }
        });
    }

    @Keep
    public static void logEvent(final String eventName, final String propertiesJson) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                Map<String, Object> properties = parseObject(propertiesJson);
                if (properties.isEmpty()) UXCam.logEvent(eventName);
                else UXCam.logEvent(eventName, properties);
            }
        });
    }

    @Keep
    public static void reportBugEvent(final String eventName, final String propertiesJson) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                Map<String, Object> properties = parseObject(propertiesJson);
                if (properties.isEmpty()) UXCam.reportBugEvent(eventName);
                else UXCam.reportBugEvent(eventName, properties);
            }
        });
    }

    @Keep
    public static void reportExceptionEvent(final String name, final String reason, final String framesJson,
                                            final String propertiesJson) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                UnityException exception = new UnityException(reason.isEmpty() ? name : name + ": " + reason);
                exception.setStackTrace(parseFrames(framesJson));
                Map<String, Object> properties = parseObject(propertiesJson);
                if (properties.isEmpty()) UXCam.reportExceptionEvent(exception);
                else UXCam.reportExceptionEvent(exception, properties);
            }
        });
    }

    @Keep
    public static void setPushNotificationToken(final String token) {
        dispatch(new Runnable() { @Override public void run() { UXCam.setPushNotificationToken(token); } });
    }

    @Keep
    public static String urlForCurrentUser() {
        try {
            return UXCam.urlForCurrentUser();
        } catch (Throwable t) {
            Log.e(TAG, "urlForCurrentUser failed", t);
            return null;
        }
    }

    @Keep
    public static String urlForCurrentSession() {
        try {
            return UXCam.urlForCurrentSession();
        } catch (Throwable t) {
            Log.e(TAG, "urlForCurrentSession failed", t);
            return null;
        }
    }

    // Deprecated in favour of a callback because it blocks; it's called on the Unity thread, not the UI thread.
    @Keep
    @SuppressWarnings("deprecation")
    public static int pendingUploads() {
        try {
            return UXCam.pendingUploads();
        } catch (Throwable t) {
            Log.e(TAG, "pendingUploads failed", t);
            return 0;
        }
    }

    @Keep
    public static void deletePendingUploads() {
        dispatch(new Runnable() { @Override public void run() { UXCam.deletePendingUploads(); } });
    }

    @Keep
    public static void occludeSensitiveScreen(final boolean hide, final boolean hideGestures) {
        dispatch(new Runnable() { @Override public void run() { UXCam.occludeSensitiveScreen(hide, hideGestures); } });
    }

    @Keep
    public static void occludeAllTextFields(final boolean occlude) {
        dispatch(new Runnable() { @Override public void run() { UXCam.occludeAllTextFields(occlude); } });
    }

    @Keep
    public static void applyOcclusion(final String occlusionJson) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                UXCamOcclusion occlusion = buildOcclusion(parseJsonObject(occlusionJson));
                if (occlusion != null) UXCam.applyOcclusion(occlusion);
            }
        });
    }

    @Keep
    public static void removeOcclusion(final String occlusionJson) {
        dispatch(new Runnable() {
            @Override
            public void run() {
                // The SDK matches occlusions to remove by type and screens, not by instance.
                UXCamOcclusion occlusion = buildOcclusion(parseJsonObject(occlusionJson));
                if (occlusion != null) UXCam.removeOcclusion(occlusion);
            }
        });
    }

    // The SDK pulls rects on the UI thread after capturing each frame, so they're cached and converted on demand.
    @Keep
    public static void setOcclusionRects(float[] normalizedRects) {
        sOcclusionRects = normalizedRects != null ? normalizedRects : new float[0];
        if (sOcclusionRects.length > 0) {
            MAIN.post(new Runnable() { @Override public void run() { attachOcclusionListener(); } });
        }
    }

    private static void attachOcclusionListener() {
        if (sOcclusionListenerAttached) return;
        try {
            UXCam.getDelegate().setListener(new OcclusionRectRequestListener() {
                @Override
                public void requestOcclusionRects(OcclusionReadyCallback callback) {
                    callback.onRectsReady(currentScreenRects());
                }
            });
            sOcclusionListenerAttached = true;
        } catch (Throwable t) {
            Log.e(TAG, "Could not attach occlusion listener", t);
        }
    }

    static List<Rect> currentScreenRects() {
        float[] rects = sOcclusionRects;
        if (rects.length < 4) return Collections.emptyList();

        Activity activity = sActivity.get();
        View view = activity != null ? activity.findViewById(android.R.id.content) : null;
        if (view == null || view.getWidth() == 0 || view.getHeight() == 0) return Collections.emptyList();

        int[] origin = new int[2];
        view.getLocationOnScreen(origin);
        return toScreenRects(rects, origin[0], origin[1], view.getWidth(), view.getHeight());
    }

    static List<Rect> toScreenRects(float[] rects, int left, int top, int width, int height) {
        List<Rect> out = new ArrayList<>(rects.length / 4);
        for (int i = 0; i + 3 < rects.length; i += 4) {
            int l = left + Math.round(rects[i] * width);
            int t = top + Math.round(rects[i + 1] * height);
            int r = left + Math.round((rects[i] + rects[i + 2]) * width);
            int b = top + Math.round((rects[i + 1] + rects[i + 3]) * height);
            if (r > l && b > t) out.add(new Rect(l, t, r, b));
        }
        return out;
    }

    private static void dispatch(final Runnable call) {
        Runnable safe = new Runnable() {
            @Override
            public void run() {
                try {
                    call.run();
                } catch (Throwable t) {
                    Log.e(TAG, "UXCam call failed", t);
                }
            }
        };
        if (sStartPending) MAIN.post(safe);
        else safe.run();
    }

    @SuppressWarnings("deprecation")
    static UXConfig buildConfig(JSONObject json) throws JSONException {
        UXConfig.Builder builder = new UXConfig.Builder(json.getString("userAppKey"))
                .enableIntegrationLogging(json.optBoolean("enableIntegrationLogging", false))
                .enableMultiSessionRecord(json.optBoolean("enableMultiSessionRecord", true))
                .enableCrashHandling(json.optBoolean("enableCrashHandling", true))
                .enableAutomaticScreenNameTagging(json.optBoolean("enableAutomaticScreenNameTagging", false))
                .enableImprovedScreenCapture(json.optBoolean("enableImprovedScreenCapture", false));
        // enableImprovedScreenCapture is a no-op on Android; enableAdvancedGestureRecognition and
        // enableNetworkLogging are iOS-only.

        JSONArray occlusionsJson = json.optJSONArray("occlusions");
        if (occlusionsJson != null && occlusionsJson.length() > 0) {
            List<UXCamOcclusion> occlusions = new ArrayList<>();
            for (int i = 0; i < occlusionsJson.length(); i++) {
                UXCamOcclusion occlusion = buildOcclusion(occlusionsJson.optJSONObject(i));
                if (occlusion != null) occlusions.add(occlusion);
            }
            if (!occlusions.isEmpty()) builder.occlusions(occlusions);
        }
        return builder.build();
    }

    static UXCamOcclusion buildOcclusion(JSONObject json) {
        if (json == null) return null;
        List<String> screens = toStringList(json.optJSONArray("screens"));
        boolean exclude = json.optBoolean("excludeMentionedScreens", false);
        boolean hideGestures = json.optBoolean("hideGestures", true);

        switch (json.optInt("type")) {
            case 1: {
                UXCamOccludeAllTextFields.Builder builder = new UXCamOccludeAllTextFields.Builder().excludeMentionedScreens(exclude);
                if (!screens.isEmpty()) builder.screens(screens);
                return builder.build();
            }
            case 2: {
                // The overlay colour is fixed by the Android SDK.
                UXCamOverlay.Builder builder = new UXCamOverlay.Builder().excludeMentionedScreens(exclude).withoutGesture(hideGestures);
                if (!screens.isEmpty()) builder.screens(screens);
                return builder.build();
            }
            case 3: {
                UXCamBlur.Builder builder = new UXCamBlur.Builder()
                        .blurRadius(json.optInt("blurRadius", 10))
                        .excludeMentionedScreens(exclude)
                        .withoutGesture(hideGestures);
                if (!screens.isEmpty()) builder.screens(screens);
                return builder.build();
            }
            default:
                Log.w(TAG, "Unknown occlusion type: " + json.opt("type"));
                return null;
        }
    }

    static Object parseScalar(String json) {
        if (json == null || json.isEmpty()) return null;
        try {
            Object value = new JSONTokener(json).nextValue();
            if (value instanceof Boolean || value instanceof String) return value;
            if (value instanceof Integer) return value;
            if (value instanceof Long) {
                long l = (Long) value;
                return l >= Integer.MIN_VALUE && l <= Integer.MAX_VALUE ? (Object) (int) l : (Object) Long.toString(l);
            }
            if (value instanceof Number) return ((Number) value).floatValue();
            return null;
        } catch (JSONException e) {
            Log.w(TAG, "Invalid property value: " + json);
            return null;
        }
    }

    static JSONObject parseJsonObject(String json) {
        if (json == null || json.isEmpty()) return null;
        try {
            return new JSONObject(json);
        } catch (JSONException e) {
            Log.w(TAG, "Invalid JSON object: " + json);
            return null;
        }
    }

    static Map<String, Object> parseObject(String json) {
        JSONObject object = parseJsonObject(json);
        return object == null ? new HashMap<String, Object>() : toMap(object);
    }

    private static Map<String, Object> toMap(JSONObject object) {
        Map<String, Object> map = new HashMap<>();
        Iterator<String> keys = object.keys();
        while (keys.hasNext()) {
            String key = keys.next();
            map.put(key, unwrap(object.opt(key)));
        }
        return map;
    }

    private static Object unwrap(Object value) {
        if (value == null || value == JSONObject.NULL) return null;
        if (value instanceof JSONObject) return toMap((JSONObject) value);
        if (value instanceof JSONArray) {
            JSONArray array = (JSONArray) value;
            List<Object> list = new ArrayList<>(array.length());
            for (int i = 0; i < array.length(); i++) list.add(unwrap(array.opt(i)));
            return list;
        }
        return value;
    }

    private static List<String> toStringList(JSONArray array) {
        List<String> list = new ArrayList<>();
        if (array == null) return list;
        for (int i = 0; i < array.length(); i++) {
            String value = array.optString(i, null);
            if (value != null) list.add(value);
        }
        return list;
    }

    static StackTraceElement[] parseFrames(String json) {
        JSONArray frames;
        try {
            frames = json == null || json.isEmpty() ? new JSONArray() : new JSONArray(json);
        } catch (JSONException e) {
            frames = new JSONArray();
        }

        StackTraceElement[] elements = new StackTraceElement[frames.length()];
        for (int i = 0; i < frames.length(); i++) {
            JSONObject frame = frames.optJSONObject(i);
            String type = frame != null ? frame.optString("class", "") : "";
            String method = frame != null ? frame.optString("method", "") : "";
            String file = frame != null ? frame.optString("file", "") : "";
            int line = frame != null ? frame.optInt("line", -1) : -1;
            elements[i] = new StackTraceElement(type.isEmpty() ? "Unity" : type, method, file.isEmpty() ? null : file, line);
        }
        return elements;
    }

    @Keep
    static final class UnityException extends Exception {
        private static final long serialVersionUID = 1L;

        UnityException(String message) {
            super(message);
        }
    }

    private static final class OneShotVerificationListener implements OnVerificationListener {
        private final VerificationCallback callback;
        private boolean delivered;

        OneShotVerificationListener(VerificationCallback callback) {
            this.callback = callback;
        }

        @Override
        public void onVerificationSuccess() {
            deliver(true, null);
        }

        @Override
        public void onVerificationFailed(String errorMessage) {
            deliver(false, errorMessage);
        }

        private synchronized void deliver(boolean verified, String error) {
            if (delivered) return;
            delivered = true;
            // Unregister so later verifications (for example after startNewSession) don't call back again.
            UXCam.removeVerificationListener(this);
            try {
                callback.onResult(verified, error);
            } catch (Throwable t) {
                Log.e(TAG, "Verification callback failed", t);
            }
        }
    }
}
