using System.Collections;
using UnityEngine;

namespace HummingbirdX
{
    public enum GameState { Loading, Menu, Explore, Photo, Paused }

    /// <summary>
    /// Boots every system in order, owns the game state (menu, exploring, photo mode, pause),
    /// progress (nectar, discoveries, regions, secrets, photos) and pause/resume handling.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }
        public GameState State { get; private set; } = GameState.Loading;
        public SaveData Save { get; private set; }
        public bool Relaxation { get; private set; }
        public string CurrentRegionName { get; private set; } = "";

        Region currentRegion = (Region)(-1);
        float regionTimer, saveTimer;
        GameState beforePause;

        void Awake()
        {
            I = this;
            Save = SaveData.Load();
        }

        IEnumerator Start()
        {
            gameObject.AddComponent<QualityManager>();
            gameObject.AddComponent<InputRouter>();
            gameObject.AddComponent<TouchGestures>();
            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            audio.transform.SetParent(transform, false);
            var ui = new GameObject("UIRoot").AddComponent<UIManager>();
            ui.transform.SetParent(transform, false);
            ui.Loading.Set(0f, "Waking the rainforest");
            yield return null;

            var world = new GameObject("WorldGenerator").AddComponent<WorldGenerator>();
            world.transform.SetParent(transform, false);
            var gen = world.Generate();
            while (gen.MoveNext())
            {
                ui.Loading.Set(world.Progress * 0.85f, world.ProgressLabel);
                yield return gen.Current;
            }

            ui.Loading.Set(0.87f, "Opening flowers");
            yield return null;
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(transform, false);
            var rig = camGo.AddComponent<CameraRig>();
            rig.Build();

            var env = new GameObject("Environment").AddComponent<EnvironmentSystem>();
            env.transform.SetParent(transform, false);
            env.Build(camGo.transform);
            env.SetTime(8.5f);

            var flowers = new GameObject("Flowers").AddComponent<FlowerManager>();
            flowers.transform.SetParent(transform, false);
            flowers.BuildAll();

            ui.Loading.Set(0.93f, "Hatching hummingbirds");
            yield return null;
            var birdGo = new GameObject("Hummingbird");
            birdGo.transform.SetParent(transform, false);
            var bird = birdGo.AddComponent<BirdController>();
            bird.Build();
            bird.PlaceOnPerch(new Perch { pos = world.SpawnPerch, along = Vector3.right });
            bird.NectarCollected += OnNectar;
            bird.FlowerEmptied += OnFlowerEmptied;
            rig.SnapToBird();

            var wild = new GameObject("Wildlife").AddComponent<WildlifeManager>();
            wild.transform.SetParent(transform, false);
            wild.Spawn(birdGo.transform.position);

            ui.Loading.Set(0.97f, "Tuning the dawn chorus");
            yield return null;
            AudioManager.I.BuildSoundscape(camGo.transform);
            ui.BuildScreens();

            ui.Loading.Set(1f, "");
            yield return new WaitForSecondsRealtime(0.4f);
            ShowMenu();
            yield return ui.Loading.Hide();
        }

        // ------------------------------------------------------------------ states
        public void ShowMenu()
        {
            Time.timeScale = 1f;
            AudioManager.I.SetPaused(false);
            State = GameState.Menu;
            Relaxation = false;
            var b = BirdController.I;
            b.Frozen = false; b.Autopilot = false; b.InputEnabled = false;
            if (b.State != BirdState.Perched)
            {
                if (WorldGenerator.I.NearestPerch(b.transform.position, 25f, out var p)) b.PlaceOnPerch(p);
                else b.PlaceOnPerch(new Perch { pos = WorldGenerator.I.SpawnPerch, along = Vector3.right });
            }
            CameraRig.I.MenuOrbit = true;
            CameraRig.I.SetMode(CamMode.ThirdPerson);
            var ui = UIManager.I;
            ui.HideAll();
            ui.School.Stop();
            ui.Menu.Show(true);
            Application.targetFrameRate = 30;  // battery: the menu doesn't need 60
            InputRouter.I.ClearAll();
        }

        public void StartExplore()
        {
            Time.timeScale = 1f;
            State = GameState.Explore;
            Application.targetFrameRate = 60;
            var b = BirdController.I;
            b.InputEnabled = true; b.Autopilot = false; b.Frozen = false;
            CameraRig.I.MenuOrbit = false;
            CameraRig.I.SetMode(CamMode.ThirdPerson);
            CameraRig.I.ResetBehind();
            var ui = UIManager.I;
            ui.HideAll();
            ui.Hud.SetRelax(false);
            ui.Hud.SetPhotoLayout(false);
            ui.Hud.Show(true);
            InputRouter.I.ClearAll();
            if (!Settings.FlightSchoolDone && Save.secondsFlown < 1f)
                UIManager.Toast("New to flying? Try Flight School from the menu");
        }

