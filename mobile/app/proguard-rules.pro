# R8 rules for Tannous POS.
#
# Read this before adding a rule: a keep rule that is not needed is invisible, and a keep rule that
# is missing shows up as a crash on a screen that works perfectly in debug. Both directions are
# expensive, so every block below says why it is here.

# ---------------------------------------------------------------------------
# App code
# ---------------------------------------------------------------------------
# Deliberately broad. Room entities, kotlinx.serialization DTOs, Hilt-generated components and
# WorkManager workers all live under this package and are all reached reflectively or by generated
# code. Keeping the lot means R8 shrinks libraries but leaves our own classes alone, which is the
# trade we want while the app has never once shipped a release build.
#
# The cost is real: app code is neither shrunk nor obfuscated. Worth revisiting only after a
# release build has run a service without incident, and only by narrowing this rule one package at
# a time with a test after each.
-keep class com.tannous.pos.** { *; }

# ---------------------------------------------------------------------------
# Readable stack traces
# ---------------------------------------------------------------------------
# FileLogTree writes Log.getStackTraceString output to a file on the tablet, and that file is the
# only diagnostic this app produces in production. Without SourceFile and LineNumberTable every
# trace in it loses its line numbers.
#
# These two lines used to sit under a "Firebase Crashlytics" heading. Firebase went in Step 128;
# they are not Firebase rules and must not go with it.
-keepattributes SourceFile,LineNumberTable

# Exception class names stay readable in that log file. keepnames rather than keep, so an exception
# type nothing throws can still be shrunk out.
-keepnames class * extends java.lang.Exception

# ---------------------------------------------------------------------------
# Retrofit + coroutines
# ---------------------------------------------------------------------------
# Retrofit 2.9.0 ships no R8 rules of its own (those arrived in later versions), and AGP 8 turns on
# R8 full mode, which erases generic signatures more aggressively. Suspend functions returning
# Response<T> break without these: the return type is carried in the signature, and Retrofit reads
# it at runtime to pick a converter.
-keepattributes Signature, InnerClasses, EnclosingMethod
-keepattributes RuntimeVisibleAnnotations, RuntimeVisibleParameterAnnotations
-keepclasseswithmembers class * {
    @retrofit2.http.* <methods>;
}
-keep,allowobfuscation,allowshrinking interface retrofit2.Call
-keep,allowobfuscation,allowshrinking class retrofit2.Response
-keep,allowobfuscation,allowshrinking class kotlin.coroutines.Continuation
-dontwarn retrofit2.**

# OkHttp names optional platform integrations it does not ship.
-dontwarn okhttp3.**
-dontwarn okio.**
-dontwarn javax.annotation.**
-dontwarn org.conscrypt.**
-dontwarn org.bouncycastle.**
-dontwarn org.openjsse.**

# ---------------------------------------------------------------------------
# kotlinx.serialization
# ---------------------------------------------------------------------------
# The generated $$serializer classes and the Companion.serializer() entry points are found by name.
-keepattributes *Annotation*
-keep,includedescriptorclasses class com.tannous.pos.**$$serializer { *; }
-keepclassmembers class com.tannous.pos.** {
    *** Companion;
}
-keepclasseswithmembers class com.tannous.pos.** {
    kotlinx.serialization.KSerializer serializer(...);
}

# ---------------------------------------------------------------------------
# Room
# ---------------------------------------------------------------------------
-keep class * extends androidx.room.RoomDatabase
-keep @androidx.room.Entity class *
-dontwarn androidx.room.paging.**

# ---------------------------------------------------------------------------
# Hilt / WorkManager
# ---------------------------------------------------------------------------
-keep,allowobfuscation @dagger.hilt.android.lifecycle.HiltViewModel class * extends androidx.lifecycle.ViewModel
-keep class * extends androidx.work.ListenableWorker

# ---------------------------------------------------------------------------
# AndroidX libraries reached reflectively
# ---------------------------------------------------------------------------
# Broad, and the main reason minification saves less than it looks like it should. Narrowing these
# is the first place to look if APK size ever matters; it does not while the app is installed by
# hand on tablets we own.
-keep class androidx.compose.** { *; }
-keep class androidx.datastore.** { *; }
-keep class androidx.paging.** { *; }
-keepnames class * extends android.os.Parcelable
-keepnames class * extends java.io.Serializable

# ---------------------------------------------------------------------------
# Third party
# ---------------------------------------------------------------------------
# ZXing, used by feature/settings for QR generation.
-keep class com.google.zxing.** { *; }
-dontwarn com.google.zxing.**

# DantSu ESCPOS thermal printer library. No rules of its own, and printing is the one path with no
# fallback if it breaks in the field.
-keep class com.dantsu.escposprinter.** { *; }
-dontwarn com.dantsu.escposprinter.**

-keep class timber.log.** { *; }

# ---------------------------------------------------------------------------
# Standard shapes
# ---------------------------------------------------------------------------
-keepclasseswithmembernames class * {
    native <methods>;
}
-keepclassmembers enum * {
    public static **[] values();
    public static ** valueOf(java.lang.String);
}
-keep class * implements android.os.Parcelable {
    public static final android.os.Parcelable$Creator *;
}
-keepclassmembers class * implements java.io.Serializable {
    static final long serialVersionUID;
    private static final java.io.ObjectStreamField[] serialPersistentFields;
    private void writeObject(java.io.ObjectOutputStream);
    private void readObject(java.io.ObjectInputStream);
    java.lang.Object writeReplace();
    java.lang.Object readResolve();
}

-allowaccessmodification

# ---------------------------------------------------------------------------
# Deliberately absent
# ---------------------------------------------------------------------------
# Firebase Crashlytics and Analytics rules: the SDKs were removed in Step 128.
#
# -keep rules for android.app.**, android.content.**, android.view.**, android.widget.**,
# android.bluetooth.** and java.net.**: these are framework and JDK classes, supplied by the
# platform and never part of the APK. R8 never touched them, so the rules did nothing except look
# like protection.
#
# Gson rules: the converter-gson dependency was removed alongside them. Retrofit is wired to
# kotlinx.serialization in NetworkModule and nothing in this app has ever called Gson.
#
# -optimizations and -optimizationpasses: ProGuard directives that R8 ignores outright. Five
# optimisation passes were never happening.
#
# -assumenosideeffects on android.util.Log d/v: Timber routes through Log.println, so this stripped
# nothing that the app actually calls, and it made the release logging path harder to reason about.
