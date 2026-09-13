# Discontinuity: Unity Prototype

A native, turn-based prototype of four interconnected lives in one household. Sixteen 15-minute turns lead from breakfast to the noon accusation. Play a person, alter a choice, then inhabit someone else and encounter its consequences. The original browser prototype is preserved in `../site/`.

## Open and Play

Use Unity Hub's **Add project from disk**, select this `unity` folder, and open it with **Unity 6000.6.0f1**. Open `Assets/Scenes/Household.unity` and press Play. No Asset Store packages or external services are needed.

From the repository root, `./prototype.ps1 Play` opens the native player, building it first if needed. `./prototype.ps1 Build` rebuilds; `./prototype.ps1 Verify` runs the simulation checks. Close this project in the editor before a command-line build. The executable is `builds/Discontinuity/Discontinuity.exe`; it needs its neighboring data folders.

Run `./install-desktop.ps1` from the repository root after building to create the **Discontinuity** desktop shortcut. Both the executable and shortcut use the custom blue-envelope/broken-clock icon; see [icon artwork and provenance](ICON.md).

## Compose a Life

1. Begin as Clara in the Kitchen. Go to the Hall to meet Jonah, then choose how to treat him.
2. Choose one action for the next fifteen minutes. All four people decide from the same start-of-turn world. **Just happened** shows witnessed results alongside the next available choices. There are no separate next-moment or continue-to-time confirmations in adjustment mode.
3. The illustration and room information are beside the current situation and action buttons. Complete custom illustrations depict the cloth exchange and the Clara/Jonah Archive crossing; other combinations use room paintings and independent character cutouts. Scene selection checks the actual cast, action, exposed items and movement directions. A new bystander or failed action falls back safely instead of displaying an incompatible picture. See [artwork and generation prompts](ART.md) and the [scene-library workflow](STORY_SCENES.md).
4. Each action shows its score, sorted highest first. The **i** opens the score equation, active/inactive contributions, the increment if chosen, recorded increments, resolution phase, availability checklist and effects. Only your incarnation's factors are available. **Actions** also lists unavailable choices with the criteria that block them. **Earlier today** retains every witnessed event and historical rankings for your own choices.
5. The **+** beside Actions opens the editor. **Weights** lists your incarnation's retained manual amounts and conditional weights. The undo arrow restores the last turn, crossing reaction, or edit. The download arrow opens **State records**. The play/pause icon follows top choices without changing weights; it pauses at crossings and noon. Opening a modal suspends playback while it is open.
6. Finish all sixteen turns. **Wake as Jonah** begins the next incarnation, or **Replay Clara** revisits the same character. All composing weights persist in either case. Completing a pass through either command writes a state record before starting again. Ordinary undo cannot cross this life transition; a saved state can restore it.

New games and older saves without a mode start in adjustment mode. Legacy `mode: story` fixtures remain for regression checks of the previous incarnation/reset and moment-by-moment interface; they are not the default composing workflow. Construct a composing simulation with `new Simulation(data, new Campaign())`; the single-argument constructor retains the legacy fixture behavior.

## Author While Playing

The action editor works on the current incarnation. It does not advance time. Save immediately refreshes the playable ranking; cancel discards the draft. Invalid drafts show errors without changing the save. Existing authored actions can be edited from their inspector, even while unavailable in the catalog.

- **Action:** label, optional target, and room-turn or crossing moment. Resolution details are folded away. A target must be present. Travel uses an adjacent destination and the simultaneous resolver.
- **Availability:** location, inclusive time range, possessions, people present, items present. `Any` adds no requirement; `Have`/`Do not have` and `Present`/`Absent` add one simple clause. All selected clauses must hold. Items here include loose room objects and objects carried by occupants. In a passage, only objects carried by its travelers count. No Cartesian product of world states is stored. Existing specific-owner requirements are retained and displayed. Newly authored actions are repeatable unless their simple requirements cease to hold.
- **Score:** baseline zero, plus independent condition sets and the stored adjustment for this action, location and turn. Each condition set has points and an inclusive time range. Conditions within a set are AND; separate sets add. Existing story facts and completion slots now condition the example's score contributions instead of hiding otherwise physically possible actions. These are shown in the score inspector, not in basic availability. Generated movement, following, waiting and crossing reactions can all receive direct score conditions.
- **Effects & prose:** actor/target/observer text, activity caption, quiet-event grouping and key/value fact changes. The item-transfer controls select an item and recipient; `owner:envelope = $actor` is the equivalent fact change. Every transfer requires the item to be here, any recipient to be here, and any destination room to be the current room. These automatic gates also appear in the inspector. Custom facts can feed other actions' conditions. Travel effects cannot teleport another person or also modify unrelated facts.

