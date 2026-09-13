using System.Collections.Generic;

namespace Discontinuity
{
    public static class RoomLife
    {
        // Low-weight, finite tasks use the same preconditions, effects and scoring as plot actions.
        public static void AddMissing(Scenario s)
        {
            Chain(s, "c20", "clara", "hall", 6, 15,
                new[] { "Read the notice for the accounts reading", "The notice promises an account of every wage. Your brother's name ought to be among them.", "Clara reads the notice beside the clock." },
                new[] { "Set out chairs for the reading", "You leave a clear aisle to the table. Whoever speaks will have to face the room.", "Clara arranges chairs around the accounts table." },
                new[] { "Check the names on the wage list", "Your brother's name is there. The space for the amount is empty.", "Clara traces a line on the wage list with her finger." },
                new[] { "Prepare cups for the household", "You line up the cups, giving your hands something steady to do.", "Clara sets cups beside the chairs." },
                new[] { "Write down the missing wage entry", "You copy the blank entry before anyone can tell you that you misread it.", "Clara makes a short note about the accounts." },
                new[] { "Place your note by the reading table", "The note lies where you can reach it when the reading begins.", "Clara places a folded note beside her chair." },
                new[] { "Open the curtains for the reading", "Light reaches the table. No one will have to read these accounts in shadow.", "Clara opens the Hall curtains." },
                new[] { "Take a place near the accounts table", "You choose a place where your voice will carry.", "Clara takes a place beside the accounts table." });
            Chain(s, "j20", "jonah", "hall", 5, 15,
                new[] { "Check the printer's delivery slip", "The errand was simple on paper. Copy the address, bring it back.", "Jonah checks the slip tucked in his satchel." },
                new[] { "Put your delivery papers in order", "You smooth the folded paper without showing it to the room.", "Jonah straightens the papers in his satchel." },
                new[] { "Read the estate's announcement", "The announcement names the time of the reading, but not why a printer was needed.", "Jonah reads the announcement by the clock." },
                new[] { "Check your ink-stained cuff", "The stain is dry now. You fold the edge inward.", "Jonah folds his stained cuff inward." },
                new[] { "Sharpen your pencil", "A small task makes the minutes less conspicuous.", "Jonah sharpens his pencil over a scrap of paper." },
                new[] { "Pack the delivery papers", "You put the papers together so nothing can be mistaken for something you took.", "Jonah carefully packs his delivery papers." },
                new[] { "Fasten your satchel", "You close the buckle and keep the bag beside you.", "Jonah fastens the buckle of his satchel." },
                new[] { "Take a place by the Hall door", "From here you can hear the reading, and see the way out.", "Jonah takes a place near the door." });
            Chain(s, "v20", "vale", "chapel", 0, 8,
                new[] { "Straighten the service books", "Each book returns to its proper place. Order is a small reassurance.", "Father Vale straightens the service books." },
                new[] { "Light the morning candle", "The taper catches. You watch until the flame steadies.", "Father Vale lights a candle beside the lectern." },
                new[] { "Review the order of service", "The familiar lines hold your attention for a few quiet minutes.", "Father Vale reviews the order of service." });
            Chain(s, "v30", "vale", "chapel", 9, 10,
                new[] { "Put the service papers away", "You square the papers and close the cover over them.", "Father Vale puts the service papers away." },
                new[] { "Check the chapel before leaving", "You check the windows and the aisle before turning toward the door.", "Father Vale checks the chapel before leaving." });
            Chain(s, "v40", "vale", "hall", 12, 15,
                new[] { "Arrange the papers for the reading", "You place the account sheets in the order you mean to read them.", "Father Vale orders the papers on the reading table." },
                new[] { "Mark your place in the accounts", "Your finger finds the next line. The room has not yet emptied.", "Father Vale marks a place in the accounts." },
                new[] { "Gather the account sheets", "You gather the loose sheets without turning your back on the room.", "Father Vale gathers the account sheets." });
            Chain(s, "m20", "merrow", "garden", 1, 4,
                new[] { "Wash after dressing the wound", "Cold water takes the last traces of the dressing from your hands.", "Dr. Merrow washes her hands beside the awning." },
                new[] { "Record the patient's injury", "You note the time and the condition of the wound.", "Dr. Merrow records the injury in her notebook." },
                new[] { "Check the new bandage", "The dressing is holding. The groundskeeper can rest here a little longer.", "Dr. Merrow checks the groundskeeper's bandage." },
                new[] { "Pack the medical bag", "You count the dressings and close the bag.", "Dr. Merrow packs her medical bag." });
            Chain(s, "m30", "merrow", "hall", 6, 15,
                new[] { "Check the time of the reading", "The clock agrees with your watch. You put the watch away.", "Dr. Merrow compares her watch with the Hall clock." },
                new[] { "Put your clinical notes in order", "You separate what you saw from what you were told.", "Dr. Merrow puts her clinical notes in order." },
                new[] { "Choose a clear view of the table", "You move a chair so you can see the papers as well as the speaker.", "Dr. Merrow moves a chair toward the reading table." },
                new[] { "Review the morning's appointments", "The next patient will have to wait until this household has finished.", "Dr. Merrow reviews her appointment book." },
                new[] { "Put away the appointment book", "You close the book. The room has your attention now.", "Dr. Merrow puts away her appointment book." },
                new[] { "Pour a glass of water", "You set the glass within reach of the people speaking.", "Dr. Merrow places water on the reading table." },
                new[] { "Check your medical bag", "Everything is where you left it. You close the clasp.", "Dr. Merrow checks and closes her medical bag." },
                new[] { "Take a seat facing the speaker", "You settle where you can see the speaker's face.", "Dr. Merrow takes a seat facing the speaker." });
            if (!s.rules.Exists(r => r.id == "c40")) s.rules.Add(new Rule { id = "c40", actor = "clara", action = "follow:jonah", from = 4, until = 10, amount = 7,
                conditions = new List<Condition> { new Condition("ledger_read", "yes"), new Condition("asked", "yes", true), new Condition("crossed:clara:jonah", "$previousTurn") } });
        }
        static void Chain(Scenario s, string prefix, string actor, string room, int from, int until, params string[][] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                string id = prefix + "_" + i, done = "done:" + id;
                var conditions = new List<Condition> { new Condition("at:" + actor, room), new Condition(done, "yes", true) };
                if (i > 0) conditions.Add(new Condition("done:" + prefix + "_" + (i - 1), "yes"));
                if (prefix == "m20") conditions.Add(new Condition("patient_tended", "yes"));
                if (!s.choices.Exists(c => c.id == id)) s.choices.Add(new Choice { id = id, actor = actor, location = room, slot = id, from = from, until = until,
                    label = tasks[i][0], activity = tasks[i][0], actorText = tasks[i][1], observerText = tasks[i][2], quiet = true,
                    requires = new List<Condition>(conditions), effects = new List<Effect> { new Effect(done, "yes") } });
                if (!s.rules.Exists(r => r.id == id)) s.rules.Add(new Rule { id = id, actor = actor, action = id, from = from, until = until, amount = 1, conditions = conditions });
            }
        }
    }
}
