using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    // Pure C#: the same deterministic evaluator drives play, forecasts, and verification.
    public partial class Simulation
    {
        public readonly Scenario Data;
        public Campaign Save;
        public World State { get { return Save.world; } }
        public bool Ended { get { return State.turn >= Data.turns; } }
        public Simulation(Scenario data, Campaign save = null)
        {
            Save = save ?? new Campaign(); Data = Save.frozenScenario ? Save.scenario : data;
            if (Save.choices == null) Save.choices = new List<Choice>();
            if (Save.rules == null) Save.rules = new List<Rule>();
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
            Save.reviewPending = false; Save.reviewIndex = 0;
        }
        public string NextIncarnation
        {
            get { return Data.people[(Data.people.FindIndex(p => p.id == Save.player) + 1) % Data.people.Count].id; }
        }
        public bool ContinueAsNext()
        {
            if (!Ended || Save.reviewPending) return false;
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
            if (State.events.All(e => e.witnesses != null && e.witnesses.Count > 0 && e.sceneAfter != null && e.sceneAfter.Count > 0)) return;
            // Reconstruct old presentation snapshots in event order, without changing live facts.
            var world = Initial();
            for (int index = 0; index < State.events.Count; index++)
            {
                var e = State.events[index];
                if (e.travelBatch)
                {
                    var batch = State.events.Skip(index).TakeWhile(v => v.turn == e.turn && v.travelBatch).ToList();
                    var before = Story.Snapshot(world);
                    foreach (var move in batch.Where(v => v.kind != "crossing" && !v.blocked)) world.Set("at:" + move.actor, move.destination);
                    var after = Story.Snapshot(world);
                    foreach (var move in batch) { move.sceneBefore = before; move.sceneAfter = after; TravelWitnesses(move); }
                    index += batch.Count - 1;
                    continue;
                }
                e.sceneBefore = Story.Snapshot(world);
                e.witnesses = new List<string>(); AddWitnesses(e, world);
                if (!e.blocked)
                {
                    if (e.action.StartsWith("move:", StringComparison.Ordinal)) world.Set("at:" + e.actor, e.action.Substring(5));
                    var choice = Definitions.FirstOrDefault(c => c.id == e.action && c.actor == e.actor);
                    if (choice != null) foreach (var effect in choice.effects)
                        world.Set(Resolve(effect.key, e.actor, e.target), Resolve(effect.value, e.actor, e.target));
                }
                AddWitnesses(e, world);
                e.sceneAfter = Story.Snapshot(world);
            }
        }
        public string Name(string id)
        {
            var p = Data.people.Find(v => v.id == id); if (p != null) return p.name;
            var r = Data.rooms.Find(v => v.id == id); if (r != null) return r.name;
            var t = Data.items.Find(v => v.id == id); return t == null ? id : t.name;
        }
        public string Resolve(string text, string actor, string target = "")
        { return (text ?? "").Replace("$actor", actor ?? "").Replace("$target", target ?? "").Replace("$here", State.Get("at:" + actor)).Replace("$previousTurn", (State.turn - 1).ToString()); }
        public bool Met(Condition c, string actor, string target = "")
        {
            string key = Resolve(c.key, actor, target);
            bool equal = (key.StartsWith("near:") ? (ItemHere(key.Substring(5), actor) ? "yes" : "no") : State.Get(key)) == Resolve(c.value, actor, target);
            return c.not ? !equal : equal;
        }
        public string Describe(Condition c, string actor, string target = "")
        {
            string key = Resolve(c.key, actor, target), val = Resolve(c.value, actor, target);
            if (key.StartsWith("near:")) return Name(key.Substring(5)) + (c.not ? " absent from the room and its occupants" : " in the room or carried by someone here");
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
            return Availability(c).All(v => v.met);
        }
        public List<Choice> Choices(string actor)
        {
            var result = Definitions.Where(c => c.actor == actor && Valid(c)).ToList();
            string here = State.Get("at:" + actor);
            foreach (var exit in Data.rooms.Find(r => r.id == here).exits)
                result.Add(new Choice { id = "move:" + exit, actor = actor, location = here, slot = "travel:" + here,
                    label = "Go to " + Name(exit), destination = exit, phase = 2, once = false,
                    actorText = "You cross to the " + Name(exit) + ".", observerText = Name(actor) + " goes to the " + Name(exit) + ".",
                    effects = new List<Effect> { new Effect("at:$actor", exit) } });
            foreach (var person in Data.people.Where(p => p.id != actor && State.Get("crossed:" + actor + ":" + p.id) == (State.turn - 1).ToString()))
            {
                string destination = State.Get("seen:" + actor + ":" + person.id);
                if (!Data.rooms.Find(r => r.id == here).exits.Contains(destination)) continue;
                result.Add(new Choice { id = "follow:" + person.id, actor = actor, target = person.id, location = here, destination = destination,
                    slot = "follow:" + person.id, from = State.turn, until = State.turn, phase = 2, once = false,
                    label = "Follow " + person.name + " toward the " + Name(destination), activity = "Following " + person.name,
                    requires = new List<Condition> { new Condition("crossed:" + actor + ":" + person.id, "$previousTurn") },
                    effects = new List<Effect> { new Effect("at:$actor", destination) } });
            }
            result.Add(new Choice { id = "wait", actor = actor, location = here, slot = "travel:" + here, once = false, quiet = true, phase = 3,
                label = "Wait here", actorText = "You stay, listening to the house around you.", observerText = Name(actor) + " stays in the " + Name(here) + "." });
            return result.Where(Valid).ToList();
        }
        public string Context(Choice c) { return string.IsNullOrEmpty(c.slot) ? c.id : c.slot; }
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
            foreach (var rule in Rules.Where(r => r.actor == choice.actor))
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
                    foreach (var c in rule.conditions) AddConditionCauses(term.causes, c, choice.actor, choice.target);
                option.terms.Add(term); option.conditions += term.amount;
            }
            var guidance = Matching(choice);
            if (guidance != null) { option.manual = guidance.amount; option.manualId = guidance.id; }
            return option;
        }
        public float Increment(Option chosen, List<Option> options)
        {
            return Math.Max(0, options.Max(o => o.Score) - chosen.Score) + 1;
        }
        void Record(Option chosen, List<Option> options)
        {
            var c = chosen.choice;
            int turn = State.turn;
            // A later decision in this context closes the earlier interval, including natural choices.
            foreach (var g in Save.guidance.Where(g => g.actor == c.actor && g.context == Context(c) && g.until >= turn)) g.until = turn - 1;
            chosen.recorded = Increment(chosen, options);
            Save.guidance.Add(new Guidance { id = Guid.NewGuid().ToString("N"), actor = c.actor, action = c.id,
                context = Context(c), location = c.location, from = turn, until = Math.Min(c.until, turn + 2), amount = chosen.recorded });
        }
        static void AddCause(List<int> list, int id) { if (id >= 0 && !list.Contains(id)) list.Add(id); }
        public int Priority(string actor)
        {
            var order = Data.people.Select(p => p.id).OrderBy(id => id, StringComparer.Ordinal).ToList();
            return (order.IndexOf(actor) - State.turn % order.Count + order.Count) % order.Count;
        }
        public void Step(string humanChoice = null)
        {
            if (Ended) return;
            var rankings = Data.people.ToDictionary(p => p.id, p => Rank(p.id));
            var proposals = Data.people.Select(p => rankings[p.id][0]).ToList();
            if (!string.IsNullOrEmpty(Save.player) && humanChoice != null)
            {
                var choices = rankings[Save.player]; var picked = choices.Find(o => o.choice.id == humanChoice);
                if (picked == null) return;
                Record(picked, choices);
                proposals[Data.people.FindIndex(p => p.id == Save.player)] = picked;
            }
            var ordered = proposals.OrderBy(o => o.choice.phase).ThenBy(o => Priority(o.choice.actor)).ToList();
            foreach (var proposal in ordered.Where(o => string.IsNullOrEmpty(o.choice.destination) && o.choice.phase < 3)) ResolveOne(proposal, rankings);
            MoveTogether(ordered.Where(o => !string.IsNullOrEmpty(o.choice.destination)).ToList(), rankings);
            foreach (var proposal in ordered.Where(o => string.IsNullOrEmpty(o.choice.destination) && o.choice.phase >= 3)) ResolveOne(proposal, rankings);
            State.turn++;
        }
        Event ResolveOne(Option proposal, Dictionary<string, List<Option>> rankings, bool? validAtDeparture = null)
            {
                var c = proposal.choice;
                bool valid = validAtDeparture ?? Valid(c);
                var e = new Event { id = State.events.Count, turn = State.turn, actor = c.actor, target = c.target,
                    kind = "action", destination = c.destination, quiet = c.quiet, activity = c.activity,
                    action = c.id, label = c.label, location = State.Get("at:" + c.actor), score = proposal.Score, manual = proposal.manual, recorded = proposal.recorded,
                    actorText = c.actorText, targetText = c.targetText, observerText = c.observerText, blocked = !valid,
                    alternatives = rankings[c.actor].Select(o => new DecisionOption { action = o.choice.id, label = o.choice.label,
                        conditions = o.conditions, manual = o.manual }).ToList(),
                    decision = string.Join("\n", proposal.terms.Where(t => t.active).Select(t => "+" + t.amount.ToString("0.#") + "  " + t.description)) };
                if (proposal.manual > 0) e.decision += "\n+" + proposal.manual.ToString("0.#") + "  previous choice";
                e.sceneBefore = Story.Snapshot(State);
                AddWitnesses(e, State);
                AddCause(e.causes, State.Source("at:" + c.actor));
                foreach (var term in proposal.terms.Where(t => t.active)) foreach (int cause in term.causes) AddCause(e.causes, cause);
                foreach (var condition in c.requires) AddConditionCauses(e.causes, condition, c.actor, c.target);
                if (valid)
                {
                    foreach (var effect in c.effects)
                    {
                        string key = Resolve(effect.key, c.actor, c.target), value = Resolve(effect.value, c.actor, c.target);
                        State.Set(key, value, e.id); e.effects.Add(key + " = " + value);
                    }
                    if (c.once) State.used.Add(c.actor + ":" + Context(c));
                    if (proposal.manualId != null) State.applied.Add(proposal.manualId);
                    AddWitnesses(e, State);
                }
                else
                {
                    e.actorText = "Your opportunity closes before you can act: " + c.label + ".";
                    e.observerText = Name(c.actor) + " cannot complete: " + c.label + ".";
                    e.targetText = e.observerText;
                }
                e.sceneAfter = Story.Snapshot(State);
                var before = Save.previous.Find(v => v.turn == e.turn && v.actor == e.actor && v.kind != "crossing");
                e.changed = before != null && (before.action != e.action || before.blocked != e.blocked);
                State.events.Add(e);
                return e;
            }
        void MoveTogether(List<Option> proposals, Dictionary<string, List<Option>> rankings)
        {
            var before = Story.Snapshot(State);
            var valid = proposals.ToDictionary(o => o.choice.actor, o => Valid(o.choice));
            var moves = proposals.Where(o => valid[o.choice.actor]).Select(o => o.choice).OrderBy(c => c.actor, StringComparer.Ordinal).ToList();
            var batch = new List<Event>();
            for (int a = 0; a < moves.Count; a++) for (int b = a + 1; b < moves.Count; b++)
            {
                var first = moves[a]; var second = moves[b];
                if (first.location != second.destination || second.location != first.destination) continue;
                var crossing = new Event { id = State.events.Count, turn = State.turn, actor = first.actor, target = second.actor,
                    kind = "crossing", action = "crossing", location = first.location, destination = first.destination,
                    label = Name(first.actor) + " and " + Name(second.actor) + " cross paths", travelBatch = true,
                    actorText = "You pass " + Name(second.actor) + " between the " + Name(first.location) + " and the " + Name(first.destination) + ". You continue to the " + Name(first.destination) + "; " + Name(second.actor) + " goes on to the " + Name(second.destination) + ".",
                    targetText = "You pass " + Name(first.actor) + " between the " + Name(second.location) + " and the " + Name(second.destination) + ". You continue to the " + Name(second.destination) + "; " + Name(first.actor) + " goes on to the " + Name(first.destination) + "." };
                State.events.Add(crossing); batch.Add(crossing);
                crossing.observerText = Name(first.actor) + " and " + Name(second.actor) + " pass each other between the " + Name(first.location) + " and the " + Name(first.destination) + ".";
                crossing.participants = moves.Where(c => c.location == first.location && c.destination == first.destination || c.location == first.destination && c.destination == first.location).Select(c => c.actor).ToList();
                foreach (var pair in new[] { new[] { first.actor, second.actor, second.destination }, new[] { second.actor, first.actor, first.destination } })
                {
                    State.Set("crossed:" + pair[0] + ":" + pair[1], State.turn.ToString(), crossing.id);
                    State.Set("seen:" + pair[0] + ":" + pair[1], pair[2], crossing.id);
                    crossing.effects.Add("crossed:" + pair[0] + ":" + pair[1] + " = " + State.turn);
                    crossing.effects.Add("seen:" + pair[0] + ":" + pair[1] + " = " + pair[2]);
                }
            }
            foreach (var proposal in proposals)
            {
                var e = ResolveOne(proposal, rankings, valid[proposal.choice.actor]); e.travelBatch = true; batch.Add(e);
            }
            var after = Story.Snapshot(State);
            foreach (var e in batch) { e.sceneBefore = before; e.sceneAfter = after; TravelWitnesses(e); }
        }
        void TravelWitnesses(Event e)
        {
            e.witnesses = new List<string>();
            foreach (var person in Data.people)
            {
                bool sees = e.kind == "crossing" ? person.id == e.actor || person.id == e.target || e.participants.Contains(person.id) :
                    person.id == e.actor || Story.Get(e.sceneBefore, "at:" + person.id) == e.location ||
                    (!e.blocked && Story.Get(e.sceneAfter, "at:" + person.id) == e.destination);
                // Opposite travelers saw the crossing, not each other's later arrival.
                if (e.kind != "crossing" && person.id != e.actor &&
                    Story.Get(e.sceneBefore, "at:" + person.id) == e.destination && Story.Get(e.sceneAfter, "at:" + person.id) == e.location) sees = false;
                if (sees) e.witnesses.Add(person.id);
            }
        }
        public List<Event> Forecast()
        {
            var campaign = new Campaign { player = "", world = State.Copy(), guidance = new List<Guidance>(Save.guidance.Where(g => g.actor != Save.player)),
                weights = Save.weights.Select(s => new NumberSetting { id = s.id, value = s.value }).ToList(), previous = Save.previous,
                choices = new List<Choice>(Save.choices), rules = new List<Rule>(Save.rules) };
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
