using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HummingbirdX
{
    // ================================================================== pause
    public class PauseScreen : Screen2D
    {
        public PauseScreen(Transform parent) : base(parent, "Pause", true)
        {
            var bg = rt.GetComponent<Image>(); bg.color = new Color(0, 0.02f, 0.015f, 0.45f);
            var p = UIKit.Panel(rt, "Panel");
            p.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 760));
            var t = UIKit.Label(p.rectTransform, "Paused", 52, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -40), new Vector2(600, 70));

            var col = UIKit.Rect("Col", p.rectTransform);
            col.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -140), new Vector2(420, 580));
            UIKit.VStack(col, 14, TextAnchor.UpperLeft);
            Btn(col, "Resume", () => GameManager.I.Resume());
            Btn(col, "Rainforest map", () => UIManager.I.Map.Show(true));
            Btn(col, "Wildlife journal", () => UIManager.I.Journal.Show(true));
            Btn(col, "Settings", () => UIManager.I.SettingsPanel.Show(true));
            Btn(col, "Flight School", () => { GameManager.I.Resume(); GameManager.I.StartFlightSchool(); });
            Btn(col, "Main menu", () => GameManager.I.ShowMenu());

            // environment controls
            var env = UIKit.Rect("Env", p.rectTransform);
            env.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -140), new Vector2(600, 580));
            var et = UIKit.Label(env, "TIME OF DAY", 22, UIKit.Accent, TextAnchor.UpperLeft, Res.SemiBold);
            et.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(600, 30));
            var times = UIKit.Rect("Times", env);
            times.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -40), new Vector2(600, 180));
            var tg = times.gameObject.AddComponent<GridLayoutGroup>();
            tg.cellSize = new Vector2(190, 76); tg.spacing = new Vector2(12, 12);
            (string, float)[] tt = { ("Sunrise", 5.9f), ("Morning", 8.5f), ("Afternoon", 14f), ("Golden hour", 16.9f), ("Sunset", 18.0f), ("Night", 22f) };
            foreach (var (n, h) in tt) UIKit.TextButton(times, n, () => EnvironmentSystem.I.SetTime(h), 24);

            var wt = UIKit.Label(env, "WEATHER", 22, UIKit.Accent, TextAnchor.UpperLeft, Res.SemiBold);
            wt.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -250), new Vector2(600, 30));
            var weathers = UIKit.Rect("Weathers", env);
            weathers.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -290), new Vector2(600, 180));
            var wg = weathers.gameObject.AddComponent<GridLayoutGroup>();
            wg.cellSize = new Vector2(190, 76); wg.spacing = new Vector2(12, 12);
            for (int i = 0; i < EnvironmentSystem.WeatherNames.Length; i++)
            {
                var w = (Weather)i;
                UIKit.TextButton(weathers, EnvironmentSystem.WeatherNames[i], () => EnvironmentSystem.I.SetWeather(w), 22);
            }
            var hint = UIKit.Label(env, "Weather also changes on its own (Settings > Dynamic weather).", 20, UIKit.TextDim, TextAnchor.UpperLeft);
            hint.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -490), new Vector2(600, 60));
        }

        static void Btn(RectTransform parent, string text, System.Action a)
        {
            var b = UIKit.TextButton(parent, text, a, 28);
            ((RectTransform)b.transform).sizeDelta = new Vector2(420, 78);
            var l = b.GetComponentInChildren<Text>();
            l.alignment = TextAnchor.MiddleLeft;
            l.rectTransform.Stretch(30, 0, 10, 0);
        }
    }

    // ================================================================== settings
    public class SettingsScreen : Screen2D
    {
        readonly List<Button> presetButtons = new List<Button>();

        public SettingsScreen(Transform parent) : base(parent, "Settings", true)
        {
            var bg = rt.GetComponent<Image>(); bg.color = new Color(0, 0.02f, 0.015f, 0.55f);
            var p = UIKit.Panel(rt, "Panel");
            p.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 900));
            var t = UIKit.Label(p.rectTransform, "Settings", 52, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -36), new Vector2(600, 70));
            var close = UIKit.IconButton(p.rectTransform, "ui_close", () => Show(false), 76);
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(76, 76));

            // graphics
            var g = UIKit.Rect("Graphics", p.rectTransform);
            g.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -130), new Vector2(620, 740));
            UIKit.VStack(g, 10, TextAnchor.UpperLeft);
            Header(g, "GRAPHICS");
            var row = UIKit.Rect("Presets", g); row.sizeDelta = new Vector2(620, 80);
            UIKit.HStack(row, 10, TextAnchor.MiddleLeft);
            string[] names = { "Auto", "Performance", "High", "Ultra" };
            for (int i = 0; i < names.Length; i++)
            {
                int v = i - 1;
                var b = UIKit.TextButton(row, names[i], () => { Settings.PresetChoice = v; Refresh(); }, 22);
                ((RectTransform)b.transform).sizeDelta = new Vector2(i == 1 ? 170 : 140, 70);
                presetButtons.Add(b);
            }
            UIKit.Toggle(g, "Adaptive quality (keeps 60 fps)", Settings.AdaptiveQuality, v => Settings.AdaptiveQuality = v, 620);
            UIKit.Toggle(g, "Show frame rate", Settings.ShowFps, v => Settings.ShowFps = v, 620);
            Header(g, "CONTROLS");
            UIKit.Slider(g, "Look speed", 0.3f, 2.5f, Settings.LookSensitivity, v => Settings.LookSensitivity = v, 620);
            UIKit.Toggle(g, "Invert vertical look", Settings.InvertY, v => Settings.InvertY = v, 620);
            Header(g, "WORLD");
            UIKit.Slider(g, "Day length (min)", 4f, 60f, Settings.DayLengthMinutes, v => Settings.DayLengthMinutes = Mathf.Round(v), 620);
            UIKit.Toggle(g, "Dynamic weather", Settings.DynamicWeather, v => Settings.DynamicWeather = v, 620);

            // audio
            var a = UIKit.Rect("Audio", p.rectTransform);
            a.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -130), new Vector2(620, 740));
            UIKit.VStack(a, 10, TextAnchor.UpperLeft);
            Header(a, "SOUND");
            foreach (AudioCategory c in new[] { AudioCategory.Master, AudioCategory.Nature, AudioCategory.Birds, AudioCategory.Water, AudioCategory.Ambient, AudioCategory.UI })
            {
                var cat = c;
                string label = c == AudioCategory.Master ? "Master" : c == AudioCategory.Nature ? "Nature" : c == AudioCategory.Birds ? "Birds" :
                               c == AudioCategory.Water ? "Water" : c == AudioCategory.Ambient ? "Wind & rain" : "Interface";
                UIKit.Slider(a, label, 0f, 1f, Settings.Volume(cat), v => Settings.SetVolume(cat, v), 620);
            }
            UIKit.Toggle(a, "Mute all sound", Settings.Muted, v => Settings.Muted = v, 620);
            var credit = UIKit.Label(a, "All sounds and textures in this app are original and generated for HUMMINGBIRD X. Typeface: Inter (SIL Open Font License).", 18, UIKit.TextDim);
            credit.rectTransform.sizeDelta = new Vector2(620, 90);
            Refresh();
        }

        static void Header(RectTransform parent, string text)
        {
            var l = UIKit.Label(parent, text, 22, UIKit.Accent, TextAnchor.LowerLeft, Res.SemiBold);
            l.rectTransform.sizeDelta = new Vector2(620, 46);
        }

        void Refresh()
        {
            for (int i = 0; i < presetButtons.Count; i++)
            {
                bool on = Settings.PresetChoice == i - 1;
                ((Image)presetButtons[i].targetGraphic).color = on ? new Color(0.3f, 0.6f, 0.48f, 0.85f) : UIKit.GlassLight;
            }
        }

        protected override void OnShow() { Refresh(); }
    }

    // ================================================================== map
    public class MapScreen : Screen2D
    {
        readonly RectTransform mapRt, player;
        readonly RawImage map;
        readonly List<(Button b, Text t, int region)> regionButtons = new List<(Button, Text, int)>();
        readonly List<RectTransform> markers = new List<RectTransform>();
        const float MapSize = 860f;

        public MapScreen(Transform parent) : base(parent, "Map", true)
        {
            var bg = rt.GetComponent<Image>(); bg.color = new Color(0.01f, 0.025f, 0.02f, 0.92f);
            var title = UIKit.Label(rt, "Rainforest Map", 52, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -40), new Vector2(800, 70));
            var close = UIKit.IconButton(rt, "ui_close", () => Show(false), 80);
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(80, 80));

            var frame = UIKit.Panel(rt, "Frame");
            frame.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(70, -30), new Vector2(MapSize + 30, MapSize + 30));
            mapRt = UIKit.Rect("MapImage", frame.rectTransform);
            mapRt.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapSize, MapSize));
            map = mapRt.gameObject.AddComponent<RawImage>();
            map.raycastTarget = false;

            for (int i = 0; i < TerrainField.RegionCenters.Length; i++)
            {
                var lbl = UIKit.Label(mapRt, TerrainField.RegionNames[i], 20, UIKit.TextColor, TextAnchor.MiddleCenter, Res.SemiBold);
                lbl.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ToMap(TerrainField.RegionCenters[i]), new Vector2(260, 30));
                var sh = lbl.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f);
            }
            player = UIKit.Rect("Player", mapRt);
            var pi = player.gameObject.AddComponent<Image>();
            pi.sprite = Res.Sprite("ui_up"); pi.color = new Color(1f, 0.85f, 0.4f); pi.raycastTarget = false;
            player.sizeDelta = new Vector2(46, 46);

            var list = UIKit.Rect("Regions", rt);
            list.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-70, -30), new Vector2(760, 860));
            UIKit.VStack(list, 12, TextAnchor.UpperLeft);
            var lt = UIKit.Label(list, "FAST TRAVEL", 22, UIKit.Accent, TextAnchor.LowerLeft, Res.SemiBold);
            lt.rectTransform.sizeDelta = new Vector2(760, 40);
            for (int i = 0; i < TerrainField.RegionNames.Length; i++)
            {
                int r = i;
                var b = UIKit.Button(list, "Region" + i, () => GameManager.I.FastTravel(r), UIKit.Glass);
                ((RectTransform)b.transform).sizeDelta = new Vector2(760, 104);
                var t = UIKit.Label(b.transform, "", 26, UIKit.TextColor, TextAnchor.MiddleLeft, Res.SemiBold);
                t.rectTransform.Stretch(30, 0, 20, 0);
                regionButtons.Add((b, t, r));
            }
        }

        static Vector2 ToMap(Vector2 world) => world / TerrainField.Size * MapSize;

        protected override void OnShow()
        {
            if (map.texture == null && WorldGenerator.I) map.texture = WorldGenerator.I.MapTexture;
            foreach (var m in markers) Object.Destroy(m.gameObject);
            markers.Clear();
            var save = GameManager.I.Save;
            foreach (var f in TerrainField.Falls)
            {
                if (f.secret && !save.IsDiscovered("secret_" + f.id)) continue;
                var mk = UIKit.Icon(mapRt, "ui_drop", 26, new Color(0.6f, 0.85f, 1f));
                mk.rectTransform.anchoredPosition = ToMap(new Vector2(f.bottom.x, f.bottom.z));
                markers.Add(mk.rectTransform);
            }
            if (save.IsDiscovered("secret_grotto"))
            {
                var mk = UIKit.Icon(mapRt, "ui_circle", 20, new Color(0.5f, 1f, 0.9f));
                mk.rectTransform.anchoredPosition = ToMap(TerrainField.SecretGrotto);
                markers.Add(mk.rectTransform);
            }
            foreach (var (b, t, r) in regionButtons)
            {
                bool unlocked = GameManager.I.RegionUnlocked(r);
                int need = TerrainField.RegionUnlockNectar[r];
                t.text = unlocked
                    ? $"{TerrainField.RegionNames[r]}\n<size=20><color=#9fffd0>Fly there now</color></size>"
                    : $"{TerrainField.RegionNames[r]}\n<size=20><color=#ffffff88>Collect {need} nectar to unlock fast travel (you can always fly there)</color></size>";
            }
        }

        public void Tick()
        {
            if (!Visible || BirdController.I == null) return;
            var p = BirdController.I.transform.position;
            player.anchoredPosition = ToMap(new Vector2(p.x, p.z));
            player.localRotation = Quaternion.Euler(0, 0, -BirdController.I.transform.eulerAngles.y);
        }
    }

    // ================================================================== journal
    public static class JournalDb
    {
        public struct Entry { public string id, title, latin, category, body; }

        public static List<Entry> All()
        {
            var l = new List<Entry>();
            foreach (var f in FlowerSpecies.All) l.Add(new Entry { id = f.id, title = f.name, latin = f.latin, category = "Flowers", body = f.description + $"\n\nNectar per bloom: {f.nectar:0.0} · refills in about {f.refillSeconds:0} s" });
            foreach (var h in HummerSpecies.Others) l.Add(new Entry { id = h.id, title = h.name, latin = h.latin, category = "Wildlife", body = h.description });
            foreach (var w in WildSpecies.All) l.Add(new Entry { id = w.id, title = w.name, latin = w.latin, category = "Wildlife", body = w.description });
            for (int i = 0; i < TerrainField.RegionNames.Length; i++)
                l.Add(new Entry { id = "region_" + TerrainField.RegionIds[i], title = TerrainField.RegionNames[i], latin = "Region", category = "Places", body = RegionText(i) });
            foreach (var f in TerrainField.Falls)
                if (f.secret) l.Add(new Entry { id = "secret_" + f.id, title = f.name, latin = "Secret waterfall", category = "Places", body = "A hidden waterfall few visitors ever find." });
            l.Add(new Entry { id = "secret_grotto", title = "Spirit Grotto", latin = "Hidden area", category = "Places", body = "A ring of ancient giants deep in the Night Forest, where Spirit Orchids glow." });
            return l;
        }

        public static int TotalEntries() => All().Count;

        static string RegionText(int i)
        {
            switch (i)
            {
                case 0: return "Dense tropical forest where sunlight falls through the canopy in long golden shafts. Heliconias and ferns crowd the riverbanks.";
                case 1: return "An open valley of colourful flowers around a quiet lake. Thousands of nectar sources and clouds of butterflies.";
                case 2: return "A great waterfall pours off the northern plateau into a misty plunge pool. Rainbows, wet rocks and ferns everywhere.";
                case 3: return "Emergent giants rise above the forest roof. Fly between their crowns, where macaws and toucans perch.";
                case 4: return "A high western knoll that catches the last golden light of the day. Best visited at sunset.";
                default: return "A dark, ancient forest. After dusk, moonflowers open, mushrooms glow and fireflies drift between the trunks.";
            }
        }

        public static Entry? Find(string id)
        {
            foreach (var e in All()) if (e.id == id) return e;
            return null;
        }
    }

    public class JournalScreen : Screen2D
    {
        readonly RectTransform grid, detail;
        readonly Text detailTitle, detailLatin, detailBody, counter;
        readonly RawImage detailPhoto;
        string tab = "Flowers";
        readonly List<Texture2D> loaded = new List<Texture2D>();

        public JournalScreen(Transform parent) : base(parent, "Journal", true)
        {
            var bg = rt.GetComponent<Image>(); bg.color = new Color(0.01f, 0.025f, 0.02f, 0.94f);
            var title = UIKit.Label(rt, "Wildlife Journal", 52, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -40), new Vector2(800, 70));
            counter = UIKit.Label(rt, "", 24, UIKit.TextDim, TextAnchor.UpperLeft, Res.SemiBold);
            counter.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -112), new Vector2(800, 40));
            var close = UIKit.IconButton(rt, "ui_close", () => Show(false), 80);
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(80, 80));

            var tabs = UIKit.Rect("Tabs", rt);
            tabs.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -170), new Vector2(900, 70));
            UIKit.HStack(tabs, 12, TextAnchor.MiddleLeft);
            foreach (var t in new[] { "Flowers", "Wildlife", "Places", "Photos" })
            {
                var name = t;
                var b = UIKit.TextButton(tabs, t, () => { tab = name; Rebuild(); }, 24);
                ((RectTransform)b.transform).sizeDelta = new Vector2(200, 64);
            }

            // scrollable grid
            var view = UIKit.Rect("View", rt);
            view.anchorMin = new Vector2(0, 0); view.anchorMax = new Vector2(0.58f, 1);
            view.offsetMin = new Vector2(80, 50); view.offsetMax = new Vector2(0, -260);
            view.gameObject.AddComponent<RectMask2D>();
            var vi = view.gameObject.AddComponent<Image>(); vi.color = new Color(0, 0, 0, 0.001f);
            grid = UIKit.Rect("Grid", view);
            grid.anchorMin = new Vector2(0, 1); grid.anchorMax = new Vector2(1, 1); grid.pivot = new Vector2(0.5f, 1);
            grid.offsetMin = grid.offsetMax = Vector2.zero;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(330, 150); gl.spacing = new Vector2(16, 16);
            var fit = grid.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = grid; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.viewport = view;

            // detail
            var d = UIKit.Panel(rt, "Detail");
            detail = d.rectTransform;
            detail.anchorMin = new Vector2(0.6f, 0); detail.anchorMax = new Vector2(1, 1);
            detail.offsetMin = new Vector2(10, 50); detail.offsetMax = new Vector2(-70, -170);
            detailPhoto = UIKit.Rect("Photo", detail).gameObject.AddComponent<RawImage>();
            detailPhoto.rectTransform.anchorMin = new Vector2(0, 0.45f); detailPhoto.rectTransform.anchorMax = new Vector2(1, 1);
            detailPhoto.rectTransform.offsetMin = new Vector2(30, 0); detailPhoto.rectTransform.offsetMax = new Vector2(-30, -30);
            detailPhoto.gameObject.SetActive(false);
            detailTitle = UIKit.Label(detail, "Select an entry", 40, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            detailTitle.rectTransform.Stretch(40, 0, 40, 40);
            detailLatin = UIKit.Label(detail, "", 24, UIKit.Accent, TextAnchor.UpperLeft, Res.SemiBold);
            detailLatin.rectTransform.Stretch(40, 0, 40, 100);
            detailBody = UIKit.Label(detail, "", 26, UIKit.TextDim, TextAnchor.UpperLeft);
            detailBody.rectTransform.Stretch(40, 40, 40, 150);
        }

        protected override void OnShow() { Rebuild(); }

        void Rebuild()
        {
            foreach (Transform c in grid) Object.Destroy(c.gameObject);
            foreach (var t in loaded) Object.Destroy(t);
            loaded.Clear();
            var save = GameManager.I.Save;
            int found = 0, total = 0;
            foreach (var e in JournalDb.All()) { total++; if (save.IsDiscovered(e.id)) found++; }
            counter.text = $"{found} of {total} discovered  ·  {save.photos.Count} photos";
            detailPhoto.gameObject.SetActive(false);

            if (tab == "Photos")
            {
                for (int i = save.photos.Count - 1; i >= 0 && i >= save.photos.Count - 24; i--)
                {
                    var ph = save.photos[i];
                    var b = UIKit.Button(grid, "Photo", () => ShowPhoto(ph), UIKit.Glass);
                    var tex = LoadThumb(ph.file);
                    if (tex != null)
                    {
                        var ri = UIKit.Rect("Thumb", b.transform).gameObject.AddComponent<RawImage>();
                        ri.texture = tex; ri.raycastTarget = false;
                        ri.rectTransform.Stretch(6, 6, 6, 6);
                    }
                }
                if (save.photos.Count == 0) Detail("No photos yet", "", "Open Photo Mode from the HUD or the main menu, frame a flower or an animal and press the shutter.");
                return;
            }
            foreach (var e in JournalDb.All())
            {
                if (e.category != tab) continue;
                bool known = save.IsDiscovered(e.id);
                var entry = e;
                var b = UIKit.Button(grid, e.id, () =>
                {
                    if (known) Detail(entry.title, entry.latin, entry.body);
                    else Detail("Undiscovered", "", HintFor(entry));
                }, known ? UIKit.GlassLight : new Color(0.03f, 0.05f, 0.045f, 0.5f));
                var t = UIKit.Label(b.transform, known ? e.title : "? ? ?", 26, known ? UIKit.TextColor : UIKit.TextDim, TextAnchor.UpperLeft, Res.SemiBold);
                t.rectTransform.Stretch(22, 10, 16, 18);
                var l = UIKit.Label(b.transform, known ? e.latin : "Not yet found", 19, known ? UIKit.Accent : UIKit.TextDim, TextAnchor.LowerLeft);
                l.rectTransform.Stretch(22, 16, 16, 0);
            }
        }

        static string HintFor(JournalDb.Entry e)
        {
            if (e.category == "Flowers") return "Drink from this flower to add it to your journal.";
            if (e.category == "Places") return e.id.StartsWith("secret") ? "Somewhere hidden… explore the edges of the forest." : "Fly into this region to record it.";
            return "Get close to this animal and keep it in view to record it.";
        }

        void Detail(string title, string latin, string body)
        {
            detailPhoto.gameObject.SetActive(false);
            detailTitle.rectTransform.Stretch(40, 0, 40, 40);
            detailLatin.rectTransform.Stretch(40, 0, 40, 100);
            detailBody.rectTransform.Stretch(40, 40, 40, 150);
            detailTitle.text = title; detailLatin.text = latin; detailBody.text = body;
        }

        void ShowPhoto(PhotoEntry ph)
        {
            var tex = LoadThumb(ph.file);
            detailPhoto.texture = tex;
            detailPhoto.gameObject.SetActive(tex != null);
            detailTitle.rectTransform.Stretch(40, 0, 40, 0);
            detailTitle.rectTransform.anchorMax = new Vector2(1, 0.42f);
            detailTitle.text = ph.subject;
            detailLatin.text = "";
            detailBody.text = $"{ph.region} · {ph.time}\n\nSaved to:\n{ph.file}";
            detailBody.rectTransform.anchorMax = new Vector2(1, 0.32f);
        }

        Texture2D LoadThumb(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(File.ReadAllBytes(file))) return null;
                loaded.Add(tex);
                return tex;
            }
            catch { return null; }
        }
    }

    // ================================================================== photo mode
    public class PhotoScreen : Screen2D
    {
        readonly CanvasGroup chrome;
        readonly Image flash;
        bool hidden;

        public PhotoScreen(Transform parent) : base(parent, "Photo", false)
        {
            var c = UIKit.Rect("Chrome", rt).Stretch();
            chrome = UIKit.Group(c.gameObject);

            var close = UIKit.IconButton(c, "ui_close", () => GameManager.I.ExitPhotoMode(), 84, "EXIT");
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(84, 84));
            var eye = UIKit.IconButton(c, "ui_eye", () => SetHidden(true), 84, "HIDE UI");
            ((RectTransform)eye.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -40), new Vector2(84, 84));

            var shutter = UIKit.Button(c, "Shutter", () => GameManager.I.StartCoroutine(Capture()), new Color(1, 1, 1, 0.18f));
            var sr = (RectTransform)shutter.transform;
            sr.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-60, 40), new Vector2(150, 150));
            ((Image)shutter.targetGraphic).sprite = Res.Sprite("ui_circle");
            ((Image)shutter.targetGraphic).type = Image.Type.Simple;
            var ring = UIKit.Icon(sr, "ui_ring", 170, Color.white);

            var panel = UIKit.Panel(c, "Controls");
            panel.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), new Vector2(560, 470));
            var col = UIKit.Rect("Col", panel.rectTransform);
            col.Stretch(24, 20, 24, 20);
            UIKit.VStack(col, 4, TextAnchor.UpperLeft);
            var t = UIKit.Label(col, "PHOTO MODE", 22, UIKit.Accent, TextAnchor.MiddleLeft, Res.SemiBold);
            t.rectTransform.sizeDelta = new Vector2(500, 40);
            UIKit.Slider(col, "Zoom", 15f, 90f, 55f, v => { if (CameraRig.I) CameraRig.I.PhotoFov = 105f - v; }, 500);
            UIKit.Slider(col, "Focus (m)", 0.1f, 15f, 0.6f, v => { if (CameraRig.I) CameraRig.I.Post.FocusDistance = v * v / 15f + 0.08f; }, 500);
            UIKit.Slider(col, "Background blur", 0f, 1f, 0.6f, v => { if (CameraRig.I) CameraRig.I.Post.Aperture = v; }, 500);
            UIKit.Slider(col, "Exposure", -2f, 2f, 0f, v => { if (CameraRig.I) CameraRig.I.Post.ExposureBias = v; }, 500);
            var hint = UIKit.Label(col, "Left stick moves the camera, right stick or swipe rotates, UP/DOWN rise and fall. Time is frozen.", 18, UIKit.TextDim);
            hint.rectTransform.sizeDelta = new Vector2(500, 80);

            // tap anywhere to bring the UI back when hidden
            var catcher = UIKit.Rect("Unhide", rt).Stretch();
            var ci = catcher.gameObject.AddComponent<Image>(); ci.color = new Color(0, 0, 0, 0.001f);
            var cb = catcher.gameObject.AddComponent<Button>();
            cb.onClick.AddListener(() => SetHidden(false));
            catcher.gameObject.SetActive(false);
            unhide = catcher.gameObject;

            var fl = UIKit.Rect("Flash", rt).Stretch();
            flash = fl.gameObject.AddComponent<Image>();
            flash.color = new Color(1, 1, 1, 0); flash.raycastTarget = false;
        }

        readonly GameObject unhide;

        void SetHidden(bool h)
        {
            hidden = h;
            chrome.alpha = h ? 0f : 1f;
            chrome.blocksRaycasts = !h;
            unhide.SetActive(h);
            UIManager.I.Hud.MoveStick.transform.parent.gameObject.SetActive(!h);
        }

        protected override void OnShow() { SetHidden(false); }

        IEnumerator Capture()
        {
            Directory.CreateDirectory(SaveData.PhotoDir);
            string file = Path.Combine(SaveData.PhotoDir, $"HBX_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            bool wasHidden = hidden;
            chrome.alpha = 0f;
            var hud = UIManager.I.Hud;
            bool hudOn = hud.Visible;
            hud.rt.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            // "8K" capture where the device allows: supersample towards 7680 px wide
            int super = 1;
            var q = QualityManager.I;
            if (q && q.Active == GraphicsPreset.Ultra) super = Mathf.Clamp(Mathf.CeilToInt(7680f / Screen.width), 1, 4);
            else if (q && q.Active == GraphicsPreset.High) super = 2;
            while (super > 1 && Screen.width * super > SystemInfo.maxTextureSize) super--;
            ScreenCapture.CaptureScreenshot(file, super);
            yield return null;
            yield return null;
            AudioManager.I.PlayUI("sfx_shutter", 0.8f);
            float tt = 0f;
            while (tt < 0.35f) { tt += Time.unscaledDeltaTime; flash.color = new Color(1, 1, 1, 1f - tt / 0.35f); yield return null; }
            flash.color = new Color(1, 1, 1, 0);
            hud.rt.gameObject.SetActive(hudOn);
            chrome.alpha = wasHidden ? 0f : 1f;
            GameManager.I.RegisterPhoto(file);
        }
    }

    // ================================================================== flight school
    public class FlightSchoolOverlay
    {
        readonly RectTransform card;
        readonly Text step, title, body;
        readonly CanvasGroup g;
        int index = -1;
        float progress, startYaw, accumYaw, lastYaw, startY, lookAccum, holdTimer, nectarStart;
        Vector3 startPos;
        bool cameraSwitched;

        static readonly string[,] Steps =
        {
            { "Take off", "Push the left stick or hold UP to lift off the twig." },
            { "Hover", "Let go of the stick. Then tap HOVER to brake and hold perfectly still." },
            { "Fly forward", "Push the left stick forward. Fly about 8 metres." },
            { "Turn", "Steer with the stick, or swipe to turn the camera. Make a half turn." },
            { "Climb", "Hold UP to fly upward 4 metres. Hold DOWN to descend." },
            { "Visit a flower", "Fly slowly to a flower and hover with your bill at the bloom, or simply tap a flower." },
            { "Drink nectar", "Hold still at the flower. Collect 3 nectar." },
            { "Use the camera", "Swipe or use the right stick to look around, and tap the camera button to switch views." },
            { "Land", "Tap LAND near a branch or twig to perch." },
        };

        public FlightSchoolOverlay(Transform parent)
        {
            var p = UIKit.Panel(parent, "FlightSchool");
            card = p.rectTransform;
            card.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(880, 170));
            g = UIKit.Group(card.gameObject);
            g.blocksRaycasts = false;
            step = UIKit.Label(card, "", 20, UIKit.Accent, TextAnchor.UpperLeft, Res.SemiBold);
            step.rectTransform.Stretch(34, 0, 34, 18);
            title = UIKit.Label(card, "", 38, UIKit.TextColor, TextAnchor.UpperLeft, Res.Light);
            title.rectTransform.Stretch(34, 0, 34, 46);
            body = UIKit.Label(card, "", 24, UIKit.TextDim, TextAnchor.UpperLeft);
            body.rectTransform.Stretch(34, 10, 34, 102);
            card.gameObject.SetActive(false);
        }

        public bool Active => index >= 0;

        public void Begin()
        {
            index = -1;
            card.gameObject.SetActive(true);
            g.alpha = 1f;
            Next();
        }

        public void Stop()
        {
            index = -1;
            card.gameObject.SetActive(false);
        }

        void Next()
        {
            index++;
            if (index >= Steps.GetLength(0))
            {
                Settings.FlightSchoolDone = true;
                AudioManager.I.PlayUI("sfx_discover", 0.7f);
                UIManager.I.Discovery("Flight School", "You're a natural!", "Explore freely. All controls are now yours.");
                Stop();
                return;
            }
            if (index > 0) AudioManager.I.PlayUI("sfx_reward", 0.5f);
            step.text = $"FLIGHT SCHOOL · STEP {index + 1} OF {Steps.GetLength(0)}";
            title.text = Steps[index, 0];
            body.text = Steps[index, 1];
            var b = BirdController.I;
            startPos = b.transform.position;
            startY = startPos.y;
            lastYaw = b.transform.eulerAngles.y;
            accumYaw = 0; lookAccum = 0; holdTimer = 0; cameraSwitched = false;
            nectarStart = GameManager.I.Save.nectar;
            startCamMode = CameraRig.I.Mode;
        }

        CamMode startCamMode;

        public void Tick()
        {
            if (!Active) return;
            var b = BirdController.I;
            if (b == null) return;
            float dt = Time.deltaTime;
            float yaw = b.transform.eulerAngles.y;
            accumYaw += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw));
            lastYaw = yaw;
            var ir = InputRouter.I;
            if (ir) lookAccum += ir.Look.magnitude * dt;
            if (CameraRig.I.Mode != startCamMode) cameraSwitched = true;
            bool done = false;
            switch (index)
            {
                case 0: done = b.State != BirdState.Perched && b.State != BirdState.Landing; break;
                case 1:
                    if (b.State == BirdState.Flying && b.Velocity.magnitude < 0.35f) holdTimer += dt; else holdTimer = 0;
                    done = holdTimer > 1.2f; break;
                case 2: done = new Vector2(b.transform.position.x - startPos.x, b.transform.position.z - startPos.z).magnitude > 8f; break;
                case 3: done = accumYaw > 160f; break;
                case 4: done = b.transform.position.y - startY > 4f; break;
                case 5: done = b.State == BirdState.Feeding; break;
                case 6: done = GameManager.I.Save.nectar - nectarStart >= 3f; break;
                case 7: done = lookAccum > 90f && cameraSwitched; break;
                case 8: done = b.State == BirdState.Perched; break;
            }
            if (done) Next();
        }
    }
}
