using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Discontinuity
{
    [Serializable] public class SceneRequest
    {
        public string id, signature, room, action, resource, prompt;
        public int occurrences;
        public List<string> cast;
    }
    [Serializable] public class SceneCoverageReport
    {
        public string protocol = "All one-choice deviations from each incarnation's unmodified morning, plus their continuations and one multi-life crossing fixture. This is bounded sampling, not exhaustive reachability.";
        public int runs, discovered, illustrated;
        public List<SceneRequest> requests;
    }
    public static class SceneCoverage
    {
        public static void Export(Scenario data)
        {
            var library = JsonUtility.FromJson<SceneLibrary>(Resources.Load<TextAsset>("SceneLibrary").text);
            var requests = new Dictionary<string, SceneRequest>(); int runs = 0;
            Action<Simulation> collect = sim =>
            {
                foreach (var e in sim.State.events) foreach (string viewer in e.witnesses)
                {
                    string room = Story.Room(e, viewer);
                    var scene = Story.Scene(e, viewer);
                    string key = SceneLibrary.Signature(data, room, scene, e);
                    if (requests.TryGetValue(key, out var existing)) { existing.occurrences++; continue; }
                    string id;
                    using (var hash = SHA256.Create()) id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "").Substring(0, 16).ToLowerInvariant();
                    var cast = Story.Cast(data, scene, room, e).Select(p => p.name).ToList();
                    requests.Add(key, new SceneRequest { id = id, signature = key, room = room, action = e.action, cast = cast, occurrences = 1,
                        resource = library.Find(key)?.resource ?? "",
                        prompt = "Discontinuity, complete illustrated story tableau, landscape 3:2. Use the room and Characters.png identity references. Location: " + sim.Name(room) +
                            ". Exactly these people: " + string.Join(", ", cast) + ". Visible beat: " + (e.blocked ? "The attempt was blocked; do NOT illustrate a completed action. " : "") + Story.Text(sim, e, viewer) +
                            " Visual-state key (cast, exposed items, route directions, success): " + key +
                            ". Depict the physical action, not a lineup. Do not add bystanders, plot objects, UI, or lettering. Keep faces, costumes and architecture consistent. Do not depict private thoughts from the prose." });
                }
            };
            foreach (var person in data.people)
            {
                var prefix = new Simulation(data); prefix.Save.player = person.id;
                while (!prefix.Ended)
                {
                    foreach (var option in prefix.Rank(person.id))
                    {
                        var branch = new Simulation(data, JsonUtility.FromJson<Campaign>(JsonUtility.ToJson(prefix.Save)));
                        branch.Step(option.choice.id);
                        while (!branch.Ended) branch.Step();
                        collect(branch); runs++;
                    }
                    prefix.Step();
                }
            }
            var crossing = new Simulation(data); crossing.Save.player = "vale";
            while (!crossing.Ended) crossing.Step(crossing.State.turn == 3 ? "wait" : null);
            crossing.Begin("clara");
            crossing.Step("move:hall"); crossing.Step("ignore"); crossing.Step("wait"); crossing.Step("wait"); crossing.Step("move:archive");
            while (!crossing.Ended) crossing.Step(); collect(crossing); runs++;
            var report = new SceneCoverageReport { runs = runs, discovered = requests.Count,
                illustrated = requests.Values.Count(r => r.resource != ""), requests = requests.Values.OrderByDescending(r => r.occurrences).ThenBy(r => r.id).ToList() };
            string path = Path.GetFullPath("../artifacts/scene-catalog.json"); Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("SCENE_CATALOG " + report.discovered + " distinct visible states, " + report.illustrated + " illustrated, from " + runs + " sampled runs");
        }
    }
}
