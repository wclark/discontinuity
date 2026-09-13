using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    public partial class Simulation
    {
        public IEnumerable<Choice> Definitions => Data.choices.Where(c => !Save.choices.Any(v => v.actor == c.actor && v.id == c.id)).Concat(Save.choices);
        public IEnumerable<Rule> Rules => Data.rules.Where(r => !Save.rules.Any(v => v.id == r.id)).Concat(Save.rules);

        public bool ItemHere(string item, string actor)
        {
            string owner = State.Get("owner:" + item), here = State.Get("at:" + actor);
            return !string.IsNullOrEmpty(here) && (owner == here || Data.people.Any(p => p.id == owner && State.Get("at:" + p.id) == here));
        }
        void AddConditionCauses(List<int> causes, Condition c, string actor, string target)
        {
            string key = Resolve(c.key, actor, target);
            if (key.StartsWith("near:"))
            {
                key = "owner:" + key.Substring(5);
                AddCause(causes, State.Source("at:" + State.Get(key)));
            }
            if (c.value == "$here") AddCause(causes, State.Source("at:" + actor));
            AddCause(causes, State.Source(key));
        }
        public List<Criterion> Availability(Choice c)
        {
            string here = State.Get("at:" + c.actor);
            var criteria = new List<Criterion>
            {
                new Criterion("Actor: " + Name(c.actor), Data.people.Any(p => p.id == c.actor)),
                new Criterion("Time: " + Clock(c.from) + "-" + Clock(c.until) + " (turns " + (c.from + 1) + "-" + (c.until + 1) + ", inclusive)", !Ended && State.turn >= c.from && State.turn <= c.until),
                new Criterion("Location: " + (string.IsNullOrEmpty(c.location) ? "any room" : Name(c.location)), string.IsNullOrEmpty(c.location) || here == c.location)
            };
            if (!string.IsNullOrEmpty(c.target) && string.IsNullOrEmpty(c.destination))
                criteria.Add(new Criterion("Target: " + Name(c.target) + " is here", State.Get("at:" + c.target) == here));
            if (!string.IsNullOrEmpty(c.destination))
                criteria.Add(new Criterion("Adjacent exit: " + Name(c.destination), Data.rooms.Any(r => r.id == here && r.exits.Contains(c.destination))));
            criteria.Add(new Criterion(c.once ? "Unused decision slot: " + Context(c) : "Repeatable decision slot: " + Context(c), !c.once || !State.used.Contains(c.actor + ":" + Context(c))));
            criteria.AddRange(c.requires.Select(v => new Criterion(Describe(v, c.actor, c.target), Met(v, c.actor, c.target))));
            foreach (var effect in c.effects.Where(e => (e.key ?? "").StartsWith("owner:")))
            {
                string item = effect.key.Substring(6), owner = Resolve(effect.value, c.actor, c.target);
                criteria.Add(new Criterion("Transfer: " + Name(item) + " is here", ItemHere(item, c.actor)));
                if (Data.people.Any(p => p.id == owner)) criteria.Add(new Criterion("Recipient: " + Name(owner) + " is here", State.Get("at:" + owner) == here));
                if (Data.rooms.Any(r => r.id == owner)) criteria.Add(new Criterion("Place item in current room: " + Name(owner), owner == here));
            }
            return criteria;
        }
        public List<string> ValidateDefinition(Choice choice, IEnumerable<Rule> rules, bool writeChoice)
        {
            var errors = new List<string>();
            Action<bool, string> require = (ok, message) => { if (!ok) errors.Add(message); };
            require(choice.actor == Save.player, "Only the current incarnation can be edited.");
            require(!string.IsNullOrWhiteSpace(choice.id), "An action ID is required.");
            require(!string.IsNullOrWhiteSpace(choice.label), "An action label is required.");
            Action<int, int> window = (from, until) => require(from >= 0 && until >= from && until < Data.turns, "Time range must be ordered and inside this morning.");
            window(choice.from, choice.until);
            require(string.IsNullOrEmpty(choice.location) || Data.rooms.Any(r => r.id == choice.location), "Choose an existing location.");
            require(string.IsNullOrEmpty(choice.target) || Data.people.Any(p => p.id == choice.target && p.id != choice.actor), "Choose another person as the target.");
            if (writeChoice)
            {
                require(choice.id != "wait" && !choice.id.StartsWith("move:") && !choice.id.StartsWith("follow:"), "Generated movement and waiting definitions cannot be replaced; their score conditions can be edited.");
                require(!string.IsNullOrWhiteSpace(choice.actorText) && !string.IsNullOrWhiteSpace(choice.observerText), "Actor and observer prose are required.");
                require(string.IsNullOrEmpty(choice.target) || !string.IsNullOrWhiteSpace(choice.targetText), "Target prose is required when there is a target.");
                require(choice.phase >= 0 && choice.phase <= 3, "Choose a valid resolution phase.");
                require(choice.phase != 2 || !string.IsNullOrEmpty(choice.destination), "Movement needs a destination.");
                if (!string.IsNullOrEmpty(choice.destination))
                {
                    require(Data.rooms.Any(r => r.id == choice.destination), "Choose an existing destination.");
                    require(choice.phase == 2, "Travel resolves in the movement phase.");
                    require(choice.effects.Count(e => Resolve(e.key, choice.actor, choice.target) == "at:" + choice.actor && e.value == choice.destination) == 1, "Travel must move the actor to its destination exactly once.");
                    require(choice.effects.Count == 1, "Travel only changes location; use a room action for other effects.");
                }
                foreach (var effect in choice.effects)
                {
                    CheckKey(effect.key, choice, false, errors);
                    if ((effect.key ?? "").StartsWith("at:"))
                        require(!string.IsNullOrEmpty(choice.destination) && Resolve(effect.key, choice.actor, choice.target) == "at:" + choice.actor, "Use a travel action to move its actor; other people cannot be teleported.");
                }
            }
            foreach (var condition in choice.requires) CheckKey(condition.key, choice, true, errors);
            foreach (var rule in rules)
            {
                require(rule.actor == choice.actor && rule.action == choice.id && string.IsNullOrEmpty(rule.route), "A score condition must target this action.");
                require(!string.IsNullOrWhiteSpace(rule.id), "Score condition IDs cannot be empty.");
                var old = Rules.FirstOrDefault(r => r.id == rule.id);
                require(old == null || old.actor == choice.actor && old.action == choice.id, "A score condition ID belongs to another action.");
                require(!float.IsNaN(rule.amount) && !float.IsInfinity(rule.amount) && rule.amount >= 0 && rule.amount <= 100000, "Points must be between 0 and 100000.");
                window(rule.from, rule.until);
                foreach (var condition in rule.conditions) CheckKey(condition.key, choice, true, errors);
            }
            require(rules.Select(r => r.id).Distinct().Count() == rules.Count(), "Score condition IDs must be unique.");
            return errors.Distinct().ToList();
        }
        void CheckKey(string key, Choice choice, bool condition, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(key)) { errors.Add("A condition or effect has an empty fact key."); return; }
            string resolved = Resolve(key, choice.actor, choice.target);
            if (resolved.StartsWith("at:") && !Data.people.Any(p => p.id == resolved.Substring(3))) errors.Add("Unknown person in " + key + ".");
            if (resolved.StartsWith("owner:") && !Data.items.Any(i => i.id == resolved.Substring(6))) errors.Add("Unknown item in " + key + ".");
            if (resolved.StartsWith("near:") && (!condition || !Data.items.Any(i => i.id == resolved.Substring(5)))) errors.Add("Item presence is a read-only condition for an existing item.");
            if (resolved.Contains("$")) errors.Add("Unknown variable in " + key + ".");
        }
        public bool StoreDefinition(Choice choice, List<Rule> rules, bool writeChoice, out string error)
        {
            error = string.Join("\n", ValidateDefinition(choice, rules, writeChoice));
            if (error.Length > 0) return false;
            if (writeChoice)
            {
                Save.choices.RemoveAll(c => c.actor == choice.actor && c.id == choice.id);
                Save.choices.Add(choice);
            }
            var omitted = Rules.Where(r => r.actor == choice.actor && r.action == choice.id && !rules.Any(v => v.id == r.id)).ToList();
            Save.rules.RemoveAll(r => r.actor == choice.actor && r.action == choice.id);
            foreach (var old in omitted.Where(r => Data.rules.Any(v => v.id == r.id)))
            {
                Save.rules.Add(new Rule { id = old.id, actor = old.actor, action = old.action, from = old.from, until = old.until, amount = 0 });
                Save.weights.RemoveAll(w => w.id == old.id);
            }
            foreach (var rule in rules)
            {
                Save.rules.RemoveAll(r => r.id == rule.id); Save.rules.Add(rule);
                Save.weights.RemoveAll(w => w.id == rule.id);
            }
            return true;
        }
    }
}
