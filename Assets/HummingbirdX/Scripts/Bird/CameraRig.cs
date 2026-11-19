using UnityEngine;

namespace HummingbirdX
{
    public enum CamMode { ThirdPerson, FirstPerson, Cinematic, Free }

    /// <summary>
    /// Third-person follow camera with collision, first-person "bird's eye" view, automatic
    /// cinematic shots with depth of field, and a free camera for photo mode.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }
        public Camera Cam { get; private set; }
        public PostFx Post { get; private set; }
        public CamMode Mode { get; private set; } = CamMode.ThirdPerson;
        public float HeadingYaw => yaw;
        public float PitchSin { get; private set; }
        public float Distance => dist;
        public float PhotoFov = 55f;

        float yaw, pitch = 12f, dist = 0.55f, targetDist = 0.55f;
        float noLookTimer;
        Vector3 vel;
        float shotTimer; int shot; Vector3 shotOffset;
        float fov = 60f;
        CamMode beforeCinematic = CamMode.ThirdPerson;
        public bool MenuOrbit;  // slow scenic orbit used behind the main menu

        public void Build()
        {
            I = this;
            Cam = gameObject.AddComponent<Camera>();
            Cam.tag = "MainCamera";
            gameObject.tag = "MainCamera";
            Cam.nearClipPlane = 0.012f;
            Cam.farClipPlane = 400f;
            Cam.clearFlags = CameraClearFlags.Skybox;
            Cam.allowHDR = true;
            Cam.allowMSAA = true;
            Cam.fieldOfView = 60f;
            gameObject.AddComponent<AudioListener>();
            Post = gameObject.AddComponent<PostFx>();
        }

        public void SetMode(CamMode m)
        {
            if (m == CamMode.Cinematic && Mode != CamMode.Cinematic) beforeCinematic = Mode;
            Mode = m;
            shotTimer = 0f;
            if (Post) Post.DofEnabled = m == CamMode.Cinematic || m == CamMode.Free;
            var b = BirdController.I;
            if (b && b.Rig != null)
            {
                bool fp = m == CamMode.FirstPerson;
                // hide our own head in first person so it doesn't fill the screen
                var hr = b.Rig.head.GetComponent<MeshRenderer>();
                if (hr) hr.shadowCastingMode = fp ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        public void CycleMode()
        {
            var next = Mode == CamMode.ThirdPerson ? CamMode.FirstPerson : Mode == CamMode.FirstPerson ? CamMode.Cinematic : CamMode.ThirdPerson;
            SetMode(next);
            UIManager.Toast(next == CamMode.ThirdPerson ? "Follow camera" : next == CamMode.FirstPerson ? "Hummingbird's-eye view" : "Cinematic camera");
        }

        public void ResetBehind()
        {
            var b = BirdController.I;
            if (b) yaw = b.transform.eulerAngles.y;
            pitch = 12f;
        }

        void LateUpdate()
        {
            var b = BirdController.I;
            if (b == null || b.Rig == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var input = InputRouter.I;
            var q = QualityManager.I;
            Cam.farClipPlane = (q ? q.DrawDistance : 260f) + 80f;

            Vector2 look = input ? input.Look : Vector2.zero;
            float sens = Settings.LookSensitivity;
            if (input && input.Consume(ref input.CameraCyclePressed)) CycleMode();
            if (input) targetDist = Mathf.Clamp(targetDist * (1f - input.Zoom), 0.18f, 3.5f);
            dist = Mathf.Lerp(dist, targetDist, 1f - Mathf.Exp(-dt * 8f));

            if (look.sqrMagnitude > 0.01f) noLookTimer = 0f; else noLookTimer += dt;
            yaw += look.x * sens * dt;
            pitch += look.y * sens * dt * (Settings.InvertY ? 1f : -1f);
            pitch = Mathf.Clamp(pitch, -70f, 80f);

            if (MenuOrbit) { MenuShot(b, dt); return; }

            switch (Mode)
            {
                case CamMode.ThirdPerson: ThirdPerson(b, dt); break;
                case CamMode.FirstPerson: FirstPerson(b, dt); break;
                case CamMode.Cinematic: Cinematic(b, dt); break;
                case CamMode.Free: Free(dt, input); break;
            }
            PitchSin = Mathf.Sin(-pitch * Mathf.Deg2Rad);
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, fov, 1f - Mathf.Exp(-dt * 4f));
        }

        void ThirdPerson(BirdController b, float dt)
        {
            Vector3 pivot = b.transform.position + Vector3.up * 0.025f;
            // drift behind the bird when flying fast and the player isn't steering the camera
            if (noLookTimer > 1.2f && b.Speed01 > 0.35f && b.State == BirdState.Flying)
            {
                float by = b.transform.eulerAngles.y;
                yaw = Mathf.LerpAngle(yaw, by, 1f - Mathf.Exp(-dt * 1.2f * b.Speed01));
                pitch = Mathf.Lerp(pitch, 10f, 1f - Mathf.Exp(-dt * 0.8f));
            }
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            float d = dist * (1f + b.Speed01 * 0.5f);
            Vector3 want = pivot - rot * Vector3.forward * d + Vector3.up * d * 0.12f;
            Vector3 dir = want - pivot;
            float len = dir.magnitude;
            if (Physics.SphereCast(pivot, 0.02f, dir / len, out var hit, len, ~(1 << FlowerManager.FlowerLayer), QueryTriggerInteraction.Ignore))
                want = pivot + dir / len * Mathf.Max(0.05f, hit.distance - 0.02f);
            float g = TerrainField.Height(want.x, want.z) + 0.03f;
            if (want.y < g) want.y = g;
            float follow = Mathf.Lerp(16f, 30f, b.Speed01);
            transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-dt * follow));
            transform.rotation = Quaternion.LookRotation((pivot + rot * Vector3.forward * 0.3f) - transform.position, Vector3.up);
            fov = 58f + b.Speed01 * 14f;
            if (Post) Post.FocusDistance = Vector3.Distance(transform.position, pivot);
        }

