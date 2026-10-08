#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <UXCam/UXCam.h>
#import <UXCam/UXCamPlugin.h>

typedef void (*UXCamUnityVerificationCallback)(int requestId, int verified);

// Rect occlusion pushed from Unity is stored under this identity, so each push replaces the previous set.
static NSString *const kUXCamUnityRectIdentity = @"unity";

static NSString *UXCamUnityString(const char *value)
{
    return value ? [NSString stringWithUTF8String:value] : nil;
}

// Unity frees strings returned to C#, so they must be heap copies.
static char *UXCamUnityCopyString(NSString *value)
{
    return value ? strdup(value.UTF8String) : NULL;
}

static id UXCamUnityJSON(const char *json)
{
    if (json == NULL || json[0] == '\0') return nil;
    NSData *data = [NSData dataWithBytes:json length:strlen(json)];
    NSError *error = nil;
    id value = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingFragmentsAllowed error:&error];
    if (error) NSLog(@"[UXCam Unity] Invalid JSON from Unity: %@", error.localizedDescription);
    return value;
}

static NSDictionary *UXCamUnityDictionary(const char *json)
{
    id value = UXCamUnityJSON(json);
    return [value isKindOfClass:NSDictionary.class] ? value : nil;
}

static BOOL UXCamUnityBool(NSDictionary *json, NSString *key, BOOL fallback)
{
    id value = json[key];
    return [value isKindOfClass:NSNumber.class] ? [value boolValue] : fallback;
}

static NSArray<NSString *> *UXCamUnityStringArray(id value)
{
    if (![value isKindOfClass:NSArray.class]) return @[];
    NSMutableArray<NSString *> *strings = [NSMutableArray array];
    for (id item in (NSArray *)value) {
        if ([item isKindOfClass:NSString.class]) [strings addObject:item];
    }
    return strings;
}

static id<UXCamOcclusionSetting> UXCamUnityOcclusionSetting(NSDictionary *json)
{
    BOOL hideGestures = UXCamUnityBool(json, @"hideGestures", YES);
    switch ([json[@"type"] integerValue]) {
        case UXOcclusionTypeOccludeAllTextFields:
            return [UXCamOccludeAllTextFields new];

        case UXOcclusionTypeOverlay: {
            UIColor *color = UIColor.blackColor;
            NSArray *rgba = json[@"color"];
            if ([rgba isKindOfClass:NSArray.class] && rgba.count >= 3) {
                CGFloat alpha = rgba.count >= 4 ? [rgba[3] doubleValue] / 255.0 : 1.0;
                color = [UIColor colorWithRed:[rgba[0] doubleValue] / 255.0
                                        green:[rgba[1] doubleValue] / 255.0
                                         blue:[rgba[2] doubleValue] / 255.0
                                        alpha:alpha];
            }
            UXCamOverlaySetting *overlay = [[UXCamOverlaySetting alloc] initWithColor:color];
            overlay.hideGestures = hideGestures;
            return overlay;
        }

        case UXOcclusionTypeBlur: {
            id radius = json[@"blurRadius"];
            UXCamBlurSetting *blur = [[UXCamBlurSetting alloc] initWithBlurType:(UXBlurType)[json[@"blurType"] integerValue]
                                                                         radius:[radius isKindOfClass:NSNumber.class] ? [radius intValue] : 10];
            blur.hideGestures = hideGestures;
            return blur;
        }

        default:
            NSLog(@"[UXCam Unity] Unknown occlusion type: %@", json[@"type"]);
            return nil;
    }
}

static UXCamConfiguration *UXCamUnityConfiguration(NSDictionary *json)
{
    UXCamConfiguration *configuration = [[UXCamConfiguration alloc] initWithAppKey:json[@"userAppKey"] ?: @""];
    configuration.enableIntegrationLogging = UXCamUnityBool(json, @"enableIntegrationLogging", configuration.enableIntegrationLogging);
    configuration.enableMultiSessionRecord = UXCamUnityBool(json, @"enableMultiSessionRecord", configuration.enableMultiSessionRecord);
    configuration.enableCrashHandling = UXCamUnityBool(json, @"enableCrashHandling", configuration.enableCrashHandling);
    configuration.enableAutomaticScreenNameTagging = UXCamUnityBool(json, @"enableAutomaticScreenNameTagging", configuration.enableAutomaticScreenNameTagging);
    configuration.enableImprovedScreenCapture = UXCamUnityBool(json, @"enableImprovedScreenCapture", configuration.enableImprovedScreenCapture);
    configuration.enableAdvancedGestureRecognition = UXCamUnityBool(json, @"enableAdvancedGestureRecognition", configuration.enableAdvancedGestureRecognition);
    configuration.enableNetworkLogging = UXCamUnityBool(json, @"enableNetworkLogging", configuration.enableNetworkLogging);

    NSArray *occlusions = json[@"occlusions"];
    if ([occlusions isKindOfClass:NSArray.class] && occlusions.count > 0) {
        UXCamOcclusion *occlusion = [[UXCamOcclusion alloc] init];
        for (NSDictionary *item in occlusions) {
            if (![item isKindOfClass:NSDictionary.class]) continue;
            id<UXCamOcclusionSetting> setting = UXCamUnityOcclusionSetting(item);
            if (setting == nil) continue;
            [occlusion applySettings:@[setting]
                             screens:UXCamUnityStringArray(item[@"screens"])
             excludeMentionedScreens:UXCamUnityBool(item, @"excludeMentionedScreens", NO)];
        }
        configuration.occlusion = occlusion;
    }
    return configuration;
}

