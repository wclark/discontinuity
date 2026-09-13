using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    [Serializable] public class Fact { public string key; public string value; public int source = -1; }
    [Serializable] public class Room { public string id, name, description; public float x, y; public string[] exits; }
    [Serializable] public class Person { public string id, name, role, color, location, concern; }
    [Serializable] public class Thing { public string id, name, owner; }
    [Serializable] public class Condition
    {
        public string key, value;
        public bool not;
        public Condition() { }
        public Condition(string key, string value, bool not = false) { this.key = key; this.value = value; this.not = not; }
    }
    [Serializable] public class Effect { public string key, value; public Effect(string k, string v) { key = k; value = v; } }
    [Serializable] public class Choice
    {
        public string id, actor, target, location, slot, label, actorText, targetText, observerText;
        public string activity;
        public string destination;
        public bool quiet, reaction;
        public int from, until = 15, phase = 1;
        public bool once = true;
        public List<Condition> requires = new List<Condition>();
        public List<Effect> effects = new List<Effect>();
    }
    [Serializable] public class Rule
    {
        public string id, actor, action, route;
        public int from, until = 15;
        public float amount;
        public List<Condition> conditions = new List<Condition>();
    }
    [Serializable] public class Scenario
    {
        public List<Room> rooms = new List<Room>();
        public List<Person> people = new List<Person>();
        public List<Thing> items = new List<Thing>();
        public List<Fact> facts = new List<Fact>();
        public List<Choice> choices = new List<Choice>();
        public List<Rule> rules = new List<Rule>();
        public int turns = 16;
    }
    [Serializable] public class Contribution
    {
        public string id, description;
        public float amount;
        public bool active;
        public List<int> causes = new List<int>();
    }
    [Serializable] public class Option
    {
        public Choice choice;
        public float conditions, manual, recorded;
        public string manualId;
        public bool departureValid;
        public float Score { get { return conditions + manual; } }
        public List<Contribution> terms = new List<Contribution>();
    }
    [Serializable] public class Guidance
    {
        public string id, actor, action, context, location;
        public int from, until;
        public float amount;
    }
    [Serializable] public class NumberSetting { public string id; public float value; }
    [Serializable] public class DecisionOption
    {
        public string action, label;
        public float conditions, manual;
    }
    [Serializable] public class Event
    {
        public int id, turn;
        public string actor, target, action, location, label, actorText, targetText, observerText;
        public string decision;
        public string kind, destination, activity;
        public bool travelBatch, quiet;
        public float score, manual, recorded;
        public bool blocked, changed;
        public List<int> causes = new List<int>();
        public List<string> effects = new List<string>();
        public List<DecisionOption> alternatives = new List<DecisionOption>();
        public List<string> witnesses = new List<string>();
        public List<string> participants = new List<string>();
        public List<Fact> sceneBefore = new List<Fact>();
        public List<Fact> sceneAfter = new List<Fact>();
        public string Text(string viewer)
        {
            return viewer == actor ? actorText : viewer == target && !string.IsNullOrEmpty(targetText) ? targetText : observerText;
        }
    }
    [Serializable] public class World
    {
        public int turn;
        public List<Fact> facts = new List<Fact>();
        public List<Event> events = new List<Event>();
        public List<string> used = new List<string>();
        public List<string> applied = new List<string>();
        public Transit transit;
        public string Get(string key) { var f = facts.Find(v => v.key == key); return f == null ? "" : f.value; }
        public int Source(string key) { var f = facts.Find(v => v.key == key); return f == null ? -1 : f.source; }
        public void Set(string key, string value, int source = -1)
        {
            var f = facts.Find(v => v.key == key);
            if (f == null) { f = new Fact { key = key }; facts.Add(f); }
            f.value = value; f.source = source;
        }
        public World Copy()
        {
            return new World { turn = turn, facts = facts.Select(f => new Fact { key = f.key, value = f.value, source = f.source }).ToList(),
                events = new List<Event>(events), used = new List<string>(used), applied = new List<string>(applied), transit = transit?.Copy() };
        }
    }
    [Serializable] public class Campaign
    {
        public int version = 1, day = 1;
        public string player = "clara";
        public bool reviewPending;
        public int reviewIndex;
        public World world;
        public List<Guidance> guidance = new List<Guidance>();
        public List<NumberSetting> weights = new List<NumberSetting>();
        public List<Event> previous = new List<Event>();
        public List<Choice> choices = new List<Choice>();
        public List<Rule> rules = new List<Rule>();
        // Only portable records pin the base scenario; ordinary saves use the installed content.
        public Scenario scenario;
        public bool frozenScenario;
        public string mode = "adjustment", recordKind = "state";
        public bool adjustmentsImported;
        public List<Guidance> adjustments = new List<Guidance>();
    }
    [Serializable] public class RankedDecision
    {
        public string actor;
        public List<Option> options = new List<Option>();
    }
    [Serializable] public class Transit
    {
        public bool active;
        public List<Option> moves = new List<Option>(), tail = new List<Option>();
        public List<RankedDecision> decisions = new List<RankedDecision>();
        public List<int> crossings = new List<int>();
        public List<Fact> before = new List<Fact>();
        public Transit Copy()
        {
            return new Transit { active = active, moves = new List<Option>(moves), tail = new List<Option>(tail),
                decisions = new List<RankedDecision>(decisions), crossings = new List<int>(crossings), before = new List<Fact>(before) };
        }
    }
    public class Criterion
    {
        public string description;
        public bool met;
        public Criterion(string description, bool met) { this.description = description; this.met = met; }
    }
}
