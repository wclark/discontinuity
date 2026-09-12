using System;
using System.IO;
using System.Linq;
using UnityEditor;
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
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .16f, .14f); camera.orthographic = true;
            var go = new GameObject("Discontinuity Workbench"); var doc = go.AddComponent<UIDocument>(); doc.panelSettings = panel; go.AddComponent<Workbench>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Household.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Household.unity", true) };
            PlayerSettings.companyName = "Discontinuity"; PlayerSettings.productName = "Discontinuity";
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
            Debug.Log("DISCONTINUITY_BUILD " + path);
        }
    }
}