// Not UnityGetGLView(): without Unity trampoline headers the bridge also builds in Unity 6.5+'s Swift Xcode project.
static UIView *UXCamUnityRenderView(void)
{
    UIWindow *window = nil;
    id<UIApplicationDelegate> delegate = UIApplication.sharedApplication.delegate;
    if ([delegate respondsToSelector:@selector(window)]) window = delegate.window;

    if (window == nil) {
        if (@available(iOS 13.0, *)) {
            for (UIScene *scene in UIApplication.sharedApplication.connectedScenes) {
                if (![scene isKindOfClass:UIWindowScene.class]) continue;
                for (UIWindow *candidate in ((UIWindowScene *)scene).windows) {
                    if (candidate.isKeyWindow) { window = candidate; break; }
                }
                if (window) break;
            }
        }
    }
    return window.rootViewController.view ?: window;
}

extern "C" {

void _UXCam_StartWithConfiguration(const char *configJson, const char *pluginType, const char *pluginVersion,
                                   int requestId, UXCamUnityVerificationCallback callback)
{
    NSDictionary *json = UXCamUnityDictionary(configJson);
    if (json == nil) {
        if (callback && requestId != 0) callback(requestId, 0);
        return;
    }

    // Must precede start so the session is attributed to the Unity plugin.
    [UXCam pluginType:UXCamUnityString(pluginType) ?: @"unity" version:UXCamUnityString(pluginVersion) ?: @""];

    // iOS defaults to no video. Opting in before start avoids the session restart an opt-in triggers mid-session.
    if (UXCamUnityBool(json, @"optIntoVideoRecording", NO)) [UXCam optIntoVideoRecordings];

    [UXCam startWithConfiguration:UXCamUnityConfiguration(json) completionBlock:^(BOOL started) {
        if (callback == NULL || requestId == 0) return;
        // Call back into C# on the main thread, which is Unity's thread on iOS.
        dispatch_async(dispatch_get_main_queue(), ^{
            callback(requestId, started ? 1 : 0);
        });
    }];
}

void _UXCam_StartNewSession(void) { [UXCam startNewSession]; }
void _UXCam_StopSessionAndUploadData(void) { [UXCam stopSessionAndUploadData]; }
void _UXCam_CancelCurrentSession(void) { [UXCam cancelCurrentSession]; }
bool _UXCam_IsRecording(void) { return UXCam.isRecording; }
void _UXCam_PauseScreenRecording(void) { [UXCam pauseScreenRecording]; }
void _UXCam_ResumeScreenRecording(void) { [UXCam resumeScreenRecording]; }

void _UXCam_AllowShortBreakForAnotherApp(bool continueSession)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    [UXCam allowShortBreakForAnotherApp:continueSession];
#pragma clang diagnostic pop
}

void _UXCam_OptInOverall(void) { [UXCam optInOverall]; }
void _UXCam_OptOutOverall(void) { [UXCam optOutOverall]; }
bool _UXCam_OptInOverallStatus(void) { return UXCam.optInOverallStatus; }
void _UXCam_OptIntoVideoRecording(void) { [UXCam optIntoVideoRecordings]; }
void _UXCam_OptOutOfVideoRecording(void) { [UXCam optOutOfVideoRecordings]; }
bool _UXCam_OptInVideoRecordingStatus(void) { return UXCam.optInVideoRecordingStatus; }

void _UXCam_TagScreenName(const char *screenName, bool isAutomatic)
{
    NSString *name = UXCamUnityString(screenName);
    if (name.length == 0) return;

    if (isAutomatic) {
        // Scene changes go through the plugin handler so the SDK treats them as framework navigation rather
        // than a manual tag.
        static UXCamPlugin *plugin;
        static dispatch_once_t once;
        dispatch_once(&once, ^{ plugin = [[UXCamPlugin alloc] initWithPlugin:@"unity"]; });
        [plugin tagScreenName:name];
    } else {
        [UXCam tagScreenName:name];
    }
}

void _UXCam_AddScreenNameToIgnore(const char *screenName) { [UXCam addScreenNameToIgnore:UXCamUnityString(screenName)]; }
void _UXCam_RemoveScreenNameToIgnore(const char *screenName) { [UXCam removeScreenNameToIgnore:UXCamUnityString(screenName)]; }
void _UXCam_RemoveAllScreenNamesToIgnore(void) { [UXCam removeAllScreenNamesToIgnore]; }

void _UXCam_SetUserIdentity(const char *userIdentity) { [UXCam setUserIdentity:UXCamUnityString(userIdentity)]; }