Definitions are per-save overrides, not edits to the bundled asset. Definitions and composing adjustments both persist through new incarnations, revisits and restarts. A reaction created while passing defaults to that passage; it uses the same editor and evaluator as a room action. Reactions cannot begin another movement or create a recursive interruption. New scene combinations use the existing illustration fallback.

**State records** writes readable JSON containing the base scenario, overrides, current world/facts, current and previous event logs, rankings, recorded increments, inventory, phase cursor and incarnation. Its folder can be opened from the game. A record can be restored from the list or an absolute JSON file path. Restoration requires confirmation, first writes a backup of the current state, and can be undone. Restored records pin their included base scenario for reproducibility. Keep separate records to retain more than the current and immediately preceding life. Ordinary automatic saves also retain all action/rule edits but use installed base content until a pinned record is restored.

**Save all weights**, available in Weights and State records, writes a `.weights.json` profile containing every character's adjustments, all default and overridden conditional weights, action definitions and the base scenario. It does not include a partially completed day: loading it explicitly begins a fresh pass after confirmation and a backup. State records, by contrast, resume the exact moment, including a pending crossing's committed journeys and reaction opportunity. All edits and adjustments are also automatically saved during ordinary play.

## The Design Choice

Use explicit **condition-weighted action selection**, with a graph helper for travel. Do not introduce a neural network, opaque personality multipliers, or a general-purpose planner yet. The complexity comes from shared scarce objects, timed encounters, and facts left by other people. This keeps each choice inspectable and lets the mechanism, rather than a replay script, run the day.

```text
score(action) = 0
              + sum(increment of each currently satisfied condition set)
              + at most one eligible prior-choice adjustment
```

All conditions within a set must hold. Separate sets targeting the same action add together. Missing or false conditions contribute zero. Positive and negative presence conditions use fact equality and inequality, not special semantic categories. Rule identifiers are deliberately neutral.

Availability is separate: scores cannot conjure an item, make an absent person present, or revive an expired time window. A deterministic tie-break prefers waiting (or Keep going in a crossing), then action ID, without adding points.

### Travel and Interruption

A rule's `route` field expands its contribution onto the next adjacent movement choice on a shortest path. It supplies no points at the destination. For example, an envelope in the Archive and an uncopied address can activate movement toward the Archive. A separate condition set contributes to copying the address once there. Possessing the envelope activates movement toward the Chapel and a separate action there.

These are not queued scripts. Every turn re-evaluates every valid choice. A competing condition can interrupt travel; if the original conditions still hold afterward, the route continues from the new room. Missing objects invalidate dependent actions and can deactivate their associated travel contributions. Windows tolerate small delays but deliberately allow a missed encounter to matter.

### Turns and Conflicts

All four people rank options against the same start-of-turn world. Each commits one action. Conversations (`0`) and room activity (`1`) resolve before movement (`2`); ordinary waiting (`3`) resolves last. Within a nonmovement phase, priority rotates through stable actor IDs each turn. This is a contention rule, not extra preference points: scores from different people are never compared to decide who is faster. Preconditions are rechecked; a lost item claim is recorded as blocked.

Movement is a batch. Opposite travelers on the same edge get one **crossing half-turn** at its midpoint, e.g. 08:07:30 within the 08:00-08:15 turn. The travelers choose one reaction each from the same midpoint snapshot; rotating actor priority resolves conflicts. Keep going is the zero-score default. Other characters get no extra action. All committed journeys then finish at 08:15. The half-turn splits the existing interval; it does not add another fifteen minutes to the day. Reactions do not cancel or redirect the already committed journey in this prototype.

Only travelers on the same edge see the reaction. A third traveler joins that one opportunity rather than creating a chain of pairwise extra turns. NPC-only crossings resolve without interrupting an absent player. Simultaneous room arrivals see each other after movement; different doorway crossings do not manufacture encounters. New facts affect subsequent rankings, including reactions after prior room activity. Generated following on the next full turn still uses the last witnessed direction, not hidden current whereabouts. See [movement cases and artwork design](STORY_SCENES.md).

### Household Activity

`RoomLife.cs` adds finite, interruptible room-task chains using ordinary choices, condition sets, and completion facts. Each contributes one point only while its location, time and prerequisite conditions hold. The existing stronger plot conditions take precedence; an interrupted task remains available when its conditions hold again. There is no nonzero baseline, random wandering, or automatic score accumulation. The default morning contains five waits among 64 chosen actions; the remaining quiet time becomes preparations, examinations and other visible household tasks. Quiet NPC activity is shown together under **Also witnessed**, rather than requiring a separate click for every minor task, and remains individually recorded in the journal.

