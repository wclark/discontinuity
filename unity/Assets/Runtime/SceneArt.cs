using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    public sealed class SceneArt
    {
        readonly Dictionary<string, Sprite> figures = new Dictionary<string, Sprite>();
        readonly SceneLibrary library;
        public SceneArt()
        {
            library = JsonUtility.FromJson<SceneLibrary>(Resources.Load<TextAsset>("SceneLibrary").text);
            var atlas = Resources.Load<Texture2D>("Characters");
            if (atlas == null) return;
            string[] ids = { "clara", "jonah", "vale", "merrow" };
            float width = atlas.width / 4f;
            for (int i = 0; i < ids.Length; i++)
                figures[ids[i]] = Sprite.Create(atlas, new Rect(i * width, 0, width, atlas.height), new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect);
        }
        public void Dispose()
        {
            foreach (var figure in figures.Values) Object.Destroy(figure);
            figures.Clear();
        }
        public void Render(VisualElement parent, Simulation sim, Room room, List<Fact> snapshot, Event beat)
        {
            var scene = new VisualElement { name = "illustrated-scene", userData = room.id };
            scene.AddToClassList("scene-art"); parent.Add(scene);
            string signature = SceneLibrary.Signature(sim.Data, room.id, snapshot, beat);
            var tableau = library.Find(signature);
            var custom = tableau == null ? null : Resources.Load<Texture2D>(tableau.resource);
            var cast = Story.Cast(sim.Data, snapshot, room.id, beat);
            if (custom != null && (tableau.castOrder == null || tableau.castOrder.Distinct().Count() != cast.Count || tableau.castOrder.Count != cast.Count || !cast.All(p => tableau.castOrder.Contains(p.id)))) custom = null;
            if (custom != null) cast = tableau.castOrder.Select(id => cast.Find(p => p.id == id)).ToList();
            var painting = new Image { image = custom != null ? custom : Resources.Load<Texture2D>("Scenes/" + room.id), scaleMode = custom != null ? ScaleMode.ScaleToFit : ScaleMode.ScaleAndCrop,
                userData = custom != null ? tableau.resource : "Scenes/" + room.id };
            painting.AddToClassList("room-painting"); scene.Add(painting);
            scene.RegisterCallback<GeometryChangedEvent>(e => scene.style.height = e.newRect.width * 2f / 3f);
            var stage = new VisualElement(); stage.AddToClassList("cast-stage"); scene.Add(stage);
            var captions = new VisualElement(); captions.AddToClassList("cast-captions"); parent.Add(captions);
            foreach (var person in cast)
            {
                bool active = beat != null && beat.actor == person.id;
                bool departing = Story.Departing(beat, person.id, room.id);
                var place = new VisualElement { userData = person.id }; place.AddToClassList("cast-person"); stage.Add(place);
                if (active) place.AddToClassList("acting");
                if (departing) place.AddToClassList("departing");
                if (custom == null && figures.TryGetValue(person.id, out var figure))
                {
                    var image = new Image { sprite = figure, scaleMode = ScaleMode.ScaleToFit };
                    image.AddToClassList("character-image"); place.Add(image);
                }
                var caption = new VisualElement { userData = person.id }; caption.AddToClassList("cast-caption"); captions.Add(caption);
                if (active) caption.AddToClassList("acting-caption");
                var name = new Label(person.name + (person.id == sim.Save.player ? " (you)" : ""));
                name.AddToClassList("cast-name"); caption.Add(name);
                var activity = new Label(Story.Activity(sim, beat, person, room.id));
                activity.AddToClassList("cast-activity"); caption.Add(activity);
            }
        }
    }
}
