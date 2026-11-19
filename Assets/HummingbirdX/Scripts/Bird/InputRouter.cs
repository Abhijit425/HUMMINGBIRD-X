using UnityEngine;

namespace HummingbirdX
{
    /// <summary>
    /// Collects player intent from the touch UI (joysticks, buttons, gestures) and, for editor
    /// testing, keyboard/mouse. Everything that moves the bird or camera reads from here.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter I { get; private set; }

        // continuous
        public Vector2 Move;        // left stick
        public Vector2 Look;        // right stick + swipe (deg/s style units)
        public float Vertical;      // up/down buttons
        public bool BoostHeld;
        public float Zoom;          // pinch delta (positive = zoom in)

        // one-shot events (consumed by readers)
        public bool HoverPressed, LandPressed, BoostTapped, CameraCyclePressed;
        public Vector2? TapScreen;

        // UI writes
        public Vector2 StickMove, StickLook;
        public bool UpHeld, DownHeld, UiBoostHeld;
        public Vector2 SwipeLook;
        public float PinchZoom;

        float boostTapTimer;

        void Awake() { I = this; }

        void Update()
        {
            Move = StickMove;
            Look = StickLook * 140f + SwipeLook;
            Vertical = (UpHeld ? 1f : 0f) - (DownHeld ? 1f : 0f);
            BoostHeld = UiBoostHeld || boostTapTimer > 0f;
            Zoom = PinchZoom;
            boostTapTimer -= Time.unscaledDeltaTime;

#if UNITY_EDITOR || UNITY_STANDALONE
            var kb = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                                 (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            if (kb.sqrMagnitude > 0) Move = kb.normalized;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) Vertical = 1f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q)) Vertical = -1f;
            if (Input.GetKey(KeyCode.LeftShift)) BoostHeld = true;
            if (Input.GetKeyDown(KeyCode.H)) HoverPressed = true;
            if (Input.GetKeyDown(KeyCode.L)) LandPressed = true;
            if (Input.GetKeyDown(KeyCode.C)) CameraCyclePressed = true;
            if (Input.GetMouseButton(1))
                Look += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 600f;
            Zoom += Input.mouseScrollDelta.y * 0.2f;
#endif
        }

        public void DoubleTapBoost()
        {
            boostTapTimer = 1.4f;
            BoostTapped = true;
        }

        public bool Consume(ref bool flag)
        {
            bool v = flag;
            flag = false;
            return v;
        }

        public void ClearAll()
        {
            StickMove = StickLook = SwipeLook = Vector2.zero;
            UpHeld = DownHeld = UiBoostHeld = false;
            HoverPressed = LandPressed = BoostTapped = CameraCyclePressed = false;
            TapScreen = null;
        }
    }
}
