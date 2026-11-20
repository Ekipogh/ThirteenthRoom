# Monster navigation

The mansion builds a runtime NavMesh after its rooms, openings, furniture, and stairs are finalized. Monster spawning waits for that build and samples a floor position in a tagged room. No pre-baked scene data or off-mesh teleport links are required.

The default agent type uses radius **0.9** and height **5.5**, matching the project's character scale and clearing the **5.9** unit door openings. Generated geometry is collected from physics colliders beneath the mansion's floor parent. Polygon furniture props are Not Walkable; rugs remain walkable. Moving door leaves are excluded from the static bake and carve while closed or opening. The editor importer keeps the two rug meshes used as mesh colliders readable for player runtime baking, including when third-party asset metadata is freshly imported.

## Behavior and tuning

`MonsterController` owns Roaming, Chasing, Searching, and Attacking. Inspector fields control roaming delay and speed, doorway pause/emote, view range and angle, eye height, visibility layers, chase speed, destination refresh, search duration, and attack reach/impact timing.

- Defaults: roam 1.5 units/s, chase 3 units/s, view 25 units at 160 degrees, all-direction close awareness within 8 units, eye height 4.8 units, refresh every 0.2 seconds, search for 6 seconds after arrival, attack reach 1.5 units beyond the player's collider radius.
- Roaming uses the room graph to select neighboring rooms and samples floor destinations. Complete NavMesh paths supply all movement. Unreachable destinations are discarded; stalled movement replans without repositioning the monster.
- Detection requires an unobstructed view. Within 8 units the monster notices the player from any direction; farther away it uses its 160-degree view cone. During a chase it retains sight regardless of facing, so turning along a route does not immediately lose the player. Walls and closed doors block every awareness check. Losing sight sends the monster to the last seen position; it searches nearby before returning to roaming. Unreachable search travel times out after 30 seconds at the default search duration.
- Ordinary interior doors open away from the monster through `DoorInteractable.OpenFrom`. It waits for the door and configured pause before continuing. The operation is idempotent while opening. Player door interaction retains its normal toggle behavior.
- Attacks stop locomotion and apply the existing lethal outcome once, at the configured impact delay, only if reach, vertical separation, and sight still permit it. Leaving reach before impact avoids death. Pause/game over stop navigation and encounter audio; game over clears the attack animation flag.
- Room direction conversion follows the mansion's mirrored X axis: increasing maze column moves toward negative world X. This matters for rotated room and landing openings.

## Validation

Run `NavigationBehaviorTests.MonsterPhysicsAndNavigationRegressionChecks` in Unity Test Runner's PlayMode tab. It executes 23 checks using real NavMesh and physics queries: visibility, field of view, occlusion, chase/search transitions, failed paths, attack reach, different storeys, wall protection, missed attacks, one consumed impact, pause/game-over stopping, and idempotent door opening.

For actual mansion geometry, enter the GameScene in Play Mode and evaluate `MansionNavigationChecks.GenerateSeed(101)` through the Unity CLI. Wait for doors to finish opening, then evaluate `MansionNavigationChecks.AuditGeneratedMansion()`. Repeat with seed 202 and `GenerateSeed(303, 3)` for three floors. These validation helpers replace generated runtime objects; exit Play Mode afterwards.

Validated on 2026-11-20:

- Unity compilation: no errors or warnings.
- Unity Test Runner: one test passed, containing all 16 focused checks.
- Seeds 101 and 202: 100/100 directed room connections each; seed 303 with three floors: 150/150. All paths were complete, including entrance and staircase connections.
- Live door pursuit: searched, opened a closed door, reacquired the player, and crossed into the adjoining room while chasing at 3 units/s. Largest displacement over a 0.2-second observation was 0.62 units. A camera capture confirmed the monster in the doorway with locomotion active and Attack false.
- Live stair traversal: walked from floor 0 through floor 1 to floor 2 continuously, reaching height 27.51. Largest observed vertical displacement over a 0.2-second sample was 0.56 units.

These are Editor Play Mode checks; a standalone player build has not been run.

Awareness follow-up: increased sight and peripheral vision, added close rear detection and chase target retention, and set the monster prefab eye height to 4.8 units to match the existing character scale. Regression coverage includes rear approaches, wide peripheral entry, long-range sight, and obstruction of close awareness.

Encounter gasp: the existing short player clip now uses a configurable 3x gain and priority 32. Sprint/recovery breathing pauses immediately for the cue. Chase, search, and attack remain one encounter, so those transitions do not replay it. Navigation and gasp Play Mode regression tests both passed; actual scene clip playback was confirmed when the monster began chasing.

