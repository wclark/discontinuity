using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    public partial class Workbench
    {
        static T Copy<T>(T value) { return JsonUtility.FromJson<T>(JsonUtility.ToJson(value)); }
        TextField Field(VisualElement parent, string label, string value, Action<string> changed, bool multiline = false)
        {
            var field = new TextField(label) { value = value ?? "", multiline = multiline, name = label };
            field.AddToClassList(multiline ? "prose-field" : "editor-field");
            field.RegisterValueChangedCallback(e => changed(e.newValue)); parent.Add(field); return field;
        }
        void Pick(VisualElement parent, string label, List<string> values, string selected, Action<string> changed, Func<string, string> display = null)
        {
            var labels = values.Select(v => display == null ? v : display(v)).ToList();
            var field = new DropdownField(label, labels, Math.Max(0, values.IndexOf(selected))) { name = label };
            field.AddToClassList("editor-field");
            field.RegisterValueChangedCallback(e => changed(values[labels.IndexOf(e.newValue)])); parent.Add(field);
        }
        void Toggle(VisualElement parent, string label, bool value, Action<bool> changed)
        {
            var field = new Toggle(label) { value = value, name = label }; field.AddToClassList("editor-field");
            field.RegisterValueChangedCallback(e => changed(e.newValue)); parent.Add(field);
        }
        void WindowFields(VisualElement parent, int from, int until, Action<int> start, Action<int> end)
        {
            var row = Box(parent, "editor-pair");
            var turns = Enumerable.Range(0, sim.Data.turns).Select(t => t.ToString()).ToList();
            Func<string, string> label = t => Simulation.Clock(int.Parse(t)) + " / turn " + (int.Parse(t) + 1);
            Pick(row, "From", turns, from.ToString(), v => start(int.Parse(v)), label);
            Pick(row, "Through", turns, until.ToString(), v => end(int.Parse(v)), label);
        }
        string EntityName(string id)
        {
            return id == "" ? "Any room" : id == "$actor" ? "This character" : id == "$target" ? "Target" : id == "$here" ? "Here (actor's room)" : sim.Name(id);
        }
        List<string> Holders(Choice c)
        {
            return new[] { "$actor", "$here" }.Concat(string.IsNullOrEmpty(c.target) ? new string[0] : new[] { "$target" })
                .Concat(sim.Data.people.Select(p => p.id)).Concat(sim.Data.rooms.Select(r => r.id))
                .Concat(sim.Definitions.SelectMany(v => v.effects).Where(e => e.key.StartsWith("owner:")).Select(e => e.value))
                .Concat(sim.State.facts.Where(f => f.key.StartsWith("owner:")).Select(f => f.value)).Distinct().ToList();
        }
        void Conditions(VisualElement parent, List<Condition> conditions, Choice choice)
        {
            var list = Box(parent, "condition-list");
            Action refresh = null;
            refresh = () =>
            {
                list.Clear();
                foreach (var condition in conditions.ToList())
                {
                    var row = Box(list, "condition-row");
                    string kind = condition.key.StartsWith("at:") ? "Person location" : condition.key.StartsWith("owner:") ? "Item owner" : condition.key.StartsWith("near:") ? "Item here" : "Fact";
                    var heading = Box(row, "editor-pair");
                    Pick(heading, "Condition", new List<string> { "Person location", "Item owner", "Item here", "Fact" }, kind, v =>
                    {
                        condition.key = v == "Person location" ? "at:" + choice.actor : v == "Item owner" ? "owner:" + sim.Data.items[0].id : v == "Item here" ? "near:" + sim.Data.items[0].id : "new_fact";
                        condition.value = v == "Person location" ? "$here" : v == "Item owner" ? "$actor" : "yes"; refresh();
                    });
                    Button(heading, "\u00d7", () => { conditions.Remove(condition); refresh(); }, "inspect-button", "Remove condition");
                    if (kind == "Person location")
                    {
                        Pick(row, "Person", sim.Data.people.Select(p => p.id).ToList(), sim.Resolve(condition.key.Substring(3), choice.actor, choice.target), v => condition.key = "at:" + v, sim.Name);
                        Pick(row, "Location", new[] { "$here" }.Concat(sim.Data.rooms.Select(r => r.id)).ToList(), condition.value, v => condition.value = v, EntityName);
                    }
                    else if (kind == "Item owner" || kind == "Item here")
                    {
                        string prefix = kind == "Item here" ? "near:" : "owner:";
                        Pick(row, "Item", sim.Data.items.Select(t => t.id).ToList(), condition.key.Substring(prefix.Length), v => condition.key = prefix + v, sim.Name);
                        if (kind == "Item owner") Pick(row, "With / in", Holders(choice), condition.value, v => condition.value = v, EntityName);
                    }
                    else
                    {
                        Field(row, "Fact key", condition.key, v => condition.key = v);
                        Field(row, "Value", condition.value, v => condition.value = v);
                    }
                    Toggle(row, kind == "Item here" ? "Absent" : "Is not", condition.not, v => condition.not = v);
                }
            };
            refresh();
            Button(parent, "+ Condition", () => { conditions.Add(new Condition("at:" + choice.actor, "$here")); refresh(); }, "tool-button");
        }
        void OpenCatalog()
        {
            var content = OpenModal(sim.Name(sim.Save.player) + " / actions");
            Button(content, "+ New action", () => OpenEditor(null), "tool-button");
            var search = new TextField("Search") { name = "Action search" }; content.Add(search);
            var list = Box(content, "catalog");
            Action render = () =>
            {
                list.Clear();
                var choices = sim.Definitions.Where(c => c.actor == sim.Save.player).Concat(sim.Choices(sim.Save.player))
                    .GroupBy(c => c.id).Select(g => g.First()).Where(c => c.label.IndexOf(search.value ?? "", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(c => sim.Evaluate(c)).OrderByDescending(o => sim.Valid(o.choice)).ThenByDescending(o => o.Score).ThenBy(o => o.choice.id);
                foreach (var option in choices)
                {
                    var row = Box(list, "catalog-entry");
                    bool valid = sim.Valid(option.choice);
                    Button(row, option.choice.label, () => OpenFactors(option), "catalog-action");
                    Text(row, valid ? "Available / score " + option.Score.ToString("0.##") : "Unavailable / " + string.Join("; ", sim.Availability(option.choice).Where(v => !v.met).Select(v => v.description)), valid ? "criterion-met" : "criterion-unmet");
                }
            };
            search.RegisterValueChangedCallback(e => render()); render();
        }
        void OpenEditor(Choice source)
        {
            if (sim.Save.reviewPending || sim.Ended) return;
            bool generated = source != null && !sim.Definitions.Any(c => c.actor == source.actor && c.id == source.id);
            string id = "custom_" + Guid.NewGuid().ToString("N");
            var draft = source == null ? new Choice { id = id, slot = id, actor = sim.Save.player, target = "", location = sim.State.Get("at:" + sim.Save.player), from = sim.State.turn, until = sim.Data.turns - 1, label = "", actorText = "", observerText = "" } : Copy(source);
            var rules = sim.Rules.Where(r => r.actor == draft.actor && r.action == draft.id && string.IsNullOrEmpty(r.route)).Select(r =>
            {
                var copy = Copy(r); copy.amount = sim.Weight(r); return copy;
            }).ToList();
            var content = OpenModal(source == null ? "New action / " + sim.Name(draft.actor) : "Edit / " + draft.label);
            modal.Q(className: "factor-modal").AddToClassList("editor-modal");
            var tabs = Box(content, "editor-tabs");
            var form = Box(content, "editor-form");
            var footer = Box(modal.Q(className: "factor-modal"), "editor-footer");
            var error = Text(footer, "", "editor-error");
            var commands = Box(footer, "editor-commands");
            Button(commands, "Cancel", CloseModal, "tool-button");
            Button(commands, "Save action", () =>
            {
                string before = RecordJson(false);
                if (!sim.StoreDefinition(Copy(draft), rules.Select(Copy).ToList(), !generated, out string message)) { error.text = message; return; }
                undo.Push(before); notice = "Saved: " + draft.label; CloseModal(); Persist(); Render();
            }, "save-action");
            Action<string> show = null;
            show = tab =>
            {
                form.Clear();
                foreach (var b in tabs.Query<Button>().ToList()) b.EnableInClassList("selected", b.text == tab);
                if (tab == "Action")
                {
                    Text(form, sim.Name(draft.actor) + " / " + draft.id, "caption");
                    if (generated)
                    {
                        foreach (var criterion in sim.Availability(draft)) Text(form, criterion.description, "factor-copy");
                        return;
                    }
                    Field(form, "Action label", draft.label, v => draft.label = v);
                    Pick(form, "Location", new[] { "" }.Concat(sim.Data.rooms.Select(r => r.id)).ToList(), draft.location ?? "", v => draft.location = v, EntityName);
                    Pick(form, "Target", new[] { "" }.Concat(sim.Data.people.Where(p => p.id != draft.actor).Select(p => p.id)).ToList(), draft.target ?? "", v => draft.target = v, v => v == "" ? "None" : sim.Name(v));
                    WindowFields(form, draft.from, draft.until, v => draft.from = v, v => draft.until = v);
                    Toggle(form, "Once per decision slot", draft.once, v => draft.once = v);
                    Field(form, "Decision slot", draft.slot, v => draft.slot = v);
                    Pick(form, "Phase", new List<string> { "0", "1", "2", "3" }, draft.phase.ToString(), v =>
                    {
                        draft.phase = int.Parse(v);
                        if (draft.phase != 2) { draft.destination = ""; draft.effects.RemoveAll(e => e.key == "at:$actor"); }
                        show(tab);
                    }, v => new[] { "Conversation", "Room activity", "Movement", "End of turn" }[int.Parse(v)]);
                    if (draft.phase == 2)
                        Pick(form, "Destination", new[] { "" }.Concat(sim.Data.rooms.Select(r => r.id)).ToList(), draft.destination ?? "", v =>
                        {
                            draft.destination = v; draft.effects = v == "" ? new List<Effect>() : new List<Effect> { new Effect("at:$actor", v) };
                        }, v => v == "" ? "Choose a destination" : sim.Name(v));
                }
                if (tab == "Availability")
                {
                    Text(form, "ALL REQUIRED", "section-label");
                    if (generated) foreach (var criterion in sim.Availability(draft)) Text(form, criterion.description, "factor-copy");
                    else Conditions(form, draft.requires, draft);
                }
                if (tab == "Score")
                {
                    Text(form, "BASE SCORE / 0", "section-label");
                    foreach (var rule in rules.ToList())
                    {
                        var group = Box(form, "rule-editor");
                        var row = Box(group, "editor-pair");
                        var amount = new FloatField("Points") { value = rule.amount, name = "Points" }; row.Add(amount);
                        amount.RegisterValueChangedCallback(e => rule.amount = e.newValue);
                        Button(row, "\u00d7", () => { rules.Remove(rule); show(tab); }, "inspect-button", "Remove score condition");
                        WindowFields(group, rule.from, rule.until, v => rule.from = v, v => rule.until = v);
                        Conditions(group, rule.conditions, draft);
                    }
                    Button(form, "+ Score condition", () =>
                    {
                        rules.Add(new Rule { id = "custom_rule_" + Guid.NewGuid().ToString("N"), actor = draft.actor, action = draft.id, from = draft.from, until = draft.until, amount = 1 }); show(tab);
                    }, "tool-button");
                    foreach (var term in sim.Evaluate(draft).terms.Where(t => sim.Rules.First(r => r.id == t.id).route != null && sim.Rules.First(r => r.id == t.id).route != ""))
                        Text(form, "Route contribution / " + term.description + " / +" + term.amount, "factor-copy");
                }
                if (tab == "Effects & prose")
                {
                    if (generated) { Text(form, "Generated travel and waiting prose", "section-label"); return; }
                    Field(form, "As actor", draft.actorText, v => draft.actorText = v, true);
                    Field(form, "As target", draft.targetText, v => draft.targetText = v, true);
                    Field(form, "As observer", draft.observerText, v => draft.observerText = v, true);
                    Field(form, "Activity caption", draft.activity, v => draft.activity = v);
                    Toggle(form, "Quiet event", draft.quiet, v => draft.quiet = v);
                    Text(form, "FACT CHANGES", "section-label");
                    foreach (var effect in draft.effects.ToList())
                    {
                        var row = Box(form, "effect-row");
                        if (effect.key.StartsWith("owner:"))
                        {
                            Pick(row, "Item", sim.Data.items.Select(i => i.id).ToList(), effect.key.Substring(6), v => effect.key = "owner:" + v, sim.Name);
                            Pick(row, "To", Holders(draft), effect.value, v => effect.value = v, EntityName);
                        }
                        else
                        {
                            Field(row, "Set fact", effect.key, v => effect.key = v);
                            Field(row, "To value", effect.value, v => effect.value = v);
                        }
                        if (string.IsNullOrEmpty(draft.destination)) Button(row, "\u00d7", () => { draft.effects.Remove(effect); show(tab); }, "inspect-button", "Remove effect");
                    }
                    if (string.IsNullOrEmpty(draft.destination))
                    {
                        Button(form, "+ Fact change", () => { draft.effects.Add(new Effect("new_fact", "yes")); show(tab); }, "tool-button");
                        Button(form, "+ Item transfer", () =>
                        {
                            draft.effects.Add(new Effect("owner:" + sim.Data.items[0].id, "$actor"));
                            show(tab);
                        }, "tool-button");
                    }
                }
            };
            foreach (string tab in new[] { "Action", "Availability", "Score", "Effects & prose" }) Button(tabs, tab, () => show(tab), "editor-tab");
            show(generated ? "Score" : "Action");
        }

        string RecordsDirectory => sandbox ? Path.GetFullPath("artifacts/records-" + Screen.width + "x" + Screen.height) : Path.Combine(Path.GetDirectoryName(savePath), "records");
        string RecordJson(bool freeze = true)
        {
            var record = Copy(sim.Save); record.scenario = sim.Data; record.frozenScenario = freeze || sim.Save.frozenScenario;
            return JsonUtility.ToJson(record, true);
        }
        string WriteRecord()
        {
            Directory.CreateDirectory(RecordsDirectory);
            string path = Path.Combine(RecordsDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-life" + sim.Save.day + "-" + sim.Save.player + "-turn" + (sim.State.turn + 1) + "-" + Guid.NewGuid().ToString("N").Substring(0, 4) + ".json");
            File.WriteAllText(path, RecordJson()); return path;
        }
        void OpenRecords()
        {
            var content = OpenModal("State records");
            var result = Text(content, "Life " + sim.Save.day + " / " + sim.Name(sim.Save.player) + " / " + Simulation.Clock(sim.State.turn), "factor-copy");
            Button(content, "Record current state", () =>
            {
                try { string path = WriteRecord(); OpenRecords(); modal.Q<Label>(className: "record-status").text = "Recorded: " + Path.GetFileName(path); }
                catch (Exception ex) { result.text = ex.Message; }
            }, "tool-button");
            Text(content, "", "record-status");
            Field(content, "Records folder", RecordsDirectory, v => { }).isReadOnly = true;
            Button(content, "Open records folder", () => { Directory.CreateDirectory(RecordsDirectory); Application.OpenURL(new Uri(RecordsDirectory + Path.DirectorySeparatorChar).AbsoluteUri); }, "tool-button");
            var import = new TextField("Record file") { name = "Record file" }; content.Add(import);
            Button(content, "Load record file", () => ConfirmRecord(import.value), "tool-button");
            if (Directory.Exists(RecordsDirectory)) foreach (string path in Directory.GetFiles(RecordsDirectory, "*.json").OrderByDescending(p => p).Take(30))
                Button(content, Path.GetFileNameWithoutExtension(path), () => ConfirmRecord(path), "record-entry");
        }
        void ConfirmRecord(string path)
        {
            try
            {
                var saved = JsonUtility.FromJson<Campaign>(File.ReadAllText(path));
                if (saved == null || saved.version != 1 || !saved.frozenScenario || saved.scenario == null || saved.world == null || !saved.scenario.people.Any(p => p.id == saved.player)) throw new Exception("Not a supported Discontinuity state record.");
                var candidate = new Simulation(saved.scenario, saved);
                if (saved.world.turn < 0 || saved.world.turn > candidate.Data.turns || candidate.Data.people.Any(p => !candidate.Data.rooms.Any(r => r.id == saved.world.Get("at:" + p.id)))) throw new Exception("The record contains invalid positions or time.");
                foreach (var person in candidate.Data.people) candidate.Rank(person.id);
                var content = OpenModal("Restore state?");
                Text(content, "Life " + saved.day + " / " + candidate.Name(saved.player) + " / " + Simulation.Clock(saved.world.turn), "prose");
                Text(content, Path.GetFileName(path), "factor-copy");
                Button(content, "Cancel", CloseModal, "tool-button");
                Button(content, "Restore recorded state", () =>
                {
                    try
                    {
                        WriteRecord(); undo.Push(RecordJson(false)); sim = candidate;
                        notice = "Restored: " + Path.GetFileName(path); CloseModal(); Persist(); Render();
                    }
                    catch (Exception ex) { Text(content, "Could not preserve the current state: " + ex.Message, "editor-error"); }
                }, "save-action");
            }
            catch (Exception ex)
            {
                var content = OpenModal("Record not loaded"); Text(content, ex.Message, "editor-error");
            }
        }
    }
}
