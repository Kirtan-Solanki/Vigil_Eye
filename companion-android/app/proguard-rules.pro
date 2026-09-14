# ProGuard rules for Vigileye Companion (Phase 1 scaffold)
# Preserves Room entities, Retrofit interfaces, SQLCipher, Kotlin coroutines
-keep class com.vigileye.companion.data.local.* { *; }
-keepclassmembers class com.vigileye.companion.data.local.* { *; }
-keep class net.zetetic.** { *; }
-keep class androidx.work.** { *; }
-keep class retrofit2.** { *; }
-keep class okhttp3.** { *; }
-keepclassmembers class * {
    @android.webkit.JavascriptInterface <methods>;
}
-dontwarn okio.**
-dontwarn javax.annotation.**
