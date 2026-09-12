# Illustrated Story Assets

Generated with OpenAI ImageGen for this prototype on 2026-09-12. These are original generated game assets, not photographs of a real place or people.

The five room backgrounds contain no named characters or movable plot objects. The four figures are independent transparent cutouts, composited from the atlas at runtime. Their location, highlighted actor, and activity caption come from the exact resolved event. Departures retain a fading figure for that moment only. The first art pass uses standing figures, not action-specific poses or full animation. The prose remains authoritative for physical actions. The groundskeeper and background household crowd are currently prose-only.

## Asset Layout

- `Assets/Resources/Scenes/<room-id>.png`: five 2172 x 724 room paintings.
- `Assets/Resources/Characters.png`: 1774 x 887 RGBA atlas, four equal-width columns: Clara, Jonah, Father Vale, Dr. Merrow. Runtime slicing keeps fractional quarter boundaries, with no atlas resampling.
- `SceneArt.cs`: room image, independent cutouts, observed action captions. No graphics are generated at runtime.
- New rooms need a matching background path; new characters need a sprite mapping in `SceneArt`. New actions can specify an `activity` caption in `Household.asset` and their actor/target/observer prose. Missing captions fall back to the action label.

## Generation Provenance

Original output folder on the development machine: `C:/Users/bill/.codex/generated_images/019e1a1a-9e8c-7f50-920e-337bf03247d7/`. Originals were copied unchanged into the project. Image dimensions produced by the generator differ from the requested pixel sizes while retaining the requested aspect ratios.

### Hall

Source: `exec-fe535d32-1fa4-425c-b735-917b59db4186.png`.

Prompt:

> Game environment background for Discontinuity, a literary Victorian household mystery. Empty manor HALL, panoramic 3:1 composition, 1536x512, eye-level theatrical stage view. Restrained hand-painted gouache and fine ink, elegant illustrated storybook for adults, visible brushwork, cool sage plaster, muted terracotta floor, walnut doorways, misty blue morning light. Broad central hallway with a staircase to the left, a simple brass pendulum clock with NO readable time marks, framed empty landscape painting, side doorways leading into the house. Architectural details sharp and legible, softly lit but NOT dark. Furnishings mainly along back wall; lower foreground is an uncluttered open floor for separate character sprites. No people, silhouettes, statues, faces, human portraits, text, lettering, UI, envelope, loose cloth, loose ledger, or other plot objects. Entire image is one coherent wide environment, not a collage or framed illustration. The wide aspect ratio is important.

### Kitchen

Source: `exec-7b89cb30-203d-46fa-b8bd-a6027737fc62.png`.

Prompt:

> Game environment background for Discontinuity, a literary Victorian household mystery. Empty manor KITCHEN, panoramic 3:1 composition, eye-level theatrical stage view. Restrained hand-painted gouache and fine ink with realistic architecture, sophisticated storybook style, crisp visible details. Morning daylight, sea-green painted cupboards, off-white plaster, copper pans above a dark cast-iron range, pale flagstone floor. A broad worktable sits against the back wall, shelves and tall windows on the sides, low orange light from the stove contrasts cool blue daylight. Furnishings at back and sides; keep the lower foreground floor uncluttered for separately drawn character sprites. Bright enough to inspect the room. No people, silhouettes, statues, faces, readable text, UI, envelope, loose cloth, ledger, or plot objects. One coherent very wide environment filling the image, not a collage or frame. Aspect ratio exactly 3:1, 1536x512.

### Archive

Source: `exec-2252cd74-a577-4cb6-a33d-d74bb705c725.png`.

Prompt:

> Game environment background for Discontinuity, a literary Victorian household mystery. Empty manor ARCHIVE, panoramic 3:1 composition, eye-level theatrical stage view. Restrained hand-painted gouache and fine ink with realistic architecture, sophisticated adult storybook style, crisp legible details. Blue-grey walls, tall dark wooden shelves filled with uniformly closed books and pigeonholes, a broad empty writing desk against the back wall under a tall window, a green-glass banker's lamp and an empty wooden chair to one side. Brisk cool morning daylight with small amber accents. Lower foreground clear wooden floor for separately composited character sprites. The desk surface must have NO blue envelope, NO open book, NO loose ledger or movable story objects. No people, silhouettes, statues, portraits, faces, readable lettering, text, UI, frames or collage. Entire image one coherent room. Bright enough to read spatial details. Aspect exactly 3:1, 1536x512.

