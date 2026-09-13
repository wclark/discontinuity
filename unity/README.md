# Discontinuity: Unity Prototype

A native, turn-based prototype of four interconnected lives in one household. Sixteen 15-minute turns lead from breakfast to the noon accusation. Play a person, alter a choice, then inhabit someone else and encounter its consequences. The original browser prototype is preserved in `../site/`.

## Open and Play

Use Unity Hub's **Add project from disk**, select this `unity` folder, and open it with **Unity 6000.6.0f1**. Open `Assets/Scenes/Household.unity` and press Play. No Asset Store packages or external services are needed.

From the repository root, `./prototype.ps1 Play` opens the native player, building it first if needed. `./prototype.ps1 Build` rebuilds; `./prototype.ps1 Verify` runs the simulation checks. Close this project in the editor before a command-line build. The executable is `builds/Discontinuity/Discontinuity.exe`; it needs its neighboring data folders.

Run `./install-desktop.ps1` from the repository root after building to create the **Discontinuity** desktop shortcut. Both the executable and shortcut use the custom blue-envelope/broken-clock icon; see [icon artwork and provenance](ICON.md).

## Play a Life

1. Begin as Clara in the Kitchen. Go to the Hall to meet Jonah, then choose how to treat him.
2. Choose one action for the next fifteen minutes. All four people decide from the same start-of-turn world, then the witnessed events unfold one moment at a time. **Next moment** reads the next event; **Continue to [time]** finishes the recap. Reading does not advance the simulation or record another adjustment. You cannot choose again until the turn has finished unfolding.
3. The illustration and room information are beside the current situation and action buttons. Complete custom illustrations depict the cloth exchange and the Clara/Jonah Archive crossing; other combinations use room paintings and independent character cutouts. Scene selection checks the actual cast, action, exposed items and movement directions. A new bystander or failed action falls back safely instead of displaying an incompatible picture. See [artwork and generation prompts](ART.md) and the [scene-library workflow](STORY_SCENES.md).
4. Each action shows its score, sorted highest first. The **i** opens the score equation, active/inactive contributions, the increment if chosen, recorded increments, resolution phase, availability checklist and effects. Only your incarnation's factors are available. **Actions** also lists unavailable choices with the criteria that block them. **Earlier today** retains every witnessed event and historical rankings for your own choices.
5. The **+** beside Actions opens the action editor. The undo arrow restores the last turn or authoring change within this life. The header's download arrow opens **State records**. There is no Observe mode, free character selector, autoplay or omniscient timeline.
6. Finish all sixteen turns and their witnessed moments. **Wake as Jonah** then begins the next incarnation. The order is Clara, Jonah, Father Vale, Dr. Merrow, then Clara again. The next person's previous adjustments are cleared; everyone else's remain. Undo cannot cross this transition.

Development fixtures remain available through command-line flags only: a kindness can supply missing corroboration, humiliation can redirect the accusation onto Clara, and a warning can interrupt Jonah's Archive errand. They are not alternate story scripts; the same ordinary rules produce every event.

## Author While Playing

The action editor works on the current incarnation. It does not advance time. Save immediately refreshes the playable ranking; cancel discards the draft. Invalid drafts show errors without changing the save. Existing authored actions can be edited from their inspector, even while unavailable in the catalog.

- **Action:** label, location or any room, target, inclusive time range, once-per-slot or repeatable, shared decision slot, and resolution phase. A target on a room action must be co-located. Travel specifies an adjacent destination and uses the simultaneous movement resolver.
- **Availability:** all listed conditions must be true. Person-location conditions accept a fixed room or `Here`, resolved relative to the actor. Item-owner conditions distinguish carried objects from objects in a room. Item-here conditions include room objects and objects carried by occupants. `Is not`/`Absent` invert a condition. Arbitrary fact equality supports social and story state.
- **Score:** baseline zero, plus independent condition sets. Each set has points, a time range and zero or more required conditions. All conditions within a set are AND; separate sets add. An empty set applies during its time range. Points never bypass availability. Existing direct condition sets can be changed or removed. Generated movement, following and waiting can also receive direct conditions; their automatic definitions and route contributions are shown, not replaced.
- **Effects & prose:** actor/target/observer text, activity caption, quiet-event grouping and key/value fact changes. The item-transfer controls select an item and recipient; `owner:envelope = $actor` is the equivalent fact change. Every transfer requires the item to be here, any recipient to be here, and any destination room to be the current room. These automatic gates also appear in the inspector. Custom facts can feed other actions' conditions. Travel effects cannot teleport another person or also modify unrelated facts.

