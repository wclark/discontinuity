using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    public static class PrototypeBuild
    {
        [MenuItem("Discontinuity/Prepare prototype")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var household = AssetDatabase.LoadAssetAtPath<Household>("Assets/Resources/Household.asset");
            if (household == null)
            {
                household = ScriptableObject.CreateInstance<Household>(); household.definition = HouseholdContent.Create();
                AssetDatabase.CreateAsset(household, "Assets/Resources/Household.asset");
            }
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/Panel.asset");
            if (panel == null) { panel = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(panel, "Assets/Resources/Panel.asset"); }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1440, 900);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = .5f;
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Resources/RuntimeTheme.tss");
            EditorUtility.SetDirty(panel);
            if (!File.Exists("Assets/Scenes/Household.unity"))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.12f, .16f, .14f); camera.orthographic = true;
                var go = new GameObject("Discontinuity Workbench"); var doc = go.AddComponent<UIDocument>(); doc.panelSettings = panel; go.AddComponent<Workbench>();
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/Household.unity");
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Household.unity", true) };
            PlayerSettings.companyName = "Discontinuity"; PlayerSettings.productName = "Discontinuity";
            PlayerSettings.bundleVersion = "0.3.0";
            foreach (string path in Directory.GetFiles("Assets/Resources/Scenes", "*.png").Concat(new[] { "Assets/Resources/Characters.png" }))
            {
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                textureImporter.textureType = TextureImporterType.Default;
                textureImporter.alphaIsTransparency = true;
                textureImporter.isReadable = path.EndsWith("Characters.png", StringComparison.Ordinal);
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.maxTextureSize = 4096;
                textureImporter.mipmapEnabled = false;
                textureImporter.npotScale = TextureImporterNPOTScale.None;
                textureImporter.SaveAndReimport();
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/DiscontinuityIcon.png");
            importer.isReadable = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024; importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/DiscontinuityIcon.png");
            foreach (var target in new[] { NamedBuildTarget.Unknown, NamedBuildTarget.Standalone })
                PlayerSettings.SetIcons(target, Enumerable.Repeat(icon, PlayerSettings.GetIconSizes(target, IconKind.Any).Length).ToArray(), IconKind.Any);
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        [MenuItem("Discontinuity/Verify simulation")]
        public static void Verify()
        {
            var data = HouseholdContent.Create(); int passed = 0;
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAILED: " + name); Debug.Log("PASS: " + name); passed++; };
            var sim = new Simulation(data); sim.Save.player = "";
            check(sim.Rank("clara").Find(o => o.choice.id == "wait").Score == 0, "zero baseline");
            var baseForecast = sim.Forecast();
            while (!sim.Ended) sim.Step();
            check(sim.State.Get("accused") == "jonah" && sim.State.Get("cleared") != "yes", "unmodified day accuses Jonah");
            check(string.Join(",", baseForecast.Select(e => e.action)) == string.Join(",", sim.State.events.Select(e => e.action)), "forecast equals resolved unmodified day");
            check(sim.State.Get("testimony") == "yes", "Merrow sees and reports the envelope through scheduled encounters");
            foreach (string action in new[] { "help", "mock", "threaten" })
            {
                sim = new Simulation(data);
                while (!sim.Ended) sim.Step(sim.State.turn == 1 ? action : null);
                check(sim.Save.guidance.Count == 1, action + " records only the single necessary adjustment");
                check(sim.Save.guidance[0].amount == 6, action + " gets gap plus one");
                sim.Begin("jonah");
                while (!sim.Ended) sim.Step();
                check(sim.State.events.Any(e => e.actor == "clara" && e.action == action && e.manual == 6), action + " recurs through NPC scoring");
                if (action == "help")
                {
                    check(sim.State.Get("cleared") == "yes", "kindness supplies the missing corroboration and clears Jonah");
                    var vouch = sim.State.events.Find(e => e.action == "vouch");
                    check(vouch != null && vouch.causes.Any(id => sim.State.events[id].action == "help"), "causal trace links vouch to help");
                }
                if (action == "mock") check(sim.State.Get("accused") == "clara", "humiliation moves the accusation to Clara");
                if (action == "threaten") check(sim.State.Get("address_copied") != "yes", "warning interrupts the Archive route");
                sim.Begin("clara"); check(!sim.Save.guidance.Any(g => g.actor == "clara"), "replaying clears only that character's adjustments");
            }
            sim = new Simulation(data); sim.Step("move:hall");
            check(sim.Save.guidance.Count == 0, "natural highest choice stores nothing");
            sim.Step("help");
            check(sim.Rank("clara").All(o => o.manual == 0), "current player never receives its own adjustments");
            sim.Begin("jonah"); sim.Step();
            sim.State.Set("owner:cloth", "kitchen");
            check(sim.Rank("clara").All(o => o.choice.id != "help"), "missing item invalidates a guided choice");
            sim = new Simulation(data); sim.Step("wait"); sim.Step("wait");
            sim.Begin("jonah"); sim.Step();
            check(sim.Rank("clara").Find(o => o.choice.id == "wait").manual <= 5, "repeat waiting never accumulates scores");
            sim = new Simulation(data); sim.Step("move:hall"); sim.Step("help");
            sim.Begin("jonah"); sim.Step(); sim.SetWeight("c02", 20);
            check(sim.Rank("clara")[0].choice.id == "ignore", "stronger condition can outweigh prior guidance");
            check(sim.NextRoom("kitchen", "archive") == "hall", "route expands into an adjacent graph step");
            sim = new Simulation(data); sim.Step("move:hall"); sim.Step("help");
            string unchanged = JsonUtility.ToJson(sim.Save); sim.Forecast();
            check(JsonUtility.ToJson(sim.Save) == unchanged, "forecast never mutates live state or guidance");
            var restored = new Simulation(data, JsonUtility.FromJson<Campaign>(unchanged));
            check(restored.State.Get("owner:cloth") == "jonah" && restored.Save.guidance[0].amount == 6, "JSON round trip preserves world and guidance");
            restored.Begin("jonah"); restored.Step();
            check(restored.Rank("clara")[0].choice.id == "help", "restored guidance affects NPC evaluation");
            restored.State.Set("at:jonah", "garden");
            check(!restored.Rank("clara").Any(o => o.choice.id == "help"), "absent person invalidates guided interaction");
            restored.Step(); restored.State.Set("at:jonah", "hall");
            check(restored.Rank("clara")[0].choice.id == "help", "one-turn delay still allows the authored interaction");
            restored.Step();
            check(restored.State.applied.Count == 1, "authored interaction applies only once");
            sim = new Simulation(data); sim.Step("wait"); sim.Step("wait");
            check(sim.Save.guidance.Count == 2 && sim.Save.guidance.All(g => g.amount == 5), "repeated choice records fixed increments, not accumulated amounts");
            check(sim.Save.guidance[0].until < sim.Save.guidance[1].from, "repeated decision windows never overlap");
            sim.Begin("jonah");
            check(sim.Rank("clara")[0].manual == 5, "first waiting decision uses exactly one increment");
            sim.Step(); check(sim.Rank("clara")[0].manual == 5, "next waiting decision still uses exactly one increment");
            sim = new Simulation(data); sim.Step("wait"); sim.Step("move:hall");
            check(sim.Save.guidance.Count == 1 && sim.Save.guidance[0].until == 0, "natural choice closes earlier interval without adding an adjustment");
            sim.Begin("jonah"); sim.Step();
            check(sim.Rank("clara")[0].choice.id == "move:hall", "natural continuation is not suppressed by expired waiting guidance");
            sim = new Simulation(data); sim.State.turn = 3; sim.Save.player = "clara";
            sim.State.Set("at:clara", "archive"); sim.State.Set("at:vale", "archive");
            sim.Step("take_envelope_clara");
            check(sim.State.Get("owner:envelope") == "clara", "simultaneous claims leave exactly one item owner");
            check(sim.State.events.Any(e => e.actor == "vale" && e.blocked), "losing item claim is revalidated and reported blocked");
            sim = new Simulation(data); sim.State.turn = 5; sim.State.Set("at:clara", "hall"); sim.State.Set("at:jonah", "hall");
            sim.State.Set("ledger_read", "yes"); sim.State.Set("address_copied", "yes"); sim.State.Set("trust", "yes"); sim.Step("ask");
            check(sim.State.Get("asked") == "yes" && sim.State.Get("answered") != "yes", "all actors decide on the same start-of-turn snapshot");
            check(sim.Rank("jonah")[0].choice.id == "vouch", "new facts influence next turn's ranking");
            sim.Step(); var historical = sim.State.events.Find(e => e.action == "vouch");
            check(historical.alternatives.Any(o => o.action == "refuse" && o.conditions == 5), "event retains alternative rankings for later inspection");
            var tied = new Simulation(data); tied.State.turn = 14; tied.State.Set("at:clara", "kitchen"); tied.Step("move:hall");
            check(tied.Save.guidance.Count == 0, "choice tied at highest score needs no increment");
            sim = new Simulation(data);
            check(!sim.ContinueAsNext() && sim.Save.player == "clara" && sim.Save.day == 1, "incarnation is locked until day completion");
            sim.Step();
            check(!sim.Experienced("clara").Any(e => e.action == "tend"), "offscreen treatment is not in Clara's experience");
            check(sim.Experienced("jonah").Any(e => e.action == "tend"), "Jonah witnesses treatment in the Garden");
            check(sim.Experienced("clara").Any(e => e.actor == "jonah" && e.action == "move:hall"), "arrival is visible to people already in the room");
            sim.Step("help");
            check(sim.Experienced("jonah").Any(e => e.action == "help"), "interaction target witnesses the kindness");
            var oldSave = JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(sim.Save));
            foreach (var e in oldSave.world.events) e.witnesses = null;
            var migrated = new Simulation(data, oldSave);
            check(string.Join(",", migrated.Experienced("clara").Select(e => e.id)) == string.Join(",", sim.Experienced("clara").Select(e => e.id)), "legacy saves recover the same witnessed events");
            while (!sim.Ended) sim.Step();
            check(sim.ContinueAsNext() && sim.Save.player == "jonah" && sim.State.turn == 0, "completed life advances to exactly the next incarnation");
            check(sim.Save.guidance.Any(g => g.actor == "clara" && g.amount == 6), "incarnation transition preserves other character adjustments");
            while (!sim.Ended) sim.Step();
            check(sim.ContinueAsNext() && sim.Save.player == "vale", "second completed life advances to Vale");
            sim = new Simulation(data); sim.Step();
            var arrival = sim.Experienced("clara").Find(e => e.actor == "jonah");
            check(Story.Text(sim, arrival, "clara") == "Jonah arrives from the Garden.", "arrival prose uses the observer's actual location");
            check(Story.Text(sim, arrival, "merrow") == "Jonah leaves for the Hall.", "departure prose differs for the person left behind");
            check(Story.Cast(data, arrival.sceneAfter, "garden", arrival).Any(p => p.id == "jonah"), "departing figure remains visible during the departure beat");
            check(!Story.Cast(data, Story.Snapshot(sim.State), "garden").Any(p => p.id == "jonah"), "departed figure is absent from the next scene");
            var first = sim.State.events.Find(e => e.actor == "clara");
            check(Story.Cast(data, first.sceneAfter, "hall", first).Count == 1, "event snapshot excludes a later arrival");
            sim.Step("help");
            check(sim.Experienced("clara").Any(e => e.turn == 1 && e.actor == "jonah" && e.action == "wait"), "co-located waiting is observable");
            var help = sim.State.events.Find(e => e.action == "help");
            check(Story.Get(help.sceneBefore, "owner:cloth") == "clara" && Story.Get(help.sceneAfter, "owner:cloth") == "jonah", "event records the visible item transfer");
            sim.Step(); sim.Step();
            var copy = sim.Experienced("clara").Find(e => e.action == "copy");
            check(copy != null && Story.Room(copy, "clara") == "archive", "Clara witnesses Jonah copying in the Archive");
            check(Story.Activity(sim, copy, data.people.Find(p => p.id == "jonah"), "archive") == "Copying the address", "scene activity comes from the resolved action definition");
            while (!sim.Ended) sim.Step();
            check(Story.Get(help.sceneAfter, "owner:cloth") == "jonah" && Story.Room(copy, "clara") == "archive", "later simulation cannot mutate earlier scene snapshots");
            sim.Save.reviewPending = true; sim.Save.reviewIndex = 1;
            var resume = new Simulation(data, JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(sim.Save)));
            check(resume.Save.reviewPending && resume.Save.reviewIndex == 1 && !resume.ContinueAsNext(), "pending story stage survives save and gates incarnation");
            var legacy = JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(sim.Save));
            foreach (var e in legacy.world.events) { e.sceneBefore = null; e.sceneAfter = null; }
            var recovered = new Simulation(data, legacy);
            check(JsonUtility.ToJson(recovered.State) == JsonUtility.ToJson(sim.State), "legacy presentation migration restores exact scene snapshots without changing live facts");
            foreach (var room in data.rooms)
            {
                var painting = Resources.Load<Texture2D>("Scenes/" + room.id);
                check(painting != null && painting.width >= 2000 && painting.height >= 700, room.id + " illustration is full resolution");
            }
            var cast = Resources.Load<Texture2D>("Characters");
            check(cast != null && cast.GetPixel(0, 0).a == 0, "character atlas has a transparent background");
            for (int i = 0; i < 4; i++)
            {
                var pixels = cast.GetPixels(i * cast.width / 4, 0, cast.width / 4, cast.height);
                check(pixels.Any(p => p.a > .9f) && pixels.Any(p => p.a < .1f), "character column " + i + " contains a separate cutout");
            }
            string output = Path.GetFullPath("../artifacts"); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "simulation-verification.txt"), passed + " checks passed.\n" + string.Join("\n", baseForecast.Select(e => Simulation.Clock(e.turn) + " " + e.actor + ": " + e.label)));
            Debug.Log("DISCONTINUITY_VERIFIED " + passed);
        }
        [MenuItem("Discontinuity/Build Windows prototype")]
        public static void Windows()
        {
            Prepare(); Verify();
            string path = Path.GetFullPath("../builds/Discontinuity/Discontinuity.exe"); Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Household.unity" }, locationPathName = path,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            ExportIcon(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/DiscontinuityIcon.png"), Path.Combine(Path.GetDirectoryName(path), "Discontinuity.ico"));
            Debug.Log("DISCONTINUITY_BUILD " + path);
        }
        static void ExportIcon(Texture2D source, string path)
        {
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
            var images = sizes.Select(size =>
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                    pixels[y * size + x] = source.GetPixelBilinear((x + .5f) / size, (y + .5f) / size);
                texture.SetPixels(pixels);
                byte[] png = texture.EncodeToPNG(); UnityEngine.Object.DestroyImmediate(texture); return png;
            }).ToArray();
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                    writer.Write(images[i].Length); writer.Write(offset); offset += images[i].Length;
                }
                foreach (byte[] png in images) writer.Write(png);
            }
        }
    }
}
