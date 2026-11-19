// HUMMINGBIRD X — Living Rainforest
// Editor build pipeline. Creates the boot scene from code, configures the Android
// player and builds an APK or AAB. Called by GameCI (buildMethod) or from the menu.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HummingbirdX.EditorTools
{
    public static class BuildScript
    {
        public const string ScenePath = "Assets/HummingbirdX/Scenes/Boot.unity";
        const string PackageId = "com.abhijit.hummingbirdx";

        [MenuItem("HUMMINGBIRD X/1. Create Boot Scene")]
        public static void CreateBootScene()
        {
            Directory.CreateDirectory("Assets/HummingbirdX/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("Bootstrap");
            go.AddComponent<HummingbirdX.Bootstrap>();

            // Fog must be enabled in a built scene, otherwise Unity strips the fog
            // shader variants that the runtime turns on later.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.ambientMode = AmbientMode.Trilight;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[HBX] Boot scene created at " + ScenePath);
        }

        [MenuItem("HUMMINGBIRD X/2. Configure Android Player")]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Abhijit";
            PlayerSettings.productName = "HUMMINGBIRD X";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.runInBackground = false;
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.02f, 0.05f, 0.04f);

            KeepShaderVariants();
            AssetDatabase.SaveAssets();
            Debug.Log("[HBX] Android player configured");
        }

        // Runtime-created materials are invisible to Unity's variant stripping,
        // so keep fog and instancing variants explicitly.
        static void KeepShaderVariants()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            SetInt(so, "m_FogStripping", 1);
            SetBool(so, "m_FogKeepLinear", true);
            SetBool(so, "m_FogKeepExp", true);
            SetBool(so, "m_FogKeepExp2", true);
            SetInt(so, "m_InstancingStripping", 2);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(SerializedObject so, string name, int v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.intValue = v;
        }

        static void SetBool(SerializedObject so, string name, bool v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.boolValue = v;
        }

        [MenuItem("HUMMINGBIRD X/3. Build APK")]
        public static void BuildApkMenu() => Build("Build/Android/HummingbirdX.apk", false, true);

        [MenuItem("HUMMINGBIRD X/4. Build App Bundle (AAB)")]
        public static void BuildAabMenu() => Build("Build/Android/HummingbirdX.aab", true, true);

        // Entry point for GameCI: -executeMethod HummingbirdX.EditorTools.BuildScript.BuildAndroid
        public static void BuildAndroid()
        {
            string path = Arg("-customBuildPath") ?? "Build/Android/HummingbirdX.apk";
            bool aab = (Arg("-androidExportType") ?? "").Equals("androidAppBundle", StringComparison.OrdinalIgnoreCase)
                       || path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase);

            string version = Arg("-buildVersion");
            if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
            if (int.TryParse(Arg("-androidVersionCode"), out int code) && code > 0)
                PlayerSettings.Android.bundleVersionCode = code;

            string ks = Arg("-androidKeystoreName");
            if (!string.IsNullOrEmpty(ks) && File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(ks);
                PlayerSettings.Android.keystorePass = Arg("-androidKeystorePass") ?? "";
                PlayerSettings.Android.keyaliasName = Arg("-androidKeyaliasName") ?? "";
                PlayerSettings.Android.keyaliasPass = Arg("-androidKeyaliasPass") ?? "";
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }

            if (aab && !path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
                path = Path.ChangeExtension(path, ".aab");
            if (!aab && !path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                path = Path.ChangeExtension(path, ".apk");

            Build(path, aab, false);
        }

        static void Build(string path, bool aab, bool interactive)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            CreateBootScene();
            ConfigurePlayer();
            EditorUserBuildSettings.buildAppBundle = aab;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[HBX] Build {s.result}: {path} size={s.totalSize / (1024 * 1024)} MB errors={s.totalErrors} time={s.totalTime}");

            if (!interactive)
                EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }

    // Import settings for generated textures and audio.
    public class HbxAssetPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("HummingbirdX/Resources/Textures")) return;
            var ti = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.anisoLevel = 4;
            ti.maxTextureSize = 2048;
            ti.isReadable = false;
            if (file.EndsWith("_n"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = !file.EndsWith("_mask");
                ti.alphaIsTransparency = file.Contains("leaf") || file.Contains("petal") || file.Contains("fern")
                                         || file.Contains("wing") || file.Contains("feather");
            }
            if (file.Contains("ui_")) { ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = false; }
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("HummingbirdX/Resources/Audio")) return;
            var ai = (AudioImporter)assetImporter;
            ai.forceToMono = !assetPath.Contains("amb_");
            var s = ai.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.loadType = assetPath.Contains("amb_") || assetPath.Contains("loop_")
                ? AudioClipLoadType.CompressedInMemory
                : AudioClipLoadType.DecompressOnLoad;
            ai.defaultSampleSettings = s;
            ai.loadInBackground = true;
        }
    }
}