        public void StartFlightSchool()
        {
            StartExplore();
            var b = BirdController.I;
            b.PlaceOnPerch(new Perch { pos = WorldGenerator.I.SpawnPerch, along = Vector3.right });
            EnvironmentSystem.I.SetTime(9f);
            EnvironmentSystem.I.SetWeather(Weather.Sunny, true);
            CameraRig.I.SnapToBird();
            UIManager.I.School.Begin();
        }

        public void StartRelaxation()
        {
            StartExplore();
            Relaxation = true;
            BirdController.I.Autopilot = true;
            CameraRig.I.SetMode(CamMode.Cinematic);
            UIManager.I.Hud.SetRelax(true);
            UIManager.Toast("Relaxation mode — sit back and listen");
        }

        public void StopRelaxation()
        {
            Relaxation = false;
            BirdController.I.Autopilot = false;
            CameraRig.I.SetMode(CamMode.ThirdPerson);
            UIManager.I.Hud.SetRelax(false);
        }

        public void Pause()
        {
            if (State != GameState.Explore) return;
            beforePause = State;
            State = GameState.Paused;
            Time.timeScale = 0f;
            Application.targetFrameRate = 30;
            UIManager.I.Pause.Show(true);
            InputRouter.I.ClearAll();
        }

        public void Resume()
        {
            var ui = UIManager.I;
            ui.Pause.Show(false); ui.SettingsPanel.Show(false); ui.Map.Show(false); ui.Journal.Show(false);
            State = GameState.Explore;
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
        }

        public void EnterPhotoMode()
        {
            if (State != GameState.Explore) return;
            State = GameState.Photo;
            Time.timeScale = 0f;
            BirdController.I.Frozen = true;
            CameraRig.I.SetMode(CamMode.Free);
            CameraRig.I.Post.FocusDistance = CameraRig.I.Distance;
            var ui = UIManager.I;
            ui.Hud.SetPhotoLayout(true);
            ui.Photo.Show(true);
            InputRouter.I.ClearAll();
        }

        public void ExitPhotoMode()
        {
            State = GameState.Explore;
            Time.timeScale = 1f;
            BirdController.I.Frozen = false;
            CameraRig.I.Post.ExposureBias = 0f;
            CameraRig.I.SetMode(Relaxation ? CamMode.Cinematic : CamMode.ThirdPerson);
            var ui = UIManager.I;
            ui.Photo.Show(false);
            ui.Hud.SetPhotoLayout(false);
            ui.Hud.Show(true);
            InputRouter.I.ClearAll();
        }

        public void RegisterPhoto(string file)
        {
            var cam = Camera.main;
            string subject = "Rainforest";
            var c = WildlifeManager.I ? WildlifeManager.I.NearestInView(cam, 8f) : null;
            if (c != null)
            {
                var ws = WildSpecies.Get(c.speciesId);
                subject = ws != null ? ws.name : "Hummingbird";
                foreach (var h in HummerSpecies.Others) if (h.id == c.speciesId) subject = h.name;
            }
            else if (FlowerManager.I)
            {
                var f = FlowerManager.I.Nearest(cam.transform.position + cam.transform.forward * 0.5f, 1.2f, false);
                if (f) subject = f.species.name;
            }
            Save.photos.Add(new PhotoEntry { file = file, subject = subject, region = CurrentRegionName, time = EnvironmentSystem.I.TimeLabel() });
            Save.Save();
            UIManager.Toast("Photo saved to your journal");
        }

        public bool RegionUnlocked(int r) => Save.nectar >= TerrainField.RegionUnlockNectar[r] || Save.unlockedRegions.Contains(TerrainField.RegionIds[r]);

        public void FastTravel(int r)
        {
            if (!RegionUnlocked(r)) { UIManager.Toast($"Collect {TerrainField.RegionUnlockNectar[r]} nectar to unlock fast travel"); return; }
            if (State == GameState.Menu || State == GameState.Paused) StartExplore();
            UIManager.I.Map.Show(false);
            Vector2 c = TerrainField.RegionCenters[r];
            if (r == (int)Region.WaterfallParadise) c = new Vector2(10, 100);
            Vector3 g = TerrainField.Ground(c.x, c.y);
            float wd = TerrainField.WaterDistance(c.x, c.y, out float lvl);
            float y = Mathf.Max(g.y, wd < 0 ? lvl : g.y) + 2.5f;
            float yaw = r == (int)Region.WaterfallParadise ? 0f : Random.Range(0f, 360f);
            BirdController.I.PlaceFlying(new Vector3(c.x, y, c.y), yaw);
            CameraRig.I.SnapToBird();
            if (r == (int)Region.GoldenSunset) EnvironmentSystem.I.SetTime(17.0f);
            if (r == (int)Region.NightForest) EnvironmentSystem.I.SetTime(21.5f);
            if (r == (int)Region.WaterfallParadise && EnvironmentSystem.I.Night > 0.5f) EnvironmentSystem.I.SetTime(9f);
        }