void _UXCam_SetUserProperty(const char *key, const char *jsonValue)
{
    id value = UXCamUnityJSON(jsonValue);
    if (value == nil || value == NSNull.null) return;
    [UXCam setUserProperty:UXCamUnityString(key) value:value];
}

void _UXCam_SetSessionProperty(const char *key, const char *jsonValue)
{
    id value = UXCamUnityJSON(jsonValue);
    if (value == nil || value == NSNull.null) return;
    [UXCam setSessionProperty:UXCamUnityString(key) value:value];
}

void _UXCam_LogEvent(const char *eventName, const char *propertiesJson)
{
    NSDictionary *properties = UXCamUnityDictionary(propertiesJson);
    if (properties.count > 0)
        [UXCam logEvent:UXCamUnityString(eventName) withProperties:properties];
    else
        [UXCam logEvent:UXCamUnityString(eventName)];
}

void _UXCam_ReportBugEvent(const char *eventName, const char *propertiesJson)
{
    // reportBugEvent:properties: is deprecated on iOS in favour of exception events.
    NSException *bug = [NSException exceptionWithName:UXCamUnityString(eventName) reason:nil userInfo:nil];
    [UXCam reportExceptionEvent:bug properties:UXCamUnityDictionary(propertiesJson)];
}

void _UXCam_ReportExceptionEvent(const char *name, const char *reason, const char *framesJson, const char *propertiesJson)
{
    NSMutableArray<NSString *> *callStacks = [NSMutableArray array];
    id frames = UXCamUnityJSON(framesJson);
    if ([frames isKindOfClass:NSArray.class]) {
        for (NSDictionary *frame in (NSArray *)frames) {
            if (![frame isKindOfClass:NSDictionary.class]) continue;
            NSString *type = frame[@"class"], *method = frame[@"method"], *file = frame[@"file"];
            NSInteger line = [frame[@"line"] integerValue];
            NSString *symbol = type.length > 0 ? [NSString stringWithFormat:@"%@.%@", type, method] : (method ?: @"");
            [callStacks addObject:file.length > 0
                ? [NSString stringWithFormat:@"%@ (%@:%ld)", symbol, file, (long)line]
                : symbol];
        }
    }

    [UXCam reportExceptionEvent:UXCamUnityString(name) ?: @"Exception"
                         reason:UXCamUnityString(reason) ?: @""
                     callStacks:callStacks
                     properties:UXCamUnityDictionary(propertiesJson)];
}

void _UXCam_SetPushNotificationToken(const char *token) { [UXCam setPushNotificationToken:UXCamUnityString(token)]; }
char *_UXCam_UrlForCurrentUser(void) { return UXCamUnityCopyString(UXCam.urlForCurrentUser); }
char *_UXCam_UrlForCurrentSession(void) { return UXCamUnityCopyString(UXCam.urlForCurrentSession); }
int _UXCam_PendingUploads(void) { return (int)UXCam.pendingUploads; }
void _UXCam_DeletePendingUploads(void) { [UXCam deletePendingUploads]; }

void _UXCam_OccludeSensitiveScreen(bool hide, bool hideGestures) { [UXCam occludeSensitiveScreen:hide hideGestures:hideGestures]; }
void _UXCam_OccludeAllTextFields(bool occlude) { [UXCam occludeAllTextFields:occlude]; }

void _UXCam_ApplyOcclusion(const char *occlusionJson)
{
    NSDictionary *json = UXCamUnityDictionary(occlusionJson);
    id<UXCamOcclusionSetting> setting = json ? UXCamUnityOcclusionSetting(json) : nil;
    if (setting == nil) return;
    [UXCam applyOcclusion:setting toScreens:UXCamUnityStringArray(json[@"screens"])];
}

void _UXCam_RemoveOcclusion(const char *occlusionJson)
{
    NSDictionary *json = UXCamUnityDictionary(occlusionJson);
    if (json == nil) return;
    [UXCam removeOcclusionOfType:(UXOcclusionType)[json[@"type"] integerValue]];
}

void _UXCam_SetOcclusionRects(const float *rects, int count)
{
    NSMutableArray<NSArray<NSNumber *> *> *list = [NSMutableArray arrayWithCapacity:(NSUInteger)MAX(count, 1)];
    UIView *view = count > 0 ? UXCamUnityRenderView() : nil;
    if (view != nil) {
        CGRect bounds = view.bounds;
        for (int i = 0; i < count; i++) {
            const float *r = rects + i * 4;
            CGRect local = CGRectMake(bounds.origin.x + r[0] * bounds.size.width,
                                      bounds.origin.y + r[1] * bounds.size.height,
                                      r[2] * bounds.size.width,
                                      r[3] * bounds.size.height);
            // UXCam expects window coordinates in points.
            CGRect inWindow = [view convertRect:local toView:nil];
            [list addObject:@[@(inWindow.origin.x), @(inWindow.origin.y), @(inWindow.size.width), @(inWindow.size.height)]];
        }
    }

    // The SDK ignores an empty array; a single zero-size rect clears this identity instead.
    if (list.count == 0) [list addObject:@[@0, @0, @0, @0]];
    [UXCam occludeRectsOnNextFrame:list withIdentity:kUXCamUnityRectIdentity];
}

}