### Prior Choices

In adjustment mode:

```text
if chosen == first_ranked_option:
    increment = 0
else:
    increment = highest_valid_score - chosen_score + 1
stored_amount[action, actor, location, turn] += increment
```

Scores already include previous adjustments, even for the person you control. Accepting first place does not change anything. Selecting a different tied option adds one; selecting a lower option adds exactly the gap plus one. The other options keep their existing weights. Retuning an option changes its existing entry rather than appending another record. Passive play never writes increments.

The deliberately bounded key is **actor + action + actual location + turn**. It does not include every inventory, person-position or social-state combination, and does not depend on hidden consumption windows. Passages have canonical edge IDs and reaction action IDs, so they form separate midpoint opportunities. Only entries you actually adjust are stored. A different turn or room does not inherit that entry; ordinary conditional rules provide behavior there. Changed condition contributions can outweigh an adjustment, and unavailable actions are excluded regardless of weight. Replaying a character never clears composing weights.

Older saves import retained prior-choice amounts once, at the original selected turn. Old wider eligibility windows are not expanded into multiple composing entries. The legacy guidance data remains intact for compatibility. Events keep the score at decision time and the newly added increment as separate fields.

## Data and Source

`Assets/Resources/Household.asset` is the editable **ScriptableObject** containing all rooms, people, items, choices, and condition sets. Unity's Inspector can edit its lists and values. `HouseholdContent.cs` supplies the original fixture. The build adds missing `RoomLife` entries by stable ID without overwriting existing Inspector edits. To disable an included task permanently, edit its conditions/window rather than deleting its entry.

| File | Responsibility |
| --- | --- |
| `Assets/Core/Model.cs` | Serializable entities, facts, decisions, events, campaign |
| `Assets/Core/Simulation.cs` | Validation, scoring, pathfinding, resolution, adjustments, forecast |
| `Assets/Core/Authoring.cs` | Definition overrides, authoring validation, availability checklist, item presence |
| `Assets/Core/Composition.cs` | Persistent per-option adjustments, migration and composing scoring |
| `Assets/Core/Crossings.cs` | Saved midpoint state, participants, reaction choices and simultaneous arrival |
| `Assets/Core/HouseholdContent.cs` | Authored example definitions |
| `Assets/Core/RoomLife.cs` | Low-weight, finite household tasks and additive content migration |
| `Assets/Core/SceneLibrary.cs` | Observable scene signatures and exact artwork selection |
| `Assets/Core/Story.cs` | Event-time scenes, observed arrival/departure prose, activity captions |
| `Assets/Runtime/Household.cs` | ScriptableObject wrapper |
| `Assets/Runtime/Workbench.cs` | Incarnation-focused UI, persistence, interaction tests |
| `Assets/Runtime/ActionEditor.cs` | Action catalog, structured editor, portable state records |
| `Assets/Runtime/CompositionView.cs` | Direct turn flow, simple availability, passive playback, weight profiles |
| `Assets/Runtime/CompositionSmoke.cs` | Native composing, weight export, replay and crossing interaction checks |
| `Assets/Runtime/AuthoringSmoke.cs` | Native create/edit/play/record/restore interaction checks |
| `Assets/Runtime/SceneArt.cs` | Room paintings and independently composited character cutouts |
| `Assets/Resources/Workbench.uss` | Interface styling |
| `Assets/Editor/PrototypeBuild.cs` | Scene setup, regression checks, Windows build |
| `Assets/Editor/MovementChecks.cs` | Crossing, arrivals, tie priority, follow and scene-selection regressions |
| `Assets/Editor/AuthoringChecks.cs` | New content, hard gates, strict increments, override and record regressions |
| `Assets/Editor/CompositionChecks.cs` | No-inflation persistence, sparse weights, crossing save/restore and replay |
| `Assets/Editor/SceneCoverage.cs` | Bounded scene discovery and reusable image-generation queue |

The core has no Unity dependency. Only the asset wrapper, UI, serialization adapter, and build tools use Unity APIs.

### Adding Content

