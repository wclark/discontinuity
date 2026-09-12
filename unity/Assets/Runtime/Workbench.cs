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
        VisualElement root, body, inspector, timeline, scene;
        Household asset;
        string inspecting = "clara", inspectorTab = "Choices", notice = "", savePath;
        int selectedEvent = -1;
        bool running, sandbox, showForecast;
        float nextTick;
        List<Event> forecast;
        readonly Stack<string> undo = new Stack<string>();
        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
        void OnEnable()
        {
            asset = Resources.Load<Household>("Household");
            var data = asset == null ? HouseholdContent.Create() : asset.definition;
            savePath = Path.Combine(Application.persistentDataPath, "household-v1.json");
            Campaign saved = null;
            sandbox = Argument("-demo") != null || Argument("-capture") != null || Environment.GetCommandLineArgs().Contains("-smoke");
            if (!sandbox && File.Exists(savePath))
                try { saved = JsonUtility.FromJson<Campaign>(File.ReadAllText(savePath)); }
                catch (Exception ex) { notice = "Save could not be loaded: " + ex.Message; }
            sim = new Simulation(data, saved);
            inspecting = string.IsNullOrEmpty(sim.Save.player) ? "clara" : sim.Save.player;
            root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("Workbench"));
            root.AddToClassList("app");
            if (Argument("-demo") != null) Example(Argument("-demo")); else Render();
            if (Environment.GetCommandLineArgs().Contains("-smoke")) StartCoroutine(Smoke());
            else if (Argument("-capture") != null) StartCoroutine(Capture());
        }
        IEnumerator Capture()
        {
            // Capture the real player framebuffer after UI layout and rendering settle.
            for (int i = 0; i < 45; i++) yield return null;
            string path = Path.GetFullPath(Argument("-capture")); Directory.CreateDirectory(Path.GetDirectoryName(path));
            ScreenCapture.CaptureScreenshot(path);
            for (int i = 0; i < 10; i++) yield return null;
            var checks = new List<string>();
            foreach (var p in sim.Data.people) checks.Add(p.name + ": " + sim.Rank(p.id).Count + " options");
            File.WriteAllText(path + ".txt", "Unity player capture\n" + Screen.width + "x" + Screen.height + "\n" + string.Join("\n", checks));
            if (Environment.GetCommandLineArgs().Contains("-captureQuit")) Application.Quit();
        }
        void Click(string label)
        {
            var button = root.Query<Button>().ToList().FirstOrDefault(b => b.text == label || b.Q<Label>(className: "choice-label")?.text == label);
            if (button == null) { Application.Quit(1); throw new Exception("UI button not found: " + label); }
            button.Focus();
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
        }
        IEnumerator Smoke()
        {
            // Exercise the live UI through its submit events, without touching the user's save.
            yield return null;
            var results = new List<string>();
            Action<bool, string> check = (ok, name) => { if (!ok) { Application.Quit(1); throw new Exception("UI FAILED: " + name); } results.Add("PASS " + name); Debug.Log("UI PASS: " + name); };
            Click("Unchanged day"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.turn == 1, "example button");
            Click("Give Jonah the clean cloth"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.Get("trust") == "yes" && sim.Save.guidance.Count == 1, "manual action and necessary adjustment");
            Click("Undo"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.turn == 1 && sim.Save.guidance.Count == 0, "undo restores facts and adjustments");
            Click("A small kindness"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.Save.player == "jonah" && sim.State.turn == 6, "viewpoint consequence example");
            Click("Conditions"); yield return new WaitForSecondsRealtime(.2f);
            var slider = root.Query<Slider>(className: "condition-increment").ToList().First(); slider.value = 12; yield return null;
            check(sim.Save.weights.Any(w => w.value == 12), "condition increment control");
            Click("Apply and inspect"); yield return new WaitForSecondsRealtime(.2f);
            check(inspectorTab == "Choices", "return to rankings");
            Click("Forecast remaining day"); yield return new WaitForSecondsRealtime(.2f);
            check(forecast.Count == 64 && sim.State.turn == 6, "forecast does not advance the live day");
            var cell = root.Query<Button>(className: "cell").ToList().First();
            cell.Focus(); using (var e = NavigationSubmitEvent.GetPooled()) { e.target = cell; cell.SendEvent(e); }
            yield return new WaitForSecondsRealtime(.2f);
            check(selectedEvent >= 0 && root.Query<Label>().ToList().Any(l => l.text == "RANKED OPTIONS AT THIS TURN"), "timeline opens historical rankings");
            Click("A small kindness"); yield return new WaitForSecondsRealtime(.2f);
            Click("Vouch for Clara"); yield return new WaitForSecondsRealtime(.2f);
            check(sim.State.Get("vouched") == "yes", "second viewpoint action");
            Directory.CreateDirectory("artifacts"); File.WriteAllLines("artifacts/ui-verification.txt", results);
            if (Argument("-capture") != null) yield return Capture();
            else Application.Quit();
        }
        void Update()
        {
            if (!running || Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + .85f; Advance();
            if (sim.Ended) { running = false; Render(); }
        }
        VisualElement Box(VisualElement parent, string cls)
        { var v = new VisualElement(); if (!string.IsNullOrEmpty(cls)) v.AddToClassList(cls); parent.Add(v); return v; }
        Label Text(VisualElement parent, string text, string cls = null)
        { var l = new Label(text ?? ""); if (cls != null) l.AddToClassList(cls); parent.Add(l); return l; }
        Button Button(VisualElement parent, string text, Action click, string cls = null, string tip = null)
        { var b = new Button(click) { text = text, tooltip = tip ?? text }; if (cls != null) b.AddToClassList(cls); parent.Add(b); return b; }
        Color Tint(string actor) { var p = sim.Data.people.Find(v => v.id == actor); Color c; ColorUtility.TryParseHtmlString(p == null ? "#a0a0a0" : p.color, out c); return c; }
        void Snapshot() { undo.Push(JsonUtility.ToJson(sim.Save)); }
        void Persist()
        {
            if (sandbox) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(savePath)); File.WriteAllText(savePath, JsonUtility.ToJson(sim.Save, true)); }
            catch (Exception ex) { notice = "Could not save: " + ex.Message; }
        }
        void Advance(string action = null)
        {
            if (sim.Ended) return;
            Snapshot(); sim.Step(action); forecast = null; selectedEvent = -1;
            Persist(); Render();
        }
        void Begin(string actor)
        { Snapshot(); running = false; sim.Begin(actor); inspecting = actor == "" ? "clara" : actor; selectedEvent = -1; forecast = null; notice = ""; Persist(); Render(); }
        void Example(string kind)
        {
            Snapshot(); running = false;
            sim = new Simulation(sim.Data); sim.Save.player = "clara";
            if (kind != "baseline")
            {
                while (!sim.Ended)
                {
                    string action = sim.State.turn == 1 ? (kind == "kindness" ? "help" : kind == "warning" ? "threaten" : "mock") : null;
                    sim.Step(action);
                }
                sim.Begin("jonah");
                while (sim.State.turn < (kind == "warning" ? 3 : 6)) sim.Step();
                inspecting = "jonah";
            }
            else { sim.Save.player = "clara"; sim.Step(); inspecting = "clara"; }
            selectedEvent = -1; forecast = null;
            notice = kind == "baseline" ? "Unchanged day" : kind == "kindness" ? "Clara gave Jonah the cloth. You are Jonah now." : kind == "warning" ? "Clara's warning has interrupted Jonah's errand." : "Clara mocked Jonah. You are Jonah now.";
            Persist(); Render();
        }
        void Render()
        {
            root.Clear();
            var header = Box(root, "header");
            var brand = Box(header, "brand"); Text(brand, "DISCONTINUITY", "title"); Text(brand, "THE HOUSEHOLD / A STUDY IN CONSEQUENCES", "eyebrow");
            var clock = Box(header, "clock"); Text(clock, Simulation.Clock(sim.State.turn), "time"); Text(clock, "SATURDAY, 17 OCTOBER  /  PASS " + sim.Save.day, "eyebrow");
            var headerButtons = Box(header, "header-buttons");
            Button(headerButtons, "Undo", () => { if (undo.Count == 0) return; running = false; sim.Save = JsonUtility.FromJson<Campaign>(undo.Pop()); forecast = null; selectedEvent = -1; Persist(); Render(); }, "subtle").SetEnabled(undo.Count > 0);
            Button(headerButtons, running ? "Pause" : "Run day", () => { running = !running; nextTick = Time.unscaledTime + .6f; Render(); }, "subtle").SetEnabled(!sim.Ended);
            Button(headerButtons, "Next turn  >", () => Advance(), "primary").SetEnabled(!sim.Ended);
            var strip = Box(root, "toolbar"); Text(strip, "VIEWPOINT", "caption");
            foreach (var p in sim.Data.people)
            {
                var id = p.id;
                Button(strip, p.name, () => Begin(id), sim.Save.player == p.id ? "tab selected" : "tab", "Begin the day as " + p.name + "; replace this character's previous choices");
            }
            Button(strip, "Observe", () => Begin(""), sim.Save.player == "" ? "tab selected" : "tab");
            Text(strip, sim.Ended ? sim.Outcome() : "Blue envelope: " + sim.Name(sim.State.Get("owner:envelope")), "status-line");
            body = Box(root, "body");
            RenderMap(Box(body, "map-panel"));
            scene = Box(body, "scene-panel"); RenderScene();
            inspector = Box(body, "inspector-panel"); RenderInspector();
            timeline = Box(root, "timeline-panel"); RenderTimeline();
            var footer = Box(root, "footer"); Text(footer, "EXPERIMENTS", "caption");
            Button(footer, "Unchanged day", () => Example("baseline"), "experiment");
            Button(footer, "A small kindness", () => Example("kindness"), "experiment");
            Button(footer, "A public humiliation", () => Example("humiliation"), "experiment");
            Button(footer, "A serious warning", () => Example("warning"), "experiment");
            Text(footer, notice != "" ? notice : "Saved locally", "footer-note");
        }
        void RenderMap(VisualElement parent)
        {
            Text(parent, "01 / THE HOUSE", "section-index");
            var map = Box(parent, "map");
            map.generateVisualContent += ctx =>
            {
                var paint = ctx.painter2D; paint.lineWidth = 1.5f; paint.strokeColor = new Color(.31f, .40f, .39f, .45f);
                float w = map.contentRect.width, h = map.contentRect.height;
                foreach (var room in sim.Data.rooms) foreach (string exit in room.exits)
                {
                    if (string.CompareOrdinal(room.id, exit) >= 0) continue;
                    var end = sim.Data.rooms.Find(r => r.id == exit);
                    paint.BeginPath(); paint.MoveTo(new Vector2(w * room.x, h * room.y)); paint.LineTo(new Vector2(w * end.x, h * end.y)); paint.Stroke();
                }
            };
            foreach (var room in sim.Data.rooms)
            {
                var node = Box(map, "room");
                node.style.left = Length.Percent(room.x * 100); node.style.top = Length.Percent(room.y * 100);
                if (sim.State.Get("at:" + inspecting) == room.id) node.AddToClassList("room-selected");
                Text(node, room.name.ToUpperInvariant(), "room-name");
                var occupants = Box(node, "occupants");
                foreach (var p in sim.Data.people.Where(p => sim.State.Get("at:" + p.id) == room.id))
                {
                    string id = p.id;
                    var token = Button(occupants, p.name.Substring(0, 1), () => { inspecting = id; Render(); }, "token", p.name + ", " + p.role);
                    token.style.backgroundColor = Tint(id);
                }
                if (sim.State.Get("owner:envelope") == room.id || (room.id == "chapel" && sim.State.Get("owner:envelope") == "lectern")) Text(node, "[ BLUE ENVELOPE ]", "map-item");
            }
            var people = Box(parent, "roster");
            foreach (var p in sim.Data.people)
            {
                var row = Box(people, "person-row");
                var dot = Box(row, "person-dot"); dot.style.backgroundColor = Tint(p.id);
                var id = p.id;
                Button(row, p.name, () => { inspecting = id; Render(); }, "person-name");
                Text(row, sim.Name(sim.State.Get("at:" + p.id)), "person-location");
                var next = sim.Ended ? null : sim.Rank(p.id).FirstOrDefault();
                Text(row, next == null ? "--" : next.Score.ToString("0.#"), "person-score");
            }
            var info = Box(parent, "house-note");
            Text(info, sim.State.Get("cleared") == "yes" ? "THE ACCOUNT IS CORROBORATED" : "THE NOON READING", "caption");
            Text(info, sim.Outcome(), "body-copy");
        }
        void RenderScene()
        {
            string viewer = string.IsNullOrEmpty(sim.Save.player) ? inspecting : sim.Save.player;
            var person = sim.Data.people.Find(p => p.id == viewer);
            string here = sim.State.Get("at:" + viewer); var room = sim.Data.rooms.Find(r => r.id == here);
            Text(scene, "02 / " + (sim.Save.player == "" ? "OBSERVING " : "INHABITING ") + person.name.ToUpperInvariant(), "section-index");
            Text(scene, room.name, "scene-title"); Text(scene, person.concern, "concern");
            var inventory = sim.Data.items.Where(t => sim.State.Get("owner:" + t.id) == viewer).Select(t => t.name);
            Text(scene, "CARRYING   " + (inventory.Any() ? string.Join(" / ", inventory) : "Nothing"), "inventory");
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("scene-scroll"); scene.Add(scroll);
            if (sim.State.turn == 0) Text(scroll, room.description, "prose");
            else
            {
                var recent = sim.State.events.Where(e => e.turn == sim.State.turn - 1 && (e.actor == viewer || e.target == viewer || e.location == here) && (e.action != "wait" || e.actor == viewer)).ToList();
                foreach (var e in recent)
                {
                    var block = Box(scroll, "event-prose"); Text(block, Simulation.Clock(e.turn) + " / " + sim.Name(e.actor), "caption");
                    Text(block, e.Text(viewer), "prose");
                }
                if (recent.Count == 0) Text(scroll, "The house is quiet here. Elsewhere, the morning continues.", "prose");
            }
            if (sim.Ended)
            {
                Text(scroll, sim.Outcome(), "ending");
                Button(scroll, "Begin again as " + sim.Name(sim.Data.people[(sim.Data.people.FindIndex(p => p.id == viewer) + 1) % sim.Data.people.Count].id),
                    () => Begin(sim.Data.people[(sim.Data.people.FindIndex(p => p.id == viewer) + 1) % sim.Data.people.Count].id), "primary");
                return;
            }
            Text(scroll, sim.Save.player == "" ? "NEXT DECISION" : "YOUR CHOICE", "action-heading");
            foreach (var option in sim.Rank(viewer))
            {
                var action = option.choice.id;
                var button = Button(scroll, "", () => { if (sim.Save.player != "") Advance(action); else Advance(); }, "choice");
                var line = Box(button, "choice-line"); Text(line, option.choice.label, "choice-label"); Text(line, option.Score.ToString("0.#"), "choice-score");
                Text(button, option.Score > 0 ? string.Join(" + ", option.terms.Where(t => t.active).Select(t => t.amount.ToString("0.#"))) + (option.manual > 0 ? " / prior choice +" + option.manual : "") : "0", "choice-detail");
            }
        }
        void RenderInspector()
        {
            Text(inspector, "03 / DECISION INSPECTOR", "section-index");
            var personTabs = Box(inspector, "inspect-people");
            foreach (var p in sim.Data.people)
            { string id = p.id; Button(personTabs, p.name.Replace("Father ", "").Replace("Dr. ", ""), () => { inspecting = id; selectedEvent = -1; Render(); }, id == inspecting ? "tab selected" : "tab"); }
            var tabs = Box(inspector, "inspect-tabs");
            foreach (var title in new[] { "Choices", "Conditions", "Prior choices" })
            { var tab = title; Button(tabs, title, () => { inspectorTab = tab; selectedEvent = -1; Render(); }, title == inspectorTab ? "tab selected" : "tab"); }
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("inspect-scroll"); inspector.Add(scroll);
            if (selectedEvent >= 0) { RenderEvent(scroll); return; }
            if (inspectorTab == "Choices")
            {
                Text(scroll, sim.Name(inspecting) + " / " + sim.Name(sim.State.Get("at:" + inspecting)), "inspector-name");
                if (sim.Ended) { Text(scroll, "Day complete", "muted"); return; }
                int rank = 0;
                foreach (var o in sim.Rank(inspecting))
                {
                    var fold = new Foldout { text = (++rank) + "   " + o.choice.label + "    " + o.Score.ToString("0.#"), value = rank == 1 };
                    fold.AddToClassList("option-fold"); scroll.Add(fold);
                    Text(fold, "0  +  " + o.conditions.ToString("0.#") + " conditions  +  " + o.manual.ToString("0.#") + " prior choice", "equation");
                    foreach (var t in o.terms)
                    {
                        var term = Box(fold, t.active ? "term active-term" : "term");
                        Text(term, (t.active ? "+" + t.amount.ToString("0.#") : "0") + "   " + t.id, "term-amount"); Text(term, t.description, "term-copy");
                    }
                    if (o.terms.Count == 0) Text(fold, "No condition contributions", "muted");
                    if (o.manual > 0) Text(fold, "+" + o.manual.ToString("0.#") + " from the previous pass", "manual-note");
                }
                var unavailable = sim.Data.choices.Where(c => c.actor == inspecting && !sim.Valid(c)).ToList();
                var blocked = new Foldout { text = "Unavailable (" + unavailable.Count + ")", value = false }; scroll.Add(blocked);
                foreach (var c in unavailable)
                {
                    string reason = sim.State.used.Contains(c.actor + ":" + (c.slot ?? c.id)) ? "already resolved" : sim.State.turn < c.from || sim.State.turn > c.until ? "outside time window" : sim.State.Get("at:" + c.actor) != c.location ? "requires " + sim.Name(c.location) : string.Join("; ", c.requires.Where(v => !sim.Met(v, c.actor, c.target)).Select(v => sim.Describe(v, c.actor, c.target)));
                    Text(blocked, c.label + "\n" + reason, "blocked-copy");
                }
            }
            else if (inspectorTab == "Conditions")
            {
                foreach (var r in sim.Data.rules.Where(r => r.actor == inspecting))
                {
                    var block = Box(scroll, "rule");
                    Text(block, r.id + " / " + (r.route != null ? "Move toward " + sim.Name(r.route) : sim.Data.choices.Find(c => c.id == r.action)?.label ?? r.action), "rule-name");
                    Text(block, Simulation.Clock(r.from) + " - " + Simulation.Clock(r.until), "caption");
                    foreach (var c in r.conditions) Text(block, (sim.Met(c, r.actor) ? "[+] " : "[ ] ") + sim.Describe(c, r.actor), "term-copy");
                    var slider = new Slider("Increment", 0, 20) { value = sim.Weight(r), showInputField = true }; slider.AddToClassList("condition-increment"); block.Add(slider);
                    slider.RegisterValueChangedCallback(e => { Snapshot(); sim.SetWeight(r.id, Mathf.Round(e.newValue)); forecast = null; Persist(); });
                }
                Button(scroll, "Apply and inspect", () => { inspectorTab = "Choices"; Render(); }, "primary");
                Button(scroll, "Restore condition increments", () => { Snapshot(); sim.Save.weights.Clear(); forecast = null; Persist(); Render(); }, "subtle");
            }
            else
            {
                var records = sim.Save.guidance.Where(g => g.actor == inspecting && g.until >= g.from).ToList();
                if (records.Count == 0) Text(scroll, "No adjustments. Choices already led the ranking.", "muted");
                foreach (var g in records)
                {
                    var block = Box(scroll, "rule"); Text(block, (sim.Data.choices.Find(c => c.id == g.action)?.label ?? g.action) + "   +" + g.amount, "rule-name");
                    Text(block, Simulation.Clock(g.from) + " - " + Simulation.Clock(g.until) + " / " + sim.Name(g.location), "caption");
                    Text(block, g.actor == sim.Save.player ? "Recorded for a later pass" : sim.State.applied.Contains(g.id) ? "Applied" : sim.State.turn > g.until ? "Window closed" : "Available in its decision window", "term-copy");
                }
            }
        }
        void RenderTimeline()
        {
            var head = Box(timeline, "timeline-heading"); Text(head, "04 / FOUR LIVES, ONE MORNING", "section-index");
            Button(head, showForecast ? "Hide forecast" : "Forecast remaining day", () => { showForecast = !showForecast; forecast = null; Render(); }, "subtle");
            Text(head, "K Kitchen   H Hall   A Archive   G Garden   C Chapel", "legend");
            var ticks = Box(timeline, "track"); Text(ticks, "", "track-name");
            for (int i = 0; i < sim.Data.turns; i++) Text(ticks, Simulation.Clock(i), "tick");
            if (showForecast && forecast == null) forecast = sim.Forecast();
            foreach (var p in sim.Data.people)
            {
                var row = Box(timeline, "track"); Text(row, p.name, "track-name").style.color = Tint(p.id);
                for (int i = 0; i < sim.Data.turns; i++)
                {
                    int turn = i;
                    var evt = sim.State.events.Find(e => e.actor == p.id && e.turn == turn);
                    var projected = evt ?? (showForecast ? forecast.Find(e => e.actor == p.id && e.turn == turn) : null);
                    var previous = sim.Save.previous.Find(e => e.actor == p.id && e.turn == turn);
                    string label = projected == null ? "" : RoomLetter(projected.effects.Find(v => v.StartsWith("at:"))?.Split('=').Last().Trim() ?? projected.location);
                    var cell = Button(row, label, () => { if (evt == null) return; selectedEvent = evt.id; inspecting = p.id; Render(); }, "cell",
                        projected == null ? Simulation.Clock(turn) : Simulation.Clock(turn) + " / " + projected.label + (previous == null ? "" : "\nPrevious pass: " + previous.label));
                    if (projected != null) { cell.style.backgroundColor = Tint(p.id) * new Color(1, 1, 1, evt == null ? .14f : .25f); }
                    if (evt != null && evt.changed) cell.AddToClassList("changed-cell");
                    if (projected != null && projected.manual > 0) cell.AddToClassList("guided-cell");
                    if (turn == sim.State.turn) cell.AddToClassList("now-cell");
                    if (evt != null && evt.id == selectedEvent) cell.AddToClassList("selected-cell");
                }
            }
        }
        string RoomLetter(string id) { return id == "chapel" ? "C" : id == "kitchen" ? "K" : id == "archive" ? "A" : id == "hall" ? "H" : "G"; }
        void RenderEvent(VisualElement parent)
        {
            var e = sim.State.events.Find(v => v.id == selectedEvent); if (e == null) return;
            Button(parent, "< Current decisions", () => { selectedEvent = -1; Render(); }, "subtle");
            Text(parent, Simulation.Clock(e.turn) + " / " + sim.Name(e.actor), "caption"); Text(parent, e.label, "inspector-name");
            Text(parent, e.Text(string.IsNullOrEmpty(sim.Save.player) ? inspecting : sim.Save.player), "prose");
            Text(parent, "SCORE AT DECISION  " + e.score.ToString("0.#"), "caption"); Text(parent, e.decision == "" ? "0" : e.decision, "term-copy");
            foreach (var effect in e.effects) Text(parent, effect, "effect");
            var before = sim.Save.previous.Find(v => v.actor == e.actor && v.turn == e.turn);
            if (before != null) Text(parent, "PREVIOUS PASS\n" + before.label + (before.action == e.action ? " / same choice" : " / changed"), "comparison");
            Text(parent, "EARLIER EVENTS BEHIND THIS CHOICE", "action-heading");
            foreach (int id in e.causes)
            {
                var cause = sim.State.events.Find(v => v.id == id); if (cause == null) continue;
                Button(parent, Simulation.Clock(cause.turn) + "  " + sim.Name(cause.actor) + ": " + cause.label, () => { selectedEvent = id; Render(); }, "cause-link");
            }
            if (e.causes.Count == 0) Text(parent, "Initial conditions", "muted");
            Text(parent, "RANKED OPTIONS AT THIS TURN", "action-heading");
            if (e.alternatives != null) foreach (var option in e.alternatives)
            {
                Text(parent, (option.action == e.action ? "> " : "") + option.label + "   " + (option.conditions + option.manual).ToString("0.#"), "rule-name");
                Text(parent, "0 + " + option.conditions.ToString("0.#") + " conditions + " + option.manual.ToString("0.#") + " prior choice", "term-copy");
            }
        }
    }
}