Definitions are per-save overrides, not edits to the bundled asset. They persist through new incarnations and application restarts. Replaying a character clears their prior manual increments, not their action definitions. New scene combinations continue to use the existing safe illustration fallback.

**State records** writes readable JSON containing the base scenario, overrides, current world/facts, current and previous event logs, rankings, recorded increments, inventory, phase cursor and incarnation. Its folder can be opened from the game. A record can be restored from the list or an absolute JSON file path. Restoration requires confirmation, first writes a backup of the current state, and can be undone. Restored records pin their included base scenario for reproducibility. Keep separate records to retain more than the current and immediately preceding life. Ordinary automatic saves also retain all action/rule edits but use installed base content until a pinned record is restored.

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

All four people rank options against the same start-of-turn world. Each commits one action. Conversations (`0`) and room activity (`1`) resolve before movement (`2`); ordinary waiting (`3`) resolves last. Within a nonmovement phase, priority rotates through stable actor IDs each turn. This is a contention rule, not extra preference points: scores from different people are never compared to decide who is faster. Preconditions are rechecked; a lost item claim is recorded as blocked.

Movement is a batch with one shared before/after snapshot. Opposite travelers on the same edge witness a **crossing**, then both reach their destinations. Simultaneous arrivals see each other symmetrically. A person who left through a different doorway does not witness a later arrival, and arriving does not reveal earlier room activity. Crossing facts include each traveler's observed destination. On the next turn, `follow:<person>` offers movement toward that last-seen destination, not omniscient tracking of the person's current position. The opportunity expires after one turn. Condition sets can give this action points just like any other action.

Reactions to a new encounter, request or warning affect the **next** turn's ranking. There are no free reaction chains or second actions in the same turn. A future interrupt system could add an explicit, bounded reaction phase, but this prototype deliberately uses the simpler rule. See [movement cases and artwork design](STORY_SCENES.md).

### Household Activity

`RoomLife.cs` adds finite, interruptible room-task chains using ordinary choices, condition sets, and completion facts. Each contributes one point only while its location, time and prerequisite conditions hold. The existing stronger plot conditions take precedence; an interrupted task remains available when its conditions hold again. There is no nonzero baseline, random wandering, or automatic score accumulation. The default morning contains five waits among 64 chosen actions; the remaining quiet time becomes preparations, examinations and other visible household tasks. Quiet NPC activity is shown together under **Also witnessed**, rather than requiring a separate click for every minor task, and remains individually recorded in the journal.

### Prior Choices

Whenever the human selects an available action:

```text
increment = max(0, highest_valid_score - chosen_score) + 1
```