        // ------------------------------------------------------------------ progress
        void OnNectar(Flower f, float amount)
        {
            float before = Save.nectar;
            Save.nectar += amount;
            if (!Save.IsDiscovered(f.species.id)) Discover(f.species.id);
            for (int r = 0; r < TerrainField.RegionUnlockNectar.Length; r++)
            {
                int need = TerrainField.RegionUnlockNectar[r];
                if (need > 0 && before < need && Save.nectar >= need)
                {
                    UIManager.I.Discovery("Unlocked", TerrainField.RegionNames[r], "Fast travel is now available from the map");
                    if (!Save.unlockedRegions.Contains(TerrainField.RegionIds[r])) Save.unlockedRegions.Add(TerrainField.RegionIds[r]);
                }
            }
        }

        void OnFlowerEmptied(Flower f)
        {
            Save.flowersVisited++;
            var fm = FlowerManager.I;
            fm.Reward.transform.position = f.Mouth;
            fm.Reward.Emit(30);
            AudioManager.I.PlayAt("sfx_reward", f.Mouth, AudioCategory.UI, 0.6f, 1f, 20f);
        }

        public void Discover(string id)
        {
            if (Save.IsDiscovered(id)) return;
            Save.discovered.Add(id);
            Save.Save();
            var e = JournalDb.Find(id);
            if (e.HasValue)
            {
                var v = e.Value;
                string kind = v.category == "Places" ? (id.StartsWith("secret") ? "Secret found" : "New region") : "New discovery";
                UIManager.I.Discovery(kind, v.title, v.latin);
            }
            AudioManager.I.PlayUI("sfx_discover", 0.7f);
        }

        void Update()
        {
            if (State == GameState.Loading) return;
            var b = BirdController.I;
            if (b == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Android back button
                var ui = UIManager.I;
                if (ui.Map.Visible) ui.Map.Show(false);
                else if (ui.Journal.Visible) ui.Journal.Show(false);
                else if (ui.SettingsPanel.Visible) ui.SettingsPanel.Show(false);
                else if (State == GameState.Photo) ExitPhotoMode();
                else if (State == GameState.Paused) Resume();
                else if (State == GameState.Explore) Pause();
            }

            regionTimer -= Time.unscaledDeltaTime;
            if (regionTimer <= 0f && (State == GameState.Explore))
            {
                regionTimer = 0.5f;
                Vector3 p = b.transform.position;
                var r = TerrainField.RegionAt(p.x, p.z);
                if (r != currentRegion)
                {
                    currentRegion = r;
                    CurrentRegionName = TerrainField.RegionNames[(int)r];
                    UIManager.I.RegionBanner(CurrentRegionName, EnvironmentSystem.I.TimeLabel().ToUpperInvariant());
                    Discover("region_" + TerrainField.RegionIds[(int)r]);
                }
                foreach (var f in TerrainField.Falls)
                    if (f.secret && Vector3.Distance(p, f.bottom) < 30f) Discover("secret_" + f.id);
                if (Vector2.Distance(new Vector2(p.x, p.z), TerrainField.SecretGrotto) < 18f) Discover("secret_grotto");
            }
            else if (State == GameState.Menu && string.IsNullOrEmpty(CurrentRegionName))
            {
                var p = b.transform.position;
                CurrentRegionName = TerrainField.RegionNames[(int)TerrainField.RegionAt(p.x, p.z)];
            }

            saveTimer -= Time.unscaledDeltaTime;
            if (saveTimer <= 0f) { saveTimer = 30f; Save.Save(); }
        }

        void OnApplicationPause(bool paused)
        {
            if (Save == null) return;
            if (paused)
            {
                Save.Save();
                PlayerPrefs.Save();
                if (AudioManager.I) AudioManager.I.SetPaused(true);
                if (State == GameState.Explore) Pause();
            }
            else if (AudioManager.I) AudioManager.I.SetPaused(false);
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && Save != null) Save.Save();
        }

        void OnApplicationQuit() { Save?.Save(); }
    }
}
