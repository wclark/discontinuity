# Discontinuity

Discontinuity is a static prototype for a graphical, turn-based text adventure about many playable characters experiencing the same day.

The player begins as one character, makes choices, reaches the end of the day, then begins again as another character. Previous player deviations do not replay through a separate script system. They become small score adjustments inside the same action system used by NPCs, so characters tend to bend toward the day the player gave them while still reacting to current location, timing, people present, objects, and social pressure.

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
- Social consequences through relationships, fear, resentment, gratitude, suspicion, guilt, and obligation.
- `localStorage` saves for unlocked characters, discovered timeline entries, current run state, and behavior adjustments from prior runs.

## Engine Shape

The prototype is deliberately plain HTML/CSS/JavaScript:

- `site/index.html` loads the app shell.
- `site/styles.css` handles the responsive illustrated dashboard UI.
- `site/scripts/data.js` defines the world and action templates.
- `site/scripts/engine.js` runs turns, validates actions, scores actions, applies effects, saves state, and builds the render model.
- `site/scripts/app.js` renders the UI and wires buttons to engine actions.
- `site/assets/locations/*.svg` contains empty atmospheric location images.

The world model is data-driven around:

- time slots
- locations
- people
- items
- facts
- relationships
- actions
- events

Preconditions decide whether an action can happen. Scoring decides whether a character wants to do it. NPCs choose from the same ranked candidate list as the player sees, plus generated movement and waiting actions.

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
- a `routine` mapping time slot IDs to mildly preferred locations

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
- optional `timeWindow`, for actions that can survive small delays
- `locationId`
- `baseScore`
- `tags`
- `preconditions`
- `modifiers`
- `effects`
- perspective text: `actor`, `target`, and `observer`

## Behavior Scoring

Every turn, the engine gathers a list of valid candidate actions for each character. Validity is strict: a character cannot give away an object they do not have, accuse someone who is not present when presence is required, or perform an action outside its time range.

Scoring is flexible. A score can include:

- the action's `baseScore`
- data modifiers in `DATA.actions`
- turn-window urgency
- whether the target is present
- crowd effects for public, private, risky, or humiliating actions
- relationship pressure
- movement pressure toward useful locations, people, or items
- a prior-run score adjustment that starts on or after a specific turn

When the player chooses something that differs from the character's baseline top choice, the engine stores a capped adjustment for that character and action context:

```js
behaviorFudges[characterId][slotOrActionId] = {
  actionId,
  actionLabel,
  amount,
  startsAt,
  locationId,
  targetId,
  tags
}
```

On later runs, the adjustment applies only on or after `startsAt`. It boosts the same action if it is valid, gives a smaller pull to similar actions, and nudges movement toward the relevant location. If the action is impossible, it cannot fire. If the current situation makes another valid action score higher, the character can diverge.

Choosing the baseline top action again clears the stored adjustment for that decision context.

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
