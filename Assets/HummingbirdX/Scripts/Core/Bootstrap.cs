using UnityEngine;

namespace HummingbirdX
{
    /// <summary>
    /// The only component placed in the boot scene. Everything else (world, bird, UI, audio)
    /// is generated from code so the project builds without hand-authored scene files.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Input.multiTouchEnabled = true;

            if (FindAnyObjectByType<GameManager>() == null)
            {
                var gm = new GameObject("HummingbirdX");
                DontDestroyOnLoad(gm);
                gm.AddComponent<GameManager>();
            }
        }
    }
}
