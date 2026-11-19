# HUMMINGBIRD X — Living Rainforest

An Android tablet nature simulation built with **Unity 6.3 LTS (6000.3.8f1)**. You play a hummingbird
flying freely through a procedurally built Amazon rainforest: hover at flowers and drink nectar, explore
waterfalls and giant trees, photograph wildlife and fill a journal, by day and by night, in sun and rain.

> **Status: Milestone 1.** All systems below are written and the C# compiles against Unity 6 reference
> assemblies in CI. The first full Unity build runs once you add your Unity licence (see Setup). Expect a
> fix-up round after that first build: shaders and the editor build script are only compiled by Unity itself.

---

## 1. Setup: get the APK built by GitHub (one time, ~10 minutes)

The repo builds the **APK** and **AAB** automatically on every push using
[GameCI](https://game.ci). It needs your free Unity Personal licence as three repository secrets.

1. Install **Unity Hub** on your PC and sign in with your Unity account.
2. Hub → **Preferences → Licenses → Add → Get a free personal license**.
3. Open `C:\ProgramData\Unity\Unity_lic.ulf` in Notepad (the folder is hidden; type the path in Explorer).
4. On GitHub, open this repo → **Settings → Secrets and variables → Actions → New repository secret**, then add:
   | Secret | Value |
   |---|---|
   | `UNITY_LICENSE` | the whole contents of `Unity_lic.ulf` |
   | `UNITY_EMAIL` | your Unity account email |
   | `UNITY_PASSWORD` | your Unity account password |
5. Go to **Actions → Build Android (APK + AAB) → Run workflow**.
   The first build takes about 40–70 minutes because Unity imports everything; later builds are faster.

Without these secrets, only the quick *compile-check* job runs and the APK/AAB build is skipped. You'll see a yellow warning.

### Optional: sign the AAB for Google Play
Add `ANDROID_KEYSTORE_BASE64` (your `.keystore` base64-encoded), `ANDROID_KEYSTORE_PASS`,
`ANDROID_KEYALIAS_NAME` and `ANDROID_KEYALIAS_PASS`. Without them the build is debug-signed: fine for
installing on your tablet, not accepted by Play.

## 2. Install on your tablet

1. When the workflow finishes, open **Releases** (right side of the repo page) → newest
   `HUMMINGBIRD X build N` → download `HummingbirdX.apk`.
   (Also available under **Actions → the run → Artifacts**.)
2. On the tablet, allow *Install unknown apps* for your browser or file manager, open the APK and install.
3. Requirements: Android 8.0+ (API 26), 64-bit ARM, OpenGL ES 3.2 or Vulkan. 6 GB+ RAM recommended for *Ultra*.

## 3. Open in the Unity Editor (optional)

Open the folder with Unity **6000.3.8f1** (add Android Build Support in Hub). Then use the menu
**HUMMINGBIRD X → 1. Create Boot Scene**, open `Assets/HummingbirdX/Scenes/Boot.unity` and press Play.
Editor controls: WASD move, Space/E up, Ctrl/Q down, Shift boost, H hover, L land, C camera, right mouse look,
left click a flower to fly to it. Menu items 3 and 4 build the APK and AAB locally.

---

## 4. What's in Milestone 1

| Area | Implemented |
|---|---|
| **Hummingbird** | Sculpted procedural model (metallic emerald plumage, iridescent thin-film gorget, glossy eyes, slender bill, 8-feather tail fan, feathered wings) with fully procedural animation: figure-8 hover stroke with motion-blur discs, forward-flight strokes, boost, dive, take-off, landing with feet out, perching with folded wings, preening (head turns to the wing, nibbling, fluffing), nectar feeding (bill in the flower, probing), saccadic head movement with head stabilisation, tail pumping/fanning, banking |
| **Flight** | Inertia, smooth acceleration, natural braking, true hover, sideways and backwards flight when slow, banking, camera-guided climb, wind drift, air drag, sphere-cast collision with sliding, ground/water/ceiling/world limits |
| **Controls** | Floating left joystick, right camera joystick, UP/DOWN, BOOST, HOVER, LAND, camera cycle (follow / bird's-eye first person / cinematic), swipe to look, pinch to zoom, double-tap to boost, tap a flower to fly to it, Android back button |
| **World** | 800 × 800 m: terrain with cliffs and plateau, river system, plunge pool and valley lake, the Great Emerald Falls plus two secret waterfalls, giant emergent trees with buttress roots, canopy trees, palms, lianas, heliconias, banana plants, monstera, ferns, rocks, glowing mushrooms, flower carpets, light shafts, pollen, fireflies, mist and spray |
| **Regions** | Emerald Forest · Flower Valley · Waterfall Paradise · Rainforest Canopy · Golden Sunset · Mystical Night Forest, plus a hidden Spirit Grotto |
| **Flowers** | 8 species: Scarlet Bush, Lobster-claw Heliconia, Golden Shrimp Plant, Blue Ginger, Red Passionflower, Purple Glory Trumpet, Moonflower (opens at night), Spirit Orchid (secret). Different nectar amounts and refill rates; they sway, tremble in the bird's downwash, sparkle while you drink and burst when emptied. About 700 interactive flowers |
| **Wildlife** | Blue Morpho, Postman, Glasswing and Owl butterflies; Helicopter Damselfly; Orchid Bees; Scarlet Macaws; White-throated Toucans; Paradise Tanagers; four AI hummingbird species (White-necked Jacobin, Fork-tailed Woodnymph, Long-tailed Hermit, Black-eared Fairy) that feed, perch and chase you off their flowers; fireflies. Birds fly perch to perch, call, and flee when you come close. Butterflies visit flowers |
| **Graphics** | PBR surface shaders, wind-reactive two-sided foliage with translucency, bird push on leaves, wetness in rain, iridescent feathers, water with fresnel sky reflections, procedural sky with sun, moon, stars and clouds, realtime reflection probe, soft shadows, HDR bloom, ACES tone mapping, colour grading, vignette, depth of field (cinematic and photo modes), GPU-instanced vegetation with LODs and distance culling |
| **Presets** | Ultra / High / Performance, automatic device detection and an adaptive governor that steps down if the frame rate stays below ~42 fps |
| **Day & weather** | Sunrise → night cycle (adjustable day length), Sunny, Cloudy, Light rain, Heavy rain with thunder and lightning, Misty morning, After the rain. Changes lighting, fog, wind, wetness, wildlife, flowers and sound |
| **Audio** | Original synthesised soundscape: day and night forest beds, 3D rivers and waterfalls (louder as you approach), cicadas in the trees, spatial bird calls (dawn chorus, quieter in rain), wind stronger above the canopy, rain, thunder, wing hum, nectar, reward and camera sounds. Volume controls: master, nature, birds, water, wind & rain, interface, plus mute |
| **Gameplay** | Free exploration, nectar collecting, region unlocks for fast travel (flying there is never blocked), species discovery, secret waterfalls and grotto, wildlife journal, photo mode (frozen time, free camera, zoom, focus, blur, exposure, hide UI, super-sampled capture up to ~8K width where the device allows), Flight School (9 steps), Relaxation mode (autopilot plus cinematic camera) |
| **UI** | Dark translucent glass panels, Inter typeface, fades, auto-dimming minimal HUD, safe-area aware |
| **Performance** | Instanced rendering, LOD switching, chunk/frustum culling, round-robin updates, 30 fps cap in menus, pause/resume and save on background |

## 5. Assets and licensing

Everything shipped in this repo is original or openly licensed:
- **Textures**: generated by `tools/gen_textures.py` (bark, moss, leaf litter, rock, leaf atlas, fern, petals, feathers, butterfly wings, water, icons).
- **Audio**: synthesised by `tools/gen_audio.py` (no recordings).
- **3D models**: generated in code at startup (`PlantFactory`, `HummingbirdModel`, `WildlifeModels`, `Flowers`).
- **Font**: Inter, SIL Open Font License (`Assets/HummingbirdX/Resources/Fonts/OFL-LICENSE.txt`).

### Upgrading toward photo-real (optional, free-first)
Procedural models look good but are not scanned assets. To push realism further, these sources are legitimate.
**Always check the licence on each item before importing.**

| Need | Source | Licence |
|---|---|---|
| Ground, bark, rock, moss PBR textures, sky HDRIs | [Poly Haven](https://polyhaven.com), [ambientCG](https://ambientcg.com) | CC0 (free, no attribution) |
| Hummingbird model (free) | CGTrader “Rufous Hummingbird” by VPArt (free) | Royalty Free (no AI) |
| Hummingbird model (rigged and animated) | Sketchfab, filtered to *Downloadable* and a CC licence, e.g. user SabininAA's animated hummingbirds; check each licence | CC-BY etc. (credit the author) |
| Higher-end hummingbird | CGTrader “Animated Hummingbird” (about $79), Blender Market “Animated Hummingbird” (NoneCG) | Royalty free (paid) |
| Real rainforest recordings | [Freesound](https://freesound.org), filtered to CC0 | CC0 |
| Tropical plants and trees | Unity Asset Store, *Free Assets* filter, "tropical" / "jungle" | Unity Asset Store EULA |

Replace a generated texture by dropping a file with the same name into `Assets/HummingbirdX/Resources/Textures`
(names ending in `_n` are imported as normal maps automatically).

## 6. Project layout

```
Assets/HummingbirdX/
  Editor/BuildScript.cs          boot scene creation, Android player config, APK/AAB build, import settings
  Resources/Shaders/             foliage, bark, terrain, feather, water, sky, particles, light shafts, waterfall, wings, post
  Resources/Textures, Audio, Fonts
  Scripts/Core/                  Bootstrap, GameManager, Settings + SaveData, QualityManager, MeshBuilder, Noise, Res/Mats
  Scripts/World/                 TerrainField (shape of the world), WorldGenerator (terrain, water, instancing), PlantFactory, Flowers
  Scripts/Bird/                  HummingbirdModel, HummingbirdAnimator, BirdController, CameraRig, InputRouter
  Scripts/Wildlife/              WildlifeModels, Wildlife (butterflies, damselflies, bees, birds, AI hummingbirds, manager)
  Scripts/Environment/           EnvironmentSystem (day/night + weather), PostFx, Fx (particles)
  Scripts/Audio/AudioManager.cs
  Scripts/UI/                    UIKit, UIManager, HudScreen (+ touch gestures), Screens (pause, settings, map, journal, photo, flight school)
ci/                              licence-free C# compile check used by the workflow
tools/                           texture + audio generators
```

### Technical notes
- **Rendering pipeline:** the Built-in pipeline with custom shaders. A URP asset can't be created reliably without opening the editor, and a broken one turns every material pink. Moving to URP is a later option.
- **No hand-written scene files:** the boot scene is created by the editor build script, and everything else is generated at runtime from a fixed seed, so the forest is identical on every device.
- **Unity 6 vs plain Kotlin:** Unity's Android player is the Kotlin/Java host; all gameplay is C#.
