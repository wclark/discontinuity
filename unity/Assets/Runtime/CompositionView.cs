using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    public partial class Workbench
    {
        bool riding;
        Coroutine ride;
        string EventClock(Event e) { return e.kind == "crossing" || e.kind == "reaction" ? Simulation.MidpointClock(e.turn) : Simulation.Clock(e.turn); }
        void ReplayCurrent()
        {
            try { WriteRecord(); sim.Begin(sim.Save.player); undo.Clear(); notice = ""; Persist(); Render(); }
            catch (System.Exception ex) { notice = "Could not record this pass: " + ex.Message; Render(); }
        }
        void ToggleRide()
        {
            riding = !riding;
            if (riding && ride == null) ride = StartCoroutine(Ride());
            Render();
        }
        IEnumerator Ride()
        {
            while (riding)
            {
                yield return new WaitForSecondsRealtime(1.25f);
                if (!riding) break;
                if (modal != null) continue;
                if (sim.Ended || sim.InTransit) { riding = false; break; }
                Advance(null);
            }
            ride = null;
        }
        void RenderComposition(VisualElement side, VisualElement world, Event crossing)
        {
            Text(side, sim.Ended ? "NOON" : "TURN " + (sim.State.turn + 1) + (sim.InTransit ? " / CROSSING HALF-TURN" : " / ADJUSTMENT"), "eyebrow");
            if (sim.Ended)
            {
                Text(side, "The morning ends", "story-title");
                Text(side, "The clock strikes noon.", "prose");
                Button(side, "Wake as " + sim.Name(sim.NextIncarnation), Continue, "continue-button");
                Button(side, "Replay " + sim.Name(sim.Save.player), ReplayCurrent, "tool-button");
                Button(side, "Weights", OpenWeights, "tool-button");
            }
            else
            {
                Text(side, sim.InTransit ? "In passing" : "What will you do?", "story-title");
                Text(side, sim.InTransit ? Story.Text(sim, crossing, sim.Save.player) : Story.Situation(sim), "prose");
                RenderChoices(side);
            }
            int turn = sim.InTransit ? sim.State.turn : sim.State.turn - 1;
            var recent = sim.Experienced(sim.Save.player).Where(e => e.turn == turn).ToList();
            if (recent.Count == 0) return;
            var summary = Box(world, "turn-summary");
            Text(summary, "JUST HAPPENED / " + Simulation.Clock(turn) + "-" + (sim.InTransit ? sim.Now : Simulation.Clock(turn + 1)), "section-label");
            foreach (var e in recent.Where(e => !Quiet(e) || e.actor == sim.Save.player))
            {
                var text = Text(summary, (e.kind == "reaction" ? EventClock(e) + " / " : "") + Story.Text(sim, e, sim.Save.player), "summary-event"); text.userData = e.id;
            }
            var quiet = recent.Where(e => Quiet(e) && e.actor != sim.Save.player).ToList();
            if (quiet.Count > 0)
            {
                var fold = Fold(summary, "Also witnessed (" + quiet.Count + ")");
                foreach (var e in quiet) Text(fold, Story.Text(sim, e, sim.Save.player), "summary-event");
            }
        }
        void SimpleAvailability(VisualElement parent, Choice choice)
        {
            Pick(parent, "Location", new[] { "" }.Concat(choice.reaction ? sim.Data.rooms.SelectMany(r => r.exits.Select(e => Simulation.Edge(r.id, e))).Distinct() : sim.Data.rooms.Select(r => r.id)).ToList(),
                choice.location ?? "", v => choice.location = v, v => v == "" ? "Any" : sim.Name(v));
            WindowFields(parent, choice.from, choice.until, v => choice.from = v, v => choice.until = v);
            var represented = new HashSet<Condition>();
            System.Action<string, string, string, string> field = (name, key, value, group) =>
            {
                var matches = choice.requires.Where(c => c.key == key && (c.value == value || value == "$actor" && c.value == choice.actor)).ToList();
                foreach (var c in matches) represented.Add(c);
                string selected = matches.Count == 0 ? "Any" : matches[0].not ? "Absent" : "Present";
                Pick(parent, name, new List<string> { "Any", "Present", "Absent" }, selected, v =>
                {
                    choice.requires.RemoveAll(c => c.key == key && (c.value == value || value == "$actor" && c.value == choice.actor));
                    if (v != "Any") choice.requires.Add(new Condition(key, value, v == "Absent"));
                }, v => group == "possessions" ? v == "Present" ? "Have" : v == "Absent" ? "Do not have" : "Any" : v);
            };
            Text(parent, "POSSESSIONS", "section-label");
            foreach (var item in sim.Data.items) field(item.name, "owner:" + item.id, "$actor", "possessions");
            Text(parent, "PEOPLE PRESENT", "section-label");
            foreach (var person in sim.Data.people.Where(p => p.id != choice.actor)) field(person.name, "at:" + person.id, "$here", "people");
            Text(parent, "ITEMS PRESENT", "section-label");
            foreach (var item in sim.Data.items) field(item.name + " here", "near:" + item.id, "yes", "items");
            foreach (var c in choice.requires.Where(c => Simulation.Spatial(c) && !represented.Contains(c)).ToList())
            {
                var row = Box(parent, "editor-pair");
                Text(row, sim.Describe(c, choice.actor, choice.target), "factor-copy");
                Button(row, "\u00d7", () => { choice.requires.Remove(c); row.RemoveFromHierarchy(); }, "inspect-button", "Remove requirement");
            }
        }
        void OpenWeights()
        {
            var content = OpenModal(sim.Name(sim.Save.player) + " / weights");
            Button(content, "Save all weights", SaveWeights, "tool-button");
            Text(content, "MANUAL ADJUSTMENTS", "section-label");
            foreach (var g in sim.VisibleAdjustments.Where(g => g.actor == sim.Save.player).OrderBy(g => g.location).ThenBy(g => g.from).ThenBy(g => g.action))
            {
                string label = sim.Definitions.FirstOrDefault(c => c.id == g.action && c.actor == g.actor)?.label ?? g.action;
                Text(content, "+" + g.amount.ToString("0.##") + " / " + label + "\n" + sim.Name(g.location) + " / " + ((g.location ?? "").StartsWith("edge:") ? Simulation.MidpointClock(g.from) : Simulation.Clock(g.from)) + (g.from == g.until ? "" : "-" + Simulation.Clock(g.until)), "weight-entry");
            }
            Text(content, "CONDITION WEIGHTS", "section-label");
            foreach (var rule in sim.Rules.Where(r => r.actor == sim.Save.player))
                Text(content, "+" + sim.Weight(rule).ToString("0.##") + " / " + (string.IsNullOrEmpty(rule.route) ? rule.action : "toward " + sim.Name(rule.route)) +
                    "\n" + Simulation.Clock(rule.from) + "-" + Simulation.Clock(rule.until) + " / " + string.Join("; ", rule.conditions.Select(c => sim.Describe(c, rule.actor))), "weight-entry");
        }
        string WriteWeights()
        {
            var profile = Copy(sim.Save);
            profile.scenario = sim.Data; profile.frozenScenario = true; profile.recordKind = "weights";
            profile.world = sim.Initial(); profile.previous.Clear(); profile.reviewPending = false; profile.reviewIndex = 0;
            Directory.CreateDirectory(RecordsDirectory);
            string path = Path.Combine(RecordsDirectory, System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".weights.json");
            File.WriteAllText(path, JsonUtility.ToJson(profile, true)); return path;
        }
        void SaveWeights()
        {
            try { string path = WriteWeights(); Text(modal.Q<ScrollView>(), "Saved all characters' weights: " + Path.GetFileName(path), "record-status"); }
            catch (System.Exception ex) { Text(modal.Q<ScrollView>(), ex.Message, "editor-error"); }
        }
    }
}
