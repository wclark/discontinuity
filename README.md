# Discontinuity

Discontinuity is a static prototype for a graphical, turn-based text adventure about many playable characters experiencing the same day.

The player begins as one character, makes choices, reaches the end of the day, then begins again as another character. Previous authored choices do not replay through a separate script system. They become persistent scoring biases inside the same action system used by NPCs, so characters tend to repeat the day the player gave them unless changed circumstances make another valid action more compelling.

## Run Locally

Open `site/index.html` in a browser. There is no build step and no server requirement.

If you prefer a local server:

```powershell
cd site
python -m http.server 8080
```

Then open `http://localhost:8080`.

## Prototype Scope

The current vertical slice includes:

- Playable viewpoints for Clara, Jonah, Father Vale, and Dr. Merrow, unlocked through completed runs.
- Fixed locations: Hall, Kitchen, Chapel, Archive, and Garden.
- A blue envelope mystery and a late-morning accusation.
- Discrete 15-minute turns.
- Explicit action buttons instead of a parser.
- Perspective-sensitive event prose.
- Social consequences through memories, relationships, fear, resentment, gratitude, suspicion, guilt, and obligation.
- `localStorage` saves for unlocked characters, discovered timeline entries, current run state, and authored decision biases.

## Engine Shape

The prototype is deliberately plain HTML/CSS/JavaScript:

- `site/index.html` loads the app shell.
- `site/styles.css` handles the responsive illustrated dashboard UI.
- `site/scripts/data.js` defines the world and authored action templates.
- `site/scripts/engine.js` runs turns, validates actions, scores actions, applies effects, saves state, and builds the render model.
- `site/scripts/app.js` renders the UI and wires buttons to engine actions.
- `site/assets/locations/*.svg` contains empty atmospheric location images.

The world model is data-driven around:

- time slots
- locations
- people
- items
- facts
- memories
- relationships
- actions
- events

Preconditions decide whether an action can happen. Scoring decides whether a character wants to do it.

## Adding Content

Add locations in `DATA.locations` with:

- `id`
- `name`
- `crowd`
- `image`
- `description`
- `exits`

Add characters in `DATA.characters` with:

- `id`
- `name`
- `role`
- `startLocation`
- `unlocksAfter`
- `motive`
- a `routine` mapping time slot IDs to preferred locations

Add items in `DATA.items` with:

- `id`
- `name`
- `startLocation` or `startOwner`
- `description`

Add actions in `DATA.actions`. Important fields:

- `id`
- `label`
- `actorIds`
- optional `targetId`
- optional `slotId`
- `timeIds`
- `locationId`
- `baseScore`
- `tags`
- `preconditions`
- `modifiers`
- `effects`
- perspective text: `actor`, `target`, and `observer`

## Authored Bias

When the player chooses an action in a named decision slot, the engine compares that action's score against the highest-scoring valid action at that moment.

If needed, it saves a bonus large enough to make the chosen action win by a small margin. The saved entry is stored by character and decision slot:

```js
authoredBiases[characterId][slotId] = {
  actionId,
  actionLabel,
  bonus
}
```

On later runs, NPC scoring includes that bonus only if the same action is still valid. If the action's preconditions fail, it disappears from consideration. If relationships, memories, facts, or inventory changes make another valid action score higher, the character can deviate naturally.

Only one authored choice exists per character per decision slot. Replaying that character and choosing differently overwrites the old bias.

## Deploy

The deployment script mirrors the existing static-site S3 process:

```powershell
.\deploy.ps1 -Profile georgist-login
```

It syncs only the `site/` folder to `s3://discontinuity.org`, the S3 website origin serving `discontinuity.org`, with a short cache time and deletes removed files. It also invalidates CloudFront distribution `E3V4IEGTIQS7EP`.

If the AWS SSO session has expired, refresh the existing profile first:

```powershell
aws login --profile georgist-login
```
