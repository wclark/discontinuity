using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    public partial class Workbench
    {
        void ComposeCrossingFixture()
        {
            sim = new Simulation(sim.Data, new Campaign()); undo.Clear(); notice = "";
            sim.State.Set("at:clara", "hall"); sim.State.Set("at:jonah", "archive"); sim.State.Set("at:vale", "hall");
            sim.Save.rules.Add(new Rule { id = "cross_fixture", actor = "jonah", action = "move:hall", amount = 20 });
            Advance("move:archive");
        }
        IEnumerator CompositionSmoke(Action<bool, string> check)
        {
            sim = new Simulation(sim.Data, new Campaign()); undo.Clear(); notice = ""; Render(); yield return null;
            Click("Go to Hall"); yield return null;
            check(sim.State.turn == 1 && !sim.Save.reviewPending && root.Q<Button>(className: "moment-button") == null, "adjustment turns advance without next-moment confirmations");
            check(root.Query<Label>(className: "summary-event").ToList().Any(l => l.text.Contains("Jonah")), "the next decision screen includes witnessed results");
            Click("Give Jonah the clean cloth"); yield return null;
            check(sim.Save.adjustments.Single().amount == 6, "UI records only the necessary new winning amount");
            sim.Begin("clara"); sim.Step(); Render(); yield return null;
            check(root.Query<Button>(className: "choice").ToList()[0].text == "Give Jonah the clean cloth" && root.Query<Label>(className: "choice-score").ToList()[0].text == "6", "revisited character shows their retained adjustment in first place");
            Click("Decision factors: Give Jonah the clean cloth"); yield return null;
            check(root.Query<Label>(className: "adjustment").ToList().Any(l => l.text == "Increment if chosen: +0"), "inspector confirms that accepting the leader will not inflate weights");
            Click("Close decision factors"); Click("Give Jonah the clean cloth"); yield return null;
            check(sim.Save.adjustments.Single().amount == 6, "reaccepting the leader through UI keeps the original amount");
            sim.Begin("clara"); sim.Step(); Render(); yield return null;
            Click("Mock Jonah's ink-stained cuff"); yield return null;
            check(sim.Save.adjustments.Count == 2 && sim.Save.adjustments.Find(g => g.action == "mock").amount == 7, "changing choices retains the previous option's weight and exceeds it minimally");
            Click("Weights"); yield return null;
            check(root.Query<Label>(className: "weight-entry").ToList().Any(l => l.text.StartsWith("+6")) && root.Query<Label>(className: "weight-entry").ToList().Any(l => l.text.StartsWith("+7")), "weights panel retains amounts for both previously selected options");
            Click("Save all weights"); yield return null;
            string profilePath = Directory.GetFiles(RecordsDirectory, "*.weights.json").OrderByDescending(p => p).First();
            var profile = JsonUtility.FromJson<Campaign>(File.ReadAllText(profilePath));
            check(profile.adjustments.Count == 2 && profile.scenario.rules.Count > 20 && profile.world.events.Count == 0, "weight export includes all defaults and adjustments without a partial run");
            Click("Close decision factors"); Click("State records"); yield return null;
            root.Q<TextField>("Record file").value = profilePath; Click("Load record file"); yield return null;
            check(sim.State.turn == 2, "loading weights asks before beginning another pass");
            Click("Begin with these weights"); yield return null;
            check(sim.State.turn == 0 && sim.Save.adjustments.Count == 2, "weight set restores into a fresh pass with every adjustment intact");
            Click("Create action"); yield return null;
            root.Q<TextField>("Action label").value = "Check the kitchen latch";
            Click("Availability"); yield return null;
            check(root.Q<DropdownField>("Location") != null && root.Q<TextField>("Fact key") == null && root.Q<DropdownField>("Jonah") != null, "basic availability exposes location, time and presence, not arbitrary fact syntax");
            root.Q<DropdownField>("Clean cloth").value = "Have";
            root.Q<DropdownField>("Jonah").value = "Absent";
            root.Q<DropdownField>("Through").value = "08:00 / turn 1";
            Click("Effects & prose"); yield return null;
            root.Q<TextField>("As actor").value = "You check the latch."; root.Q<TextField>("As observer").value = "Clara checks the latch.";
            Click("Save action"); yield return null;
            var custom = sim.Save.choices.Last();
            check(!custom.once && custom.requires.All(Simulation.Spatial) && custom.until == 0, "new actions have simple presence requirements and no hidden one-shot gate");
            Click("Decision factors: Check the kitchen latch"); yield return null;
            check(root.Query<Label>(className: "criterion-met").ToList().Any(l => l.text.Contains("Cloth") || l.text.Contains("cloth")), "simplified requirements are inspectable against actual possessions");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/composition-inspector-" + Screen.width + "x" + Screen.height + ".png"));
            for (int i = 0; i < 10; i++) yield return null;
            Click("Edit action"); Click("Availability"); yield return new WaitForSecondsRealtime(.1f);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/composition-requirements-" + Screen.width + "x" + Screen.height + ".png"));
            for (int i = 0; i < 10; i++) yield return null;
            Click("Cancel");
            sim = new Simulation(sim.Data, new Campaign()); Render(); yield return null;
            Click("Follow top choices"); yield return new WaitForSecondsRealtime(1.4f);
            Click("Pause"); yield return null;
            check(sim.State.turn == 1 && sim.Save.adjustments.Count == 0 && !riding, "passive ride follows the leader without creating adjustments and can be paused");
            ComposeCrossingFixture(); yield return new WaitForSecondsRealtime(.1f);
            check(sim.InTransit && root.Q<Label>(className: "time").text == "08:07:30", "crossing UI displays the midpoint clock");
            check(root.Query<Button>(className: "choice").ToList().Any(b => b.text == "Keep going") && root.Q<Button>(className: "moment-button") == null, "crossing offers direct reactions instead of a recap button");
            check(root.Query<Label>(className: "cast-name").ToList().Count == 2 && root.Q<Image>(className: "room-painting").image != null, "midpoint depicts only the actual travelers");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/composition-crossing-" + Screen.width + "x" + Screen.height + ".png"));
            for (int i = 0; i < 10; i++) yield return null;
            Click("State records"); Click("Record current state"); yield return null;
            string pendingPath = Directory.GetFiles(RecordsDirectory, "*.json").Where(p => !p.EndsWith(".weights.json")).OrderByDescending(p => p).First();
            Click("Close decision factors"); Click("Offer Jonah the cloth"); yield return null;
            check(sim.State.turn == 1 && sim.State.Get("owner:cloth") == "jonah" && sim.State.Get("at:clara") == "archive", "selecting a passing reaction completes the journey and its effect");
            Click("State records"); yield return null;
            root.Q<TextField>("Record file").value = pendingPath; Click("Load record file"); Click("Restore recorded state"); yield return null;
            check(sim.InTransit && sim.State.Get("owner:cloth") == "clara", "restoring a midpoint record does not skip or repeat the reaction");
            Click("Keep going"); yield return null;
            check(sim.State.turn == 1 && sim.Save.adjustments.Count == 1, "accepting the zero-ranked first reaction adds no weight");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/composition-turn-" + Screen.width + "x" + Screen.height + ".png"));
            for (int i = 0; i < 10; i++) yield return null;
            ComposeCrossingFixture(); yield return null;
            Click("Create action"); yield return null;
            check(root.Q<DropdownField>("Moment").value == "Crossing", "new actions created mid-passage default to crossing reactions");
            root.Q<TextField>("Action label").value = "Ask about the errand";
            root.Q<DropdownField>("Target").value = "Jonah";
            Click("Effects & prose"); yield return null;
            root.Q<TextField>("As actor").value = "You ask about the errand.";
            root.Q<TextField>("As target").value = "Clara asks about your errand.";
            root.Q<TextField>("As observer").value = "Clara asks Jonah about the errand.";
            Click("Save action"); yield return null;
            check(sim.InTransit && sim.Rank("clara").Any(o => o.choice.label == "Ask about the errand"), "an authored crossing reaction becomes usable without resolving the encounter");
            Click("Ask about the errand"); yield return null;
            check(sim.State.turn == 1 && sim.State.events.Any(e => e.kind == "reaction" && e.label == "Ask about the errand"), "new crossing reactions execute through the same UI and resolver");
        }
    }
}
