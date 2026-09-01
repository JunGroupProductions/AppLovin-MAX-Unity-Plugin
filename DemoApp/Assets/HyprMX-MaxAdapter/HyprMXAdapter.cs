using UnityEngine;
using System.Runtime.InteropServices;
public class HyprMXAdapter
{
    static public void enableTestMode()
    {
#if (UNITY_5 && UNITY_IOS) || UNITY_IPHONE
        HyprMXExterns.HyprMX_Max_enableTestMode();
#elif UNITY_ANDROID
        HyprMXAndroidAdapter.enableTestMode();
#endif
    }
}

#if (UNITY_5 && UNITY_IOS) || UNITY_IPHONE
// Externs used by the iOS component.
internal class HyprMXExterns
{
    [DllImport("__Internal")]
    internal static extern void HyprMX_Max_enableTestMode();
}
#endif

#if UNITY_ANDROID
internal class HyprMXAndroidAdapter {
    public static void enableTestMode()
    {
        try
        {
            new AndroidJavaClass("com.hyprmx.android.HyprMXMaxAdapter").CallStatic("enableTestMode");
        }
        catch (AndroidJavaException e)
        {
            Debug.LogError("HyprMX: Error enabling test mode: " + e.Message);
        }
    }
}
#endif
