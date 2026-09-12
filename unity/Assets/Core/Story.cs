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
            return Get(e.sceneAfter, "at:" + viewer);
        }
        public static bool Departing(Event e, string actor, string room)
        {
            return e != null && e.actor == actor && !e.blocked && e.action.StartsWith("move:", StringComparison.Ordinal) &&
                Get(e.sceneBefore, "at:" + actor) == room && Get(e.sceneAfter, "at:" + actor) != room;
        }
        public static List<Person> Cast(Scenario data, List<Fact> scene, string room, Event e = null)
        {
            return data.people.Where(p => Get(scene, "at:" + p.id) == room || Departing(e, p.id, room)).ToList();
        }
        public static string Text(Simulation sim, Event e, string viewer)
        {
            if (e.blocked || !e.action.StartsWith("move:", StringComparison.Ordinal)) return e.Text(viewer);
            string from = Get(e.sceneBefore, "at:" + e.actor), to = Get(e.sceneAfter, "at:" + e.actor);
            if (e.actor == viewer) return "You leave the " + sim.Name(from) + " and enter the " + sim.Name(to) + ".";
            if (Room(e, viewer) == to) return sim.Name(e.actor) + " arrives from the " + sim.Name(from) + ".";
            return sim.Name(e.actor) + " leaves for the " + sim.Name(to) + ".";
        }
        public static string Activity(Simulation sim, Event e, Person person, string room)
        {
            if (e == null) return "Here";
            if (e.actor != person.id) return e.target == person.id ? "With " + sim.Name(e.actor) : "Here";
            if (e.blocked) return "Unable to act";
            if (e.action.StartsWith("move:", StringComparison.Ordinal))
                return Departing(e, person.id, room) ? "Leaving for " + sim.Name(e.action.Substring(5)) : "Arriving";
            if (e.action == "wait") return "Waiting";
            string activity = sim.Data.choices.Find(c => c.id == e.action && c.actor == e.actor)?.activity;
            return string.IsNullOrEmpty(activity) ? e.label : activity;
        }
    }
}
