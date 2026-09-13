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
        IEnumerator AuthoringSmoke(Action<bool, string> check)
        {
            Fixture("kitchen"); Render(); yield return null;
            string unchanged = JsonUtility.ToJson(sim.Save);
            Click("Create action"); yield return null;
            Click("Save action"); yield return null;
            check(modal.Q<Label>(className: "editor-error").text.Contains("label"), "empty action cannot be saved and displays validation");
            check(JsonUtility.ToJson(sim.Save) == unchanged, "failed form submission cannot change the world");
            Click("Cancel"); Click("Create action"); yield return null;
            root.Q<TextField>("Action label").value = "Check the kitchen latch";
            Click("Availability"); Click("+ Condition"); yield return null;
            var condition = root.Q<VisualElement>(className: "condition-row");
            condition.Q<DropdownField>("Person").value = "Jonah";
            condition.Q<Toggle>("Is not").value = true;
            Click("Score"); Click("+ Score condition"); yield return null;
            root.Q<FloatField>("Points").value = 9;
            Click("+ Condition"); yield return null;
            root.Q<VisualElement>(className: "condition-row").Q<DropdownField>("Condition").value = "Item owner";
            yield return null;
            root.Q<VisualElement>(className: "condition-row").Q<DropdownField>("Item").value = "Clean cloth";
            Click("Effects & prose"); yield return null;
            root.Q<TextField>("As actor").value = "You check the kitchen latch. It is secure.";
            root.Q<TextField>("As observer").value = "Clara checks the kitchen latch.";
            Click("+ Fact change"); yield return null;
            root.Q<TextField>("Set fact").value = "latch_checked";
            root.Q<TextField>("To value").value = "yes";
            foreach (string tab in new[] { "Availability", "Score", "Effects & prose" })
            {
                Click(tab); yield return new WaitForSecondsRealtime(.1f);
                check(root.Q<Button>(className: "save-action").worldBound.yMax <= root.worldBound.yMax, tab + " editor footer remains visible");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/editor-" + tab.Replace(" & ", "-").ToLowerInvariant() + "-" + Screen.width + "x" + Screen.height + ".png"));
                for (int frame = 0; frame < 10; frame++) yield return null;
            }
            Click("Action"); yield return new WaitForSecondsRealtime(.1f);
            check(root.Q<Button>(className: "save-action").worldBound.yMax <= root.worldBound.yMax, "editor save footer is on screen");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/action-editor-" + Screen.width + "x" + Screen.height + ".png"));
            for (int frame = 0; frame < 10; frame++) yield return null;
            Click("Save action"); yield return null;
            check(modal == null && sim.State.turn == 0 && sim.Save.choices.Count == 1, "saving a new action does not advance the turn");
            check(root.Query<Button>(className: "choice").ToList()[0].text == "Check the kitchen latch" && root.Query<Label>(className: "choice-score").ToList()[0].text == "9", "authored action immediately heads the scored list");
            sim = new Simulation(sim.Data, Copy(sim.Save)); Render(); yield return null;
            check(root.Query<Button>(className: "choice").ToList()[0].text == "Check the kitchen latch", "authored form data survives save reload");
            Click("Decision factors: Check the kitchen latch"); yield return null;
            check(root.Query<Label>(className: "criterion-met").ToList().Any(l => l.text.Contains("Jonah")) && root.Query<Label>(className: "factor-copy").ToList().Any(l => l.text.Contains("latch checked")), "inspector exposes authored presence gates and effects");
            Click("Edit action"); Click("Score"); yield return null;
            check(root.Q<FloatField>("Points").value == 9, "existing custom score condition opens for editing");
            root.Q<FloatField>("Points").value = 12;
            Click("Cancel"); yield return null;
            check(sim.Rank("clara")[0].Score == 9, "cancelling an edit leaves the definition unchanged");
            Click("Decision factors: Check the kitchen latch"); Click("Edit action"); Click("Score"); yield return null;
            root.Q<FloatField>("Points").value = 10;
            Click("Save action"); yield return null;
            check(sim.Rank("clara")[0].Score == 10, "saved score edits immediately update the action ranking");
            Click("Undo last turn"); yield return null;
            check(sim.Rank("clara")[0].Score == 9 && !sim.Save.frozenScenario, "undo restores score edits without pinning an ordinary save");
            Click("State records"); Click("Record current state"); yield return null;
            string recordPath = Directory.GetFiles(RecordsDirectory, "*.json").OrderByDescending(p => p).First();
            check(File.ReadAllText(recordPath).Contains("latch_checked"), "record includes authored conditions and effects");
            Click("Close decision factors"); Click("Check the kitchen latch"); yield return ReadTurn();
            check(sim.State.Get("latch_checked") == "yes" && sim.Save.guidance.Single().amount == 1, "new action executes effects and records strict winning increment");
            Click("Actions"); yield return null;
            root.Q<TextField>("Action search").value = "latch";
            check(root.Query<Label>(className: "criterion-unmet").ToList().Any(l => l.text.Contains("slot")), "catalog explains why a used action is unavailable");
            Click("Close decision factors"); Click("State records"); yield return null;
            root.Q<TextField>("Record file").value = recordPath;
            Click("Load record file"); yield return null;
            check(sim.State.turn == 1, "loading a record requires confirmation before replacing the run");
            Click("Restore recorded state"); yield return null;
            check(sim.State.turn == 0 && sim.State.Get("latch_checked") == "" && sim.Rank("clara")[0].Score == 9, "restoring a record restores time, facts, and action definitions");
            Click("Undo last turn"); yield return null;
            check(sim.State.turn == 1 && sim.State.Get("latch_checked") == "yes", "record restoration is undoable");
            Click("State records"); yield return null;
            root.Q<TextField>("Record file").value = Path.Combine(RecordsDirectory, "does-not-exist.json");
            Click("Load record file"); yield return null;
            check(sim.State.turn == 1 && sim.State.Get("latch_checked") == "yes" && modal.Q<Label>(className: "editor-error") != null, "bad record import preserves the current life and displays an error");
            Click("Close decision factors");
            Fixture("kitchen"); Render(); yield return null;
            Click("Decision factors: Go to Hall"); Click("Edit action"); Click("+ Score condition"); yield return null;
            root.Q<FloatField>("Points").value = 2;
            Click("Save action"); yield return null;
            check(sim.Rank("clara")[0].Score == 6, "generated movement also accepts authored score conditions");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/scored-actions-" + Screen.width + "x" + Screen.height + ".png"));
            for (int frame = 0; frame < 10; frame++) yield return null;
        }
    }
}
