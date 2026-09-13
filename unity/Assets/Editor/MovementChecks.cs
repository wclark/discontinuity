using System;
using System.Linq;
using UnityEngine;

namespace Discontinuity
{
    public static class MovementChecks
    {
        public static int Run()
        {
            int passed = 0;
            Action<bool, string> check = (ok, title) => { if (!ok) throw new Exception("FAILED: " + title); passed++; Debug.Log("PASS: " + title); };
            var data = HouseholdContent.Create();
            var sim = new Simulation(data);
            sim.State.turn = 4; sim.State.Set("at:clara", "hall"); sim.State.Set("at:jonah", "archive"); sim.State.Set("at:vale", "chapel");
            sim.State.Set("address_copied", "yes"); sim.State.Set("ledger_read", "yes");
            sim.State.used.Add("jonah:errand");
            sim.Step("move:archive");
            var cross = sim.State.events.Single(e => e.kind == "crossing");
            check(cross.witnesses.OrderBy(x => x).SequenceEqual(new[] { "clara", "jonah" }), "only travelers on the crossing edge witness the encounter");
            check(sim.State.Get("at:clara") == "archive" && sim.State.Get("at:jonah") == "hall", "opposite travelers complete their movement without cancelling one another");
            check(!sim.Experienced("clara").Any(e => e.actor == "jonah" && e.action == "move:hall"), "crossing is not misreported as seeing the other's arrival");
            check(sim.State.Get("crossed:clara:jonah") == "4" && sim.State.Get("crossed:jonah:clara") == "4", "both travelers receive the same encounter fact");
            check(sim.State.Get("seen:clara:jonah") == "hall", "encounter records the observed direction");
            check(sim.Rank("clara")[0].choice.id == "follow:jonah" && sim.Rank("clara")[0].Score == 7, "satisfied conditions can make following the next preferred action");
            check(sim.Rank("clara")[0].terms.Any(t => t.causes.Contains(cross.id)), "follow preference traces to the encounter");
            sim.State.Set("at:jonah", "garden");
            check(sim.Rank("clara")[0].choice.destination == "hall", "following uses the observed destination, not hidden current whereabouts");
            sim.State.turn++;
            check(!sim.Choices("clara").Any(c => c.id == "follow:jonah"), "immediate follow opportunity expires after one turn");
            sim = new Simulation(data); sim.Step();
            var arrivals = sim.State.events.Where(e => e.travelBatch).ToList();
            check(arrivals.Count == 2 && arrivals.All(e => Story.Get(e.sceneAfter, "at:clara") == "hall" && Story.Get(e.sceneAfter, "at:jonah") == "hall"), "all simultaneous arrivals share one after-snapshot");
            check(sim.Experienced("clara").Any(e => e.actor == "jonah" && Story.Moving(e)) && sim.Experienced("jonah").Any(e => e.actor == "clara" && Story.Moving(e)), "simultaneous arrival observation is symmetric");
            sim = new Simulation(data); sim.State.Set("at:clara", "hall"); sim.Step("move:kitchen");
            check(!sim.Experienced("clara").Any(e => e.actor == "jonah"), "departing through a different doorway does not witness a later arrival");
            check(!sim.Experienced("jonah").Any(e => e.actor == "clara"), "new arrival does not retroactively see a departure on another edge");
            sim = new Simulation(data);
            var reordered = HouseholdContent.Create(); reordered.people.Reverse(); var reverse = new Simulation(reordered);
            while (!sim.Ended) { sim.Step(); reverse.Step(); }
            check(string.Join(",", sim.State.events.Select(e => e.actor + ":" + e.action)) == string.Join(",", reverse.State.events.Select(e => e.actor + ":" + e.action)), "entity list order cannot change who moves or reacts first");
            sim = new Simulation(data); sim.State.turn = 3; sim.State.Set("at:clara", "archive"); sim.State.Set("at:vale", "archive");
            sim.Step("take_envelope_clara");
            check(sim.State.Get("owner:envelope") == "vale" && sim.State.events.Any(e => e.actor == "clara" && e.blocked), "turn three tie priority gives Vale the contested item");
            sim = new Simulation(data); sim.State.turn = 4; sim.State.Set("at:clara", "archive"); sim.State.Set("at:vale", "archive");
            sim.Step("take_envelope_clara");
            check(sim.State.Get("owner:envelope") == "clara" && sim.State.events.Any(e => e.actor == "vale" && e.blocked), "next turn priority rotates to Clara, independent of player control");
            sim = new Simulation(data); sim.Step("move:hall"); sim.Step("help");
            check(sim.State.Get("trust") == "yes" && sim.State.events.Count(e => e.turn == 1 && e.actor == "jonah" && e.kind != "crossing") == 1, "receiving a kindness does not grant an extra same-turn action");
            var library = JsonUtility.FromJson<SceneLibrary>(Resources.Load<TextAsset>("SceneLibrary").text);
            check(library.scenes.All(s => s.castOrder.Count == 2 && s.castOrder.Distinct().Count() == 2), "custom tableaux declare each depicted character exactly once");
            check(library.scenes.Find(s => s.resource == "Tableaux/archive-crossing").castOrder.SequenceEqual(new[] { "jonah", "clara" }), "crossing composition places Jonah left and Clara right");
            var help = sim.State.events.Single(e => e.action == "help");
            var key = SceneLibrary.Signature(data, "hall", help.sceneAfter, help);
            check(library.Find(key)?.resource == "Tableaux/cloth-exchange", "exact visible exchange selects custom artwork");
            check(key == SceneLibrary.Signature(data, Story.Room(help, "jonah"), Story.Scene(help, "jonah"), help), "one image serves two prose viewpoints of the same event");
            help.sceneAfter.Find(f => f.key == "at:vale").value = "hall";
            check(library.Find(SceneLibrary.Signature(data, "hall", help.sceneAfter, help)) == null, "extra witness prevents a falsely empty custom scene");
            help.blocked = true;
            check(library.Find(SceneLibrary.Signature(data, "hall", help.sceneAfter, help)) == null, "blocked action cannot show successful exchange art");
            sim = new Simulation(data);
            while (!sim.Ended) sim.Step();
            int waits = sim.State.events.Count(e => e.action == "wait");
            check(waits <= 8, "unmodified morning has at most eight waits across all four people; actual " + waits);
            check(sim.State.events.Count(e => e.quiet && e.action != "wait") >= 30, "household tasks replace idle time with resolved activity");
            check(sim.State.events.Where(e => e.quiet && e.action != "wait").All(e => e.score == 1 && e.effects.Count == 1), "background tasks have one conditional point and real completion effects");
            sim.Begin("jonah"); while (!sim.Ended) sim.Step();
            check(sim.State.events.All(e => !e.changed), "crossing observations do not masquerade as changed choices on an identical replay");
            return passed;
        }
    }
}
