using System;
using UnityEngine;

namespace HummingbirdX
{
    public enum BirdState { Perched, TakingOff, Flying, Feeding, AutoNav, Landing }

    /// <summary>
    /// Player hummingbird flight model: smooth acceleration with inertia, natural deceleration,
    /// true hovering (including sideways/backwards flight like a real hummingbird), banking,
    /// wind and air resistance, sphere-cast collision with sliding, perching, take-off,
    /// landing, tap-to-fly to flowers and nectar feeding.
    /// </summary>
    public class BirdController : MonoBehaviour
    {
        public static BirdController I { get; private set; }

        public HummerRig Rig { get; private set; }
        public HummingbirdAnimator Anim { get; private set; }
        public BirdState State { get; private set; } = BirdState.Perched;
        public Vector3 Velocity;
        public float Speed01 { get; private set; }
        public bool InputEnabled = true;
        public bool Autopilot;
        public bool Frozen;
        public Flower FeedingFlower { get; private set; }
        public Flower NavFlower { get; private set; }
        public float StateTime { get; private set; }

        public float MaxSpeed = 9f, BoostSpeed = 17f, ClimbSpeed = 4.2f, Radius = 0.045f;

        public event Action<Flower, float> NectarCollected;
        public event Action<Flower> FlowerEmptied;
        public event Action Landed, TookOff;

        float yaw, yawRate, hoverLock, boostEnergy = 1f, preenTimer = 6f;
        Perch perch, navPerch;
        bool navToPerch;
        Vector3 landStart; float landT;
        AudioSource wingAudio;
        int collideMask;
        float autoNavTimeout;
        Vector3 autoWander;

        void Awake()
        {
            I = this;
            collideMask = ~((1 << FlowerManager.FlowerLayer) | (1 << 2));
        }

        public void Build()
        {
            Rig = HummingbirdModel.Build(HummerSpecies.Player, transform);
            Anim = gameObject.AddComponent<HummingbirdAnimator>();
            Anim.rig = Rig;
            wingAudio = AudioManager.I.CreateLoop(transform, "loop_wing", AudioCategory.Birds, 0f, 0.5f, 6f);
        }

        // ------------------------------------------------------------------ public commands
        public void PlaceOnPerch(Perch p)
        {
            perch = p;
            Vector3 face = Vector3.Cross(p.along, Vector3.up);
            if (face.sqrMagnitude < 0.01f) face = Vector3.forward;
            yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
            transform.position = p.pos + Vector3.up * 0.012f;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            Velocity = Vector3.zero;
            SetState(BirdState.Perched);
        }

        public void PlaceFlying(Vector3 pos, float yawDeg)
        {
            transform.position = pos;
            yaw = yawDeg;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            Velocity = Vector3.zero;
            SetState(BirdState.Flying);
        }

        public void NavigateTo(Flower f)
        {
            if (f == null) return;
            NavFlower = f; navToPerch = false;
            autoNavTimeout = 25f;
            if (State == BirdState.Perched) TakeOffNow();
            SetState(BirdState.AutoNav);
        }

        public void NavigateTo(Perch p)
        {
            navPerch = p; navToPerch = true; NavFlower = null;
            autoNavTimeout = 25f;
            if (State == BirdState.Perched) TakeOffNow();
            SetState(BirdState.AutoNav);
        }

        public void RequestLand()
        {
            var fm = FlowerManager.I;
            Vector3 p = transform.position;
            if (WorldGenerator.I.NearestPerch(p, 12f, out var per, x => x.pos.y > p.y - 12f))
            {
                NavigateTo(per);
                return;
            }
            var f = fm ? fm.Nearest(p, 4f) : null;
            if (f != null) { NavigateTo(f); return; }
            float g = GroundHeight(p);
            if (p.y - g < 4f)
            {
                NavigateTo(new Perch { pos = new Vector3(p.x, g + 0.01f, p.z) + transform.forward * 0.3f, along = transform.right });
                return;
            }
            UIManager.Toast("No perch nearby — fly closer to a branch");
        }

        void TakeOffNow()
        {
            SetState(BirdState.TakingOff);
            Velocity = Vector3.up * 1.2f + transform.forward * 0.4f;
            AudioManager.I.PlayAt("sfx_whoosh", transform.position, AudioCategory.Birds, 0.35f);
            TookOff?.Invoke();
        }

