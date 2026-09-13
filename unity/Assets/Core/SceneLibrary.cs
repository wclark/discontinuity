using System;
using System.Collections.Generic;
using System.Linq;

namespace Discontinuity
{
    [Serializable] public class Tableau { public string signature, resource, description; public List<string> castOrder = new List<string>(); }
    [Serializable] public class SceneLibrary
    {
        public List<Tableau> scenes = new List<Tableau>();
        public Tableau Find(string signature) { return scenes.Find(s => s.signature == signature); }
        public static string Signature(Scenario data, string room, List<Fact> scene, Event e)
        {
            var cast = Story.Cast(data, scene, room, e).Select(p => p.id).OrderBy(id => id, StringComparer.Ordinal).ToList();
            var props = new List<string>();
            if (e != null && e.kind == "crossing")
                props.AddRange(cast.Select(id => id + ":" + Story.Get(e.sceneBefore, "at:" + id) + ">" + Story.Get(e.sceneAfter, "at:" + id)));
            else foreach (var item in data.items.OrderBy(t => t.id, StringComparer.Ordinal))
            {
                string after = Story.Get(scene, "owner:" + item.id);
                string before = e == null ? after : Story.Get(e.sceneBefore, "owner:" + item.id);
                if (before != after && (cast.Contains(before) || cast.Contains(after) || before == room || after == room)) props.Add(item.id + ":" + before + ">" + after);
                else if (after == room) props.Add(item.id + ":" + room);
            }
            return string.Join("|", room, e?.action ?? "presence", e?.actor ?? "", e?.target ?? "", string.Join(",", cast), string.Join(",", props), e != null && e.blocked ? "blocked" : "ok");
        }
    }
}
