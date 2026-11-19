using UnityEngine;

namespace HummingbirdX
{
    public enum BirdPose { Hover, Fly, Boost, Dive, Perched, Preen, Feed, TakeOff, Landing }

    /// <summary>
    /// Fully procedural animation for a hummingbird rig: figure-eight hovering strokes with motion
    /// blur, forward-flight strokes, folded wings when perched, saccadic head movement with head
    /// stabilisation, tail pumping/fanning, body bob, preening and feeding.
    /// </summary>
    public class HummingbirdAnimator : MonoBehaviour
    {
        public HummerRig rig;
        public BirdPose pose = BirdPose.Perched;
        public float speed01;          // 0 hover .. 1 max speed
        public float climb;            // -1 dive .. 1 climb
        public float turn;             // -1 .. 1
        public Vector3? lookTarget;    // world point the bird is interested in
        public float flapRateScale = 1f;
        public float FlapIntensity { get; private set; }

        float phase, bodyPitch, bodyRoll, tailSpread = 1f, tailPitch, feetOut, foldBlend = 1f, blurAlpha;
        float flapHz, ampSweep, ampElev, ampTwist, strokeTilt;
        Quaternion headLocal = Quaternion.identity, headTarget = Quaternion.identity;
        float saccadeTimer, preenTimer, preenSide = 1f, fluff;
        Vector3 saccadeOffset;

