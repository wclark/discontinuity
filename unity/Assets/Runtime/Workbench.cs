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
    public class Workbench : MonoBehaviour
    {
        Simulation sim;
        VisualElement root;
        string savePath, notice = "";
        bool sandbox, showFactors = true;
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
            if (Argument("-demo") != null) Fixture(Argument("-demo"));
            Render();
            if (Environment.GetCommandLineArgs().Contains("-smoke")) StartCoroutine(Smoke());
            else if (Argument("-capture") != null) StartCoroutine(Capture());
        }

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
            if (sim.Ended) return;
            undo.Push(JsonUtility.ToJson(sim.Save));
            sim.Step(action); Persist(); Render();
        }
        void Undo()
        {
            if (undo.Count == 0) return;
            sim = new Simulation(sim.Data, JsonUtility.FromJson<Campaign>(undo.Pop()));
            Persist(); Render();
        }
        void Continue()
        {
            if (!sim.ContinueAsNext()) return;
            undo.Clear(); notice = ""; Persist(); Render();
        }

        void Render()
        {
            root.Clear();
            string player = sim.Save.player;
            var person = sim.Data.people.Find(p => p.id == player);
            var room = sim.Data.rooms.Find(r => r.id == sim.State.Get("at:" + player));
            var header = Box(root, "header");
            var brand = Box(header, "brand");
            var icon = new Image { image = Resources.Load<Texture2D>("DiscontinuityIcon"), scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("brand-icon"); brand.Add(icon);
            Text(brand, "Discontinuity", "brand-title");
            var clock = Box(header, "clock");
            Text(clock, Simulation.Clock(sim.State.turn), "time");
            Text(clock, "SATURDAY, 17 OCTOBER", "date");
            Button(header, "\u21b6", Undo, "icon-button", "Undo last turn").SetEnabled(undo.Count > 0);

            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("page-scroll"); root.Add(scroll);
            scroll.contentContainer.style.width = Length.Percent(100);
            scroll.contentContainer.style.alignItems = Align.Center;
            var page = Box(scroll, "page");
            var identity = Box(page, "identity");
            var dot = Box(identity, "identity-dot"); Color color;
            ColorUtility.TryParseHtmlString(person.color, out color); dot.style.backgroundColor = color;
            Text(identity, person.name.ToUpperInvariant() + " / " + person.role.ToUpperInvariant(), "eyebrow");
            Text(identity, "LIFE " + sim.Save.day, "life");
            Text(page, room.name, "scene-title");
            Text(page, room.description, "room-description");
            Text(page, person.concern, "concern");

            var context = Box(page, "context");
            var present = sim.Data.people.Where(p => p.id != player && sim.State.Get("at:" + p.id) == room.id).Select(p => p.name).ToList();
            Context(context, "WITH YOU", present.Count == 0 ? "No one" : string.Join(", ", present));
            var carrying = sim.Data.items.Where(t => sim.State.Get("owner:" + t.id) == player).Select(t => t.name).ToList();
            Context(context, "CARRYING", carrying.Count == 0 ? "Nothing" : string.Join(", ", carrying));
            var visible = sim.Data.items.Where(t => sim.State.Get("owner:" + t.id) == room.id).Select(t => t.name).ToList();
            if (visible.Count > 0) Context(context, "IN THE ROOM", string.Join(", ", visible));

            var experienced = sim.Experienced(player);
            var recent = experienced.Where(e => e.turn == sim.State.turn - 1).ToList();
            if (recent.Any(e => e.action != "wait")) recent.RemoveAll(e => e.action == "wait");
            recent.RemoveAll(e => e.actor == player && e.action.StartsWith("move:", StringComparison.Ordinal));
            if (recent.Count > 0)
            {
                var narrative = Box(page, "narrative");
                foreach (var e in recent)
                {
                    Text(narrative, Simulation.Clock(e.turn) + (e.actor == player ? "" : " / " + sim.Name(e.actor)), "event-time");
                    Text(narrative, e.Text(player), "prose");
                }
            }

            if (sim.Ended)
            {
                var ending = Box(page, "ending");
                Text(ending, "The morning ends.", "ending-title");
                Text(ending, "Someone else lived through it, too.", "prose");
                Button(ending, "Wake as " + sim.Name(sim.NextIncarnation), Continue, "continue-button");
            }
            else RenderChoices(page);
            RenderJournal(page, experienced);
            if (notice != "") Text(page, notice, "notice");
        }

        void Context(VisualElement parent, string title, string value)
        {
            var group = Box(parent, "context-group"); Text(group, title, "caption"); Text(group, value, "context-value");
        }
        void RenderChoices(VisualElement page)
        {
            var heading = Box(page, "choice-heading"); Text(heading, "YOUR CHOICE", "caption");
            var toggle = new Toggle { text = "Decision factors", value = showFactors }; toggle.AddToClassList("factor-toggle"); heading.Add(toggle);
            toggle.RegisterValueChangedCallback(e => { showFactors = e.newValue; Render(); });
            Text(heading, "SCORE", "score-heading").tooltip = "Zero plus satisfied condition increments";
            foreach (var option in sim.Rank(sim.Save.player))
            {
                var row = Box(page, "action-row");
                var action = option.choice.id;
                var button = Button(row, "", () => Advance(action), "choice", option.choice.label);
                button.userData = sim.Save.player;
                Text(button, option.choice.label, "choice-label");
                Text(button, option.Score.ToString("0.#"), "choice-score");
                if (!showFactors) continue;
                foreach (var term in option.terms.Where(t => t.active)) Factor(row, term);
                var inactive = option.terms.Where(t => !t.active).ToList();
                if (inactive.Count > 0)
                {
                    var fold = Fold(row, "Inactive conditions (" + inactive.Count + ")"); fold.AddToClassList("inactive");
                    foreach (var term in inactive) Factor(fold, term);
                }
            }
            if (showFactors)
            {
                var records = sim.Save.guidance.Where(g => g.actor == sim.Save.player && g.until >= g.from).ToList();
                if (records.Count > 0)
                {
                    var fold = Fold(page, "Your adjustments (" + records.Count + ")");
                    foreach (var g in records)
                    {
                        string label = sim.Data.choices.Find(c => c.id == g.action)?.label ?? (g.action.StartsWith("move:") ? "Go to " + sim.Name(g.action.Substring(5)) : "Wait here");
                        Text(fold, Simulation.Clock(g.from) + " / " + label + " / +" + g.amount.ToString("0.#"), "adjustment");
                    }
                }
            }
        }
        void Factor(VisualElement parent, Contribution term)
        {
            var label = Text(parent, (term.active ? "+" + term.amount.ToString("0.#") : "0") + "  " + term.id + " / " + term.description, "factor-copy");
            label.userData = sim.Save.player;
        }
        void RenderJournal(VisualElement page, List<Event> experienced)
        {
            var earlier = experienced.Where(e => e.turn < sim.State.turn - 1 && e.action != "wait").Reverse().ToList();
            if (earlier.Count == 0) return;
            var journal = Fold(page, "Earlier today (" + earlier.Count + ")"); journal.AddToClassList("journal");
            foreach (var e in earlier)
            {
                var entry = Box(journal, "journal-entry"); entry.userData = e.id;
                Text(entry, Simulation.Clock(e.turn) + " / " + sim.Name(e.actor), "event-time");
                Text(entry, e.Text(sim.Save.player), "prose");
                if (showFactors && e.actor == sim.Save.player && e.alternatives != null)
                {
                    var factors = Fold(entry, "Decision factors"); factors.AddToClassList("past-factors"); factors.userData = e.actor;
                    foreach (var option in e.alternatives)
                        Text(factors, option.label + " / " + (option.conditions + option.manual).ToString("0.#") + " = 0 + " + option.conditions.ToString("0.#") + " conditions + " + option.manual.ToString("0.#") + " adjustment", "factor-copy");
                }
            }
        }

        // Reproducible fixtures are available to command-line verification, not the game UI.
        void Fixture(string kind)
        {
            sim = new Simulation(sim.Data); undo.Clear();
            if (kind == "baseline") { sim.Step(); return; }
            while (!sim.Ended) sim.Step(sim.State.turn == 1 ? (kind == "kindness" ? "help" : kind == "warning" ? "threaten" : "mock") : null);
            sim.ContinueAsNext();
            while (sim.State.turn < (kind == "warning" ? 3 : 6)) sim.Step();
        }
        void Click(string label)
        {
            var button = root.Query<Button>().ToList().FirstOrDefault(b => b.text == label || b.tooltip == label);
            if (button == null) { Application.Quit(1); throw new Exception("UI button not found: " + label); }
            button.Focus();
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
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
            check(sim.Save.player == "clara", "first life is Clara");
            check(!root.Query<Button>().ToList().Any(b => b.text == "Observe" || sim.Data.people.Any(p => p.name == b.text)), "no observation or character switching controls");
            check(!sim.ContinueAsNext(), "cannot change incarnation before the day ends");
            check(root.Query<Button>(className: "choice").ToList().All(b => (string)b.userData == "clara"), "one action list for the current character");
            check(root.Query<Label>(className: "factor-copy").ToList().All(l => (string)l.userData == "clara"), "only current character decision factors");
            Click("Go to Hall"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.turn == 1 && sim.Save.guidance.Count == 0, "natural choice advances without adjustment");
            Click("Give Jonah the clean cloth"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.Get("trust") == "yes" && sim.Save.guidance.Single().amount == 6, "manual choice records only the necessary increment");
            Click("Undo last turn"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.turn == 1 && sim.Save.guidance.Count == 0, "undo restores this life without switching");
            Click("Give Jonah the clean cloth"); yield return new WaitForSecondsRealtime(.2f);
            var toggle = root.Q<Toggle>(className: "factor-toggle"); toggle.value = false; yield return null;
            check(root.Query<Label>(className: "factor-copy").ToList().Count == 0, "decision factors can be collapsed");
            root.Q<Toggle>(className: "factor-toggle").value = true; yield return null;
            while (!sim.Ended)
            {
                Click(sim.Rank(sim.Save.player)[0].choice.label); yield return new WaitForSecondsRealtime(.12f);
            }
            check(root.Query<Button>(className: "continue-button").ToList().Count == 1, "one next incarnation at the end of the day");
            check(root.Query<VisualElement>(className: "journal-entry").ToList().All(v => sim.Experienced("clara").Any(e => e.id == (int)v.userData)), "journal includes only witnessed events");
            check(root.Query<Foldout>(className: "past-factors").ToList().All(f => (string)f.userData == "clara"), "journal never exposes NPC decision factors");
            Click("Wake as Jonah"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.Save.player == "jonah" && sim.State.turn == 0 && undo.Count == 0, "next life begins only after completion and cannot undo across lives");
            check(sim.Save.guidance.Any(g => g.actor == "clara" && g.amount == 6), "other life retains its authored choice");
            Click("Go to Hall"); yield return new WaitForSecondsRealtime(.2f);
            Click("Wait here"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.Get("trust") == "yes" && root.Query<Label>(className: "prose").ToList().Any(l => l.text.Contains("Clara places a cloth")), "next incarnation experiences the earlier kindness");
            Directory.CreateDirectory("artifacts"); File.WriteAllLines("artifacts/ui-verification.txt", results);
            Fixture("baseline"); Render();
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
