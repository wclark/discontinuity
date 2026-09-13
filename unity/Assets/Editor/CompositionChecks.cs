using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Discontinuity
{
    public static class CompositionChecks
    {
        public static Simulation CrossingFixture()
        {
            var sim = new Simulation(HouseholdContent.Create(), new Campaign());
            sim.State.Set("at:clara", "hall"); sim.State.Set("at:jonah", "archive"); sim.State.Set("at:vale", "hall");
            sim.Save.rules.Add(new Rule { id = "test_cross_route", actor = "jonah", action = "move:hall", amount = 20 });
            sim.Step("move:archive"); return sim;
        }
        public static int Run()
        {
            int count = 0;
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAILED: " + name); count++; Debug.Log("PASS: " + name); };
            var data = HouseholdContent.Create();
            var sim = new Simulation(data, new Campaign());
            check(sim.Adjusting, "a new composing campaign starts in adjustment mode");
            sim.Step("move:hall");
            check(sim.Save.adjustments.Count == 0, "accepting the first-ranked action creates no adjustment");
            sim.Step("help");
            check(sim.Save.adjustments.Single().amount == 6, "different choice receives only leader gap plus one");
            sim.Begin("clara"); sim.Step();
            check(sim.Rank("clara")[0].choice.id == "help" && sim.Rank("clara")[0].manual == 6, "revisiting keeps your own prior adjustment in the visible ranking");
            sim.Step("help");
            check(sim.Save.adjustments.Single().amount == 6 && sim.State.events.Last(e => e.actor == "clara").recorded == 0, "reaccepting the adjusted winner does not inflate its weight");
            sim.Begin("clara"); sim.Step(); sim.Step("mock");
            check(sim.Save.adjustments.Find(g => g.action == "mock").amount == 7 && sim.Save.adjustments.Find(g => g.action == "help").amount == 6, "choosing a new winner preserves both options' weights");
            sim.Begin("clara"); sim.Step(); sim.Step("help");
            check(sim.Save.adjustments.Count == 2 && sim.Save.adjustments.Find(g => g.action == "help").amount == 8, "retuning an old option adds just its remaining gap");
            string weights = JsonUtility.ToJson(new Campaign { adjustments = sim.Save.adjustments });
            for (int i = 0; i < 5; i++) { sim.Begin("clara"); while (!sim.Ended) sim.Step(); }
            check(weights == JsonUtility.ToJson(new Campaign { adjustments = sim.Save.adjustments }), "passive complete replays never mutate the full weight set");
            sim.Begin("clara"); sim.Step(); sim.State.Set("owner:cloth", "kitchen");
            check(!sim.Rank("clara").Any(o => o.choice.id == "help"), "large retained weights cannot conjure a missing possession");
            sim.State.Set("owner:cloth", "clara"); sim.State.Set("at:jonah", "garden");
            check(!sim.Rank("clara").Any(o => o.choice.id == "mock"), "retained social weights cannot reach an absent person");
            sim.State.Set("at:jonah", "hall"); sim.State.turn = 2;
            check(sim.Rank("clara").All(o => o.manual == 0), "manual weights use a single turn, not overlapping hidden windows");
            var tied = new Simulation(data, new Campaign()); tied.State.turn = 14;
            var tiedOptions = tied.Rank("clara");
            check(tied.Increment(tiedOptions[0], tiedOptions) == 0 && tied.Increment(tiedOptions.Last(), tiedOptions) == 1, "ties use stable ranking: accepting first is free, choosing another wins by one");
            var action = new Choice { id = "simple", actor = "clara", location = "kitchen", once = true,
                requires = new List<Condition> { new Condition("trust", "yes"), new Condition("owner:cloth", "$actor") } };
            tied.State.turn = 0; tied.State.used.Add("clara:simple");
            check(tied.Valid(action), "in composing, arbitrary story flags and completion do not hide a physically possible action");
            tied.State.Set("owner:cloth", "hall");
            check(!tied.Valid(action), "possession requirements still govern simple availability");
            var legacy = new Campaign { mode = null, world = new Simulation(data).Initial(), guidance = new List<Guidance> {
                new Guidance { id = "old", actor = "clara", action = "help", context = "cuff", location = "hall", from = 1, until = 3, amount = 6 } } };
            var migrated = new Simulation(data, legacy);
            check(migrated.Adjusting && migrated.Save.adjustments.Single().amount == 6 && migrated.Save.adjustments[0].until == 1, "older saves import their amounts at the originally authored turn");
            migrated = new Simulation(data, JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(migrated.Save)));
            check(migrated.Save.adjustments.Count == 1, "reloading does not import the same legacy weights twice");
            sim = CrossingFixture();
            check(sim.InTransit && sim.State.turn == 0 && sim.Now == "08:07:30", "opposite movement opens one midpoint without advancing the full turn");
            check(sim.Here("clara") == "edge:archive:hall" && sim.Here("jonah") == sim.Here("clara") && sim.Here("vale") == "hall", "only travelers share the passage, not doorway bystanders");
            check(sim.Rank("clara").All(o => o.choice.reaction) && sim.Rank("vale").Count == 0, "the half-turn offers reactions only to crossing participants");
            check(sim.Rank("clara")[0].choice.id == "cross:continue", "zero-score crossing defaults to continuing");
            check(!sim.ItemHere("ledger", "clara") && sim.ItemHere("cloth", "jonah"), "crossing item presence includes carried objects but not adjacent room objects");
            int eventsBefore = sim.State.events.Count;
            sim.Step("not-a-reaction");
            check(sim.State.events.Count == eventsBefore && sim.InTransit, "invalid reaction cannot advance or duplicate the turn");
            string checkpoint = JsonUtility.ToJson(sim.Save);
            var forecast = sim.Forecast();
            check(JsonUtility.ToJson(sim.Save) == checkpoint && forecast.Any(e => e.kind == "reaction"), "forecast can finish a pending crossing without changing the live checkpoint");
            var resumed = new Simulation(data, JsonUtility.FromJson<Campaign>(checkpoint));
            check(resumed.InTransit && resumed.Rank("clara").Any(o => o.choice.id == "cross:cloth:jonah"), "save reload restores the exact pending reaction options");
            resumed.Step("cross:cloth:jonah");
            check(!resumed.InTransit && resumed.State.turn == 1 && resumed.State.Get("at:clara") == "archive" && resumed.State.Get("at:jonah") == "hall", "one reaction beat finishes both original journeys");
            check(resumed.State.Get("owner:cloth") == "jonah", "passing can transfer an available carried item");
            var reaction = resumed.State.events.First(e => e.action == "cross:cloth:jonah");
            check(reaction.witnesses.SequenceEqual(new[] { "clara", "jonah" }) && !resumed.Experienced("vale").Contains(reaction), "reaction prose is witnessed only inside the passage");
            check(resumed.State.events.Count(e => e.kind == "reaction") == 2 && resumed.State.events.Count(e => e.action == "tend") == 1, "each traveler reacts once and other people's room actions never rerun");
            check(resumed.Save.adjustments.Any(g => g.action == "cross:cloth:jonah" && g.amount == 1 && g.location == "edge:archive:hall"), "crossing reactions acquire their own persistent action-place-turn weight");
            var nextLife = new Simulation(data, JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(resumed.Save)));
            nextLife.Begin("jonah"); nextLife.State.Set("at:clara", "hall"); nextLife.State.Set("at:jonah", "archive"); nextLife.State.Set("at:vale", "hall");
            nextLife.Step();
            check(nextLife.InTransit && nextLife.Rank("clara")[0].choice.id == "cross:cloth:jonah", "a previously authored crossing reaction ranks first when that person becomes an NPC");
            nextLife.Step("cross:continue");
            var kindness = nextLife.State.events.First(e => e.action == "cross:cloth:jonah");
            check(kindness.manual == 1 && kindness.Text("jonah").Contains("gives you a clean cloth"), "another incarnation receives the same passing kindness through ordinary scoring");
            var offscreen = CrossingFixture(); offscreen.Save.player = "merrow"; offscreen.Step();
            check(!offscreen.InTransit && !offscreen.Experienced("merrow").Any(e => e.kind == "reaction"), "NPC crossing reactions resolve without exposing them to an absent player");
            var group = new Simulation(data, new Campaign());
            group.State.Set("at:clara", "hall"); group.State.Set("at:jonah", "archive"); group.State.Set("at:vale", "archive");
            foreach (string actor in new[] { "jonah", "vale" }) group.Save.rules.Add(new Rule { id = "route_" + actor, actor = actor, action = "move:hall", amount = 50 });
            group.Step("move:archive");
            check(group.Travelers.Count == 3 && group.State.transit.crossings.Count == 2, "multiple crossing pairs share one bounded reaction opportunity per traveler");
            group.Step();
            check(group.State.turn == 1 && group.State.events.Count(e => e.kind == "reaction") == 3, "a three-person crossing is not a chain of pairwise extra turns");
            var npcCross = new Simulation(data, new Campaign { player = "merrow" });
            npcCross.State.Set("at:clara", "hall"); npcCross.State.Set("at:jonah", "archive");
            npcCross.Save.rules.Add(new Rule { id = "outbound", actor = "clara", action = "move:archive", amount = 20 });
            npcCross.Save.rules.Add(new Rule { id = "inbound", actor = "jonah", action = "move:hall", amount = 20 });
            npcCross.Step();
            check(!npcCross.InTransit && npcCross.State.turn == 1 && npcCross.State.events.Count(e => e.kind == "reaction") == 2, "a fully offscreen crossing completes inside the ordinary step");
            var authored = CrossingFixture();
            var greeting = new Choice { id = "custom_pass", actor = "clara", target = "jonah", reaction = true, once = false, phase = 0,
                label = "Ask about the errand", location = authored.Here("clara"), from = 0, until = 0,
                actorText = "You ask about Jonah's errand.", targetText = "Clara asks about your errand.", observerText = "Clara asks Jonah about his errand.",
                requires = new List<Condition> { new Condition("at:$target", "$here"), new Condition("owner:cloth", "$actor") },
                effects = new List<Effect> { new Effect("asked_in_passing", "yes") } };
            check(authored.StoreDefinition(greeting, new List<Rule> { new Rule { id = "custom_pass_weight", actor = "clara", action = greeting.id, from = 0, until = 0, amount = 3 } }, true, out string error), "a crossing supports the same authored presence criteria, effects and weights as a room action");
            authored.Step(greeting.id);
            check(authored.State.Get("asked_in_passing") == "yes" && authored.Save.adjustments.Count == 1, "selecting an already-leading custom reaction does not add another adjustment");
            return count;
        }
    }
}
