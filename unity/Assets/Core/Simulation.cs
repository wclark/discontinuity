using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    // Pure C#: the same deterministic evaluator drives play, forecasts, and verification.
    public class Simulation
    {
        public readonly Scenario Data;
        public Campaign Save;
        public World State { get { return Save.world; } }
        public bool Ended { get { return State.turn >= Data.turns; } }
        public Simulation(Scenario data, Campaign save = null)
        {
            Data = data; Save = save ?? new Campaign();
            if (Save.world == null) Save.world = Initial();
            RestoreWitnesses();
        }
        public static string Clock(int turn) { return (8 + turn / 4).ToString("00") + ":" + ((turn % 4) * 15).ToString("00"); }
        public World Initial()
        {
            var w = new World();
            foreach (var p in Data.people) w.Set("at:" + p.id, p.location);
            foreach (var t in Data.items) w.Set("owner:" + t.id, t.owner);
            foreach (var f in Data.facts) w.Set(f.key, f.value);
            return w;
        }
        public void Begin(string player)
        {
            Save.previous = new List<Event>(State.events);
            Save.guidance.RemoveAll(g => g.actor == player);
            Save.player = player; Save.day++; Save.world = Initial();
        }
        public string NextIncarnation
        {
            get { return Data.people[(Data.people.FindIndex(p => p.id == Save.player) + 1) % Data.people.Count].id; }
        }
        public bool ContinueAsNext()
        {
            if (!Ended) return false;
            Begin(NextIncarnation);
            return true;
        }
        public List<Event> Experienced(string actor)
        {
            return State.events.Where(e => e.actor == actor || e.witnesses.Contains(actor)).ToList();
        }
        void AddWitnesses(Event e, World world)
        {
            foreach (var p in Data.people.Where(p => p.id == e.actor || world.Get("at:" + p.id) == world.Get("at:" + e.actor)))
                if (!e.witnesses.Contains(p.id)) e.witnesses.Add(p.id);
        }
        void RestoreWitnesses()
        {
            if (State.events.All(e => e.witnesses != null && e.witnesses.Count > 0)) return;
            // Older saves predate witness lists. Reconstruct positions in resolution order.
            var world = Initial();
            foreach (var e in State.events)
            {
                e.witnesses = new List<string>(); AddWitnesses(e, world);
                if (!e.blocked)
                {
                    if (e.action.StartsWith("move:", StringComparison.Ordinal)) world.Set("at:" + e.actor, e.action.Substring(5));
                    var choice = Data.choices.Find(c => c.id == e.action && c.actor == e.actor);
                    if (choice != null) foreach (var effect in choice.effects)
                        world.Set(Resolve(effect.key, e.actor, e.target), Resolve(effect.value, e.actor, e.target));
                }
                AddWitnesses(e, world);
            }
        }
        public string Name(string id)
        {
            var p = Data.people.Find(v => v.id == id); if (p != null) return p.name;
            var r = Data.rooms.Find(v => v.id == id); if (r != null) return r.name;
            var t = Data.items.Find(v => v.id == id); return t == null ? id : t.name;
        }
        public string Resolve(string text, string actor, string target = "")
        { return (text ?? "").Replace("$actor", actor ?? "").Replace("$target", target ?? ""); }
        public bool Met(Condition c, string actor, string target = "")
        {
            bool equal = State.Get(Resolve(c.key, actor, target)) == Resolve(c.value, actor, target);
            return c.not ? !equal : equal;
        }
        public string Describe(Condition c, string actor, string target = "")
        {
            string key = Resolve(c.key, actor, target), val = Resolve(c.value, actor, target);
            if (key.StartsWith("at:")) return Name(key.Substring(3)) + (c.not ? " outside " : " in ") + Name(val);
            if (key.StartsWith("owner:")) return Name(key.Substring(6)) + (c.not ? " not with/in " : " with/in ") + Name(val);
            return key.Replace('_', ' ') + (c.not ? " != " : " = ") + val;
        }
        public float Weight(Rule r) { var s = Save.weights.Find(v => v.id == r.id); return s == null ? r.amount : s.value; }
        public void SetWeight(string id, float amount)
        {
            var s = Save.weights.Find(v => v.id == id);
            if (s == null) { s = new NumberSetting { id = id }; Save.weights.Add(s); }
            s.value = amount;
        }
        public string NextRoom(string from, string to)
        {
            if (from == to) return null;
            var queue = new Queue<List<string>>(); queue.Enqueue(new List<string> { from });
            var seen = new HashSet<string> { from };
            while (queue.Count > 0)
            {
                var path = queue.Dequeue(); var room = Data.rooms.Find(r => r.id == path.Last());
                if (room == null) continue;
                foreach (var exit in room.exits)
                {
                    if (!seen.Add(exit)) continue;
                    var next = new List<string>(path) { exit };
                    if (exit == to) return next[1];
                    queue.Enqueue(next);
                }
            }
            return null;
        }
        public bool Valid(Choice c)
        {
            if (Ended || State.turn < c.from || State.turn > c.until) return false;
            if (!string.IsNullOrEmpty(c.location) && State.Get("at:" + c.actor) != c.location) return false;
            if (c.once && State.used.Contains(c.actor + ":" + (c.slot ?? c.id))) return false;
            return c.requires.All(v => Met(v, c.actor, c.target));
        }
        public List<Choice> Choices(string actor)
        {
            var result = Data.choices.Where(c => c.actor == actor && Valid(c)).ToList();
            string here = State.Get("at:" + actor);
            foreach (var exit in Data.rooms.Find(r => r.id == here).exits)
                result.Add(new Choice { id = "move:" + exit, actor = actor, location = here, slot = "travel:" + here,
                    label = "Go to " + Name(exit), phase = 2, once = false,
                    actorText = "You cross to the " + Name(exit) + ".", observerText = Name(actor) + " goes to the " + Name(exit) + ".",
                    effects = new List<Effect> { new Effect("at:$actor", exit) } });
            result.Add(new Choice { id = "wait", actor = actor, location = here, slot = "travel:" + here, once = false, phase = 3,
                label = "Wait here", actorText = "You stay, listening to the house around you.", observerText = Name(actor) + " stays in the " + Name(here) + "." });
            return result;
        }
        public string Context(Choice c) { return c.slot ?? c.id; }
        Guidance Matching(Choice c)
        {
            if (c.actor == Save.player) return null;
            // Select the decision first, then its option. Alternatives never accumulate.
            var latest = Save.guidance.Where(g => g.actor == c.actor && g.context == Context(c) && g.location == c.location &&
                State.turn >= g.from && State.turn <= g.until && !State.applied.Contains(g.id))
                .OrderByDescending(g => g.from).FirstOrDefault();
            return latest != null && latest.action == c.id ? latest : null;
        }
        public List<Option> Rank(string actor)
        {
            return Choices(actor).Select(c => Evaluate(c)).OrderByDescending(o => o.Score)
                .ThenBy(o => o.choice.id == "wait" ? 0 : 1).ThenBy(o => o.choice.id, StringComparer.Ordinal).ToList();
        }
        public Option Evaluate(Choice choice)
        {
            var option = new Option { choice = choice };
            foreach (var rule in Data.rules.Where(r => r.actor == choice.actor))
            {
                string action = rule.action;
                if (!string.IsNullOrEmpty(rule.route))
                {
                    var next = NextRoom(State.Get("at:" + choice.actor), rule.route);
                    action = next == null ? null : "move:" + next;
                }
                if (action != choice.id) continue;
                bool time = State.turn >= rule.from && State.turn <= rule.until;
                bool active = time && rule.conditions.All(c => Met(c, choice.actor, choice.target));
                var term = new Contribution { id = rule.id, active = active, amount = active ? Weight(rule) : 0,
                    description = Clock(rule.from) + "-" + Clock(rule.until) + (rule.conditions.Count == 0 ? "" : " | " +
                        string.Join("; ", rule.conditions.Select(c => Describe(c, choice.actor, choice.target)))) };
                if (active)
                    foreach (var c in rule.conditions) AddCause(term.causes, State.Source(Resolve(c.key, choice.actor, choice.target)));
                option.terms.Add(term); option.conditions += term.amount;
            }
            var guidance = Matching(choice);
            if (guidance != null) { option.manual = guidance.amount; option.manualId = guidance.id; }
            return option;
        }
        void Record(Choice c, List<Option> options)
        {
            int turn = State.turn;
            // A later decision in this context closes the earlier interval, including natural choices.
            foreach (var g in Save.guidance.Where(g => g.actor == c.actor && g.context == Context(c) && g.until >= turn)) g.until = turn - 1;
            var chosen = options.Find(o => o.choice.id == c.id);
            float best = options.Max(o => o.conditions);
            if (chosen == null || chosen.conditions >= best) return;
            Save.guidance.Add(new Guidance { id = Guid.NewGuid().ToString("N"), actor = c.actor, action = c.id,
                context = Context(c), location = c.location, from = turn, until = Math.Min(c.until, turn + 2), amount = best - chosen.conditions + 1 });
        }
        static void AddCause(List<int> list, int id) { if (id >= 0 && !list.Contains(id)) list.Add(id); }
        public void Step(string humanChoice = null)
        {
            if (Ended) return;
            var rankings = Data.people.ToDictionary(p => p.id, p => Rank(p.id));
            var proposals = Data.people.Select(p => rankings[p.id][0]).ToList();
            if (!string.IsNullOrEmpty(Save.player) && humanChoice != null)
            {
                var choices = rankings[Save.player]; var picked = choices.Find(o => o.choice.id == humanChoice);
                if (picked == null) return;
                Record(picked.choice, choices);
                proposals[Data.people.FindIndex(p => p.id == Save.player)] = picked;
            }
            // Everyone chooses from the same state. Conversations resolve before departures.
            foreach (var proposal in proposals.OrderBy(o => o.choice.phase).ThenBy(o => o.choice.actor, StringComparer.Ordinal))
            {
                var c = proposal.choice;
                bool valid = Valid(c);
                var e = new Event { id = State.events.Count, turn = State.turn, actor = c.actor, target = c.target,
                    action = c.id, label = c.label, location = State.Get("at:" + c.actor), score = proposal.Score, manual = proposal.manual,
                    actorText = c.actorText, targetText = c.targetText, observerText = c.observerText, blocked = !valid,
                    alternatives = rankings[c.actor].Select(o => new DecisionOption { action = o.choice.id, label = o.choice.label,
                        conditions = o.conditions, manual = o.manual }).ToList(),
                    decision = string.Join("\n", proposal.terms.Where(t => t.active).Select(t => "+" + t.amount.ToString("0.#") + "  " + t.description)) };
                if (proposal.manual > 0) e.decision += "\n+" + proposal.manual.ToString("0.#") + "  previous choice";
                AddWitnesses(e, State);
                AddCause(e.causes, State.Source("at:" + c.actor));
                foreach (var term in proposal.terms.Where(t => t.active)) foreach (int cause in term.causes) AddCause(e.causes, cause);
                foreach (var condition in c.requires) AddCause(e.causes, State.Source(Resolve(condition.key, c.actor, c.target)));
                if (valid)
                {
                    foreach (var effect in c.effects)
                    {
                        string key = Resolve(effect.key, c.actor, c.target), value = Resolve(effect.value, c.actor, c.target);
                        State.Set(key, value, e.id); e.effects.Add(key + " = " + value);
                    }
                    if (c.once) State.used.Add(c.actor + ":" + (c.slot ?? c.id));
                    if (proposal.manualId != null) State.applied.Add(proposal.manualId);
                    AddWitnesses(e, State);
                }
                else
                {
                    e.actorText = "Your opportunity closes before you can act: " + c.label + ".";
                    e.observerText = Name(c.actor) + " cannot complete: " + c.label + ".";
                    e.targetText = e.observerText;
                }
                var before = Save.previous.Find(v => v.turn == e.turn && v.actor == e.actor);
                e.changed = before != null && (before.action != e.action || before.blocked != e.blocked);
                State.events.Add(e);
            }
            State.turn++;
        }
        public List<Event> Forecast()
        {
            var campaign = new Campaign { player = "", world = State.Copy(), guidance = new List<Guidance>(Save.guidance.Where(g => g.actor != Save.player)),
                weights = Save.weights.Select(s => new NumberSetting { id = s.id, value = s.value }).ToList(), previous = Save.previous };
            var sim = new Simulation(Data, campaign);
            while (!sim.Ended) sim.Step();
            return sim.State.events;
        }
        public string Outcome()
        {
            if (State.Get("cleared") == "yes") return "The accusation fails. Jonah leaves with his name intact.";
            if (State.Get("accused") == "clara") return "The room turns on Clara. Her earlier words return through Jonah.";
            if (State.Get("accused") == "jonah") return "Jonah is accused. The envelope is elsewhere; the room has its answer.";
            return Ended ? "No verdict. The household keeps its secret for another day." : "At noon, someone in this house will be blamed.";
        }
    }
}
