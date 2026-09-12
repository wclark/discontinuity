using System.Collections.Generic;

namespace Discontinuity
{
    public static class HouseholdContent
    {
        static Condition Is(string k, string v) { return new Condition(k, v); }
        static Condition Not(string k, string v) { return new Condition(k, v, true); }
        public static Scenario Create()
        {
            var s = new Scenario();
            s.rooms.Add(new Room { id = "kitchen", name = "Kitchen", x = .19f, y = .26f, exits = new[] { "hall", "garden" }, description = "Copper pans hang above the range. Beyond the green cupboards, a door opens toward the Hall." });
            s.rooms.Add(new Room { id = "hall", name = "Hall", x = .5f, y = .52f, exits = new[] { "kitchen", "archive", "chapel", "garden" }, description = "The house funnels everyone through this room. At noon, the estate accounts will be read aloud beneath the clock." });
            s.rooms.Add(new Room { id = "archive", name = "Archive", x = .81f, y = .26f, exits = new[] { "hall" }, description = "Tall shelves surround the writing desk. Rainlight falls across the estate's carefully ordered accounts." });
            s.rooms.Add(new Room { id = "garden", name = "Garden", x = .19f, y = .78f, exits = new[] { "kitchen", "hall", "chapel" }, description = "Rain catches on the box hedges. Beneath the awning, a stone bench faces the doors of the house." });
            s.rooms.Add(new Room { id = "chapel", name = "Chapel", x = .81f, y = .78f, exits = new[] { "hall", "garden" }, description = "Colored window light crosses the empty aisle. The lectern stands between two rows of silent pews." });
            s.people.Add(new Person { id = "clara", name = "Clara", role = "MAID", color = "#c76a59", location = "kitchen", concern = "Your brother's wages are missing from the estate accounts. The ledger may explain it." });
            s.people.Add(new Person { id = "jonah", name = "Jonah", role = "PRINTER'S APPRENTICE", color = "#488db6", location = "garden", concern = "Copy an address, finish an errand, leave quietly. Ink on your cuff makes you feel conspicuous." });
            s.people.Add(new Person { id = "vale", name = "Father Vale", role = "PRIEST", color = "#9a81b5", location = "chapel", concern = "Keep the parish's name out of the accounts. The blue envelope connects it to the missing wages." });
            s.people.Add(new Person { id = "merrow", name = "Dr. Merrow", role = "PHYSICIAN", color = "#509b80", location = "garden", concern = "Tend a patient, then attend the reading. A useful witness must have seen something, not merely suspected it." });
            s.items.Add(new Thing { id = "envelope", name = "Blue envelope", owner = "archive" });
            s.items.Add(new Thing { id = "ledger", name = "Estate ledger", owner = "archive" });
            s.items.Add(new Thing { id = "cloth", name = "Clean cloth", owner = "clara" });
            s.items.Add(new Thing { id = "medicine", name = "Medicine", owner = "merrow" });

            Add(s, "ignore", "clara", "Pass Jonah quietly", "hall", "cuff", 1, 4, 0,
                "You notice Jonah hiding the ink on his cuff. You let him have his silence.", "Clara notices the ink. She walks past without a word.", "Clara passes Jonah without speaking.", "jonah",
                new[] { Is("at:jonah", "hall") }, new[] { new Effect("cuff", "ignored") });
            Add(s, "help", "clara", "Give Jonah the clean cloth", "hall", "cuff", 1, 4, 0,
                "You turn your shoulder to the room and pass Jonah your cloth. No one else needs to notice.", "Clara places a cloth in your hand, hiding your stained cuff from the room. It is a small kindness, and not a small thing.", "Clara quietly gives Jonah a clean cloth.", "jonah",
                new[] { Is("at:jonah", "hall"), Is("owner:cloth", "clara") }, new[] { new Effect("owner:cloth", "jonah"), new Effect("cuff", "helped"), new Effect("trust", "yes") });
            Add(s, "mock", "clara", "Mock Jonah's ink-stained cuff", "hall", "cuff", 1, 4, 0,
                "You look at Jonah's cuff just long enough for the footman to laugh. Jonah stops trying to hide it.", "Clara looks at the ink on your cuff. The footman laughs before you can cover it. You will have to ask this house for kindness again.", "Clara makes Jonah's stained cuff the room's entertainment.", "jonah",
                new[] { Is("at:jonah", "hall") }, new[] { new Effect("cuff", "mocked"), new Effect("resentment", "yes") });
            Add(s, "threaten", "clara", "Warn Jonah to stay out of the Archive", "hall", "cuff", 1, 4, 0,
                "You tell Jonah that Vale is looking for someone to blame. He hears the warning as a threat.", "Clara says Vale needs someone to blame. Suddenly your harmless errand feels very dangerous.", "Clara warns Jonah away from the Archive.", "jonah",
                new[] { Is("at:jonah", "hall") }, new[] { new Effect("cuff", "threatened"), new Effect("afraid", "yes") });
            Add(s, "read_ledger", "clara", "Read the missing wage entries", "archive", "ledger", 2, 8, 1,
                "Three months of wages have been diverted to the parish. You close the ledger gently, as though the book could feel shame.", "", "Clara finds diverted wages in the estate ledger.", "",
                new[] { Is("owner:ledger", "archive") }, new[] { new Effect("ledger_read", "yes") });
            Add(s, "take_envelope_clara", "clara", "Take the blue envelope", "archive", "envelope", 0, 10, 1,
                "You tuck the envelope beneath your apron. The paper is stiff against your ribs.", "", "Clara takes the blue envelope.", "",
                new[] { Is("owner:envelope", "archive") }, new[] { new Effect("owner:envelope", "clara") });
            Add(s, "ask", "clara", "Ask Jonah to speak for you", "hall", "appeal", 5, 10, 0,
                "You ask Jonah to confirm what he saw in the Archive. Your future rests, briefly, in someone else's hands.", "Clara needs you to tell the room she was reading the accounts, not stealing them. You think of her face this morning.", "Clara asks Jonah to support her account.", "jonah",
                new[] { Is("at:jonah", "hall"), Is("ledger_read", "yes") }, new[] { new Effect("asked", "yes") });
            Add(s, "apologize", "clara", "Apologize for the joke", "hall", "repair", 4, 10, 0,
                "You say you were cruel. No excuse follows it. Jonah looks at you for a long moment.", "Clara apologizes without asking anything in return. You do not forgive everything. You do listen.", "Clara apologizes to Jonah.", "jonah",
                new[] { Is("at:jonah", "hall"), Is("resentment", "yes") }, new[] { new Effect("resentment", "no"), new Effect("trust", "yes") });
            Add(s, "copy", "jonah", "Copy the envelope's address", "archive", "errand", 2, 6, 1,
                "The envelope is addressed to the parish, not the estate. You copy the address exactly. A printer knows what a name can prove.", "", "Jonah copies the parish address from the envelope.", "",
                new[] { Is("owner:envelope", "archive") }, new[] { new Effect("address_copied", "yes") });
            Add(s, "take_envelope_jonah", "jonah", "Put the envelope in your satchel", "archive", "errand", 2, 6, 1,
                "You put the envelope in your satchel. The errand has become something less innocent.", "", "Jonah puts the envelope in his satchel.", "",
                new[] { Is("owner:envelope", "archive") }, new[] { new Effect("owner:envelope", "jonah") });
            Add(s, "vouch", "jonah", "Vouch for Clara", "hall", "answer", 5, 11, 0,
                "You say Clara was reading the accounts. Your voice steadies when she turns toward you.", "Jonah speaks for you. You remember how little it cost to hide the ink on his cuff.", "Jonah supports Clara's account in front of witnesses.", "clara",
                new[] { Is("at:clara", "hall"), Is("asked", "yes") }, new[] { new Effect("vouched", "yes"), new Effect("answered", "yes") });
            Add(s, "refuse", "jonah", "Say you cannot help", "hall", "answer", 5, 11, 0,
                "You say you cannot be certain. It is safer. You can hear how much that costs Clara.", "Jonah says he cannot help. Around you, the room grows interested.", "Jonah declines to support Clara.", "clara",
                new[] { Is("at:clara", "hall"), Is("asked", "yes") }, new[] { new Effect("answered", "yes") });
            Add(s, "expose", "jonah", "Turn the suspicion back on Clara", "hall", "answer", 5, 11, 0,
                "You tell them Clara was asking about the accounts early. You keep your voice polite. That makes it worse.", "Jonah repeats your questions to the room. You hear your morning joke in the space between his words.", "Jonah draws attention to Clara's interest in the accounts.", "clara",
                new[] { Is("at:clara", "hall"), Is("asked", "yes") }, new[] { new Effect("answered", "yes"), new Effect("clara_suspected", "yes") });
            Add(s, "take", "vale", "Remove the envelope", "archive", "envelope", 3, 9, 1,
                "You slip the envelope into your prayer book. You have told yourself this is protection.", "", "Father Vale removes the blue envelope from the Archive.", "",
                new[] { Is("owner:envelope", "archive") }, new[] { new Effect("owner:envelope", "vale") });
            Add(s, "leave", "vale", "Leave the envelope where it is", "archive", "envelope", 3, 9, 1,
                "You leave the paper on the desk. For once, let the accounts speak for themselves.", "", "Father Vale leaves the envelope on the desk.", "",
                new[] { Is("owner:envelope", "archive") }, new[] { new Effect("vale_left", "yes") });
            Add(s, "conceal", "vale", "Hide the envelope in the lectern", "chapel", "conceal", 5, 11, 1,
                "The lectern closes over the envelope. The house will have to accuse someone without it.", "", "Father Vale conceals the envelope in the lectern.", "",
                new[] { Is("owner:envelope", "vale") }, new[] { new Effect("owner:envelope", "lectern") });
            Add(s, "tend", "merrow", "Dress the groundskeeper's injury", "garden", "patient", 0, 5, 1,
                "The wound is clean. You leave the patient resting beneath the awning.", "", "Dr. Merrow tends the injured groundskeeper.", "",
                new[] { Is("owner:medicine", "merrow") }, new[] { new Effect("patient_tended", "yes") });
            Add(s, "witness", "merrow", "Notice what Vale is carrying", "hall", "sighting", 5, 10, 0,
                "Blue paper shows between Vale's prayer-book pages. You remember the color, and the time.", "Merrow's eyes rest on your prayer book. A corner of blue paper is showing.", "Dr. Merrow sees the blue envelope in Vale's prayer book.", "vale",
                new[] { Is("at:vale", "hall"), Is("owner:envelope", "vale") }, new[] { new Effect("seen_vale", "yes") });
            Add(s, "testify", "merrow", "Tell the Hall what you saw", "hall", "testimony", 9, 14, 0,
                "You tell the room exactly what you saw: blue paper, Vale's book, a quarter to ten. No speculation.", "", "Dr. Merrow tells the Hall that Vale carried the envelope.", "",
                new[] { Is("seen_vale", "yes") }, new[] { new Effect("testimony", "yes") });
            Add(s, "accuse", "vale", "Name Jonah before the household", "hall", "verdict", 13, 15, 0,
                "You say Jonah's name. The room has been waiting for a name, not for proof.", "Vale names you. You think of the address you copied, and of everyone who might speak.", "Father Vale accuses Jonah of stealing the blue envelope.", "jonah",
                new[] { Is("at:jonah", "hall"), Not("cleared", "yes"), Not("clara_suspected", "yes") }, new[] { new Effect("accused", "jonah") });
            Add(s, "accuse_clara", "vale", "Name Clara before the household", "hall", "verdict", 13, 15, 0,
                "Jonah has given you a different name to use. You let his anger do your work.", "Vale names you, repeating Jonah's words. A joke at breakfast has traveled farther than you intended.", "Father Vale turns Jonah's suspicion into an accusation against Clara.", "clara",
                new[] { Is("at:clara", "hall"), Is("clara_suspected", "yes"), Not("cleared", "yes") }, new[] { new Effect("accused", "clara") });
            Add(s, "defend", "merrow", "Join the evidence together", "hall", "verdict", 12, 15, 0,
                "Jonah's address, Clara's account, your sighting: three ordinary acts make a story Vale cannot divide.", "", "Merrow joins Jonah's address, Clara's corroborated account, and the sighting. The accusation fails.", "",
                new[] { Is("testimony", "yes"), Is("address_copied", "yes"), Is("vouched", "yes"), Is("at:jonah", "hall"), Is("at:clara", "hall") }, new[] { new Effect("cleared", "yes") });
            foreach (string actor in new[] { "clara", "jonah" })
                Add(s, "show_" + actor, actor, "Show the household the envelope", "hall", "verdict", 12, 15, 0,
                    "You place the blue envelope on the table. The missing wages have an address. No one can pretend otherwise now.", "", "The envelope is opened in front of the household. The diverted wages are exposed.", "",
                    new[] { Is("owner:envelope", actor) }, new[] { new Effect("cleared", "yes"), new Effect("owner:envelope", "hall") });

            Route(s, "c01", "clara", "hall", 0, 1, 4);
            Points(s, "c02", "clara", "ignore", 1, 4, 5, Is("at:jonah", "hall"));
            Route(s, "c03", "clara", "archive", 2, 6, 5, Not("ledger_read", "yes"));
            Points(s, "c04", "clara", "read_ledger", 2, 8, 6, Is("owner:ledger", "archive"));
            Route(s, "c05", "clara", "hall", 3, 15, 4, Is("ledger_read", "yes"));
            Points(s, "c06", "clara", "ask", 5, 10, 6, Is("ledger_read", "yes"), Is("at:jonah", "hall"));
            Points(s, "c07", "clara", "show_clara", 12, 15, 14, Is("owner:envelope", "clara"));
            Route(s, "j01", "jonah", "hall", 0, 1, 4);
            Route(s, "j02", "jonah", "archive", 2, 5, 5, Not("address_copied", "yes"), Not("afraid", "yes"), Is("owner:envelope", "archive"));
            Points(s, "j03", "jonah", "copy", 2, 6, 6, Is("owner:envelope", "archive"));
            Route(s, "j04", "jonah", "hall", 4, 15, 4, Is("address_copied", "yes"));
            Route(s, "j05", "jonah", "garden", 2, 8, 9, Is("afraid", "yes"));
            Points(s, "j06", "jonah", "refuse", 5, 11, 5, Is("asked", "yes"));
            Points(s, "j07", "jonah", "vouch", 5, 11, 9, Is("asked", "yes"), Is("trust", "yes"));
            Points(s, "j08", "jonah", "expose", 5, 11, 11, Is("asked", "yes"), Is("resentment", "yes"));
            Route(s, "j09", "jonah", "hall", 10, 15, 7);
            Points(s, "j10", "jonah", "show_jonah", 12, 15, 14, Is("owner:envelope", "jonah"));
            Route(s, "v01", "vale", "archive", 3, 8, 6, Is("owner:envelope", "archive"), Not("vale_left", "yes"));
            Points(s, "v02", "vale", "take", 3, 9, 7, Is("owner:envelope", "archive"));
            Route(s, "v03", "vale", "chapel", 5, 11, 6, Is("owner:envelope", "vale"));
            Points(s, "v04", "vale", "conceal", 5, 11, 7, Is("owner:envelope", "vale"));
            Route(s, "v05", "vale", "hall", 11, 15, 8);
            Points(s, "v06", "vale", "accuse", 13, 15, 9, Not("cleared", "yes"));
            Points(s, "v07", "vale", "accuse_clara", 13, 15, 9, Is("clara_suspected", "yes"));
            Points(s, "m01", "merrow", "tend", 0, 5, 6, Is("owner:medicine", "merrow"));
            Route(s, "m02", "merrow", "hall", 5, 15, 5, Is("patient_tended", "yes"));
            Points(s, "m03", "merrow", "witness", 5, 10, 8, Is("owner:envelope", "vale"), Is("at:vale", "hall"));
            Points(s, "m04", "merrow", "testify", 9, 14, 8, Is("seen_vale", "yes"));
            Points(s, "m05", "merrow", "defend", 12, 15, 15, Is("testimony", "yes"), Is("address_copied", "yes"), Is("vouched", "yes"));
            var activities = new Dictionary<string, string>
            {
                { "ignore", "Passing in silence" },
                { "help", "Offering a clean cloth" },
                { "mock", "Mocking Jonah's cuff" },
                { "threaten", "Warning Jonah away" },
                { "read_ledger", "Reading the wage entries" },
                { "take_envelope_clara", "Taking the envelope" },
                { "ask", "Asking Jonah for support" },
                { "apologize", "Apologizing to Jonah" },
                { "copy", "Copying the address" },
                { "take_envelope_jonah", "Pocketing the envelope" },
                { "vouch", "Speaking for Clara" },
                { "refuse", "Declining to help" },
                { "expose", "Casting suspicion on Clara" },
                { "take", "Removing the envelope" },
                { "leave", "Leaving the envelope" },
                { "conceal", "Hiding the envelope" },
                { "tend", "Tending the groundskeeper" },
                { "witness", "Noticing the blue paper" },
                { "testify", "Reporting what she saw" },
                { "accuse", "Accusing Jonah" },
                { "accuse_clara", "Accusing Clara" },
                { "defend", "Joining the evidence" },
                { "show_clara", "Showing the envelope" },
                { "show_jonah", "Showing the envelope" },
            };
            foreach (var choice in s.choices) choice.activity = activities[choice.id];
            return s;
        }
        static void Add(Scenario s, string id, string actor, string label, string location, string slot, int from, int until, int phase,
            string actorText, string targetText, string observerText, string target, Condition[] requires, Effect[] effects)
        {
            s.choices.Add(new Choice { id = id, actor = actor, label = label, location = location, slot = slot, from = from, until = until,
                phase = phase, actorText = actorText, targetText = targetText, observerText = observerText, target = target,
                requires = new List<Condition>(requires), effects = new List<Effect>(effects) });
        }
        static void Points(Scenario s, string id, string actor, string action, int from, int until, float amount, params Condition[] conditions)
        { s.rules.Add(new Rule { id = id, actor = actor, action = action, from = from, until = until, amount = amount, conditions = new List<Condition>(conditions) }); }
        static void Route(Scenario s, string id, string actor, string location, int from, int until, float amount, params Condition[] conditions)
        { s.rules.Add(new Rule { id = id, actor = actor, route = location, from = from, until = until, amount = amount, conditions = new List<Condition>(conditions) }); }
    }
}
