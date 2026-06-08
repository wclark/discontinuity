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
- `site/scripts/engine.js` runs turns, validates actions, scores condition increments and actions, applies effects, saves state, and builds the render model.
- `site/scripts/app.js` renders the play/debug workbench and wires buttons to engine actions.
- `site/assets/locations/*.svg` contains empty atmospheric location images.

The world model is data-driven around:

- time slots
- locations
- people
- items
- facts
- relationships
- condition sets
- actions
- events

Preconditions decide whether an action can happen. Condition sets add points to concrete choices when their facts line up. Scoring decides which valid action is most attractive right now. NPCs choose from the same ranked candidate list as the player sees, plus generated movement and waiting actions.

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

Add items in `DATA.items` with:

- `id`
- `name`
- `startLocation` or `startOwner`
- `description`

Add condition sets in `DATA.goals`. Important fields:

- `id`
- `label`
- `actorIds`
- conditional `adjustments`, each aimed at one `actionId` with an `amount`
- each adjustment's `conditions`, such as time, actor location, item location, item possession, people present, facts, and relationships
- optional ordered `instructions` for human-readable progress/debugging
- optional per-instruction `satisfied` conditions
- a `completion` condition

Add actions in `DATA.actions`. Important fields:

- `id`
- `label`
- `actorIds`
- optional `targetId`
- optional `slotId`
- `timeIds`
- optional `timeWindow`, for actions that can survive small delays
- `locationId`
- `tags`
- `preconditions`
- optional `conditionBonuses` or legacy `modifiers`, each with conditions and an amount
- `effects`
- perspective text: `actor`, `target`, and `observer`

## Behavior Scoring

Every turn, the engine gathers a list of valid candidate actions for each character. Validity is strict: a character cannot give away an object they do not have, accuse someone who is not present when presence is required, or perform an action outside its time range.

Every candidate action starts at zero. Scores only move when a satisfied condition adds an explicit amount to that concrete action. Those increments can come from:

- a condition set adjustment in `DATA.goals`
- a condition bonus directly on an action
- a prior-run manual adjustment that starts on or after a specific turn

Each condition set adjustment targets one concrete choice by action id, such as `move_archive`, `wait`, or `father_take_envelope`, and adds its `amount` only if its local conditions are true. Any currently available action can be a target. Conditions can reference time, the actor's location, who is present, item possession or item location, facts, relationships, and prior choices. If the choice is not currently valid or not present in the location, the adjustment cannot do anything. If no condition adds points to an NPC option, the NPC does nothing that turn.

When the player chooses an action that is not already tied for the highest adjustment-free score, the engine stores a manual adjustment for that character and action context. The amount is calculated from the adjustment-free scores for that turn: it is large enough to make the human-picked action the highest-scoring valid option by a small margin on a comparable later run. If the chosen action is already top-scored, no manual adjustment is stored.

```js
behaviorFudges[characterId][slotOrActionId] = {
  actionId,
  actionLabel,
  amount,
  startsAt,
  locationId,
  targetId
}
```

On later runs, the adjustment applies only on or after `startsAt`, in the same recorded location/target context, and only to the same action id. Manual adjustments saved during the current played run do not affect that same run. If more than one saved adjustment could match, the most recent matching adjustment is used instead of adding them together. If the action is impossible, it cannot fire. If current conditions make another valid action score higher, the character can diverge.

Starting a new run as a character clears that character's previous manual adjustments. The new run then records a fresh set from the player's choices, including generated movement and waiting choices.

## Decision Debugging

During a run, the UI is intentionally a compact behavior workbench. Each character row shows location, top condition set, top action, and final action score. Expanding a character shows condition sets with their local choice increments, instruction status, stored manual adjustment amounts, and the full ranked action list. Each action row includes its action id, total condition score, active condition chips, and any prior manual adjustment amount.

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
