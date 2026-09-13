using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Discontinuity
{
    public static class AuthoringChecks
    {
        public static int Run()
        {
            int count = 0;
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAILED: " + name); count++; Debug.Log("PASS: " + name); };
            var data = HouseholdContent.Create();
            var sim = new Simulation(data);
            string baseJson = JsonUtility.ToJson(data);
            var choice = new Choice { id = "custom_test", actor = "clara", label = "Fold the clean cloth", location = "kitchen", slot = "cloth_fold", from = 0, until = 3,
                actorText = "You fold the cloth.", observerText = "Clara folds the cloth.", activity = "Folding the cloth",
                requires = new List<Condition> { new Condition("near:cloth", "yes"), new Condition("at:jonah", "$here", true) },
                effects = new List<Effect> { new Effect("cloth_folded", "yes") } };
            var rule = new Rule { id = "custom_test_rule", actor = "clara", action = choice.id, from = 0, until = 3, amount = 7,
                conditions = new List<Condition> { new Condition("owner:cloth", "$actor") } };
            check(sim.StoreDefinition(choice, new List<Rule> { rule }, true, out string error), "author a playable action without touching the base scenario");
            check(JsonUtility.ToJson(data) == baseJson, "authoring leaves bundled definitions unchanged");
            check(sim.Rank("clara")[0].choice.id == choice.id && sim.Rank("clara")[0].Score == 7, "new action immediately participates in descending rankings");
            sim.State.Set("at:jonah", "kitchen");
            check(!sim.Valid(choice) && sim.Evaluate(choice).Score == 7, "absence is a hard gate, separate from a high score");
            check(sim.Availability(choice).Any(c => !c.met && c.description.Contains("Jonah")), "availability explains a failed presence condition");
            sim.State.Set("at:jonah", "hall"); sim.State.Set("owner:cloth", "jonah");
            check(!sim.Valid(choice), "an item carried in another room is not here");
            sim.State.Set("at:jonah", "kitchen");
            check(sim.ItemHere("cloth", "clara"), "items carried by present people count as here");
            sim.State.Set("owner:cloth", "kitchen"); sim.State.Set("at:jonah", "hall");
            check(sim.Valid(choice) && sim.Evaluate(choice).Score == 0, "item can enable an action without activating its inventory bonus");
            sim.State.turn = 4; check(!sim.Valid(choice), "action time window is a hard inclusive gate");
            sim.State.turn = 3; check(sim.Valid(choice), "last permitted turn is valid");
            sim.State.turn = 0; sim.State.Set("at:clara", "hall"); check(!sim.Valid(choice), "location is a hard gate");
            sim.State.Set("at:clara", "kitchen"); sim.State.Set("owner:cloth", "clara");
            sim.Step(choice.id);
            check(sim.State.Get("cloth_folded") == "yes" && !sim.Valid(choice), "custom effects apply and consume their decision slot");
            check(sim.Save.guidance.Single().amount == 1 && sim.State.events.First(e => e.actor == "clara").recorded == 1, "top choice records a noncumulative plus one in both save and event");
            var historic = sim.State.events.First(e => e.action == choice.id);
            choice.activity = "A revised caption";
            check(Story.Activity(sim, historic, data.people[0], "kitchen") == "Folding the cloth", "editing a definition cannot rewrite a recorded activity caption");
            sim.Begin("jonah");
            check(sim.Rank("clara")[0].Score == 8, "custom NPC option combines a condition bonus and a prior human increment");
            string saved = JsonUtility.ToJson(sim.Save);
            var restored = new Simulation(data, JsonUtility.FromJson<Campaign>(saved));
            restored.Step();
            check(restored.State.events.Any(e => e.action == choice.id && e.manual == 1 && !e.blocked), "custom action and bias survive save reload and execute as NPC behavior");
            string beforeForecast = JsonUtility.ToJson(sim.Save);
            check(sim.Forecast().Any(e => e.action == choice.id), "forecasts include custom definitions and scoring");
            check(beforeForecast == JsonUtility.ToJson(sim.Save), "custom forecast is read-only");
            sim.Begin("clara");
            check(sim.Save.guidance.All(g => g.actor != "clara") && sim.Definitions.Any(c => c.id == choice.id), "replaying clears increments but preserves authored actions");
            var bad = JsonUtility.FromJson<Choice>(JsonUtility.ToJson(choice)); bad.label = "";
            string beforeBad = JsonUtility.ToJson(sim.Save);
            check(!sim.StoreDefinition(bad, new List<Rule>(), true, out error) && error.Contains("label"), "invalid edits give a specific validation error");
            check(beforeBad == JsonUtility.ToJson(sim.Save), "invalid edits are atomic");
            bad.label = "Teleport"; bad.effects = new List<Effect> { new Effect("at:jonah", "chapel") };
            check(!sim.StoreDefinition(bad, new List<Rule>(), true, out error), "authoring cannot teleport another actor around movement resolution");
            bad = JsonUtility.FromJson<Choice>(JsonUtility.ToJson(choice)); bad.target = "jonah"; bad.targetText = "Clara folds her cloth.";
            check(sim.StoreDefinition(bad, new List<Rule> { rule }, true, out error) && !sim.Valid(bad), "targeted room interactions require co-location");
            bad.target = ""; bad.requires.Clear(); bad.from = 4; bad.until = 1;
            check(!sim.StoreDefinition(bad, new List<Rule>(), true, out error), "inverted time ranges cannot be saved");
            var wait = sim.Rank("clara").Find(o => o.choice.id == "wait").choice;
            var waitRule = new Rule { id = "custom_wait", actor = "clara", action = "wait", from = 0, until = 15, amount = 20 };
            check(sim.StoreDefinition(wait, new List<Rule> { waitRule }, false, out error) && sim.Rank("clara")[0].choice.id == "wait", "even waiting can receive authored conditional points");
            waitRule.amount = float.NaN;
            check(!sim.StoreDefinition(wait, new List<Rule> { waitRule }, false, out error), "nonfinite weights cannot be saved");
            waitRule.amount = 20;
            sim.StoreDefinition(wait, new List<Rule>(), false, out error);
            check(sim.Evaluate(wait).Score == 0, "removing a custom score condition removes its contribution");
            var fresh = new Simulation(data); fresh.Step("wait"); fresh.Step("wait"); fresh.Begin("jonah");
            check(fresh.Rank("clara")[0].Score == 5, "repeated selection does not compound its increment");
            var lower = new Simulation(data); lower.Step("move:hall");
            var options = lower.Rank("clara"); var help = options.First(o => o.choice.id == "help");
            check(help.Score + lower.Increment(help, options) == options.Max(o => o.Score) + 1, "lower choice beats the maximum by exactly one");
            var tied = new Simulation(data); tied.State.turn = 14;
            var tieOptions = tied.Rank("clara");
            check(tieOptions.All(o => o.Score == 0) && tied.Increment(tieOptions.Last(), tieOptions) == 1, "all-zero options break a tie with plus one");
            var record = JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(restored.Save)); record.scenario = data; record.frozenScenario = true;
            var frozen = new Simulation(new Scenario(), JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(record)));
            check(frozen.Data.rooms.Count == 5 && frozen.Save.choices.Any(c => c.id == choice.id) && frozen.State.Get("cloth_folded") == "yes", "portable record contains base content, custom content, and exact world state");
            var spatial = new Simulation(data);
            var transfer = new Choice { id = "custom_transfer", actor = "clara", location = "kitchen", once = false,
                effects = new List<Effect> { new Effect("owner:envelope", "$actor") } };
            check(!spatial.Valid(transfer), "an item transfer cannot fetch an object from another room");
            spatial.State.Set("owner:envelope", "clara"); transfer.effects[0].value = "jonah";
            check(!spatial.Valid(transfer), "an item transfer cannot give to an absent recipient");
            spatial.State.Set("at:jonah", "kitchen");
            check(spatial.Valid(transfer), "an item transfer can give to a present recipient");
            transfer.effects[0].value = "chapel";
            check(!spatial.Valid(transfer), "an item cannot be dropped in a remote room");
            var editing = new Simulation(data); editing.Step();
            var ignore = JsonUtility.FromJson<Choice>(JsonUtility.ToJson(editing.Definitions.First(c => c.id == "ignore")));
            check(editing.StoreDefinition(ignore, new List<Rule>(), true, out error) && editing.Evaluate(ignore).Score == 0, "removing a built-in direct score condition disables it locally");
            var reloaded = new Simulation(data, JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(editing.Save)));
            check(reloaded.Evaluate(ignore).Score == 0 && JsonUtility.ToJson(data) == baseJson, "disabled built-in contributions survive reload without altering base content");
            reloaded.Save.rules.Add(new Rule { id = "set1", actor = "clara", action = "ignore", from = 1, until = 2, amount = 3,
                conditions = new List<Condition> { new Condition("at:jonah", "$here"), new Condition("owner:cloth", "$actor") } });
            reloaded.Save.rules.Add(new Rule { id = "set2", actor = "clara", action = "ignore", from = 1, until = 2, amount = 2,
                conditions = new List<Condition> { new Condition("at:vale", "$here", true) } });
            check(reloaded.Evaluate(ignore).Score == 5, "independent satisfied sets add while their internal conditions all must hold");
            reloaded.State.Set("owner:cloth", "kitchen");
            check(reloaded.Evaluate(ignore).Score == 2, "one failed condition removes the whole set, not other satisfied sets");
            reloaded.State.turn = 3;
            check(reloaded.Evaluate(ignore).Score == 0 && reloaded.Valid(ignore), "time can deactivate all bonuses without invalidating a still-available action");
            return count;
        }
    }
}
