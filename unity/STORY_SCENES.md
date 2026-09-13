# Movement and Illustrated Story Scenes

## One Commitment per Turn

Everyone chooses against the same start-of-turn state. The player chooses manually; NPCs use the same valid options and conditional scores. Resolve conversation, then room activity, then a simultaneous movement batch, then ordinary waiting. A newly learned fact changes the next turn's ranking, not a second, free action in the current turn.

For contested actions in the same phase, rotate priority through the stable character IDs by turn number. Recheck preconditions immediately before an action. Two claims on the same envelope produce one owner and one blocked attempt. The player's status and another character's preference score do not decide initiative.

| Situation | What happens | Next decision |
| --- | --- | --- |
| Clara leaves the Hall for the Archive; Jonah comes the other way | They see each other on the shared edge, then each completes the move | Follow the last observed direction, continue the original purpose, or choose another valid action |
| Clara and Jonah enter the Hall through different doors | Both arrive in the same batch and see each other | Conversation becomes available on the next turn |
| Clara leaves the Hall for the Kitchen; Jonah enters from the Garden | Different edges, no encounter; arrival does not reveal the earlier departure | Only each person's actual observations are available |
| Jonah asks someone a question before they leave | The conversation occurs before the departure; it does not automatically cancel the departure | Its effects can make a different next action more attractive |
| A third traveler shares a crossing edge | The crossing records everyone on that edge as present and witnessing it | A two-person illustration cannot match this three-person encounter |

`crossed:<viewer>:<other>` records the turn. `seen:<viewer>:<other>` records the destination observed in the crossing. Generated `follow:<other>` actions exist only on the immediately following turn, and only when that observed destination is adjacent. They do not consult the other's hidden current position. A person may already have moved again when you reach that room.

This intentionally leaves real-time speed, mid-edge stopping, and instant counter-reaction chains out of the prototype. If stopping someone becomes important, the next extension should be one explicitly bounded reaction phase with its own valid choices, not recursive interruptions or incidental loop ordering.

## Household Tasks

`RoomLife` supplies ordinary authored actions linked by completion facts. Location, time, prerequisite completion and noncompletion conditions contribute one point. Stronger plot choices interrupt them naturally. Completed work cannot be repeated endlessly, and the chains do not grant bonuses outside their conditions. They are inspectable choices, not an ambient animation loop masquerading as decisions.

Minor NPC actions are grouped visibly under **Also witnessed**. They still have individual factual events and journal entries. The player's own action is never relegated to that group. The default morning has five ordinary waits among 64 selected actions; this is a regression metric for the default scenario, not a promise that every perturbed run will stay equally busy.

## Manage Visible Combinations

Do not generate an image for every whole-world state. Most internal states look identical. The artwork library instead keys the observable scene using:

- Room or crossing setting.
- Resolved action, actor and target.
- Exact people visible in that scene, sorted by stable ID.
- Loose items and item transfers visible during the action.
- Direction of each traveler for crossings.
- Success or blocked outcome.

Hidden scores, authored adjustments, unrelated offscreen facts and the prose viewpoint do not create another image. The same cloth-exchange image can therefore accompany Clara's prose or Jonah's prose. A new bystander, different visible item, reverse crossing direction or blocked exchange cannot reuse it accidentally.

`Assets/Resources/SceneLibrary.json` maps exact signatures to resource paths and an explicit left-to-right `castOrder`. `SceneArt` uses a complete illustration only when the signature matches, the texture exists, and the composition lists every depicted character exactly once. Labels follow the painting's composition, not the simulation's entity order. Otherwise it composites the empty room with the actual cast and activity captions. There is no network call, image-generation delay, API key or paid generation inside the player.

## Generation Workflow

1. Run `./prototype.ps1 Verify` or Build from the repository root. `SceneCoverage` samples every one-choice deviation from each incarnation's unmodified day and its continuation, plus a multi-life crossing fixture.
2. Inspect `artifacts/scene-catalog.json`. It records the sampling protocol, run count, unique signatures, illustrated count, frequency in the sample and a prompt for each signature. Current sampling finds 417 signatures across 342 runs; two have complete illustrations. This is not exhaustive reachability, and it does not imply the other 415 images have been generated.
3. Pick an unillustrated request. Its short ID is a hash of the signature, independent of frequency or list order. Use the existing room painting and `Characters.png` as identity references with the built-in image-generation tool. Prefer frequently encountered or narratively important states first.
4. Review the generated image against the exact cast, action, props, direction and outcome. Prompts are a starting point, not automatic approval. Background participants such as the groundskeeper and newly introduced movable props need explicit art direction; they are not yet a complete visual ontology.
5. Place the reviewed PNG under `Assets/Resources/Tableaux/`, add its exact signature, extensionless resource path and left-to-right `castOrder` to `SceneLibrary.json`, and retain prompt/provenance notes in `ART.md`.
6. Rebuild and capture the corresponding fixture in the actual Unity player. Test an incompatible variant too, such as adding a third person, to ensure the renderer falls back rather than showing incorrect artwork.

The renderer does not synthesize a new plot. Simulation determines the event; the library depicts it. If later prose acquires visibly different emotion or prop variants, add an explicit visual cue to the signature instead of including every social fact indiscriminately.

## Current Custom Scenes

- `Tableaux/cloth-exchange`: Clara places a clean cloth into Jonah's hand, in the Hall with only those two people present.
- `Tableaux/archive-crossing`: Clara walks into the Archive while Jonah comes out into the Hall; both notice one another in passing.

These are complete still tableaux with action-specific poses, not animations. The other five room backgrounds and independent cast remain the fallback. The scene catalog and exact-match selector establish the production path toward broader coverage without claiming that the entire combinatorial image library already exists.
