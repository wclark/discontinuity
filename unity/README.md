# Discontinuity: Unity Prototype

A native, turn-based prototype of four interconnected lives in one household. Sixteen 15-minute turns lead from breakfast to the noon accusation. Play a person, alter a choice, then inhabit someone else and encounter its consequences. The original browser prototype is preserved in `../site/`.

## Open and Play

Use Unity Hub's **Add project from disk**, select this `unity` folder, and open it with **Unity 6000.6.0f1**. Open `Assets/Scenes/Household.unity` and press Play. No Asset Store packages or external services are needed.

From the repository root, `./prototype.ps1 Play` opens the native player, building it first if needed. `./prototype.ps1 Build` rebuilds; `./prototype.ps1 Verify` runs the simulation checks. Close this project in the editor before a command-line build. The executable is `builds/Discontinuity/Discontinuity.exe`; it needs its neighboring data folders.

Run `./install-desktop.ps1` from the repository root after building to create the **Discontinuity** desktop shortcut. Both the executable and shortcut use the custom blue-envelope/broken-clock icon; see [icon artwork and provenance](ICON.md).

## Play a Life

1. Begin as Clara in the Kitchen. Go to the Hall to meet Jonah, then choose how to treat him.
2. Each action advances one turn. There is only one action list, ranked for your current incarnation. **Decision factors** shows or hides its condition contributions. Inactive conditions are collapsed. **Your adjustments** appears only when a choice actually needed an increment.
3. The scene shows the people and loose items in your room, your inventory, and your recent experience. **Earlier today** contains only events you witnessed. Historical scoring is available only for your own actions.
4. The undo arrow restores the last turn within the current life. There is no Observe mode, free character selector, autoplay, omniscient timeline, forecast button, or experiment toolbar in the game.
5. Finish all sixteen turns. **Wake as Jonah** then begins the next incarnation. The order is Clara, Jonah, Father Vale, Dr. Merrow, then Clara again. The next person's previous adjustments are cleared; everyone else's remain. Undo cannot cross this transition.

Development fixtures remain available through command-line flags only: a kindness can supply missing corroboration, humiliation can redirect the accusation onto Clara, and a warning can interrupt Jonah's Archive errand. They are not alternate story scripts; the same ordinary rules produce every event. Condition increments can still be edited in the Unity asset, without exposing other characters' internals in play.

## The Design Choice

Use explicit **condition-weighted action selection**, with a graph helper for travel. Do not introduce a neural network, opaque personality multipliers, or a general-purpose planner yet. The complexity comes from shared scarce objects, timed encounters, and facts left by other people. This keeps each choice inspectable and lets the mechanism, rather than a replay script, run the day.

```text
score(action) = 0
              + sum(increment of each currently satisfied condition set)
              + at most one eligible prior-choice adjustment
```

All conditions within a set must hold. Separate sets targeting the same action add together. Missing or false conditions contribute zero. Positive and negative presence conditions use fact equality and inequality, not special semantic categories. Rule identifiers are deliberately neutral.

Preconditions are separate: scores cannot unlock a door, conjure an item, make an absent person present, or revive an expired decision. A deterministic tie-break prefers waiting, then action ID, without adding points.

### Travel and Interruption

A rule's `route` field expands its contribution onto the next adjacent movement choice on a shortest path. It supplies no points at the destination. For example, an envelope in the Archive and an uncopied address can activate movement toward the Archive. A separate condition set contributes to copying the address once there. Possessing the envelope activates movement toward the Chapel and a separate action there.

These are not queued scripts. Every turn re-evaluates every valid choice. A competing condition can interrupt travel; if the original conditions still hold afterward, the route continues from the new room. Missing objects invalidate dependent actions and can deactivate their associated travel contributions. Windows tolerate small delays but deliberately allow a missed encounter to matter.

### Turns and Conflicts

All four people rank options against the same start-of-turn world. The human can replace their own proposed choice. Proposals resolve in explicit phases: conversation (`0`), object work (`1`), movement (`2`), waiting (`3`), then stable actor ID. Preconditions are checked again before applying effects. Thus conversation can precede a departure, two people cannot both take the same envelope, and a newly heard request influences scoring on the following turn. A blocked proposal is logged and reconsidered next turn, not repaired by an alternate script.

### Prior Choices

When a human selects below the highest condition score:

```text
adjustment = highest_condition_score - chosen_condition_score + 1
```

