using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HummingbirdX
{
    /// <summary>In-flight HUD: joysticks, up/down, boost, hover, land, camera, photo, menu, nectar.</summary>
    public class HudScreen : Screen2D
    {
        readonly CanvasGroup controls;
        readonly Text nectarText, regionText, prompt;
        readonly RectTransform relaxExit;
        float idle, shownNectar;
        Image boostRing;
        public VirtualJoystick MoveStick, LookStick;

        public HudScreen(Transform parent) : base(parent, "HUD", false)
        {
            var c = UIKit.Rect("Controls", rt).Stretch();
            controls = UIKit.Group(c.gameObject);

            // ---- left: floating movement joystick zone
            var zone = UIKit.Rect("MoveZone", c);
            zone.anchorMin = Vector2.zero; zone.anchorMax = new Vector2(0.36f, 0.62f);
            zone.offsetMin = zone.offsetMax = Vector2.zero;
            var zi = zone.gameObject.AddComponent<Image>(); zi.color = new Color(0, 0, 0, 0.001f);
            MoveStick = MakeStick(zone, new Vector2(250, 240), 250, true);
            MoveStick.OnValue = v => { if (InputRouter.I) InputRouter.I.StickMove = v; };

            // ---- right: fixed camera joystick
            var lz = UIKit.Rect("LookZone", c);
            lz.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 50), new Vector2(300, 300));
            var lzi = lz.gameObject.AddComponent<Image>(); lzi.color = new Color(0, 0, 0, 0.001f);
            LookStick = MakeStick(lz, new Vector2(150, 150), 200, false);
            LookStick.OnValue = v => { if (InputRouter.I) InputRouter.I.StickLook = v; };

            // ---- action buttons
            var act = UIKit.Rect("Actions", c).Stretch();
            actions = act.gameObject;
            var boost = Hold(act, "ui_boost", "BOOST", new Vector2(-230, 430), 132, h => { if (InputRouter.I) InputRouter.I.UiBoostHeld = h; });
            boostRing = UIKit.Icon(boost, "ui_ring", 150, UIKit.Accent);
            boostRing.type = Image.Type.Filled; boostRing.fillMethod = Image.FillMethod.Radial360;
            Hold(c, "ui_up", "UP", new Vector2(-440, 300), 104, h => { if (InputRouter.I) InputRouter.I.UpHeld = h; });
            Hold(c, "ui_down", "DOWN", new Vector2(-440, 170), 104, h => { if (InputRouter.I) InputRouter.I.DownHeld = h; });
            var hover = UIKit.IconButton(act, "ui_hover", () => { if (InputRouter.I) InputRouter.I.HoverPressed = true; Poke(); }, 104, "HOVER");
            ((RectTransform)hover.transform).Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-575, 300), new Vector2(104, 104));
            var land = UIKit.IconButton(act, "ui_land", () => { if (InputRouter.I) InputRouter.I.LandPressed = true; Poke(); }, 104, "LAND");
            ((RectTransform)land.transform).Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-575, 170), new Vector2(104, 104));

            // ---- top right
            var top = UIKit.Rect("Top", c).Stretch();
            topRight = top.gameObject;
            var cam = UIKit.IconButton(top, "ui_camera", () => CameraRig.I.CycleMode(), 88);
            ((RectTransform)cam.transform).Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-330, -70), new Vector2(88, 88));
            var photo = UIKit.IconButton(top, "ui_photo", () => GameManager.I.EnterPhotoMode(), 88);
            ((RectTransform)photo.transform).Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-220, -70), new Vector2(88, 88));
            var menu = UIKit.IconButton(top, "ui_menu", () => GameManager.I.Pause(), 88);
            ((RectTransform)menu.transform).Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-110, -70), new Vector2(88, 88));

            // ---- top left status
            var st = UIKit.Rect("Status", rt).Stretch();
            status = st.gameObject;
            var pill = UIKit.Panel(st, "Nectar");
            pill.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -36), new Vector2(230, 66));
            var drop = UIKit.Icon(pill.rectTransform, "ui_drop", 34, new Color(1f, 0.75f, 0.35f));
            drop.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(34, 34));
            nectarText = UIKit.Label(pill.rectTransform, "0", 30, UIKit.TextColor, TextAnchor.MiddleLeft, Res.SemiBold);
            nectarText.rectTransform.Stretch(70, 0, 10, 0);
            regionText = UIKit.Label(st, "", 22, UIKit.TextDim, TextAnchor.UpperLeft, Res.SemiBold);
            regionText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -112), new Vector2(700, 34));

            prompt = UIKit.Label(st, "", 26, UIKit.TextColor, TextAnchor.MiddleCenter, Res.SemiBold);
            prompt.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1000, 50));
            var sh = prompt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f);

            // relaxation exit
            var rx = UIKit.TextButton(rt, "Exit relaxation", () => GameManager.I.StopRelaxation(), 24);
            relaxExit = (RectTransform)rx.transform;
            relaxExit.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(260, 64));
            relaxExit.gameObject.SetActive(false);
        }

        VirtualJoystick MakeStick(RectTransform zone, Vector2 home, float size, bool floating)
        {
            var baseRt = UIKit.Rect("StickBase", zone);
            baseRt.Place(new Vector2(0, 0), new Vector2(0.5f, 0.5f), home, new Vector2(size, size));
            var ring = baseRt.gameObject.AddComponent<Image>();
            ring.sprite = Res.Sprite("ui_ring"); ring.color = new Color(1, 1, 1, 0.35f); ring.raycastTarget = false;
            var fill = UIKit.Icon(baseRt, "ui_circle", size * 0.94f, new Color(0, 0.03f, 0.02f, 0.25f));
            var knob = UIKit.Rect("Knob", baseRt);
            knob.sizeDelta = new Vector2(size * 0.42f, size * 0.42f);
            var ki = knob.gameObject.AddComponent<Image>();
            ki.sprite = Res.Sprite("ui_circle"); ki.color = new Color(0.9f, 1f, 0.95f, 0.75f); ki.raycastTarget = false;
            var js = zone.gameObject.AddComponent<VirtualJoystick>();
            js.floating = floating;
            js.radius = size * 0.42f;
            js.Init(zone, baseRt, knob);
            return js;
        }

        RectTransform Hold(Transform parent, string icon, string caption, Vector2 pos, float size, System.Action<bool> onChange)
        {
            var p = UIKit.Panel(parent, caption);
            var r = p.rectTransform;
            r.Place(new Vector2(1, 0), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var i = UIKit.Icon(r, icon, size * 0.48f);
            i.rectTransform.anchoredPosition = new Vector2(0, size * 0.08f);
            var l = UIKit.Label(r, caption, Mathf.RoundToInt(size * 0.15f), UIKit.TextDim, TextAnchor.MiddleCenter, Res.SemiBold);
            l.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, size * 0.06f), new Vector2(size, size * 0.25f));
            var hb = r.gameObject.AddComponent<HoldButton>();
            hb.OnChange = h => { onChange(h); p.color = h ? new Color(0.3f, 0.5f, 0.42f, 0.75f) : UIKit.Glass; };
            r.gameObject.AddComponent<PressScale>();
            return r;
        }

        readonly GameObject actions, topRight, status;

        /// <summary>Photo mode keeps only the sticks and UP/DOWN for moving the free camera.</summary>
        public void SetPhotoLayout(bool on)
        {
            actions.SetActive(!on);
            topRight.SetActive(!on);
            status.SetActive(!on);
            controls.alpha = 1f;
            idle = 0f;
        }

        public void SetRelax(bool on)
        {
            controls.gameObject.SetActive(!on);
            relaxExit.gameObject.SetActive(on);
        }

        public void Poke() { idle = 0f; }

        public void Tick()
        {
            if (!Visible) return;
            var gm = GameManager.I;
            if (gm == null) return;
            float dt = Time.unscaledDeltaTime;
            if (Input.touchCount > 0 || Input.anyKey) idle = 0f;
            idle += dt;
            float targetA = idle > 5f ? 0.35f : 1f;
            controls.alpha = Mathf.MoveTowards(controls.alpha, targetA, dt * 1.5f);

            shownNectar = Mathf.Lerp(shownNectar, gm.Save.nectar, 1f - Mathf.Exp(-dt * 6f));
            nectarText.text = shownNectar.ToString("0.0");
            var env = EnvironmentSystem.I;
            regionText.text = $"{gm.CurrentRegionName}  ·  {env.TimeLabel()}  ·  {EnvironmentSystem.WeatherNames[(int)env.Weather]}";
            if (boostRing) boostRing.fillAmount = BirdBoostEnergy();

            var b = BirdController.I;
            string p = "";
            if (b != null && !gm.Relaxation)
            {
                if (b.State == BirdState.Feeding) p = "Drinking nectar…  push the stick to back away";
                else if (b.State == BirdState.Perched) p = b.Anim.pose == BirdPose.Preen ? "Preening — push the stick or press UP to take off" : "Perched — push the stick or press UP to take off";
                else if (b.State == BirdState.AutoNav) p = b.NavFlower ? "Flying to flower…" : "Coming in to land…";
                else if (FlowerManager.I && b.Speed01 < 0.4f)
                {
                    var f = FlowerManager.I.Nearest(b.transform.position, 1.2f);
                    if (f != null) p = $"{f.species.name}: hover in front of the bloom to drink, or tap it";
                }
            }
            prompt.text = p;
        }

        static float BirdBoostEnergy()
        {
            // read boost energy through the speed proxy; full ring when not boosting
            var ir = InputRouter.I;
            return ir && ir.BoostHeld ? 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 2f, 1f) : 1f;
        }
    }

    /// <summary>Swipe to look, pinch to zoom, double-tap to boost, tap a flower to fly to it.</summary>
    [DefaultExecutionOrder(-50)]
    public class TouchGestures : MonoBehaviour
    {
        class Finger { public Vector2 start, last; public float t0; public bool moved, ui; }
        readonly Dictionary<int, Finger> fingers = new Dictionary<int, Finger>();
        float lastTapTime; Vector2 lastTapPos;
        float prevPinch = -1f;

        void Update()
        {
            var ir = InputRouter.I;
            if (ir == null) return;
            ir.SwipeLook = Vector2.zero;
            ir.PinchZoom = 0f;
            var gm = GameManager.I;
            bool active = gm != null && (gm.State == GameState.Explore || gm.State == GameState.Photo);
            if (!active) { fingers.Clear(); return; }
            var es = EventSystem.current;
            float scale = 1920f / Mathf.Max(1, Screen.width);

            var free = new List<Finger>();
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                    fingers[t.fingerId] = new Finger { start = t.position, last = t.position, t0 = Time.unscaledTime, ui = es != null && es.IsPointerOverGameObject(t.fingerId) };
                if (!fingers.TryGetValue(t.fingerId, out var f)) continue;
                if (f.ui) { if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) fingers.Remove(t.fingerId); continue; }
                Vector2 delta = t.position - f.last;
                f.last = t.position;
                if ((t.position - f.start).magnitude * scale > 22f) f.moved = true;
                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) free.Add(f);
                if (t.phase == TouchPhase.Moved && Input.touchCount - CountUi() == 1)
                    ir.SwipeLook += delta * scale * 0.22f / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                if (t.phase == TouchPhase.Ended)
                {
                    if (!f.moved && Time.unscaledTime - f.t0 < 0.3f) Tap(t.position);
                    fingers.Remove(t.fingerId);
                }
                else if (t.phase == TouchPhase.Canceled) fingers.Remove(t.fingerId);
            }
            if (free.Count == 2)
            {
                float d = (free[0].last - free[1].last).magnitude;
                if (prevPinch > 0f) ir.PinchZoom = (d - prevPinch) / Screen.height * 2.2f;
                prevPinch = d;
                ir.SwipeLook = Vector2.zero;
            }
            else prevPinch = -1f;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0) && (es == null || !es.IsPointerOverGameObject())) Tap(Input.mousePosition);
#endif
        }

        int CountUi()
        {
            int n = 0;
            foreach (var f in fingers.Values) if (f.ui) n++;
            return n;
        }

        void Tap(Vector2 pos)
        {
            var ir = InputRouter.I;
            UIManager.I?.Poke();
            if (Time.unscaledTime - lastTapTime < 0.32f && (pos - lastTapPos).magnitude < 120f)
            {
                ir.DoubleTapBoost();
                lastTapTime = 0f;
                return;
            }
            lastTapTime = Time.unscaledTime; lastTapPos = pos;
            var gm = GameManager.I;
            if (gm.State != GameState.Explore || Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(pos);
            if (Physics.SphereCast(ray, 0.12f, out var hit, 60f, 1 << FlowerManager.FlowerLayer, QueryTriggerInteraction.Collide))
            {
                var f = hit.collider.GetComponent<Flower>();
                if (f != null && BirdController.I)
                {
                    BirdController.I.NavigateTo(f);
                    UIManager.Toast(f.species.name);
                }
            }
        }
    }
}
