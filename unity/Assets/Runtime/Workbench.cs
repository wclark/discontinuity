using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    [RequireComponent(typeof(UIDocument))]
    public partial class Workbench : MonoBehaviour
    {
        Simulation sim;
        VisualElement root;
        SceneArt art;
        string savePath, notice = "";
        bool sandbox;
        VisualElement modal;
        Button modalReturn;
        readonly Stack<string> undo = new Stack<string>();

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        void OnEnable()
        {
            var asset = Resources.Load<Household>("Household");
            var data = asset == null ? HouseholdContent.Create() : asset.definition;
            savePath = Path.Combine(Application.persistentDataPath, "household-v1.json");
            sandbox = Argument("-demo") != null || Argument("-capture") != null || Environment.GetCommandLineArgs().Contains("-smoke");
            Campaign saved = null;
            if (!sandbox && File.Exists(savePath))
                try { saved = JsonUtility.FromJson<Campaign>(File.ReadAllText(savePath)); }
                catch (Exception ex) { notice = "Save could not be loaded: " + ex.Message; }
            sim = new Simulation(data, saved);
            // Preserve an older observation run's world, but resume it as a real incarnation.
            if (!data.people.Any(p => p.id == sim.Save.player))
            {
                sim.Save.player = data.people[0].id;
                sim.Save.guidance.RemoveAll(g => g.actor == sim.Save.player);
            }
            root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("Workbench"));
            root.AddToClassList("app");
            art = new SceneArt();
            if (Argument("-demo") != null) Fixture(Argument("-demo"));
            Render();
            if (Environment.GetCommandLineArgs().Contains("-smoke")) StartCoroutine(Smoke());
            else if (Argument("-capture") != null) StartCoroutine(Capture());
        }

        void OnDisable() { art?.Dispose(); }

        VisualElement Box(VisualElement parent, string cls)
        {
            var element = new VisualElement(); element.AddToClassList(cls); parent.Add(element); return element;
        }
        Label Text(VisualElement parent, string value, string cls)
        {
            var label = new Label(value ?? ""); label.AddToClassList(cls); parent.Add(label); return label;
        }
        Button Button(VisualElement parent, string value, Action click, string cls, string tip = null)
        {
            var button = new Button(click) { text = value, tooltip = tip ?? value };
            button.AddToClassList(cls); parent.Add(button); return button;
        }
        Foldout Fold(VisualElement parent, string title)
        {
            var fold = new Foldout { text = title, value = false }; fold.AddToClassList("disclosure"); parent.Add(fold); return fold;
        }
        void Persist()
        {
            if (sandbox) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllText(savePath, JsonUtility.ToJson(sim.Save, true));
            }
            catch (Exception ex) { notice = "Could not save: " + ex.Message; }
        }
        void Advance(string action)
        {
            if (sim.Ended || sim.Save.reviewPending) return;
            undo.Push(RecordJson(false));
            sim.Step(action); sim.Save.reviewPending = true; sim.Save.reviewIndex = 0;
            Persist(); Render();
        }
        bool Quiet(Event e) { return e.quiet || e.action == "wait"; }
        List<Event> Moments() { return sim.Experienced(sim.Save.player).Where(e => e.turn == sim.State.turn - 1 && (!Quiet(e) || e.actor == sim.Save.player)).ToList(); }
        List<Event> QuietMoments() { return sim.Experienced(sim.Save.player).Where(e => e.turn == sim.State.turn - 1 && Quiet(e) && e.actor != sim.Save.player).ToList(); }
        void NextMoment()
        {
            if (!sim.Save.reviewPending) return;
            sim.Save.reviewIndex++;
            if (sim.Save.reviewIndex >= Moments().Count) { sim.Save.reviewPending = false; sim.Save.reviewIndex = 0; }
            Persist(); Render();
        }
        void Undo()
        {
            if (undo.Count == 0) return;
            var restored = JsonUtility.FromJson<Campaign>(undo.Pop());
            sim = new Simulation(restored.scenario, restored);
            Persist(); Render();
        }
        void Continue()
        {
            if (!sim.ContinueAsNext()) return;
            undo.Clear(); notice = ""; Persist(); Render();
        }

        void Render()
        {
            modal = null; root.Clear();
            string player = sim.Save.player;
            var person = sim.Data.people.Find(p => p.id == player);
            var moments = Moments();
            if (sim.Save.reviewPending)
            {
                if (moments.Count == 0) sim.Save.reviewPending = false;
                sim.Save.reviewIndex = Math.Max(0, Math.Min(sim.Save.reviewIndex, moments.Count - 1));
            }
            var beat = sim.Save.reviewPending && moments.Count > 0 ? moments[Math.Min(sim.Save.reviewIndex, moments.Count - 1)] : null;
            var snapshot = beat == null ? Story.Snapshot(sim.State) : Story.Scene(beat, player);
            var room = sim.Data.rooms.Find(r => r.id == (beat == null ? sim.State.Get("at:" + player) : Story.Room(beat, player)));
            int turn = beat == null ? sim.State.turn : beat.turn;
            var header = Box(root, "header");
            var brand = Box(header, "brand");
            var icon = new Image { image = Resources.Load<Texture2D>("DiscontinuityIcon"), scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("brand-icon"); brand.Add(icon);
            Text(brand, "Discontinuity", "brand-title");
            Text(header, person.name + " / " + person.role + " / LIFE " + sim.Save.day, "incarnation");
            var clock = Box(header, "clock");
            Text(clock, Simulation.Clock(turn) + (beat == null ? "" : " - " + Simulation.Clock(turn + 1)), "time");
            Text(clock, "SATURDAY, 17 OCTOBER", "date");
            Button(header, "\u21b6", Undo, "icon-button", "Undo last turn").SetEnabled(undo.Count > 0);
            Button(header, "\u2193", OpenRecords, "icon-button", "State records");

            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("page-scroll"); root.Add(scroll);
            var page = Box(scroll, "story-layout");
            page.RegisterCallback<GeometryChangedEvent>(e => page.EnableInClassList("narrow", e.newRect.width < 850));
            var world = Box(page, "story-world");
            var side = Box(page, "story-side");
            Text(world, beat != null && beat.kind == "crossing" ? sim.Name(beat.location) + " / " + sim.Name(beat.destination) : room.name, "scene-title");
            art.Render(world, sim, room, snapshot, beat);
            Text(world, beat != null && beat.kind == "crossing" ? "The passage between the two rooms. A brief encounter, then each of you continues." : room.description, "room-description");
            var context = Box(world, "context");
            var carrying = sim.Data.items.Where(t => Story.Get(snapshot, "owner:" + t.id) == player).Select(t => t.name).ToList();
            Context(context, "CARRYING", carrying.Count == 0 ? "Nothing" : string.Join(", ", carrying));
            if (beat == null || beat.kind != "crossing")
            {
                var visible = sim.Data.items.Where(t => Story.Get(snapshot, "owner:" + t.id) == room.id).Select(t => t.name).ToList();
                if (visible.Count > 0) Context(context, "NEARBY", string.Join(", ", visible));
            }
            var experienced = sim.Experienced(player);
            if (beat != null)
            {
                Text(side, "TURN " + (turn + 1) + " / MOMENT " + (sim.Save.reviewIndex + 1) + " OF " + moments.Count, "eyebrow");
                Text(side, beat.kind == "crossing" ? "Crossing paths" : Story.Moving(beat) ? "Through the house" : beat.actor == player ? "Your part in the morning" : sim.Name(beat.actor), "story-title");
                var narrative = Box(side, "narrative"); narrative.userData = beat.id;
                Text(narrative, Story.Text(sim, beat, player), "prose");
                bool last = sim.Save.reviewIndex == moments.Count - 1;
                if (last && QuietMoments().Count > 0)
                {
                    var quiet = Box(side, "quiet-moments");
                    Text(quiet, "ALSO WITNESSED", "caption");
                    foreach (var e in QuietMoments())
                    {
                        var line = Text(quiet, sim.Name(Story.Room(e, player)) + " / " + Story.Text(sim, e, player), "quiet-copy");
                        line.userData = e.id;
                    }
                }
                Button(side, last ? (sim.Ended ? "Finish the morning" : "Continue to " + Simulation.Clock(sim.State.turn)) : "Next moment", NextMoment, "moment-button");
            }
            else if (sim.Ended)
            {
                Text(side, "The morning ends", "story-title");
                var verdict = experienced.LastOrDefault(e => e.action == "accuse" || e.action == "accuse_clara" || e.action == "defend" || e.action.StartsWith("show_"));
                Text(side, verdict == null ? "The clock strikes noon. Whatever happened elsewhere, your part in this morning is over." : Story.Text(sim, verdict, player), "prose");
                Text(side, "Someone else lived through it, too.", "concern");
                Button(side, "Wake as " + sim.Name(sim.NextIncarnation), Continue, "continue-button");
            }
            else
            {
                Text(side, "TURN " + (turn + 1) + " OF " + sim.Data.turns + " / " + Simulation.Clock(turn), "eyebrow");
                Text(side, "What will you do?", "story-title");
                Text(side, Story.Situation(sim), "prose");
                Text(side, Story.Concern(sim), "concern");
                RenderChoices(side);
            }
            RenderJournal(world, experienced);
            if (notice != "") Text(side, notice, "notice");
        }

        void Context(VisualElement parent, string title, string value)
        {
            var group = Box(parent, "context-group"); Text(group, title, "caption"); Text(group, value, "context-value");
        }
        void RenderChoices(VisualElement parent)
        {
            var tools = Box(parent, "action-tools");
            Button(tools, "Actions", OpenCatalog, "tool-button", "Inspect all your actions");
            Button(tools, "+", () => OpenEditor(null), "icon-button", "Create action");
            foreach (var option in sim.Rank(sim.Save.player))
            {
                var row = Box(parent, "action-row");
                var button = Button(row, option.choice.label, () => Advance(option.choice.id), "choice");
                button.userData = sim.Save.player;
                Text(row, option.Score.ToString("0.##"), "choice-score").tooltip = "Score";
                Button details = null;
                details = Button(row, "i", () => { modalReturn = details; OpenFactors(option); }, "inspect-button", "Decision factors: " + option.choice.label);
            }
        }
        VisualElement OpenModal(string title)
        {
            if (modal != null) CloseModal();
            foreach (var child in root.Children()) child.SetEnabled(false);
            modal = Box(root, "modal-backdrop"); modal.focusable = true;
            var panel = Box(modal, "factor-modal");
            var heading = Box(panel, "modal-heading");
            Text(heading, title, "modal-title");
            var close = Button(heading, "\u00d7", CloseModal, "modal-close", "Close decision factors");
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("modal-scroll"); panel.Add(scroll);
            modal.RegisterCallback<ClickEvent>(e => { if (e.target == modal) CloseModal(); });
            modal.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { CloseModal(); e.StopPropagation(); } });
            close.Focus();
            return scroll;
        }
        void CloseModal()
        {
            if (modal == null) return;
            modal.RemoveFromHierarchy(); modal = null;
            foreach (var child in root.Children()) child.SetEnabled(true);
            modalReturn?.Focus();
        }
        void OpenFactors(Option option)
        {
            var content = OpenModal(option.choice.label);
            Text(content, sim.Name(sim.Save.player) + " / " + Simulation.Clock(sim.State.turn), "eyebrow");
            Text(content, option.Score.ToString("0.#") + " = 0 + " + option.conditions.ToString("0.#") + " conditions + " + option.manual.ToString("0.#") + " adjustment", "score-equation");
            foreach (var term in option.terms)
            {
                var rule = sim.Rules.First(r => r.id == term.id);
                string score = term.active ? "+" + term.amount.ToString("0.#") : "0 (inactive; potential +" + sim.Weight(rule).ToString("0.#") + ")";
                var label = Text(content, score + " / " + term.id + "\n" + term.description, "factor-copy");
                label.userData = sim.Save.player;
            }
            if (option.terms.Count == 0) Text(content, "No condition contributes to this choice right now.", "factor-copy");
            if (sim.Valid(option.choice)) Text(content, "Increment if chosen: +" + sim.Increment(option, sim.Rank(sim.Save.player)).ToString("0.##"), "adjustment");
            var records = sim.Save.guidance.Where(g => g.actor == sim.Save.player && g.action == option.choice.id && g.until >= g.from).ToList();
            foreach (var g in records) Text(content, "Earlier choice: +" + g.amount.ToString("0.#") + " / " + Simulation.Clock(g.from) + "-" + Simulation.Clock(g.until), "adjustment");
            Text(content, "Resolves during " + (!string.IsNullOrEmpty(option.choice.destination) ? "simultaneous movement." : option.choice.phase == 0 ? "conversation, before departures." : option.choice.phase == 3 ? "the end of the turn." : "room activity, before departures."), "factor-copy");
            if (string.IsNullOrEmpty(option.choice.destination)) Text(content, "Same-phase priority this turn: " + (sim.Priority(sim.Save.player) + 1) + " of " + sim.Data.people.Count + ". Priority rotates each turn.", "factor-copy");
            Text(content, "AVAILABILITY / " + (sim.Valid(option.choice) ? "AVAILABLE" : "UNAVAILABLE"), "section-label");
            foreach (var criterion in sim.Availability(option.choice))
                Text(content, (criterion.met ? "YES / " : "NO / ") + criterion.description, criterion.met ? "criterion-met" : "criterion-unmet");
            Text(content, "EFFECTS", "section-label");
            foreach (var effect in option.choice.effects) Text(content, sim.Describe(new Condition(effect.key, effect.value), option.choice.actor, option.choice.target), "factor-copy");
            if (option.choice.effects.Count == 0) Text(content, "No facts change.", "factor-copy");
            if (!sim.Save.reviewPending && !sim.Ended) Button(content, "Edit action", () => OpenEditor(option.choice), "tool-button");
        }
        void RenderJournal(VisualElement page, List<Event> experienced)
        {
            var earlier = experienced.Where(e => e.turn < sim.State.turn - (sim.Save.reviewPending ? 1 : 0))
                .OrderByDescending(e => e.turn).ThenBy(e => e.id).ToList();
            if (earlier.Count == 0) return;
            var journal = Fold(page, "Earlier today"); journal.AddToClassList("journal");
            foreach (var e in earlier)
            {
                var entry = Box(journal, "journal-entry"); entry.userData = e.id;
                Text(entry, Simulation.Clock(e.turn) + " / " + sim.Name(e.actor) + " / " + sim.Name(Story.Room(e, sim.Save.player)), "event-time");
                Text(entry, Story.Text(sim, e, sim.Save.player), "journal-prose");
                if (e.actor == sim.Save.player && e.alternatives != null && e.alternatives.Count > 0)
                {
                    var factors = Button(entry, "i", () =>
                    {
                        var content = OpenModal(e.label + " / " + Simulation.Clock(e.turn));
                        float recorded = e.recorded > 0 ? e.recorded : sim.Save.guidance.Find(g => g.actor == e.actor && g.action == e.action && g.from == e.turn)?.amount ?? 0;
                        if (recorded > 0) Text(content, "Recorded choice increment: +" + recorded.ToString("0.##"), "adjustment");
                        foreach (var option in e.alternatives)
                        {
                            var line = Text(content, option.label + " / " + (option.conditions + option.manual).ToString("0.#") + " = 0 + " + option.conditions.ToString("0.#") + " conditions + " + option.manual.ToString("0.#") + " adjustment", "factor-copy");
                            line.userData = e.actor;
                        }
                    }, "inspect-button", "Past decision factors");
                    factors.AddToClassList("past-factors"); factors.userData = e.actor;
                }
            }
        }

        // Reproducible fixtures are available to command-line verification, not the game UI.
        void Fixture(string kind)
        {
            sim = new Simulation(sim.Data); undo.Clear();
            if (kind == "exchange")
            {
                sim.Step("move:hall"); sim.Step("help"); sim.Save.reviewPending = true; return;
            }
            if (kind == "crossing")
            {
                sim.Save.player = "vale";
                while (!sim.Ended) sim.Step(sim.State.turn == 3 ? "wait" : null);
                sim.Begin("clara");
                sim.Step("move:hall"); sim.Step("ignore"); sim.Step("wait"); sim.Step("wait"); sim.Step("move:archive");
                sim.Save.reviewPending = true; sim.Save.reviewIndex = Math.Max(0, Moments().FindIndex(e => e.kind == "crossing")); return;
            }
            if (kind == "kitchen") return;
            if (kind == "garden") sim.Save.player = "jonah";
            if (kind == "chapel") sim.Save.player = "vale";
            if (new[] { "baseline", "archive", "gathering", "garden", "chapel" }.Contains(kind))
            {
                int turns = kind == "archive" ? 4 : kind == "gathering" ? 14 : kind == "chapel" ? 9 : 1;
                while (sim.State.turn < turns) sim.Step();
                sim.Save.reviewPending = true;
                string action = kind == "archive" ? "copy" : kind == "gathering" ? "accuse" : kind == "garden" ? "tend" : kind == "chapel" ? "conceal" : "move:hall";
                sim.Save.reviewIndex = Math.Max(0, Moments().FindIndex(e => e.action == action && (kind != "baseline" || e.actor == "jonah")));
                return;
            }
            while (!sim.Ended) sim.Step(sim.State.turn == 1 ? (kind == "kindness" ? "help" : kind == "warning" ? "threaten" : "mock") : null);
            sim.ContinueAsNext();
            while (sim.State.turn < (kind == "warning" ? 3 : 6)) sim.Step();
            if (kind == "kindness" || kind == "humiliation")
            {
                sim = new Simulation(sim.Data, sim.Save);
                while (sim.State.turn < 7) sim.Step();
            }
            sim.Save.reviewPending = true;
        }
        void Click(string label)
        {
            var button = root.Query<Button>().ToList().FirstOrDefault(b => b.text == label || b.tooltip == label);
            if (button == null) { Application.Quit(1); throw new Exception("UI button not found: " + label); }
            button.Focus();
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
        }
        IEnumerator ReadTurn()
        {
            while (sim.Save.reviewPending)
            {
                Click(root.Q<Button>(className: "moment-button").text);
                yield return new WaitForSecondsRealtime(.05f);
            }
        }
        IEnumerator Smoke()
        {
            yield return null;
            var results = new List<string>();
            Action<bool, string> check = (ok, name) =>
            {
                if (!ok) { Application.Quit(1); throw new Exception("UI FAILED: " + name); }
                results.Add("PASS " + name); Debug.Log("UI PASS: " + name);
            };
            check(sim.Save.player == "clara" && !sim.ContinueAsNext(), "one incarnation, locked until completion");
            check(root.Query<Button>(className: "choice").ToList().All(b => (string)b.userData == "clara"), "only your actions are offered");
            check(root.Query<Label>(className: "factor-copy").ToList().Count == 0 && root.Query<Label>(className: "choice-score").ToList().Count == sim.Rank(sim.Save.player).Count, "each action shows its score, with factors in the inspector");
            check(root.Query<Label>(className: "choice-score").ToList().Select(l => float.Parse(l.text)).SequenceEqual(sim.Rank(sim.Save.player).Select(o => o.Score)), "visible scores follow descending engine ranking");
            string untouched = JsonUtility.ToJson(sim.Save);
            Click("Decision factors: Go to Hall"); yield return null;
            check(modal != null && root.Q<Label>(className: "score-equation").text.StartsWith("4 = 0 + 4"), "per-action popup shows the exact score equation");
            check(root.Query<Label>(className: "factor-copy").ToList().Where(l => l.userData != null).All(l => (string)l.userData == "clara"), "popup never exposes another character's scores");
            check(JsonUtility.ToJson(sim.Save) == untouched, "inspecting factors does not mutate or advance the world");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/factors-" + Screen.width + "x" + Screen.height + ".png"));
            for (int frame = 0; frame < 10; frame++) yield return null;
            Click("Close decision factors"); yield return null;
            check(modal == null && root.Query<Label>(className: "factor-copy").ToList().Count == 0, "closing the popup restores the clean story view");
            Click("Go to Hall"); yield return new WaitForSecondsRealtime(.1f);
            check(sim.State.turn == 1 && sim.Save.guidance.Single().amount == 1, "natural movement records plus one");
            check(sim.Save.reviewPending && root.Query<Button>(className: "choice").ToList().Count == 0, "movement resolves before the next choice");
            check(root.Query<VisualElement>(className: "cast-caption").ToList().Count == 2, "simultaneous arrivals share the same room snapshot");
            check(!sim.Experienced("clara").Any(e => e.action == "tend"), "offscreen treatment stays offscreen");
            int resolved = sim.State.turn;
            sim = new Simulation(sim.Data, JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(sim.Save))); Render();
            yield return ReadTurn();
            check(sim.State.turn == resolved, "reading and restoring moments does not advance time");
            Click("Decision factors: Give Jonah the clean cloth"); yield return null;
            check(root.Query<Label>(className: "adjustment").ToList().Any(l => l.text.Contains("+6")), "lower-scored choice discloses its necessary adjustment");
            Click("Close decision factors"); Click("Give Jonah the clean cloth"); yield return new WaitForSecondsRealtime(.1f);
            check(sim.Save.guidance.Find(g => g.action == "help").amount == 6, "choosing kindness records only gap plus one");
            check((string)root.Q<Image>(className: "room-painting").userData == "Tableaux/cloth-exchange", "cloth exchange selects its exact custom scene");
            yield return ReadTurn();
            check(root.Query<VisualElement>(className: "journal-entry").ToList().Any(v => sim.State.events[(int)v.userData].action == "wait"), "quiet events remain in the witnessed journal");
            Click("Undo last turn"); yield return null;
            check(sim.State.turn == 1 && sim.Save.guidance.Single().amount == 1, "undo restores the preceding choice and its adjustments");
            Click("Give Jonah the clean cloth"); yield return ReadTurn();
            while (!sim.Ended)
            {
                Click(sim.Rank(sim.Save.player)[0].choice.label); yield return null;
                check(!sim.ContinueAsNext(), "cannot leave a life with unread moments");
                yield return ReadTurn();
            }
            check(root.Query<Button>(className: "continue-button").ToList().Count == 1, "one next-life command after the ending");
            check(root.Query<Button>(className: "past-factors").ToList().All(f => (string)f.userData == "clara"), "historical factors remain incarnation-only");
            Click("Wake as Jonah"); yield return null;
            check(sim.Save.player == "jonah" && undo.Count == 0 && sim.Save.guidance.Any(g => g.actor == "clara"), "next incarnation retains the other life without undo across lives");
            Click("Go to Hall"); yield return ReadTurn(); Click("Wait here"); yield return null;
            check(root.Q<Label>(className: "prose").text.Contains("Clara places a cloth"), "same illustrated event has Jonah's perspective");
            foreach (string kind in new[] { "kitchen", "baseline", "archive", "gathering", "garden", "chapel", "exchange", "crossing" })
            {
                Fixture(kind); Render(); yield return new WaitForSecondsRealtime(.15f);
                var scene = root.Q<VisualElement>("illustrated-scene");
                check(scene.worldBound.width > 100 && root.Q<Image>(className: "room-painting").image != null, kind + " artwork is loaded and laid out");
                check(root.Query<VisualElement>(className: "cast-caption").ToList().Count > 0, kind + " cast is identified");
                var buttons = root.Query<Button>(className: sim.Save.reviewPending ? "moment-button" : "choice").ToList();
                check(buttons.Count > 0 && buttons.All(b => b.worldBound.yMax <= root.worldBound.yMax), kind + " choices are visible without scrolling");
                Directory.CreateDirectory("artifacts");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/adventure-" + kind + "-" + Screen.width + "x" + Screen.height + ".png"));
                for (int frame = 0; frame < 10; frame++) yield return null;
            }
            check((string)root.Q<Image>(className: "room-painting").userData == "Tableaux/archive-crossing", "opposite travelers select the crossing tableau");
            check(root.Query<Label>(className: "cast-name").ToList()[0].text == "Jonah" && root.Query<Label>(className: "cast-name").ToList()[1].text == "Clara (you)", "custom scene labels follow the painting's left-to-right cast order");
            yield return ReadTurn();
            check(sim.Rank("clara").Any(o => o.choice.id == "follow:jonah"), "crossing offers a next-turn follow choice");
            Click(sim.Rank("clara").Find(o => o.choice.id == "follow:jonah").choice.label); yield return null;
            check(sim.State.Get("at:clara") == "hall", "following travels toward the last observed destination");
            yield return AuthoringSmoke(check);
            File.WriteAllLines("artifacts/ui-verification.txt", results);
            Fixture("exchange"); Render();
            if (Argument("-capture") != null) yield return Capture();
            else Application.Quit();
        }
        IEnumerator Capture()
        {
            for (int i = 0; i < 45; i++) yield return null;
            string path = Path.GetFullPath(Argument("-capture")); Directory.CreateDirectory(Path.GetDirectoryName(path));
            ScreenCapture.CaptureScreenshot(path);
            for (int i = 0; i < 10; i++) yield return null;
            File.WriteAllText(path + ".txt", "Unity player capture\n" + Screen.width + "x" + Screen.height + "\nViewpoint: " + sim.Save.player);
            if (Environment.GetCommandLineArgs().Contains("-captureQuit")) Application.Quit();
        }
    }
}