        void SetState(BirdState s)
        {
            State = s;
            StateTime = 0f;
            if (s != BirdState.Feeding) FeedingFlower = null;
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            if (Rig == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (Frozen || dt <= 0f)
            {
                if (wingAudio) wingAudio.volume = 0f;
                return;
            }
            StateTime += dt;
            var input = InputRouter.I;
            Vector2 move = Vector2.zero; float vert = 0f; bool boost = false;
            if (InputEnabled && !Autopilot && input != null)
            {
                move = input.Move; vert = input.Vertical; boost = input.BoostHeld;
                if (input.Consume(ref input.HoverPressed)) { hoverLock = 0.8f; if (State == BirdState.AutoNav) SetState(BirdState.Flying); }
                if (input.Consume(ref input.LandPressed)) RequestLand();
            }
            if (Autopilot) AutopilotInput(ref move, ref vert, ref boost);
            bool anyInput = move.sqrMagnitude > 0.02f || Mathf.Abs(vert) > 0.1f || boost;

            switch (State)
            {
                case BirdState.Perched: UpdatePerched(dt, anyInput); break;
                case BirdState.TakingOff:
                    Integrate(dt, move, vert + 0.6f, false, 3f);
                    if (StateTime > 0.35f) SetState(BirdState.Flying);
                    break;
                case BirdState.Flying:
                    Integrate(dt, move, vert, boost, hoverLock > 0f ? 9f : 0f);
                    TryAutoFeed();
                    break;
                case BirdState.AutoNav:
                    if (anyInput && !Autopilot) { SetState(BirdState.Flying); break; }
                    UpdateAutoNav(dt);
                    break;
                case BirdState.Landing: UpdateLanding(dt); break;
                case BirdState.Feeding: UpdateFeeding(dt, move, vert); break;
            }
            hoverLock -= dt;
            boostEnergy = Mathf.Clamp01(boostEnergy + (boost ? -dt * 0.18f : dt * 0.25f));

            UpdateAnimator(dt, boost, vert);
            UpdateAudio();
            SetShaderGlobals();
            if (GameManager.I != null && State != BirdState.Perched)
            {
                GameManager.I.Save.distanceFlown += Velocity.magnitude * dt;
                GameManager.I.Save.secondsFlown += dt;
            }
        }

        void UpdatePerched(float dt, bool anyInput)
        {
            Velocity = Vector3.zero;
            transform.position = perch.pos + Vector3.up * 0.012f;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            preenTimer -= dt;
            if (anyInput) { TakeOffNow(); return; }
        }

        float CamYaw()
        {
            var cr = CameraRig.I;
            return cr ? cr.HeadingYaw : yaw;
        }

        void Integrate(float dt, Vector2 move, float vert, bool boost, float extraBrake)
        {
            float cy = CamYaw();
            Quaternion camQ = Quaternion.Euler(0, cy, 0);
            Vector3 fwd = camQ * Vector3.forward, right = camQ * Vector3.right;
            float mag = Mathf.Clamp01(move.magnitude);
            bool canBoost = boost && boostEnergy > 0.05f;
            float top = canBoost ? BoostSpeed : MaxSpeed;
            Vector3 desired = (right * move.x + fwd * move.y);
            if (desired.sqrMagnitude > 1f) desired.Normalize();
            desired *= top * Mathf.Lerp(0.15f, 1f, mag);
            if (canBoost && mag < 0.1f) desired = transform.forward * BoostSpeed; // double-tap boost dashes forward

            // camera-guided altitude: looking up/down while flying forward climbs/dives
            float camPitch = CameraRig.I ? CameraRig.I.PitchSin : 0f;
            float guided = Mathf.Max(0f, move.y) * camPitch * top * 0.55f;
            float vy = vert * ClimbSpeed * (canBoost ? 1.5f : 1f) + guided;
            desired.y = vy;

            // wind drift and resistance
            if (EnvironmentSystem.I) desired += EnvironmentSystem.I.WindVector * 0.12f;
            float k = mag > 0.05f || Mathf.Abs(vert) > 0.05f ? (canBoost ? 3.2f : 2.4f) : 3.6f;
            k += extraBrake;
            Vector3 dv = (desired - Velocity) * (1f - Mathf.Exp(-dt * k));
            Velocity += dv;
            float sp = Velocity.magnitude;
            Velocity -= Velocity * (sp * 0.0045f * dt); // quadratic air drag

            // heading: fast flight faces velocity, slow flight faces the camera so you can strafe or fly backwards
            Vector3 hv = new Vector3(Velocity.x, 0, Velocity.z);
            float targetYaw = yaw;
            if (hv.magnitude > 2.6f) targetYaw = Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg;
            else if (mag > 0.05f) targetYaw = cy;
            float maxTurn = Mathf.Lerp(420f, 200f, Mathf.Clamp01(hv.magnitude / BoostSpeed)) * dt;
            float delta = Mathf.Clamp(Mathf.DeltaAngle(yaw, targetYaw), -maxTurn, maxTurn);
            yaw += delta;
            yawRate = Mathf.Lerp(yawRate, delta / Mathf.Max(dt, 1e-4f), 1f - Mathf.Exp(-dt * 6f));
            transform.rotation = Quaternion.Euler(0, yaw, 0);

            MoveWithCollision(Velocity * dt);
            Speed01 = Mathf.Clamp01(hv.magnitude / MaxSpeed);
        }

        void MoveWithCollision(Vector3 delta)
        {
            Vector3 pos = transform.position;
            for (int it = 0; it < 3 && delta.sqrMagnitude > 1e-10f; it++)
            {
                float dist = delta.magnitude;
                Vector3 dir = delta / dist;
                if (Physics.SphereCast(pos, Radius, dir, out var hit, dist + 0.01f, collideMask, QueryTriggerInteraction.Ignore))
                {
                    float allowed = Mathf.Max(0f, hit.distance - 0.01f);
                    pos += dir * allowed;
                    Vector3 remain = delta - dir * allowed;
                    delta = Vector3.ProjectOnPlane(remain, hit.normal);
                    Velocity = Vector3.ProjectOnPlane(Velocity, hit.normal) * 0.92f;
                }
                else { pos += delta; break; }
            }

            // ground, water, ceiling and world bounds
            float g = GroundHeight(pos);
            if (pos.y < g + 0.05f) { pos.y = g + 0.05f; if (Velocity.y < 0) Velocity.y = 0; }
            float wd = TerrainField.WaterDistance(pos.x, pos.z, out float lvl);
            if (wd < 0f && pos.y < lvl + 0.07f) { pos.y = lvl + 0.07f; if (Velocity.y < 0) Velocity.y = 0; }
            if (pos.y > 160f) { pos.y = 160f; Velocity.y = Mathf.Min(0, Velocity.y); }
            float lim = 378f;
            if (Mathf.Abs(pos.x) > lim) { pos.x = Mathf.Sign(pos.x) * lim; Velocity.x *= -0.3f; }
            if (Mathf.Abs(pos.z) > lim) { pos.z = Mathf.Sign(pos.z) * lim; Velocity.z *= -0.3f; }
            transform.position = pos;
        }

        public static float GroundHeight(Vector3 p)
        {
            float h = TerrainField.Height(p.x, p.z);
            if (p.y - h < 4f && Physics.Raycast(new Vector3(p.x, h + 3f, p.z), Vector3.down, out var hit, 6f, 1, QueryTriggerInteraction.Ignore))
                h = hit.point.y;
            return h;
        }

        void TryAutoFeed()
        {
            if (Velocity.magnitude > 1.6f || FlowerManager.I == null) return;
            var tip = Rig.beakTip.position;
            var f = FlowerManager.I.Nearest(tip, 0.16f);
            if (f == null) return;
            Vector3 toMouth = (f.Mouth - transform.position); toMouth.y = 0;
            if (Vector3.Dot(transform.forward, toMouth.normalized) < 0.2f) return;
            StartFeeding(f);
        }

        void StartFeeding(Flower f)
        {
            FeedingFlower = f;
            SetState(BirdState.Feeding);
            Velocity = Vector3.zero;
        }

        void UpdateFeeding(float dt, Vector2 move, float vert)
        {
            var f = FeedingFlower;
            if (f == null) { SetState(BirdState.Flying); return; }
            Vector3 ap = f.Approach; ap.y = 0;
            if (ap.sqrMagnitude < 0.01f) ap = -transform.forward;
            ap.Normalize();
            float targetYaw = Mathf.Atan2(-ap.x, -ap.z) * Mathf.Rad2Deg;
            yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, 360f * dt);
            transform.rotation = Quaternion.Euler(0, yaw, 0);

            // put the beak tip into the flower's mouth, with a gentle in-out probing motion
            Vector3 tipOffset = Rig.beakTip.position - transform.position;
            float probe = Mathf.Sin(StateTime * 5f) * 0.003f;
            Vector3 target = f.Mouth - tipOffset + ap * (0.004f + probe);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-dt * 10f));

