using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    public partial class Simulation
    {
        public bool Adjusting => Save.mode == "adjustment";
        public bool InTransit => State.transit != null && State.transit.active;
        public string Now => InTransit ? MidpointClock(State.turn) : Clock(State.turn);
        public static string MidpointClock(int turn)
        {
            int seconds = 8 * 3600 + turn * 900 + 450;
            return (seconds / 3600).ToString("00") + ":" + (seconds / 60 % 60).ToString("00") + ":30";
        }
        void ImportAdjustments()
        {
            if (string.IsNullOrEmpty(Save.mode)) Save.mode = "adjustment";
            if (Save.adjustments == null) Save.adjustments = new List<Guidance>();
            if (Save.adjustmentsImported) return;
            if (Adjusting)
                foreach (var g in Save.guidance.Where(g => g.until >= g.from))
                    Save.adjustments.Add(new Guidance { id = g.id, actor = g.actor, action = g.action, context = g.context,
                        location = g.location, from = g.from, until = g.from, amount = g.amount });
            Save.adjustmentsImported = true;
        }
        Guidance Adjustment(Choice c)
        {
            return Save.adjustments.Where(g => g.actor == c.actor && g.action == c.id &&
                g.location == Here(c.actor) && State.turn == g.from)
                .OrderByDescending(g => g.from).ThenBy(g => g.id, StringComparer.Ordinal).FirstOrDefault();
        }
        void Adjust(Option chosen, List<Option> options)
        {
            float delta = Increment(chosen, options);
            if (delta <= 0) return;
            var c = chosen.choice;
            var adjustment = Adjustment(c);
            if (adjustment == null)
            {
                adjustment = new Guidance { id = Guid.NewGuid().ToString("N"), actor = c.actor, action = c.id, context = Context(c),
                    location = Here(c.actor), from = State.turn, until = State.turn };
                Save.adjustments.Add(adjustment);
            }
            adjustment.amount += delta;
            chosen.recorded = delta;
            // One stored amount per action, place and turn; no world-state fingerprints or per-frame updates.
            chosen.manualId = adjustment.id;
        }
        public static bool Spatial(Condition condition)
        {
            return condition.key.StartsWith("at:") || condition.key.StartsWith("owner:") || condition.key.StartsWith("near:");
        }
        bool StoryConditions(Choice c)
        {
            return (!c.once || !State.used.Contains(c.actor + ":" + Context(c))) && c.requires.Where(v => !Spatial(v)).All(v => Met(v, c.actor, c.target));
        }
        string StoryConditionDescription(Choice c)
        {
            if (!Adjusting) return "";
            var conditions = c.requires.Where(v => !Spatial(v)).Select(v => Describe(v, c.actor, c.target) + (Met(v, c.actor, c.target) ? " [yes]" : " [no]")).ToList();
            if (c.once) conditions.Add("slot " + Context(c) + " incomplete [" + (State.used.Contains(c.actor + ":" + Context(c)) ? "no" : "yes") + "]");
            return conditions.Count == 0 ? "" : " | " + string.Join("; ", conditions);
        }
        public IEnumerable<Guidance> VisibleAdjustments => Adjusting ? Save.adjustments : Save.guidance;
    }
}