        void FirstPerson(BirdController b, float dt)
        {
            var head = b.Rig.head;
            transform.position = head.position + head.up * 0.006f + head.forward * 0.004f;
            // stabilised view: follow the head's heading, but let the player look around
            Quaternion headQ = Quaternion.Euler(0, b.transform.eulerAngles.y, 0);
            Quaternion want = headQ * Quaternion.Euler(pitch * 0.8f, Mathf.DeltaAngle(b.transform.eulerAngles.y, yaw) * 0.0f, 0);
            if (noLookTimer < 1.5f) want = Quaternion.Euler(pitch, yaw, 0);
            else yaw = Mathf.LerpAngle(yaw, b.transform.eulerAngles.y, 1f - Mathf.Exp(-dt * 3f));
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-dt * 10f));
            fov = 74f + b.Speed01 * 10f;
        }

        void Cinematic(BirdController b, float dt)
        {
            shotTimer -= dt;
            if (shotTimer <= 0f)
            {
                shotTimer = Random.Range(6f, 10f);
                shot = (shot + 1) % 5;
                float a = Random.Range(0f, 360f);
                shotOffset = shot switch
                {
                    0 => Quaternion.Euler(0, a, 0) * new Vector3(0, 0.05f, 0.45f),      // close orbit
                    1 => new Vector3(0.25f, -0.06f, 0.35f),                              // low front 3/4
                    2 => new Vector3(-0.5f, 0.08f, -0.2f),                               // side tracking
                    3 => Quaternion.Euler(0, a, 0) * new Vector3(0, 0.6f, 2.2f),         // wide establishing
                    _ => new Vector3(0.1f, 0.02f, 0.22f)                                 // macro portrait
                };
            }
            Vector3 pivot = b.transform.position + Vector3.up * 0.02f;
            Vector3 off = shot == 0 ? Quaternion.Euler(0, Time.time * 12f, 0) * shotOffset : b.transform.rotation * shotOffset;
            Vector3 want = pivot + off;
            float g = TerrainField.Height(want.x, want.z) + 0.05f;
            if (want.y < g) want.y = g;
            transform.position = Vector3.SmoothDamp(transform.position, want, ref vel, 0.35f, 30f, dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pivot - transform.position, Vector3.up), 1f - Mathf.Exp(-dt * 5f));
            fov = shot == 4 ? 35f : shot == 3 ? 55f : 45f;
            if (Post) Post.FocusDistance = Vector3.Distance(transform.position, pivot);
            yaw = transform.eulerAngles.y;
        }

        void Free(float dt, InputRouter input)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            transform.rotation = rot;
            if (input)
            {
                Vector3 mv = rot * new Vector3(input.Move.x, 0, input.Move.y) + Vector3.up * input.Vertical;
                transform.position += mv * 1.6f * dt;
            }
            Vector3 p = transform.position;
            float g = TerrainField.Height(p.x, p.z) + 0.03f;
            if (p.y < g) transform.position = new Vector3(p.x, g, p.z);
            fov = PhotoFov;
        }

        void MenuShot(BirdController b, float dt)
        {
            Vector3 pivot = b.transform.position + Vector3.up * 0.03f;
            float t = Time.unscaledTime * 6f;
            Vector3 want = pivot + Quaternion.Euler(0, t, 0) * new Vector3(0.0f, 0.08f, 0.6f);
            transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-dt * 3f));
            transform.rotation = Quaternion.LookRotation(pivot + Vector3.up * 0.06f - transform.position, Vector3.up);
            fov = 40f;
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, fov, 1f - Mathf.Exp(-dt * 4f));
            if (Post) { Post.DofEnabled = true; Post.FocusDistance = Vector3.Distance(transform.position, pivot); }
            yaw = transform.eulerAngles.y;
        }

        public void SnapToBird()
        {
            var b = BirdController.I;
            if (!b) return;
            ResetBehind();
            transform.position = b.transform.position - b.transform.forward * dist + Vector3.up * 0.1f;
            transform.LookAt(b.transform.position);
        }
    }
}