Already-leading and tied choices receive +1; a lower choice receives enough to exceed the current maximum by exactly one. The event records this new increment separately from any already-applied score. Values never increase merely because another turn passed. Records are scoped to the actor, named decision context, location, and a bounded window from the original turn through two turns later (clamped to the action's window). A later manual decision in the same context closes the earlier window. Only the latest eligible record is considered and each is consumed at most once per pass.

Current human choices do not receive their newly recorded adjustments. Replaying that person clears all their previous adjustments; other people's records remain. Changed condition contributions can outweigh a record, and invalid actions never receive it. This is intentionally a soft preference, not guaranteed replay.

## Data and Source

`Assets/Resources/Household.asset` is the editable **ScriptableObject** containing all rooms, people, items, choices, and condition sets. Unity's Inspector can edit its lists and values. `HouseholdContent.cs` supplies the original fixture. The build adds missing `RoomLife` entries by stable ID without overwriting existing Inspector edits. To disable an included task permanently, edit its conditions/window rather than deleting its entry.

| File | Responsibility |
| --- | --- |
| `Assets/Core/Model.cs` | Serializable entities, facts, decisions, events, campaign |
| `Assets/Core/Simulation.cs` | Validation, scoring, pathfinding, resolution, adjustments, forecast |
| `Assets/Core/Authoring.cs` | Definition overrides, authoring validation, availability checklist, item presence |
| `Assets/Core/HouseholdContent.cs` | Authored example definitions |
| `Assets/Core/RoomLife.cs` | Low-weight, finite household tasks and additive content migration |
| `Assets/Core/SceneLibrary.cs` | Observable scene signatures and exact artwork selection |
| `Assets/Core/Story.cs` | Event-time scenes, observed arrival/departure prose, activity captions |
| `Assets/Runtime/Household.cs` | ScriptableObject wrapper |
| `Assets/Runtime/Workbench.cs` | Incarnation-focused UI, persistence, interaction tests |
| `Assets/Runtime/ActionEditor.cs` | Action catalog, structured editor, portable state records |
| `Assets/Runtime/AuthoringSmoke.cs` | Native create/edit/play/record/restore interaction checks |
| `Assets/Runtime/SceneArt.cs` | Room paintings and independently composited character cutouts |
| `Assets/Resources/Workbench.uss` | Interface styling |
| `Assets/Editor/PrototypeBuild.cs` | Scene setup, regression checks, Windows build |
| `Assets/Editor/MovementChecks.cs` | Crossing, arrivals, tie priority, follow and scene-selection regressions |
| `Assets/Editor/AuthoringChecks.cs` | New content, hard gates, strict increments, override and record regressions |
| `Assets/Editor/SceneCoverage.cs` | Bounded scene discovery and reusable image-generation queue |

The core has no Unity dependency. Only the asset wrapper, UI, serialization adapter, and build tools use Unity APIs.

### Adding Content

- **Room:** stable `id`, readable `name`, `description`, normalized map `x/y`, adjacent `exits`. Define both directions for bidirectional exits.
- **Person:** stable `id`, `name`, `role`, `color`, starting `location`, viewpoint `concern`. Add their authored choices and condition sets. The list order determines the incarnation sequence.
- **Item:** stable `id`, `name`, starting `owner`. Owners can be a person, room, or authored container. `owner:<id>` has exactly one current value.
- **Fact:** arbitrary key/value, for example `at:jonah = hall`, `owner:envelope = archive`, `trust = yes`. Use actor-qualified social keys when extending to more than this slice's single Clara/Jonah relationship.
- **Choice:** unique `id`, `actor`, optional `target`, required `location`, inclusive `from/until` turn window, named `slot`, `requires`, `effects`, phase, and actor/target/observer prose. `activity` captions the art; `quiet` groups minor NPC activity in the recap without hiding its event. Travel choices specify `destination` and a matching location effect. A `once` slot is resolved by any of its alternatives. `$actor`, `$target` and `$previousTurn` substitute in conditions.
- **Condition set:** unique `id`, `actor`, either concrete `action` or destination `route`, inclusive time window, `amount`, and conditions. Set `not` for inequality/absence. Movement IDs are `move:<room>` and waiting is `wait`, so these can be targeted directly too. There is no hidden action baseline.

Fact records track which event most recently set them. Decision records retain the exact ranked alternatives and contributing event IDs for development tools, but the player UI never exposes another person's scoring. Events record witnesses at resolution and copies of visible positions and item ownership immediately before and after each action. An arrival is visible at the destination, a departure at the origin; someone arriving later does not retroactively witness an earlier conversation. `Story` derives prose and the illustrated cast from these immutable snapshots, never from a forecast. Older saves reconstruct these presentation snapshots and witness lists from the ordered event log without resetting their world or choices.

## Verification and Persistence

`Discontinuity > Verify simulation` tests defaults, alternate outcomes, strict winning increments including ties, nonaccumulation, replay clearing, invalidation, delayed encounters, stronger conditions, JSON round trips, forecasts, simultaneous item claims, historical rankings, authored definitions and portable records. The report is `../artifacts/simulation-verification.txt`.

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
