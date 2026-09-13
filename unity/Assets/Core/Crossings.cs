using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    public partial class Simulation
    {
        public static string Edge(string a, string b) { return "edge:" + string.Join(":", new[] { a, b }.OrderBy(v => v, StringComparer.Ordinal)); }
        public bool IsEdge(string id)
        {
            return Data.rooms.Any(r => r.exits.Any(e => Edge(r.id, e) == id));
        }
        public Event Crossing(string actor)
        {
            return InTransit ? State.transit.crossings.Select(id => State.events[id]).FirstOrDefault(e => e.participants.Contains(actor)) : null;
        }
        public string Here(string actor)
        {
            var crossing = Crossing(actor);
            return crossing == null ? State.Get("at:" + actor) : Edge(crossing.location, crossing.destination);
        }
        public List<string> Travelers => InTransit ? State.transit.crossings.SelectMany(id => State.events[id].participants).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList() : new List<string>();
        bool BeginTransit(List<Option> ordered, Dictionary<string, List<Option>> rankings)
        {
            var moves = ordered.Where(o => !string.IsNullOrEmpty(o.choice.destination)).ToList();
            foreach (var option in moves) option.departureValid = Valid(option.choice);
            var moving = moves.Where(o => o.departureValid).Select(o => o.choice).OrderBy(c => c.actor, StringComparer.Ordinal).ToList();
            var crossings = new List<Event>();
            var before = Story.Snapshot(State);
            var projected = before.Select(f => new Fact { key = f.key, value = f.value }).ToList();
            foreach (var c in moving) projected.Find(f => f.key == "at:" + c.actor).value = c.destination;
            for (int a = 0; a < moving.Count; a++) for (int b = a + 1; b < moving.Count; b++)
            {
                var first = moving[a]; var second = moving[b];
                if (first.location != second.destination || second.location != first.destination) continue;
                var e = new Event { id = State.events.Count, turn = State.turn, actor = first.actor, target = second.actor,
                    kind = "crossing", action = "crossing", location = first.location, destination = first.destination,
                    label = Name(first.actor) + " meets " + Name(second.actor), sceneBefore = before, sceneAfter = projected,
                    actorText = "You meet " + Name(second.actor) + " in the passage. You are heading for the " + Name(first.destination) + "; they are heading for the " + Name(second.destination) + ".",
                    targetText = "You meet " + Name(first.actor) + " in the passage. You are heading for the " + Name(second.destination) + "; they are heading for the " + Name(first.destination) + ".",
                    observerText = Name(first.actor) + " and " + Name(second.actor) + " cross paths." };
                e.participants = moving.Where(c => Edge(c.location, c.destination) == Edge(first.location, first.destination)).Select(c => c.actor).ToList();
                e.witnesses = new List<string>(e.participants);
                State.events.Add(e); crossings.Add(e);
                foreach (var pair in new[] { new[] { first.actor, second.actor, second.destination }, new[] { second.actor, first.actor, first.destination } })
                {
                    string crossed = "crossed:" + pair[0] + ":" + pair[1], seen = "seen:" + pair[0] + ":" + pair[1];
                    State.Set(crossed, State.turn.ToString(), e.id); State.Set(seen, pair[2], e.id);
                    e.effects.Add(crossed + " = " + State.turn); e.effects.Add(seen + " = " + pair[2]);
                }
            }
            if (crossings.Count == 0) return false;
            State.transit = new Transit { active = true, before = before, moves = moves,
                crossings = crossings.Select(e => e.id).ToList(),
                decisions = rankings.Select(p => new RankedDecision { actor = p.Key, options = p.Value }).ToList(),
                tail = ordered.Where(o => string.IsNullOrEmpty(o.choice.destination) && o.choice.phase >= 3).ToList() };
            if (!Travelers.Contains(Save.player)) ResolveReactions(null);
            return true;
        }
        List<Choice> Reactions(string actor)
        {
            if (!Travelers.Contains(actor)) return new List<Choice>();
            string here = Here(actor);
            var choices = Definitions.Where(c => c.actor == actor && c.reaction && Valid(c)).ToList();
            choices.Add(new Choice { id = "cross:continue", actor = actor, location = here, reaction = true, once = false, slot = "crossing",
                label = "Keep going", from = 0, until = Data.turns - 1, phase = 0, quiet = true,
                actorText = "You continue without stopping to speak.", observerText = Name(actor) + " continues without a word." });
            foreach (string other in Travelers.Where(p => p != actor && Here(p) == here))
            {
                choices.Add(new Choice { id = "cross:greet:" + other, actor = actor, target = other, location = here, reaction = true, once = false, slot = "crossing",
                    label = "Acknowledge " + Name(other), from = 0, until = Data.turns - 1, phase = 0,
                    actorText = "You catch " + Name(other) + "'s eye and greet them as you pass.", targetText = Name(actor) + " acknowledges you in passing.",
                    observerText = Name(actor) + " greets " + Name(other) + ".", effects = new List<Effect> { new Effect("greeted:" + actor + ":" + other, "yes") } });
                choices.Add(new Choice { id = "cross:cloth:" + other, actor = actor, target = other, location = here, reaction = true, once = false, slot = "crossing",
                    label = "Offer " + Name(other) + " the cloth", from = 0, until = Data.turns - 1, phase = 0,
                    actorText = "You press your clean cloth into " + Name(other) + "'s hand before continuing.", targetText = Name(actor) + " gives you a clean cloth as you pass.",
                    observerText = Name(actor) + " hands " + Name(other) + " a clean cloth.", requires = new List<Condition> { new Condition("owner:cloth", "$actor") },
                    effects = new List<Effect> { new Effect("owner:cloth", other), new Effect("helped:" + actor + ":" + other, "yes") } });
            }
            return choices.Where(Valid).ToList();
        }
        List<Fact> MidpointScene()
        {
            var facts = Story.Snapshot(State);
            foreach (var p in Travelers) facts.Find(f => f.key == "at:" + p).value = Here(p);
            return facts;
        }
        void ResolveReactions(string humanChoice)
        {
            var actors = Travelers;
            var rankings = actors.ToDictionary(actor => actor, Rank);
            var proposals = actors.Select(actor => rankings[actor][0]).ToList();
            if (actors.Contains(Save.player) && humanChoice != null)
            {
                var selected = rankings[Save.player].Find(o => o.choice.id == humanChoice);
                if (selected == null) return;
                Record(selected, rankings[Save.player]); proposals[actors.IndexOf(Save.player)] = selected;
            }
            foreach (var option in proposals.OrderBy(o => Priority(o.choice.actor)))
            {
                var before = MidpointScene();
                string here = Here(option.choice.actor);
                var e = ResolveOne(option, rankings);
                e.kind = "reaction"; e.location = here; e.sceneBefore = before; e.sceneAfter = MidpointScene();
                e.witnesses = actors.Where(actor => Here(actor) == here).ToList(); e.participants = new List<string>(e.witnesses);
            }
            FinishTransit();
        }
        void FinishTransit()
        {
            var transit = State.transit;
            var rankings = transit.decisions.ToDictionary(d => d.actor, d => d.options);
            transit.active = false;
            var events = new List<Event>();
            foreach (var move in transit.moves)
            {
                var e = ResolveOne(move, rankings, move.departureValid); e.travelBatch = true; events.Add(e);
            }
            var after = Story.Snapshot(State);
            foreach (var e in events) { e.sceneBefore = transit.before; e.sceneAfter = after; TravelWitnesses(e); }
            foreach (var option in transit.tail) ResolveOne(option, rankings);
            State.transit = null; State.turn++;
        }
    }
}
