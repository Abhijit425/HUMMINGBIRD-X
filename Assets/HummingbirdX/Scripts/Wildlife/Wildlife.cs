using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    /// <summary>Base for every animal: discovery tracking and recycling near the player.</summary>
    public abstract class Creature : MonoBehaviour
    {
        public string speciesId;
        public float discoverDistance = 10f;
        public float home;  // radius the manager keeps this creature within
        public abstract void Respawn(Vector3 near);
        public virtual bool ActiveNow(EnvironmentSystem env) => true;
        protected Rng rng = new Rng(1);
        protected static Vector3 Player => BirdController.I ? BirdController.I.transform.position : Vector3.zero;
    }

    // ---------------------------------------------------------------------------------- butterfly
    public class Butterfly : Creature
    {
        Transform wingL, wingR;
        Vector3 vel, target;
        float flap, restTimer, retarget;
        Flower restFlower;
        static readonly string[] ids = { "bf_morpho", "bf_postman", "bf_glasswing", "bf_owl" };

        public void Init(int cell, int seed)
        {
            rng = new Rng(seed);
            speciesId = ids[cell];
            discoverDistance = 6f;
            float size = cell == 0 || cell == 3 ? 1.6f : 1.0f;
            WildlifeModels.BuildButterfly(transform, cell, out wingL, out wingR, size);
        }

        public override bool ActiveNow(EnvironmentSystem env)
        {
            if (speciesId == "bf_owl") return env.Rain < 0.3f && (env.Night < 0.7f);
            return env.Night < 0.4f && env.Rain < 0.3f;
        }

        public override void Respawn(Vector3 near)
        {
            Vector2 o = rng.InCircle * 40f;
            Vector3 p = TerrainField.Ground(near.x + o.x, near.z + o.y);
            transform.position = p + Vector3.up * rng.Range(0.6f, 3f);
            target = transform.position;
            restFlower = null; restTimer = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = transform.position;
            float toBird = (Player - pos).magnitude;

            if (restFlower != null)
            {
                restTimer -= dt;
                flap += dt * 3f;
                float open = Mathf.Sin(flap) * 0.5f + 0.5f;
                SetWings(Mathf.Lerp(10f, 85f, open));
                transform.position = Vector3.Lerp(pos, restFlower.Mouth + Vector3.up * 0.02f, dt * 4f);
                if (restTimer <= 0f || toBird < 0.35f) { restFlower = null; vel = Vector3.up * 0.8f; }
                return;
            }

            retarget -= dt;
            if (retarget <= 0f || (target - pos).sqrMagnitude < 0.2f)
            {
                retarget = rng.Range(1.5f, 4f);
                var fm = FlowerManager.I;
                var f = fm && rng.Chance(0.35f) ? fm.Nearest(pos, 10f) : null;
                if (f != null) target = f.Mouth;
                else
                {
                    Vector2 o = rng.InCircle * 6f;
                    float g = TerrainField.Height(pos.x + o.x, pos.z + o.y);
                    target = new Vector3(pos.x + o.x, g + rng.Range(0.4f, 3.5f), pos.z + o.y);
                }
            }
            // erratic fluttering flight
            Vector3 steer = (target - pos).normalized * 1.2f + new Vector3(Mathf.PerlinNoise(Time.time * 2f, pos.x) - 0.5f,
                            (Mathf.PerlinNoise(pos.z, Time.time * 3f) - 0.45f) * 1.6f, Mathf.PerlinNoise(Time.time * 2f, pos.z + 9f) - 0.5f) * 3f;
            if (toBird < 1.2f) steer += (pos - Player).normalized * 4f;  // flee the bird
            vel = Vector3.Lerp(vel, steer, dt * 2f);
            vel = Vector3.ClampMagnitude(vel, 2.2f);
            pos += vel * dt;
            float g2 = TerrainField.Height(pos.x, pos.z) + 0.15f;
            if (pos.y < g2) pos.y = g2;
            transform.position = pos;
            if (vel.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(vel.x, 0, vel.z) + Vector3.forward * 0.001f), dt * 5f);
            flap += dt * 11f;
            SetWings(Mathf.Sin(flap) * 70f + 10f);

            var fm2 = FlowerManager.I;
            if (fm2 && (target - pos).sqrMagnitude < 0.05f)
            {
                var f = fm2.Nearest(pos, 0.3f, false);
                if (f) { restFlower = f; restTimer = rng.Range(2f, 6f); }
            }
        }

        void SetWings(float angle)
        {
            wingR.localRotation = Quaternion.Euler(0, 0, angle);
            wingL.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    // ---------------------------------------------------------------------------------- damselfly over water
    public class Damselfly : Creature
    {
        Transform[] wings = new Transform[4];
        Vector3 target; float wait;
        public void Init(int seed)
        {
            rng = new Rng(seed);
            speciesId = "dragonfly";
            discoverDistance = 5f;
            var body = new GameObject("Body").transform; body.SetParent(transform, false);
            WildlifeModels.AddMR(body, WildlifeModels.InsectBodyMesh("damsel", 0.11f, 0.002f, new Color(0.25f, 0.35f, 0.2f)), Mats.InsectBody);
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject("Wing" + i).transform; w.SetParent(transform, false);
                w.localPosition = new Vector3(0, 0.002f, 0.035f - (i / 2) * 0.012f);
                WildlifeModels.AddMR(w, WildlifeModels.FlatWing("damselWing", 0.055f, 0.008f), Mats.DragonflyWing);
                if (i % 2 == 1) w.localScale = new Vector3(-1, 1, 1);
                wings[i] = w;
            }
        }
        public override bool ActiveNow(EnvironmentSystem env) => env.Night < 0.5f && env.Rain < 0.5f;
        public override void Respawn(Vector3 near)
        {
            var wg = WorldGenerator.I;
            Vector3 best = near;
            float bd = float.MaxValue;
            foreach (var p in wg.WaterSoundPoints)
            {
                float d = (p - near).sqrMagnitude;
                if (d < bd && d > 25f) { bd = d; best = p; }
            }
            transform.position = best + new Vector3(rng.Range(-4f, 4f), rng.Range(0.5f, 1.5f), rng.Range(-4f, 4f));
            target = transform.position;
        }
        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            wait -= dt;
            Vector3 pos = transform.position;
            if (wait <= 0f)
            {
                wait = rng.Range(0.6f, 2.5f);
                target = pos + new Vector3(rng.Range(-2.5f, 2.5f), rng.Range(-0.4f, 0.5f), rng.Range(-2.5f, 2.5f));
                float g = Mathf.Max(TerrainField.Height(target.x, target.z), TerrainField.WaterDistance(target.x, target.z, out float l) < 0 ? l : -99f);
                target.y = Mathf.Clamp(target.y, g + 0.3f, g + 2.5f);
            }
            Vector3 np = Vector3.Lerp(pos, target, 1f - Mathf.Exp(-dt * 4f));
            Vector3 d = np - pos; d.y = 0;
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), dt * 8f);
            transform.position = np;
            float t = Time.time * 30f;
            for (int i = 0; i < 4; i++) wings[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t + i * 1.6f) * 25f * (i % 2 == 0 ? 1 : -1));
        }
    }

    // ---------------------------------------------------------------------------------- orchid bee
    public class OrchidBee : Creature
    {
        Flower flower; float t, hop; Vector3 off;
        public void Init(int seed)
        {
            rng = new Rng(seed);
            speciesId = "orchidbee";
            discoverDistance = 2.5f;
            var body = new GameObject("Body").transform; body.SetParent(transform, false);
            var mr = WildlifeModels.AddMR(body, WildlifeModels.InsectBodyMesh("bee", 0.016f, 0.004f, new Color(0.1f, 0.75f, 0.35f)), Mats.InsectBody);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public override bool ActiveNow(EnvironmentSystem env) => env.Night < 0.4f && env.Rain < 0.3f;
        public override void Respawn(Vector3 near)
        {
            var fm = FlowerManager.I;
            Vector2 o = rng.InCircle * 30f;
            flower = fm ? fm.Nearest(near + new Vector3(o.x, 0, o.y), 20f, false) : null;
            transform.position = flower ? flower.Mouth : near;
        }
        void Update()
        {
            if (!flower) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            t += dt; hop -= dt;
            if (hop <= 0f)
            {
                hop = rng.Range(1f, 4f);
                off = new Vector3(rng.Range(-0.15f, 0.15f), rng.Range(0f, 0.12f), rng.Range(-0.15f, 0.15f));
                if (rng.Chance(0.2f) && FlowerManager.I)
                {
                    var f = FlowerManager.I.Nearest(flower.Mouth + Random.insideUnitSphere * 3f, 4f, false);
                    if (f) flower = f;
                }
            }
            Vector3 jitter = new Vector3(Mathf.Sin(t * 13f), Mathf.Sin(t * 17f) * 0.5f, Mathf.Cos(t * 11f)) * 0.01f;
            Vector3 want = flower.Mouth + off + jitter;
            Vector3 p = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-dt * 6f));
            Vector3 d = want - transform.position;
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d);
            transform.position = p;
        }
    }

    // ---------------------------------------------------------------------------------- forest birds
    public class ForestBird : Creature
    {
        WildlifeModels.BirdParts parts;
        enum S { Perched, Flying }
        S state;
        Vector3 from, to, ctrl; float t, dur, idle, flap, headTimer;
        Quaternion headRot = Quaternion.identity;
        string callClip;
        float callTimer;
        bool canopyOnly;

        public void Init(string kind, int seed)
        {
            rng = new Rng(seed);
            speciesId = kind;
            parts = WildlifeModels.BuildBird(kind, transform);
            discoverDistance = kind == "tanager" ? 9f : 22f;
            callClip = kind == "macaw" ? "sfx_parrot" : kind == "toucan" ? "sfx_toucan" : "sfx_bird_2";
            canopyOnly = kind != "tanager";
            callTimer = rng.Range(3f, 15f);
        }

        public override bool ActiveNow(EnvironmentSystem env) => env.Night < 0.85f;

        public override void Respawn(Vector3 near)
        {
            var wg = WorldGenerator.I;
            var p = PickPerch(near, 70f);
            transform.position = p.pos;
            state = S.Perched; idle = rng.Range(2f, 10f);
            FaceAcross(p);
        }

        Perch PickPerch(Vector3 near, float radius)
        {
            var wg = WorldGenerator.I;
            for (int i = 0; i < 10; i++)
            {
                Vector2 o = rng.InCircle * radius;
                if (wg.NearestPerch(near + new Vector3(o.x, 0, o.y) + Vector3.up * 30f, 30f, out var p,
                        x => !canopyOnly || x.pos.y - TerrainField.Height(x.pos.x, x.pos.z) > 12f))
                    return p;
            }
            return wg.RandomPerch(rng, near, radius);
        }

        void FaceAcross(Perch p)
        {
            Vector3 f = Vector3.Cross(p.along, Vector3.up);
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(f.normalized);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = transform.position;
            float toBird = (Player - pos).magnitude;
            callTimer -= dt;
            if (callTimer <= 0f)
            {
                callTimer = rng.Range(8f, 25f);
                if (toBird < 90f) AudioManager.I.PlayAt(callClip, pos, AudioCategory.Birds, speciesId == "tanager" ? 0.5f : 0.9f, rng.Range(0.95f, 1.08f), 150f);
            }

            if (state == S.Perched)
            {
                idle -= dt;
                headTimer -= dt;
                if (headTimer <= 0f)
                {
                    headTimer = rng.Range(0.4f, 2f);
                    headRot = Quaternion.Euler(rng.Range(-20f, 25f), rng.Range(-70f, 70f), rng.Range(-15f, 15f));
                }
                parts.head.localRotation = Quaternion.Slerp(parts.head.localRotation, headRot, dt * 10f);
                parts.wingL.localRotation = Quaternion.Euler(0, -75f, -5f);
                parts.wingR.localRotation = Quaternion.Euler(0, 75f, 5f);
                parts.body.localRotation = Quaternion.Euler(-25f, 0, 0);
                parts.tail.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.3f) * 4f + 30f, 0, 0);
                bool scared = toBird < (speciesId == "tanager" ? 1.5f : 3.5f);
                if (idle <= 0f || scared)
                {
                    var p = PickPerch(pos, scared ? 40f : 90f);
                    from = pos; to = p.pos;
                    float dist = Vector3.Distance(from, to);
                    ctrl = (from + to) * 0.5f + Vector3.up * Mathf.Clamp(dist * 0.25f, 2f, 15f);
                    dur = Mathf.Max(1.5f, dist / (speciesId == "macaw" ? 12f : speciesId == "toucan" ? 8f : 6f));
                    t = 0; state = S.Flying;
                    if (scared) AudioManager.I.PlayAt(callClip, pos, AudioCategory.Birds, 0.8f, 1.1f, 150f);
                    targetPerch = p;
                }
            }
            else
            {
                t += dt / dur;
                float e = Mathf.SmoothStep(0, 1, t);
                Vector3 np = Bezier(from, ctrl, to, e);
                Vector3 dir = np - pos;
                if (dir.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
                transform.position = np;
                // flap on the climb, glide on the descent
                bool glide = dir.y < -0.02f && speciesId != "tanager";
                flap += dt * (speciesId == "tanager" ? 18f : speciesId == "toucan" ? 9f : 7f);
                float a = glide ? 5f + Mathf.Sin(flap * 0.3f) * 3f : Mathf.Sin(flap) * 55f;
                parts.wingL.localRotation = Quaternion.Euler(0, 0, -a);
                parts.wingR.localRotation = Quaternion.Euler(0, 0, a);
                parts.body.localRotation = Quaternion.Euler(Mathf.Clamp(-dir.y * 200f, -30f, 30f), 0, 0);
                parts.tail.localRotation = Quaternion.Euler(0, 0, 0);
                if (t >= 1f)
                {
                    state = S.Perched; idle = rng.Range(4f, 18f);
                    FaceAcross(targetPerch);
                }
            }
        }

        Perch targetPerch;

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1 - t;
            return u * u * a + 2 * u * t * b + t * t * c;
        }
    }

    // ---------------------------------------------------------------------------------- other hummingbirds
    public class HummerAI : Creature
    {
        HummerRig rig;
        HummingbirdAnimator anim;
        Flower flower;
        Vector3 vel;
        float feedTime, timer, yaw;
        enum S { Travel, Feed, Perch, Chase }
        S state;
        Perch perch;
        AudioSource buzz;

        public void Init(HummerSpecies sp, int seed)
        {
            rng = new Rng(seed);
            speciesId = sp.id;
            discoverDistance = 3.5f;
            rig = HummingbirdModel.Build(sp, transform, false);
            anim = gameObject.AddComponent<HummingbirdAnimator>();
            anim.rig = rig;
            anim.pose = BirdPose.Hover;
            buzz = AudioManager.I.CreateLoop(transform, "loop_wing", AudioCategory.Birds, 0f, 0.4f, 5f);
            buzz.pitch = rng.Range(0.9f, 1.2f);
        }

        public override bool ActiveNow(EnvironmentSystem env) => env.Night < 0.7f && env.Rain < 0.8f;

        public override void Respawn(Vector3 near)
        {
            Vector2 o = rng.InCircle * 35f;
            Vector3 p = TerrainField.Ground(near.x + o.x, near.z + o.y) + Vector3.up * rng.Range(1f, 3f);
            transform.position = p;
            PickFlower();
        }

        void PickFlower()
        {
            var fm = FlowerManager.I;
            Vector2 o = rng.InCircle * 12f;
            flower = fm ? fm.Nearest(transform.position + new Vector3(o.x, 0, o.y), 25f) : null;
            state = flower ? S.Travel : S.Perch;
            if (state == S.Perch)
            {
                if (WorldGenerator.I.NearestPerch(transform.position, 20f, out var p)) perch = p;
                else perch = new Perch { pos = transform.position, along = Vector3.right };
            }
            timer = rng.Range(4f, 9f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = transform.position;
            var player = BirdController.I;
            float toPlayer = player ? (player.transform.position - pos).magnitude : 999f;

            // territorial: chase the player away from "our" flower occasionally
            if (state == S.Feed && toPlayer < 0.6f && rng.Chance(dt * 0.8f)) { state = S.Chase; timer = 2.5f; AudioManager.I.PlayAt("sfx_bird_6", pos, AudioCategory.Birds, 0.7f, 1.1f, 25f); }

            Vector3 target = pos;
            switch (state)
            {
                case S.Travel:
                    if (!flower) { PickFlower(); break; }
                    target = flower.Mouth + flower.Approach * 0.1f;
                    if ((target - pos).magnitude < 0.04f) { state = S.Feed; feedTime = rng.Range(2f, 5f); }
                    break;
                case S.Feed:
                    if (!flower) { PickFlower(); break; }
                    Vector3 tipOff = rig.beakTip.position - pos;
                    target = flower.Mouth - tipOff + flower.Approach * 0.004f;
                    feedTime -= dt;
                    flower.Drink(0.4f * dt);
                    if (feedTime <= 0f || flower.nectar < 0.1f)
                    {
                        if (rng.Chance(0.3f) && WorldGenerator.I.NearestPerch(pos, 15f, out var np)) { perch = np; state = S.Perch; timer = rng.Range(4f, 10f); }
                        else PickFlower();
                    }
                    break;
                case S.Perch:
                    target = perch.pos + Vector3.up * 0.012f;
                    timer -= dt;
                    if (timer <= 0f || toPlayer < 0.5f) PickFlower();
                    break;
                case S.Chase:
                    timer -= dt;
                    if (player) target = player.transform.position + player.transform.forward * -0.15f;
                    if (timer <= 0f) PickFlower();
                    break;
            }

            Vector3 to = target - pos;
            float d = to.magnitude;
            float sp = state == S.Chase ? 7f : Mathf.Min(8f, d * 2f + 0.1f);
            Vector3 desired = d > 1e-4f ? to / d * sp : Vector3.zero;
            vel = Vector3.Lerp(vel, desired, 1f - Mathf.Exp(-dt * (state == S.Feed ? 12f : 3f)));
            pos += vel * dt;
            float g = TerrainField.Height(pos.x, pos.z) + 0.05f;
            if (pos.y < g) pos.y = g;
            transform.position = pos;

            Vector3 hv = new Vector3(vel.x, 0, vel.z);
            float targetYaw = yaw;
            if (state == S.Feed && flower) { var a = -flower.Approach; targetYaw = Mathf.Atan2(a.x, a.z) * Mathf.Rad2Deg; }
            else if (hv.magnitude > 0.4f) targetYaw = Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg;
            float dy = Mathf.Clamp(Mathf.DeltaAngle(yaw, targetYaw), -400f * dt, 400f * dt);
            yaw += dy;
            transform.rotation = Quaternion.Euler(0, yaw, 0);

            bool perched = state == S.Perch && d < 0.03f;
            anim.pose = perched ? BirdPose.Perched : state == S.Feed ? BirdPose.Feed : hv.magnitude > 1.8f ? BirdPose.Fly : BirdPose.Hover;
            anim.speed01 = Mathf.Clamp01(hv.magnitude / 8f);
            anim.turn = Mathf.Clamp(dy / dt / 200f, -1f, 1f);
            anim.climb = Mathf.Clamp(vel.y / 4f, -1, 1);
            anim.lookTarget = state == S.Feed && flower ? flower.Mouth : (toPlayer < 2f && player ? player.transform.position : (Vector3?)null);
            if (buzz) buzz.volume = anim.FlapIntensity * 0.45f * AudioManager.I.Gain(AudioCategory.Birds);
        }
    }

    /// <summary>
    /// Keeps a living population around the player: spawns, recycles far-away animals to new
    /// spots nearby, hides animals that sleep at night or shelter from rain, and reports discoveries.
    /// </summary>
    public class WildlifeManager : MonoBehaviour
    {
        public static WildlifeManager I { get; private set; }
        readonly List<Creature> all = new List<Creature>();
        int checkIndex;
        readonly Dictionary<string, float> seenTimers = new Dictionary<string, float>();
        float fireflyTimer;

        void Awake() { I = this; }

        public void Spawn(Vector3 start)
        {
            int seed = 9000;
            var q = QualityManager.I;
            float mult = q && q.Active == GraphicsPreset.Performance ? 0.6f : 1f;
            for (int i = 0; i < (int)(26 * mult); i++) Add<Butterfly>("Butterfly", start, 60f).Init(i % 4, seed++);
            for (int i = 0; i < (int)(8 * mult); i++) Add<Damselfly>("Damselfly", start, 90f).Init(seed++);
            for (int i = 0; i < (int)(18 * mult); i++) Add<OrchidBee>("Bee", start, 50f).Init(seed++);
            for (int i = 0; i < 4; i++) Add<ForestBird>("Macaw", start, 160f).Init("macaw", seed++);
            for (int i = 0; i < 4; i++) Add<ForestBird>("Toucan", start, 140f).Init("toucan", seed++);
            for (int i = 0; i < (int)(8 * mult); i++) Add<ForestBird>("Tanager", start, 90f).Init("tanager", seed++);
            for (int i = 0; i < (int)(6 * mult); i++) Add<HummerAI>("Hummer", start, 70f).Init(HummerSpecies.Others[i % HummerSpecies.Others.Length], seed++);
            foreach (var c in all) c.Respawn(start);
        }

        T Add<T>(string name, Vector3 start, float homeRadius) where T : Creature
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<T>();
            c.home = homeRadius;
            all.Add(c);
            return c;
        }

        void Update()
        {
            var env = EnvironmentSystem.I;
            var gm = GameManager.I;
            if (env == null || BirdController.I == null || all.Count == 0) return;
            Vector3 p = BirdController.I.transform.position;
            var cam = Camera.main;

            int n = Mathf.Min(8, all.Count);
            for (int i = 0; i < n; i++)
            {
                checkIndex = (checkIndex + 1) % all.Count;
                var c = all[checkIndex];
                bool want = env != null && c.ActiveNow(env);
                if (c.gameObject.activeSelf != want)
                {
                    c.gameObject.SetActive(want);
                    if (want) c.Respawn(p);
                }
                if (!want) continue;
                float d = Vector3.Distance(c.transform.position, p);
                if (d > c.home * 1.6f) c.Respawn(p);
            }

            // discovery: close and roughly in view for a moment
            if (gm == null || cam == null) return;
            float dt = Time.deltaTime;
            foreach (var c in all)
            {
                if (!c.gameObject.activeSelf || gm.Save.IsDiscovered(c.speciesId)) continue;
                Vector3 cp = c.transform.position;
                float d = Vector3.Distance(cp, cam.transform.position);
                Vector3 vp = cam.WorldToViewportPoint(cp);
                bool inView = vp.z > 0 && vp.x > 0.1f && vp.x < 0.9f && vp.y > 0.1f && vp.y < 0.9f;
                if (d < c.discoverDistance && inView)
                {
                    seenTimers.TryGetValue(c.speciesId, out float t);
                    t += dt;
                    seenTimers[c.speciesId] = t;
                    if (t > 0.8f) gm.Discover(c.speciesId);
                }
            }
            if (env.Night > 0.6f && env.Rain < 0.2f)
            {
                fireflyTimer += dt;
                if (fireflyTimer > 8f) gm.Discover("firefly");
            }
        }

        public Creature NearestInView(Camera cam, float maxDist)
        {
            Creature best = null; float bd = maxDist;
            foreach (var c in all)
            {
                if (!c.gameObject.activeSelf) continue;
                Vector3 vp = cam.WorldToViewportPoint(c.transform.position);
                if (vp.z <= 0 || vp.x < 0.2f || vp.x > 0.8f || vp.y < 0.2f || vp.y > 0.8f) continue;
                if (vp.z < bd) { bd = vp.z; best = c; }
            }
            return best;
        }
    }
}