### Garden

Source: `exec-e12268cc-693c-4a9b-aab5-e7b439e69a38.png`.

Prompt:

> Game environment background for Discontinuity, a literary Victorian household mystery. Empty walled manor GARDEN, panoramic 3:1 composition, eye-level theatrical stage view. Restrained hand-painted gouache and fine ink with realistic architecture, sophisticated adult storybook style, visible brushwork and crisp readable detail. Rain-damp grey flagstone terrace, clipped box hedges, wet rose bushes with muted coral flowers, green lawn beyond, a stone bench sheltered beneath an awning to the left, manor side door on the right, garden gate at the far back. Soft overcast blue morning light, fresh deep greens, small warm red accents. Keep broad lower foreground terrace clear for separately composited character sprites. No people or silhouettes, no statues or faces, no readable text, no UI, no envelope, no medicine bottles, no cloth or movable plot objects. One continuous environment, not a collage, not a framed painting. Bright and legible. Aspect exactly 3:1, 1536x512.

### Chapel

Source: `exec-cd4bb9f1-140c-4e9d-beb1-1cfd611f9455.png`.

Prompt:

> Game environment background for Discontinuity, a literary Victorian household mystery. Empty private manor CHAPEL, panoramic 3:1 composition, eye-level theatrical stage view. Restrained hand-painted gouache and fine ink with realistic architecture, sophisticated adult storybook illustration. Pale grey limestone, shallow arched ceiling, tall geometric stained glass in muted amber and sea blue, a plain dark wooden lectern at the back center, a few empty pews along the sides, side door visible. Cool morning daylight with soft colored patches on the stone floor. The lower foreground is broad and uncluttered for separate character sprites. The lectern is closed and empty: no book, envelope, loose cloth, medicine or key story items. No people, silhouettes, saints, statues, faces, portraits, text, UI or lettering. Well-lit readable interior, not gloomy. One coherent environment fills the whole image, no frame or collage. Aspect exactly 3:1, 1536x512.

### Characters

Source: `exec-1fa7c528-c084-4e31-9801-bc65a9df4124.png`.

Prompt:

> A production sprite atlas for a Victorian illustrated narrative game. ONE image with exactly FOUR separate FULL-BODY adult character cutouts in a single horizontal row of FOUR EQUAL-WIDTH columns. Truly transparent alpha background, no floor, no scenery, no backdrop, no checkerboard drawn into image. Wide 2:1 canvas 2048x1024. Each figure centered at 12.5%, 37.5%, 62.5%, 87.5% of canvas width respectively, feet on same baseline at 95% height, heads near 6% height, never crossing their quarter-width cell. Comfortable transparent gaps. Render each at the same realistic human scale, crisp hand-painted gouache with fine ink edges, sophisticated adult storybook style, muted but distinctive colors, natural proportions, soft neutral frontal lighting. LEFT TO RIGHT: (1) Clara, white woman about 28, brown hair in a loose practical bun, sage-green long-sleeved working dress, ivory maid apron, reddish neck scarf, alert and composed, empty hands relaxed at waist; (2) Jonah, white man about 23, tousled auburn hair, ivory rolled-sleeve shirt with a small dark ink stain on one cuff, muted blue waistcoat, charcoal trousers, worn shoulder satchel, slightly guarded, empty hands; (3) Father Vale, white man about 55, narrow face, dark hair silver at temples, long charcoal cassock with subtle plum undertone and white clerical collar, empty hands folded, controlled expression; (4) Dr. Merrow, Black woman about 50, short natural grey curls, muted burgundy blouse, dark green Victorian tailored coat and long skirt, dignified, small brown doctor's bag by one side, watchful calm expression. All standing in a neutral conversational pose angled a little toward the center, complete head-to-shoes visible, no cropping. NO labels, text, names, numbers, panels, borders, circles, interface or extra figures. Background must be transparent so the atlas can be composited over rooms.