        void Update()
        {
            if (rig == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;

            // ------------------------------------------------ target pose parameters
            float tHz, tSweep, tElev, tTwist, tTilt, tPitch, tFold, tSpread, tFeet;
            switch (pose)
            {
                case BirdPose.Hover:
                case BirdPose.Feed:
                    tHz = 21f; tSweep = 62f; tElev = 10f; tTwist = 55f; tTilt = 0f;
                    tPitch = -38f + speed01 * 20f; tFold = 0f; tSpread = 1.35f; tFeet = 0f; break;
                case BirdPose.Fly:
                    tHz = 17f; tSweep = Mathf.Lerp(45f, 25f, speed01); tElev = Mathf.Lerp(25f, 50f, speed01); tTwist = 30f; tTilt = Mathf.Lerp(20f, 60f, speed01);
                    tPitch = Mathf.Lerp(-25f, 8f, speed01) - climb * 18f; tFold = 0f; tSpread = Mathf.Lerp(1.1f, 0.75f, speed01); tFeet = 0f; break;
                case BirdPose.Boost:
                    tHz = 26f; tSweep = 25f; tElev = 55f; tTwist = 25f; tTilt = 65f;
                    tPitch = 12f - climb * 20f; tFold = 0f; tSpread = 0.65f; tFeet = 0f; break;
                case BirdPose.Dive:
                    tHz = 5f; tSweep = 10f; tElev = 12f; tTwist = 10f; tTilt = 70f;
                    tPitch = 35f; tFold = 0.55f; tSpread = 0.6f; tFeet = 0f; break;
                case BirdPose.TakeOff:
                    tHz = 28f; tSweep = 70f; tElev = 30f; tTwist = 55f; tTilt = 15f;
                    tPitch = -50f; tFold = 0f; tSpread = 1.4f; tFeet = 0.4f; break;
                case BirdPose.Landing:
                    tHz = 23f; tSweep = 68f; tElev = 15f; tTwist = 55f; tTilt = 5f;
                    tPitch = -45f; tFold = 0f; tSpread = 1.6f; tFeet = 1f; break;
                case BirdPose.Preen:
                    tHz = 0f; tSweep = 0f; tElev = 0f; tTwist = 0f; tTilt = 0f;
                    tPitch = -18f; tFold = 1f; tSpread = 1.05f; tFeet = 1f; break;
                default: // perched
                    tHz = 0f; tSweep = 0f; tElev = 0f; tTwist = 0f; tTilt = 0f;
                    tPitch = -22f; tFold = 1f; tSpread = 0.9f; tFeet = 1f; break;
            }
            tHz *= flapRateScale;
            float k = 1f - Mathf.Exp(-dt * 10f);
            flapHz = Mathf.Lerp(flapHz, tHz, 1f - Mathf.Exp(-dt * 14f));
            ampSweep = Mathf.Lerp(ampSweep, tSweep, k);
            ampElev = Mathf.Lerp(ampElev, tElev, k);
            ampTwist = Mathf.Lerp(ampTwist, tTwist, k);
            strokeTilt = Mathf.Lerp(strokeTilt, tTilt, k);
            foldBlend = Mathf.Lerp(foldBlend, tFold, 1f - Mathf.Exp(-dt * (tFold > foldBlend ? 6f : 18f)));
            tailSpread = Mathf.Lerp(tailSpread, tSpread, k);
            feetOut = Mathf.Lerp(feetOut, tFeet, 1f - Mathf.Exp(-dt * 6f));
            FlapIntensity = Mathf.Clamp01(flapHz / 22f) * (1f - foldBlend);

            // ------------------------------------------------ wings
            phase += dt * flapHz * Mathf.PI * 2f;
            if (phase > 1000f) phase = Mathf.Repeat(phase, Mathf.PI * 2f * 64f);
            float sweep = Mathf.Sin(phase) * ampSweep;
            float elev = Mathf.Sin(phase * 2f) * ampElev * 0.25f + Mathf.Cos(phase) * ampElev * 0.75f * (strokeTilt / 60f);
            float twist = Mathf.Cos(phase) * ampTwist;

            Quaternion flapR = WingRot(1f, sweep + strokeTilt * 0.2f, elev + 10f, twist);
            Quaternion flapL = WingRot(-1f, sweep + strokeTilt * 0.2f, elev + 10f, twist);
            // folded: wings swept back over the tail
            float preenLift = pose == BirdPose.Preen ? (Mathf.Sin(Time.time * 1.7f) * 0.5f + 0.5f) * 25f : 0f;
            Quaternion foldR = WingRot(1f, -96f, -8f + (preenSide > 0 ? preenLift : 0f), 15f);
            Quaternion foldL = WingRot(-1f, -96f, -8f + (preenSide < 0 ? preenLift : 0f), 15f);
            rig.wingR.localRotation = Quaternion.Slerp(flapR, foldR, foldBlend);
            rig.wingL.localRotation = Quaternion.Slerp(flapL, foldL, foldBlend);

            // motion-blur discs follow the stroke plane
            blurAlpha = Mathf.Lerp(blurAlpha, FlapIntensity, k);
            Quaternion blurQ = WingRot(1f, strokeTilt * 0.2f, 10f + strokeTilt * 0.15f, 0f);
            Quaternion blurQL = WingRot(-1f, strokeTilt * 0.2f, 10f + strokeTilt * 0.15f, 0f);
            rig.blurR.localRotation = blurQ;
            rig.blurL.localRotation = blurQL;
            float sweepScale = Mathf.Clamp(ampSweep / 70f, 0.2f, 1f);
            rig.blurR.localScale = new Vector3(1f, 1f, sweepScale);
            rig.blurL.localScale = new Vector3(1f, 1f, sweepScale);
            bool blurOn = blurAlpha > 0.05f;
            if (rig.blurR.gameObject.activeSelf != blurOn) { rig.blurR.gameObject.SetActive(blurOn); rig.blurL.gameObject.SetActive(blurOn); }
            rig.blurMatR.SetFloat("_Opacity", blurAlpha * 0.32f);
            // the real wing flickers semi-transparent when beating fast (stroboscopic)
            rig.wingMat.SetFloat("_Opacity", Mathf.Lerp(1f, 0.55f + 0.25f * Mathf.Abs(Mathf.Sin(phase * 0.5f)), FlapIntensity));

            // ------------------------------------------------ body
            float bob = (pose == BirdPose.Hover || pose == BirdPose.Feed) ? Mathf.Sin(Time.time * 2.6f) * 0.0012f + Mathf.Sin(phase) * 0.0002f : 0f;
            if (pose == BirdPose.Fly || pose == BirdPose.Boost) bob = Mathf.Sin(phase) * 0.0006f;
            bodyPitch = Mathf.Lerp(bodyPitch, tPitch, 1f - Mathf.Exp(-dt * 6f));
            float tRoll = -turn * Mathf.Lerp(15f, 55f, speed01);
            if (pose == BirdPose.Perched || pose == BirdPose.Preen) tRoll = 0;
            bodyRoll = Mathf.Lerp(bodyRoll, tRoll, 1f - Mathf.Exp(-dt * 5f));
            fluff = Mathf.MoveTowards(fluff, 0f, dt * 2f);
            rig.body.localRotation = Quaternion.Euler(bodyPitch, 0, bodyRoll);
            rig.body.localPosition = new Vector3(0, bob, 0);
            rig.body.localScale = Vector3.one * (1f + fluff * 0.09f);

            // ------------------------------------------------ tail: pumping and fanning
            float pump = (pose == BirdPose.Hover || pose == BirdPose.Feed) ? Mathf.Sin(Time.time * 3.3f) * 10f : 0f;
            if (pose == BirdPose.Perched) pump = Mathf.Sin(Time.time * 0.9f) * 3f;
            tailPitch = Mathf.Lerp(tailPitch, pump - bodyPitch * 0.35f + climb * 10f, k);
            rig.tail.localRotation = Quaternion.Euler(tailPitch, 0, 0);
            rig.tail.localScale = new Vector3(tailSpread, 1f, 1f);

            // ------------------------------------------------ feet
            rig.feet.localScale = Vector3.one * Mathf.Max(0.001f, feetOut);
            rig.feet.gameObject.SetActive(feetOut > 0.05f);

            // ------------------------------------------------ head
            UpdateHead(dt);
        }

        static Quaternion WingRot(float side, float sweepFwd, float elev, float twist)
        {
            return Quaternion.AngleAxis(side * elev, Vector3.forward) * Quaternion.AngleAxis(-side * sweepFwd, Vector3.up) * Quaternion.AngleAxis(twist, Vector3.right);
        }

        void UpdateHead(float dt)
        {
            saccadeTimer -= dt;
            if (pose == BirdPose.Preen)
            {
                preenTimer -= dt;
                if (preenTimer <= 0f)
                {
                    preenTimer = Random.Range(1.2f, 3f);
                    preenSide = Random.value < 0.5f ? -1f : 1f;
                    if (Random.value < 0.25f) fluff = 1f;
                }
                // head turned back to the wing/breast with quick nibbling
                float nib = Mathf.Sin(Time.time * 18f) * 6f;
                headTarget = Quaternion.Euler(35f + nib, preenSide * 115f, preenSide * 20f);
                headLocal = Quaternion.Slerp(headLocal, headTarget, 1f - Mathf.Exp(-dt * 8f));
                rig.head.localRotation = headLocal;
                return;
            }

            if (saccadeTimer <= 0f)
            {
                // hummingbirds glance around in quick snaps
                saccadeTimer = pose == BirdPose.Feed ? Random.Range(0.6f, 1.4f) : Random.Range(0.35f, 1.6f);
                float amt = pose == BirdPose.Perched ? 1f : 0.45f;
                saccadeOffset = new Vector3(Random.Range(-14f, 14f) * amt, Random.Range(-35f, 35f) * amt, Random.Range(-10f, 10f) * amt);
                if (pose == BirdPose.Feed) saccadeOffset *= 0.2f;
            }

            Quaternion desiredWorld;
            if (lookTarget.HasValue && pose != BirdPose.Boost)
            {
                Vector3 dir = lookTarget.Value - rig.head.position;
                if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;
                desiredWorld = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
            else
            {
                // stabilised: keep the head level with the horizon while the body pitches
                Vector3 fwd = transform.forward; fwd.y = 0;
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                desiredWorld = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(-climb * 15f, turn * 30f, 0);
            }
            desiredWorld *= Quaternion.Euler(saccadeOffset);

            Quaternion parentRot = rig.head.parent.rotation;
            Quaternion local = Quaternion.Inverse(parentRot) * desiredWorld;
            Vector3 e = local.eulerAngles;
            e.x = ClampAngle(e.x, -60f, 60f);
            e.y = ClampAngle(e.y, -95f, 95f);
            e.z = ClampAngle(e.z, -30f, 30f);
            headTarget = Quaternion.Euler(e);
            // fast snap (saccade) then hold
            headLocal = Quaternion.Slerp(headLocal, headTarget, 1f - Mathf.Exp(-dt * 22f));
            if (pose == BirdPose.Feed) headLocal *= Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 1.5f, 0, 0);
            rig.head.localRotation = headLocal;
        }

        static float ClampAngle(float a, float min, float max)
        {
            if (a > 180f) a -= 360f;
            return Mathf.Clamp(a, min, max);
        }
    }
}
