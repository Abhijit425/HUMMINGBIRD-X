using UnityEngine;

namespace HummingbirdX
{
    /// <summary>Particle effects built from code: waterfall mist, nectar sparkles, pollen, fireflies, rain.</summary>
    public static class Fx
    {
        static ParticleSystem Make(string name, Transform parent, Vector3 pos, Material mat, bool world = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.playOnAwake = true;
            main.loop = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.None;
            return ps;
        }

        public static ParticleSystem Mist(Transform parent, Vector3 pos, float width)
        {
            var ps = Make("Mist", parent, pos, Mats.ParticleAlpha);
            var main = ps.main;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(width * 0.35f, width * 0.8f);
            main.startColor = new Color(0.9f, 0.95f, 1f, 0.12f);
            main.maxParticles = 120;
            main.gravityModifier = -0.02f;
            var em = ps.emission;
            em.rateOverTime = Mathf.Clamp(width * 2.2f, 6f, 36f);
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Hemisphere;
            sh.radius = width * 0.5f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            ps.Play();

            // fine spray
            var spray = Make("Spray", parent, pos, Mats.ParticleAdd);
            var m2 = spray.main;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            m2.startColor = new Color(0.8f, 0.9f, 1f, 0.35f);
            m2.gravityModifier = 0.6f;
            m2.maxParticles = 200;
            var e2 = spray.emission;
            e2.rateOverTime = Mathf.Clamp(width * 6f, 15f, 90f);
            var s2 = spray.shape;
            s2.shapeType = ParticleSystemShapeType.Cone;
            s2.angle = 50f;
            s2.radius = width * 0.4f;
            spray.transform.rotation = Quaternion.Euler(-90f, 0, 0);
            spray.Play();
            return ps;
        }

        public static ParticleSystem NectarSparkle(Transform parent)
        {
            var ps = Make("NectarSparkle", parent, Vector3.zero, Mats.ParticleAdd);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.012f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f, 0.9f), new Color(1f, 0.6f, 0.9f, 0.9f));
            main.maxParticles = 80;
            main.gravityModifier = -0.01f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.012f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            ps.Play();
            return ps;
        }

        public static ParticleSystem RewardBurst(Transform parent)
        {
            var ps = Make("Reward", parent, Vector3.zero, Mats.ParticleAdd);
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.02f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f, 1f), new Color(0.6f, 1f, 0.8f, 1f));
            main.maxParticles = 60;
            main.gravityModifier = -0.05f;
            var em = ps.emission;
            em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.02f;
            return ps;
        }

        /// <summary>Floating pollen motes / fireflies around the camera.</summary>
        public static ParticleSystem Motes(Transform parent, bool fireflies)
        {
            var ps = Make(fireflies ? "Fireflies" : "Pollen", parent, Vector3.zero, Mats.ParticleAdd);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(fireflies ? 4f : 6f, fireflies ? 8f : 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, fireflies ? 0.35f : 0.1f);
            main.startSize = fireflies ? new ParticleSystem.MinMaxCurve(0.05f, 0.11f) : new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
            main.startColor = fireflies
                ? new ParticleSystem.MinMaxGradient(new Color(0.75f, 1f, 0.35f, 1f), new Color(1f, 0.95f, 0.45f, 1f))
                : new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.75f, 0.55f), new Color(1f, 1f, 1f, 0.3f));
            main.maxParticles = fireflies ? 140 : 220;
            var em = ps.emission;
            em.rateOverTime = fireflies ? 22f : 24f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = fireflies ? new Vector3(36f, 8f, 36f) : new Vector3(14f, 7f, 14f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = fireflies ? 0.6f : 0.12f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.2f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            if (fireflies)
                grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.1f, 0.3f), new GradientAlphaKey(1f, 0.45f),
                            new GradientAlphaKey(0.05f, 0.6f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            else
                grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            ps.Play();
            return ps;
        }

        public static ParticleSystem Rain(Transform parent)
        {
            var ps = Make("Rain", parent, Vector3.zero, Mats.ParticleMat(false, "ui_raindrop"));
            var main = ps.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = 0.025f; main.startSizeY = 0.6f; main.startSizeZ = 1f;
            main.startColor = new Color(0.75f, 0.82f, 0.9f, 0.45f);
            main.maxParticles = 3000;
            main.gravityModifier = 0f;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-15f, -12f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var em = ps.emission;
            em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(28f, 1f, 28f);
            sh.position = new Vector3(0, 12f, 0);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1f;
            ps.Play();
            return ps;
        }
    }
}
