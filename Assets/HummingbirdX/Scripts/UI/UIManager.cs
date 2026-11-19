using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HummingbirdX
{
    /// <summary>
    /// Root of the interface: canvas, loading screen, toasts, discovery banners and the
    /// screens (menu, HUD, pause, settings, map, journal, photo mode, flight school).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }
        public RectTransform Root { get; private set; }
        public Canvas Canvas { get; private set; }

        public LoadingScreen Loading;
        public MainMenuScreen Menu;
        public HudScreen Hud;
        public PauseScreen Pause;
        public SettingsScreen SettingsPanel;
        public MapScreen Map;
        public JournalScreen Journal;
        public PhotoScreen Photo;
        public FlightSchoolOverlay School;

        RectTransform toastRoot, bannerRoot;
        readonly Queue<string> toasts = new Queue<string>();
        bool toastBusy;
        Text fpsText;

        void Awake()
        {
            I = this;
            var cgo = new GameObject("UI");
            cgo.transform.SetParent(transform, false);
            Canvas = cgo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.6f;
            cgo.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)cgo.transform;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // safe-area container so controls avoid notches / rounded corners
            var safe = UIKit.Rect("Safe", Root).Stretch();
            safe.gameObject.AddComponent<SafeArea>();
            Root = safe;

            Loading = new LoadingScreen(Root);
        }

        public void BuildScreens()
        {
            Hud = new HudScreen(Root);
            Menu = new MainMenuScreen(Root);
            Pause = new PauseScreen(Root);
            SettingsPanel = new SettingsScreen(Root);
            Map = new MapScreen(Root);
            Journal = new JournalScreen(Root);
            Photo = new PhotoScreen(Root);
            School = new FlightSchoolOverlay(Root);

            bannerRoot = UIKit.Rect("Banners", Root).Stretch();
            toastRoot = UIKit.Rect("Toasts", Root).Stretch();
            fpsText = UIKit.Label(Root, "", 22, UIKit.TextDim, TextAnchor.UpperLeft);
            fpsText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -6), new Vector2(240, 30));
        }

        public void HideAll()
        {
            Menu.Show(false); Hud.Show(false); Pause.Show(false); SettingsPanel.Show(false);
            Map.Show(false); Journal.Show(false); Photo.Show(false);
        }

        public void Poke() { Hud?.Poke(); }

        void Update()
        {
            if (fpsText)
            {
                bool show = Settings.ShowFps && QualityManager.I;
                fpsText.text = show ? $"{QualityManager.I.Fps:0} fps · {QualityManager.I.Active}" : "";
            }
            Hud?.Tick();
            Map?.Tick();
            School?.Tick();
            Menu?.Tick();
        }

        // ------------------------------------------------------------------ toasts & banners
        public static void Toast(string msg)
        {
            if (I == null || I.toastRoot == null) { Debug.Log("[HBX] " + msg); return; }
            I.toasts.Enqueue(msg);
            if (!I.toastBusy) I.StartCoroutine(I.ToastLoop());
        }

        IEnumerator ToastLoop()
        {
            toastBusy = true;
            while (toasts.Count > 0)
            {
                string msg = toasts.Dequeue();
                var p = UIKit.Panel(toastRoot, "Toast");
                var rt = p.rectTransform;
                var l = UIKit.Label(rt, msg, 26, UIKit.TextColor, TextAnchor.MiddleCenter, Res.SemiBold);
                l.rectTransform.Stretch(24, 0, 24, 0);
                float w = Mathf.Min(1100f, l.preferredWidth + 70f);
                rt.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(w, 62));
                var g = UIKit.Group(p.gameObject);
                g.alpha = 0; g.blocksRaycasts = false;
                yield return UIKit.Fade(g, 1f, 0.25f);
                g.blocksRaycasts = false;
                yield return new WaitForSecondsRealtime(2.1f);
                yield return UIKit.Fade(g, 0f, 0.35f);
                Destroy(p.gameObject);
            }
            toastBusy = false;
        }

        public void Discovery(string kind, string title, string subtitle)
        {
            StartCoroutine(DiscoveryRoutine(kind, title, subtitle));
        }

        IEnumerator DiscoveryRoutine(string kind, string title, string subtitle)
        {
            var p = UIKit.Panel(bannerRoot, "Discovery");
            var rt = p.rectTransform;
            rt.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(760, 150));
            var k = UIKit.Label(rt, kind.ToUpperInvariant(), 22, UIKit.Accent, TextAnchor.UpperCenter, Res.SemiBold);
            k.rectTransform.Stretch(20, 0, 20, 18);
            var t = UIKit.Label(rt, title, 44, UIKit.TextColor, TextAnchor.MiddleCenter, Res.Light);
            t.rectTransform.Stretch(20, 20, 20, 30);
            var s = UIKit.Label(rt, subtitle, 22, UIKit.TextDim, TextAnchor.LowerCenter);
            s.rectTransform.Stretch(20, 14, 20, 0);
            var g = UIKit.Group(p.gameObject);
            g.alpha = 0; g.blocksRaycasts = false;
            rt.localScale = Vector3.one * 0.92f;
            float e = 0;
            while (e < 0.35f) { e += Time.unscaledDeltaTime; g.alpha = e / 0.35f; rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e / 0.35f); yield return null; }
            g.alpha = 1f;
            yield return new WaitForSecondsRealtime(2.8f);
            yield return UIKit.Fade(g, 0f, 0.5f);
            Destroy(p.gameObject);
        }

        public void RegionBanner(string name, string sub)
        {
            StartCoroutine(RegionRoutine(name, sub));
        }

        IEnumerator RegionRoutine(string name, string sub)
        {
            var rt = UIKit.Rect("Region", bannerRoot);
            rt.Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 160));
            var t = UIKit.Label(rt, name, 64, UIKit.TextColor, TextAnchor.MiddleCenter, Res.Light);
            t.rectTransform.Stretch(0, 30, 0, 0);
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.5f); sh.effectDistance = new Vector2(2, -2);
            var s = UIKit.Label(rt, sub, 26, UIKit.TextDim, TextAnchor.LowerCenter, Res.SemiBold);
            s.rectTransform.Stretch(0, 0, 0, 110);
            var g = UIKit.Group(rt.gameObject);
            g.alpha = 0; g.blocksRaycasts = false;
            yield return UIKit.Fade(g, 1f, 1.0f);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return UIKit.Fade(g, 0f, 1.2f);
            Destroy(rt.gameObject);
        }
    }

    /// <summary>Keeps UI inside the device safe area.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == last || Screen.width == 0) return;
            last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Base for full screens with fade in/out.</summary>
    public abstract class Screen2D
    {
        public RectTransform rt;
        public CanvasGroup group;
        Coroutine fading;

        protected Screen2D(Transform parent, string name, bool fullBlock)
        {
            rt = UIKit.Rect(name, parent).Stretch();
            group = UIKit.Group(rt.gameObject);
            if (fullBlock)
            {
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.001f);
            }
            rt.gameObject.SetActive(false);
            group.alpha = 0;
        }

        public bool Visible => rt.gameObject.activeSelf;

        public virtual void Show(bool on)
        {
            if (on == Visible && (!on || group.alpha > 0.99f)) return;
            var ui = UIManager.I;
            if (fading != null) ui.StopCoroutine(fading);
            if (on) { rt.gameObject.SetActive(true); OnShow(); }
            fading = ui.StartCoroutine(UIKit.Fade(group, on ? 1f : 0f, 0.25f, true));
        }

        protected virtual void OnShow() { }
    }

    // ================================================================== loading
    public class LoadingScreen
    {
        readonly RectTransform rt;
        readonly Image bar;
        readonly Text label;
        readonly CanvasGroup g;

        public LoadingScreen(Transform parent)
        {
            rt = UIKit.Rect("Loading", parent.parent).Stretch();
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.015f, 0.035f, 0.03f, 1f);
            g = UIKit.Group(rt.gameObject);
            var logo = UIKit.Icon(rt, "ui_logo", 210, UIKit.Accent);
            logo.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(210, 210));
            var title = UIKit.Label(rt, "HUMMINGBIRD X", 72, UIKit.TextColor, TextAnchor.MiddleCenter, Res.Light);
            title.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1200, 100));
            var sub = UIKit.Label(rt, "LIVING RAINFOREST", 26, UIKit.Accent, TextAnchor.MiddleCenter, Res.SemiBold);
            sub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -78), new Vector2(800, 40));
            var track = UIKit.Panel(rt, "Track", new Color(1, 1, 1, 0.1f), false);
            track.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(560, 8));
            bar = UIKit.Panel(track.rectTransform, "Bar", UIKit.Accent, false);
            bar.rectTransform.anchorMin = Vector2.zero; bar.rectTransform.anchorMax = new Vector2(0, 1);
            bar.rectTransform.pivot = new Vector2(0, 0.5f);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
            label = UIKit.Label(rt, "", 24, UIKit.TextDim, TextAnchor.MiddleCenter);
            label.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -215), new Vector2(900, 40));
            var tip = UIKit.Label(rt, "Tip: a hummingbird can fly backwards. Pull the stick back while hovering.", 22, UIKit.TextDim, TextAnchor.MiddleCenter);
            tip.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1400, 40));
        }

        public void Set(float p, string text)
        {
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p), 1);
            label.text = text;
        }

        public IEnumerator Hide()
        {
            yield return UIKit.Fade(g, 0f, 0.8f, true);
        }
    }

    // ================================================================== main menu
    public class MainMenuScreen : Screen2D
    {
        readonly Text stats;

        public MainMenuScreen(Transform parent) : base(parent, "MainMenu", false)
        {
            // soft gradient on the left so text reads over the live forest
            var shade = UIKit.Rect("Shade", rt);
            shade.anchorMin = Vector2.zero; shade.anchorMax = new Vector2(0.55f, 1f);
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            var si = shade.gameObject.AddComponent<Image>();
            si.sprite = Res.Sprite("ui_softdot");
            si.color = new Color(0, 0.02f, 0.015f, 0.65f);
            si.raycastTarget = false;
            shade.localScale = new Vector3(1.6f, 1.4f, 1);

            var logo = UIKit.Icon(rt, "ui_logo", 120, UIKit.Accent);
            logo.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -90), new Vector2(120, 120));
            var title = UIKit.Label(rt, "HUMMINGBIRD X", 76, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -220), new Vector2(1000, 100));
            var sub = UIKit.Label(rt, "LIVING RAINFOREST", 26, UIKit.Accent, TextAnchor.UpperLeft, Res.SemiBold);
            sub.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(114, -315), new Vector2(600, 40));

            var col = UIKit.Rect("Buttons", rt);
            col.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -390), new Vector2(520, 620));
            UIKit.VStack(col, 16, TextAnchor.UpperLeft);
            Item(col, "EXPLORE", "Fly freely through the rainforest", () => GameManager.I.StartExplore());
            Item(col, "FLIGHT SCHOOL", "Learn to hover, feed and land", () => GameManager.I.StartFlightSchool());
            Item(col, "RAINFOREST MAP", "Regions and fast travel", () => UIManager.I.Map.Show(true));
            Item(col, "WILDLIFE JOURNAL", "Your discoveries and photos", () => UIManager.I.Journal.Show(true));
            Item(col, "PHOTO MODE", "Compose nature photographs", () => { GameManager.I.StartExplore(); GameManager.I.EnterPhotoMode(); });
            Item(col, "SETTINGS", "Graphics, sound and controls", () => UIManager.I.SettingsPanel.Show(true));

            var relax = UIKit.TextButton(rt, "Relaxation mode", () => GameManager.I.StartRelaxation(), 24);
            ((RectTransform)relax.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 60), new Vector2(300, 64));

            stats = UIKit.Label(rt, "", 22, UIKit.TextDim, TextAnchor.LowerRight);
            stats.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 50), new Vector2(900, 80));
        }

        static void Item(RectTransform parent, string title, string sub, System.Action a)
        {
            var b = UIKit.Button(parent, title, a, new Color(0.04f, 0.07f, 0.06f, 0.5f));
            var r = (RectTransform)b.transform;
            r.sizeDelta = new Vector2(520, 86);
            var t = UIKit.Label(r, title, 30, UIKit.TextColor, TextAnchor.UpperLeft, Res.SemiBold);
            t.rectTransform.Stretch(30, 0, 20, 14);
            var s = UIKit.Label(r, sub, 20, UIKit.TextDim, TextAnchor.LowerLeft);
            s.rectTransform.Stretch(30, 12, 20, 0);
        }

        public void Tick()
        {
            if (!Visible || GameManager.I == null) return;
            var s = GameManager.I.Save;
            int total = JournalDb.TotalEntries();
            stats.text = $"{s.nectar:0} nectar collected  ·  {s.discovered.Count}/{total} discoveries  ·  {s.distanceFlown / 1000f:0.0} km flown";
        }
    }
}
