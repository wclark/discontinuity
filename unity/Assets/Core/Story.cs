using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    // Presentation reads event-time positions, never the next turn's plans or scores.
    public static class Story
    {
        public static List<Fact> Snapshot(World world)
        {
            return world.facts.Where(f => f.key.StartsWith("at:", StringComparison.Ordinal) || f.key.StartsWith("owner:", StringComparison.Ordinal))
                .Select(f => new Fact { key = f.key, value = f.value }).ToList();
        }
        public static string Get(List<Fact> scene, string key)
        {
            return scene?.Find(f => f.key == key)?.value ?? "";
        }
        public static string Room(Event e, string viewer)
        {
            if (e.kind == "crossing") return e.location;
            if (e.travelBatch && e.actor != viewer && Get(e.sceneAfter, "at:" + viewer) != e.destination) return e.location;
            return Get(e.sceneAfter, "at:" + viewer);
        }
        public static List<Fact> Scene(Event e, string viewer)
        {
            return e.kind == "crossing" || (e.travelBatch && e.actor != viewer && Get(e.sceneAfter, "at:" + viewer) != e.destination) ? e.sceneBefore : e.sceneAfter;
        }
        public static bool Moving(Event e) { return !string.IsNullOrEmpty(e.destination) && e.kind != "crossing" || e.action.StartsWith("move:", StringComparison.Ordinal); }
        public static bool Departing(Event e, string actor, string room)
        {
            return e != null && e.actor == actor && !e.blocked && Moving(e) &&
                Get(e.sceneBefore, "at:" + actor) == room && Get(e.sceneAfter, "at:" + actor) != room;
        }
        public static List<Person> Cast(Scenario data, List<Fact> scene, string room, Event e = null)
        {
            if (e != null && e.kind == "crossing") return data.people.Where(p => p.id == e.actor || p.id == e.target || e.participants.Contains(p.id)).ToList();
            return data.people.Where(p => Get(scene, "at:" + p.id) == room || Departing(e, p.id, room)).ToList();
        }
        public static string Text(Simulation sim, Event e, string viewer)
        {
            if (e.blocked || !Moving(e)) return e.Text(viewer);
            string from = Get(e.sceneBefore, "at:" + e.actor), to = Get(e.sceneAfter, "at:" + e.actor);
            if (e.actor == viewer)
            {
                var companions = sim.Data.people.Where(p => p.id != viewer && Get(e.sceneBefore, "at:" + p.id) != to && Get(e.sceneAfter, "at:" + p.id) == to).Select(p => p.name).ToList();
                string follow = e.action.StartsWith("follow:", StringComparison.Ordinal) ? "You turn back toward the " + sim.Name(to) + ", where you last saw " + sim.Name(e.target) + " heading." : "You leave the " + sim.Name(from) + " and enter the " + sim.Name(to) + ".";
                return follow + (companions.Count == 0 ? "" : " " + string.Join(" and ", companions) + " arrives at the same time.");
            }
            if (Room(e, viewer) == to) return sim.Name(e.actor) + " arrives from the " + sim.Name(from) + ".";
            return sim.Name(e.actor) + " leaves for the " + sim.Name(to) + ".";
        }
        public static string Activity(Simulation sim, Event e, Person person, string room)
        {
            if (e == null) return "Here";
            if (e.kind == "crossing") return "Passing toward the " + sim.Name(Get(e.sceneAfter, "at:" + person.id));
            if (e.actor != person.id) return e.target == person.id ? "With " + sim.Name(e.actor) : "Here";
            if (e.blocked) return "Unable to act";
            if (Moving(e)) return Departing(e, person.id, room) ? "Leaving for " + sim.Name(Get(e.sceneAfter, "at:" + person.id)) : "Arriving";
            if (e.action == "wait") return "Waiting";
            string activity = sim.Data.choices.Find(c => c.id == e.action && c.actor == e.actor)?.activity;
            return string.IsNullOrEmpty(activity) ? e.label : activity;
        }
        public static string Situation(Simulation sim)
        {
            string actor = sim.Save.player, room = sim.State.Get("at:" + actor);
            var others = sim.Data.people.Where(p => p.id != actor && sim.State.Get("at:" + p.id) == room).Select(p => p.name).ToList();
            string company = others.Count == 0 ? "You are alone here." : string.Join(" and ", others) + (others.Count == 1 ? " is" : " are") + " here with you.";
            if (room == "hall" && sim.State.turn <= 4 && others.Contains("Jonah") && actor != "jonah") company += " Jonah keeps pulling at his ink-stained cuff.";
            if (room == "archive") company += sim.State.Get("owner:envelope") == "archive" ? " A blue envelope lies on the desk among the accounts." : " There is no blue envelope on the desk.";
            var crossed = sim.Experienced(actor).LastOrDefault(e => e.kind == "crossing" && e.turn == sim.State.turn - 1);
            if (crossed != null)
            {
                string other = crossed.actor == actor ? crossed.target : crossed.actor;
                company += " You just passed " + sim.Name(other) + " heading for the " + sim.Name(Story.Get(crossed.sceneAfter, "at:" + other)) + ".";
            }
            return company;
        }
        public static string Concern(Simulation sim)
        {
            string actor = sim.Save.player;
            if (actor == "clara" && sim.State.Get("ledger_read") == "yes") return "The ledger confirms the missing wages. You need someone who can support what you found.";
            if (actor == "jonah" && sim.State.Get("address_copied") == "yes") return "You have the parish address. Your errand is done, but the household has not let you go.";
            if (actor == "merrow" && sim.State.Get("seen_vale") == "yes") return "You saw blue paper in Vale's book. What you say about it may matter.";
            return sim.Data.people.Find(p => p.id == actor).concern;
        }
    }
}