- **Room:** stable `id`, readable `name`, `description`, normalized map `x/y`, adjacent `exits`. Define both directions for bidirectional exits.
- **Person:** stable `id`, `name`, `role`, `color`, starting `location`, viewpoint `concern`. Add their authored choices and condition sets. The list order determines the incarnation sequence.
- **Item:** stable `id`, `name`, starting `owner`. Owners can be a person, room, or authored container. `owner:<id>` has exactly one current value.
- **Fact:** arbitrary key/value, for example `at:jonah = hall`, `owner:envelope = archive`, `trust = yes`. Use actor-qualified social keys when extending to more than this slice's single Clara/Jonah relationship.
- **Choice:** unique `id`, `actor`, optional `target`, required `location`, inclusive `from/until` turn window, `requires`, `effects`, phase, and actor/target/observer prose. `reaction` marks a midpoint choice; its location is a canonical edge ID. `activity` captions the art; `quiet` groups minor NPC activity without hiding its event. Travel specifies `destination` and a matching location effect. An optional `once`/`slot` suppresses further condition points after use in composing mode; only legacy story mode treats it as a hard availability gate. `$actor`, `$target`, `$here` and `$previousTurn` substitute in conditions.
- **Condition set:** unique `id`, `actor`, either concrete `action` or destination `route`, inclusive time window, `amount`, and conditions. Set `not` for inequality/absence. Movement IDs are `move:<room>` and waiting is `wait`, so these can be targeted directly too. There is no hidden action baseline.

Fact records track which event most recently set them. Decision records retain the exact ranked alternatives and contributing event IDs for development tools, but the player UI never exposes another person's scoring. Events record witnesses at resolution and copies of visible positions and item ownership immediately before and after each action. An arrival is visible at the destination, a departure at the origin; someone arriving later does not retroactively witness an earlier conversation. `Story` derives prose and the illustrated cast from these immutable snapshots, never from a forecast. Older saves reconstruct these presentation snapshots and witness lists from the ordered event log without resetting their world or choices.

## Verification and Persistence

`Discontinuity > Verify simulation` tests composing persistence, first-ranked acceptance without inflation, minimal changes to alternate choices, revisiting your own adjusted character, full passive passes, sparse time keys, legacy save migration, midpoint save/restore and forecasts, offscreen reactions, multi-person crossings, custom reactions and their later NPC replay. Earlier story-mode regression checks remain separate, including that mode's prior reset behavior. The report is `../artifacts/simulation-verification.txt`.

The native player's `-smoke` flag drives actual UI Toolkit submit events through action-inspection popups, manual actions, the witnessed-moment sequence, save/resume, undo, a complete life, the locked incarnation transition, and experiencing an earlier kindness as Jonah. It also tests a crossing and next-turn following. Authoring checks create a new action with presence and inventory conditions, edit its score, cancel and undo edits, execute it, inspect why it becomes unavailable, export/restore its record, and reject a missing record file. All editor tabs and the scored action list have framebuffer captures. Eight visual fixtures check all five backgrounds, both custom tableaux, one through four figures, and choice-button visibility, capturing the framebuffer under `../artifacts/adventure-*.png`. Run from the repository root:

```powershell
./builds/Discontinuity/Discontinuity.exe -smoke -capture artifacts/ui-smoke.png -captureQuit
```

Capture flags use the actual rendered player framebuffer and need a visible graphics window. `-demo kindness`, `-demo humiliation`, `-demo warning`, or `-demo baseline` starts a deterministic fixture. Art fixtures include `-demo kitchen`, `archive`, `gathering`, `garden`, `chapel`, `exchange`, and `crossing`. Demo, smoke, and capture modes do not overwrite the player's save. Verification also exports `../artifacts/scene-catalog.json` with its exact sampling protocol, discovered signatures, coverage and generation prompts.

Ordinary play automatically writes `household-v1.json` under Unity's `Application.persistentDataPath`, normally `%USERPROFILE%/AppData/LocalLow/Discontinuity/Discontinuity/` on Windows. The current world, event history, other-viewpoint adjustments, action/rule overrides, previous pass, and exact unread-moment cursor persist. State records are JSON files in its `records` subfolder. Smoke-mode records go to `../artifacts/records-<width>x<height>` instead. This replaces browser `localStorage` for the native prototype.

## Deliberate Limits

This is a small playable prototype, not a finished adventure. Presentation and journals are viewpoint-limited, but most conditions still read authored world facts rather than a full per-person belief model. Following is explicitly limited to a witnessed direction. Social states are simple facts. Crowds and the groundskeeper are prose-only in the fallback artwork. Two interactions have complete custom still illustrations; most combinations still use standing cutouts, and none are animated. The scene catalog is bounded sampling, not proof that every reachable combination has been enumerated or illustrated. The engine retains forecasting for verification, not as a player control.

The next useful extension would be **person-scoped knowledge with event provenance**, so a secret changes a choice only after that person hears or observes it. A small bounded planner could follow later if room-by-room condition rules become burdensome. Learned behavior is not needed to prove this mechanic.

## Distribution

The verified target is a local Windows player. Distribute the entire `builds/Discontinuity` folder, not just its executable. This iteration does not replace the existing S3 website. A Unity Web build remains a separate deployment task because its generated files and hosting headers differ from the preserved plain-JavaScript site.
