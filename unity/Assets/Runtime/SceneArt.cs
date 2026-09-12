using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Discontinuity
{
    public sealed class SceneArt
    {
        readonly Dictionary<string, Sprite> figures = new Dictionary<string, Sprite>();
        public SceneArt()
        {
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
            var painting = new Image { image = Resources.Load<Texture2D>("Scenes/" + room.id), scaleMode = ScaleMode.ScaleAndCrop };
            painting.AddToClassList("room-painting"); scene.Add(painting);
            scene.RegisterCallback<GeometryChangedEvent>(e => scene.style.height = e.newRect.width / 3f);
            var cast = Story.Cast(sim.Data, snapshot, room.id, beat);
            var stage = new VisualElement(); stage.AddToClassList("cast-stage"); scene.Add(stage);
            var captions = new VisualElement(); captions.AddToClassList("cast-captions"); parent.Add(captions);
            foreach (var person in cast)
            {
                bool active = beat != null && beat.actor == person.id;
                bool departing = Story.Departing(beat, person.id, room.id);
                var place = new VisualElement { userData = person.id }; place.AddToClassList("cast-person"); stage.Add(place);
                if (active) place.AddToClassList("acting");
                if (departing) place.AddToClassList("departing");
                if (figures.TryGetValue(person.id, out var figure))
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