If already tied for highest, no adjustment is recorded. Values never increase merely because another turn passed. Records are scoped to the actor, named decision context, location, and a bounded window from the original turn through two turns later (clamped to the action's window). A later manual decision in the same context closes the earlier window, even when the new choice needs no increment. Only the latest eligible record is considered and each is consumed at most once per pass.

Current human choices do not receive their newly recorded adjustments. Replaying that person clears all their previous adjustments; other people's records remain. Changed condition contributions can outweigh a record, and invalid actions never receive it. This is intentionally a soft preference, not guaranteed replay.

## Data and Source

`Assets/Resources/Household.asset` is the editable **ScriptableObject** containing all rooms, people, items, choices, and condition sets. Unity's Inspector can edit its lists and values. `HouseholdContent.cs` supplies the original fixture and creates the asset when absent; it does not overwrite Inspector edits during builds.

| File | Responsibility |
| --- | --- |
| `Assets/Core/Model.cs` | Serializable entities, facts, decisions, events, campaign |
| `Assets/Core/Simulation.cs` | Validation, scoring, pathfinding, resolution, adjustments, forecast |
| `Assets/Core/HouseholdContent.cs` | Authored example definitions |
| `Assets/Runtime/Household.cs` | ScriptableObject wrapper |
| `Assets/Runtime/Workbench.cs` | Incarnation-focused UI, persistence, interaction tests |
| `Assets/Resources/Workbench.uss` | Interface styling |
| `Assets/Editor/PrototypeBuild.cs` | Scene setup, regression checks, Windows build |

The core has no Unity dependency. Only the asset wrapper, UI, serialization adapter, and build tools use Unity APIs.

### Adding Content

- **Room:** stable `id`, readable `name`, `description`, normalized map `x/y`, adjacent `exits`. Define both directions for bidirectional exits.
- **Person:** stable `id`, `name`, `role`, `color`, starting `location`, viewpoint `concern`. Add their authored choices and condition sets. The list order determines the incarnation sequence.
- **Item:** stable `id`, `name`, starting `owner`. Owners can be a person, room, or authored container. `owner:<id>` has exactly one current value.
- **Fact:** arbitrary key/value, for example `at:jonah = hall`, `owner:envelope = archive`, `trust = yes`. Use actor-qualified social keys when extending to more than this slice's single Clara/Jonah relationship.
- **Choice:** unique `id`, `actor`, optional `target`, required `location`, inclusive `from/until` turn window, named `slot`, `requires`, `effects`, phase, and actor/target/observer prose. A `once` slot is resolved by any of its alternatives. `$actor` and `$target` substitute in fact keys and values.
- **Condition set:** unique `id`, `actor`, either concrete `action` or destination `route`, inclusive time window, `amount`, and conditions. Set `not` for inequality/absence. Movement IDs are `move:<room>` and waiting is `wait`, so these can be targeted directly too. There is no hidden action baseline.

Fact records track which event most recently set them. Decision records retain the exact ranked alternatives and contributing event IDs for development tools, but the player UI never exposes another person's scoring. Events record witnesses at resolution, including arrivals and departures. This prevents visiting a room later from revealing its earlier events. Older saves reconstruct witness lists from the ordered event log without resetting their world or choices.

## Verification and Persistence

`Discontinuity > Verify simulation` tests defaults, alternate outcomes, necessary-only adjustments, nonaccumulation, replay clearing, invalidation, delayed encounters, stronger conditions, JSON round trips, forecasts, simultaneous item claims, and historical rankings. The report is `../artifacts/simulation-verification.txt`.

The native player's `-smoke` flag drives actual UI Toolkit submit events through manual actions, undo, factor visibility, a complete life, the locked incarnation transition, and experiencing an earlier kindness as Jonah. It also checks that NPC factors and unwitnessed events are absent from the UI. Run from the repository root:

```powershell
./builds/Discontinuity/Discontinuity.exe -smoke -capture artifacts/ui-smoke.png -captureQuit
```

Capture flags use the actual rendered player framebuffer and need a visible graphics window. `-demo kindness`, `-demo humiliation`, `-demo warning`, or `-demo baseline` starts a deterministic fixture. Demo, smoke, and capture modes do not overwrite the player's save.

Ordinary play automatically writes `household-v1.json` under Unity's `Application.persistentDataPath`, normally `%USERPROFILE%/AppData/LocalLow/Discontinuity/Discontinuity/` on Windows. The current world, event history, other-viewpoint adjustments, condition overrides, and previous pass persist. This replaces browser `localStorage` for the native prototype.

## Deliberate Limits

This is a small playable prototype, not a finished adventure. Presentation and journals are viewpoint-limited, but the condition evaluator still reads the authored world facts rather than a complete per-person belief model. Social states are simple facts, not continuous personality variables. Crowds and location paintings are not yet implemented. The engine retains forecasting and cross-character history for automated verification, not as player-facing controls.

The next useful extension would be **person-scoped knowledge with event provenance**, so a secret changes a choice only after that person hears or observes it. A small bounded planner could follow later if room-by-room condition rules become burdensome. Learned behavior is not needed to prove this mechanic.

## Distribution

The verified target is a local Windows player. Distribute the entire `builds/Discontinuity` folder, not just its executable. This iteration does not replace the existing S3 website. A Unity Web build remains a separate deployment task because its generated files and hosting headers differ from the preserved plain-JavaScript site.