            if (StateTime > 0.25f)
            {
                float got = f.Drink(1.4f * dt);
                if (got > 0f)
                {
                    NectarCollected?.Invoke(f, got);
                    var sp = FlowerManager.I.Sparkle;
                    sp.transform.position = f.Mouth;
                    sp.Emit(1);
                }
                if (f.nectar <= 0.01f)
                {
                    FlowerEmptied?.Invoke(f);
                    ExitFeeding();
                    return;
                }
            }
            if (move.sqrMagnitude > 0.1f || Mathf.Abs(vert) > 0.2f) ExitFeeding();
        }

        void ExitFeeding()
        {
            SetState(BirdState.Flying);
            Velocity = -transform.forward * 0.8f + Vector3.up * 0.2f;
            hoverLock = 0.4f;
        }

        void UpdateAutoNav(float dt)
        {
            autoNavTimeout -= dt;
            Vector3 target;
            if (navToPerch) target = navPerch.pos + Vector3.up * 0.06f;
            else
            {
                if (NavFlower == null) { SetState(BirdState.Flying); return; }
                Vector3 ap = NavFlower.Approach; ap.y = Mathf.Clamp(ap.y, -0.2f, 0.4f);
                target = NavFlower.Mouth + ap.normalized * 0.12f;
            }
            Vector3 to = target - transform.position;
            float d = to.magnitude;
            if (autoNavTimeout < 0f) { SetState(BirdState.Flying); return; }
            if (d < (navToPerch ? 0.08f : 0.04f))
            {
                if (navToPerch)
                {
                    landStart = transform.position; landT = 0;
                    perch = navPerch;
                    SetState(BirdState.Landing);
                }
                else StartFeeding(NavFlower);
                return;
            }
            // arrive: slow down near the target, rise above obstacles on long trips
            float speed = Mathf.Min(MaxSpeed * 0.9f, d * 1.6f + 0.15f);
            Vector3 desired = to / d * speed;
            if (d > 6f)
            {
                float g = GroundHeight(transform.position);
                if (transform.position.y - g < 1.5f) desired.y += 2f;
            }
            Velocity = Vector3.Lerp(Velocity, desired, 1f - Mathf.Exp(-dt * 3f));
            Vector3 hv = new Vector3(Velocity.x, 0, Velocity.z);
            float targetYaw = hv.magnitude > 0.3f ? Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg : yaw;
            if (d < 0.6f && !navToPerch)
            {
                Vector3 ap = -NavFlower.Approach; ap.y = 0;
                if (ap.sqrMagnitude > 0.01f) targetYaw = Mathf.Atan2(ap.x, ap.z) * Mathf.Rad2Deg;
            }
            float delta = Mathf.Clamp(Mathf.DeltaAngle(yaw, targetYaw), -300f * dt, 300f * dt);
            yaw += delta;
            yawRate = Mathf.Lerp(yawRate, delta / Mathf.Max(dt, 1e-4f), 1f - Mathf.Exp(-dt * 6f));
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            MoveWithCollision(Velocity * dt);
            Speed01 = Mathf.Clamp01(hv.magnitude / MaxSpeed);
        }

        void UpdateLanding(float dt)
        {
            landT += dt / 0.55f;
            Vector3 end = perch.pos + Vector3.up * 0.012f;
            float e = Mathf.SmoothStep(0, 1, landT);
            transform.position = Vector3.Lerp(landStart, end, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.02f;
            Vector3 face = Vector3.Cross(perch.along, Vector3.up);
            if (face.sqrMagnitude > 0.01f)
            {
                // face whichever side of the branch is closer to our heading
                if (Vector3.Dot(face, transform.forward) < 0) face = -face;
                float ty = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
                yaw = Mathf.MoveTowardsAngle(yaw, ty, 400f * dt);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
            Velocity = Vector3.zero;
            if (landT >= 1f)
            {
                SetState(BirdState.Perched);
                preenTimer = UnityEngine.Random.Range(5f, 10f);
                Landed?.Invoke();
            }
        }

        void UpdateAnimator(float dt, bool boost, float vert)
        {
            var a = Anim;
            a.speed01 = Speed01;
            a.climb = Mathf.Clamp(Velocity.y / ClimbSpeed, -1f, 1f);
            a.turn = Mathf.Clamp(yawRate / 180f, -1f, 1f);
            switch (State)
            {
                case BirdState.Perched:
                    if (preenTimer < 0f)
                    {
                        a.pose = BirdPose.Preen;
                        if (preenTimer < -UnityEngine.Random.Range(5f, 9f)) preenTimer = UnityEngine.Random.Range(6f, 14f);
                    }
                    else a.pose = BirdPose.Perched;
                    preenTimer -= 0f;
                    break;
                case BirdState.TakingOff: a.pose = BirdPose.TakeOff; break;
                case BirdState.Landing: a.pose = BirdPose.Landing; break;
                case BirdState.Feeding: a.pose = BirdPose.Feed; break;
                default:
                    float hs = new Vector2(Velocity.x, Velocity.z).magnitude;
                    if (boost && hs > MaxSpeed * 0.8f) a.pose = BirdPose.Boost;
                    else if (Velocity.y < -ClimbSpeed * 1.2f && hs > 4f) a.pose = BirdPose.Dive;
                    else if (hs > 1.8f) a.pose = BirdPose.Fly;
                    else a.pose = BirdPose.Hover;
                    break;
            }
            // what the bird is looking at
            if (State == BirdState.Feeding && FeedingFlower) a.lookTarget = FeedingFlower.Mouth;
            else if (State == BirdState.AutoNav && NavFlower) a.lookTarget = NavFlower.Mouth;
            else if (State == BirdState.Flying && Speed01 < 0.3f && FlowerManager.I)
            {
                var f = FlowerManager.I.Nearest(transform.position, 1.5f);
                a.lookTarget = f ? f.Mouth : (Vector3?)null;
            }
            else a.lookTarget = null;
        }

        void UpdateAudio()
        {
            if (!wingAudio) return;
            float fi = Anim.FlapIntensity;
            wingAudio.volume = fi * 0.55f * AudioManager.I.Gain(AudioCategory.Birds);
            wingAudio.pitch = 0.9f + Speed01 * 0.25f + (Anim.pose == BirdPose.Boost ? 0.15f : 0f) + (Anim.pose == BirdPose.TakeOff ? 0.2f : 0f);
        }

        void SetShaderGlobals()
        {
            Shader.SetGlobalVector("_HBX_PlayerPos", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0.9f));
            float push = State == BirdState.Perched ? 0f : Mathf.Lerp(1f, 0.5f, Speed01) * Anim.FlapIntensity;
            Shader.SetGlobalFloat("_HBX_PlayerPush", push);
        }

        // ------------------------------------------------------------------ relaxation autopilot
        void AutopilotInput(ref Vector2 move, ref float vert, ref bool boost)
        {
            // fly a gentle scenic loop: visit flowers, rise over trees, perch for a while
            if (State == BirdState.Perched && StateTime > UnityEngine.Random.Range(8f, 14f)) { TakeOffNow(); return; }
            if (State == BirdState.Flying && StateTime > 1.5f)
            {
                var fm = FlowerManager.I;
                var f = fm ? fm.Nearest(transform.position + transform.forward * 15f, 30f) : null;
                if (f != null && UnityEngine.Random.value < 0.7f) NavigateTo(f);
                else if (WorldGenerator.I.NearestPerch(transform.position + transform.forward * 12f + autoWander, 25f, out var p)) NavigateTo(p);
                autoWander = new Vector3(UnityEngine.Random.Range(-20f, 20f), 0, UnityEngine.Random.Range(-20f, 20f));
            }
            if (State == BirdState.Feeding && StateTime > 4f) ExitFeeding();
        }
    }
}
