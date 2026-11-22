# Monster pursuit and aggression

`MonsterDefinition.Aggression` configures each monster. Existing definitions and unassigned controllers use the same defaults. The run state lives in `GameManager.Aggression`; it is independent of score. No difficulty menu is introduced.

## Defaults

Aggression begins at 20, reaches 80 after 900 Playing seconds, and enters a latched maximum hunt after 1200 Playing seconds. `TimeIncrease` is an additive curve in seconds (default 0 to +80 over 1200 seconds). `EndgameSeconds` independently controls the deadline. Ordinary modifiers cannot produce a maximum hunt: values clamp to 0Ã¢â‚¬â€œ99 until a deadline or explicit override activates it. Pausing and Game Over freeze elapsed time and event expiry. Restart clears run state.

Tracking grace scales from 3 to 8 seconds across aggression 0Ã¢â‚¬â€œ99, and search duration from 8 to 20 seconds. Search time starts on arrival at the remembered location; unreachable travel expires after max(10, search duration Ãƒâ€” 5) seconds. An unseen player beyond 50 units starts a search after 2 seconds. During tracking grace the monster knows the current position; after grace expires it remembers only the last tracked position. Reacquisition resets both concealment clocks.

Sight tests head, torso and two lateral body points based on CharacterController bounds. Every point must be obstructed before concealment time accumulates. Range and initial field of view still apply. A lamp or railing obstructing just one ray is insufficient. Full-body cover held continuously can allow escape. Darkness itself and dedicated hiding interactions are not part of this change. Attacks require clear head or torso sight, reach and floor checks, rechecked at impact.

Every currently failed ritual adds +5. Restoring one removes its contribution and applies a shared Ã¢Ë†â€™10 effect for 30 Playing seconds. Further restorations refresh that effect; duplicate completion calls do not. Destroying a ritual removes its failure contribution without granting restoration calm. Generated rituals are bound at run startup; dynamically created rituals should call `BindAggressionOwner(gameManager)`.

Maximum hunt starts even without a prior sighting and continuously follows the current player position through normal NavMesh paths, doors and stairs. It cannot attack through walls or floors. The timed endgame stays active until restart, even if rituals calm the monster. Ending the last temporary hunt override restores ordinary pursuit with fresh concealment timers.

## Runtime events

```csharp
gameManager.SetMonsterDifficultyOffset(10f); // default 0, clamped to -99..99
gameManager.SetAggressionModifier("alarm", 15f, 20f); // lasts 20 Playing seconds
gameManager.SetAggressionModifier("progression", 10f); // remains until removed
gameManager.RemoveAggressionModifier("progression");
gameManager.SetMaximumHunt("special-event", true);
gameManager.SetMaximumHunt("special-event", false);
```

Stable event IDs replace prior modifiers with that ID rather than stacking them. Different maximum-hunt IDs are independent; remove every active ID to end temporary hunting. Read `MonsterController.CurrentAggression` and `IsMaximumHunt`. Definition profiles are not modified at runtime by events. Direct `Aggression.Reset()` clears clock/events/ritual bookkeeping; normal restart reloads and rebinds the scene. Do not use a direct reset as a mid-run ritual reinitialization.

## Verification

`MonsterPursuitTests` covers model boundaries and events plus Play Mode collider/navigation fixtures, attack obstruction, pause and ritual lifecycle. `NavigationBehaviorTests` retains existing navigation regression coverage with the new concealment grace expectation. `MonsterVocalAudioTests` and `PlayerGaspAudioTests` cover encounter audio compatibility. Generated-mansion validation should also exercise actual door and stair routes; fixture tests alone do not prove authored geometry works.

## Noise and agitation

Noise does not change run aggression directly. Aggression supplies the baseline for the Monster's runtime agitation, and heard noise adds a temporary bonus (see Activity and patrols below). Sprinting uses strength 1, radius 75 (two room widths) and 5 seconds; brewing uses strength 2, radius 112.5 (three room widths) and 12 seconds. A room is 37.5 units wide; hearing is radial rather than a count of connected rooms. Edit the reusable profiles under `Assets/SOInstances/Agitation`. Walking uses its own `Walking` profile and emitter on the player, including movement behind the Monster outside its field of view. Walking, sprinting and brewing pulse immediately and every Playing second while active. Walking requires actual grounded movement with movement input, and stops while sprinting, stationary, airborne, blocked or disabled. Exhausted players moving at walking speed emit walking agitation. Sprint pulses require actual grounded movement and stamina; brewing stops on cancellation, completion, missing ingredients or disable. Audio playback does not drive these signals.

Hearing uses inclusive three-dimensional straight-line radius, without wall/floor attenuation. The Monster investigates the emitted position snapshot at its agitation-scaled investigation speed, through existing doors/stairs, then searches within 3 units on arrival. The timer starts when heard and expires during travel too; unreachable destinations cannot prolong it. Stronger or equal events redirect and replace the remaining duration; weaker events are discarded. Sight, attacks and maximum hunt take priority over investigation targets. Audible sounds still refresh the temporary agitation bonus during pursuit, but never redirect a chase or attack. Investigation may interrupt a remembered-location search, but cannot follow a silent moving player. Investigation does not activate chase vocals or player encounter breathing. Seeing the player from investigation starts the usual alert with its cooldown.

`GameManager.ReportAgitation(origin, profile)` reports a one-shot event to that run's registered Monsters. `AgitationEmitter.Emit()` is a no-argument UnityEvent hook using its transform and assigned profile. Generated room and player emitters bind during run startup; dynamically spawned emitters must call `BindOwner(gameManager)`. For a future gun, call `Emit()` once on successful discharge. For a future piano, call `TickContinuous(true, deltaTime)` while playing and `StopContinuous()` when it stops. Give these actions their own profiles; no piano or gun gameplay is implemented here.

Pausing and Game Over freeze investigation and reject reports. Restart calls `ResetAgitation()` and clears emitter cadence before reloading; Monster disable clears investigation and unregisters it. Invalid profiles/positions are ignored. `MonsterAgitationTests` covers hearing, timers, priorities, lifecycle, action integration and prefab configuration.

## Activity and patrols

`MonsterDefinition.Activity` holds serializable movement, noise bonus and patrol tuning.
`MonsterController.CurrentAgitation` is read-only: current aggression plus a per-Monster temporary noise bonus, clamped to 0-100. Each audible pulse raises that bonus to at least `10 * effective strength`, capped at 30; it does not accumulate with repeated pulses. The bonus decays by 2 per Playing second. Even a weaker sound that cannot replace an investigation target can refresh this bonus. Aggression reductions immediately lower the baseline. Neither noise nor maximum agitation can activate maximum hunt.

Speeds and destination pauses interpolate linearly over agitation 0-100:

| Setting | At 0 | At 100 |
| --- | ---: | ---: |
| Patrol speed | 3 | 5 |
| Investigation/search speed | 4 | 6 |
| Chase speed | 6 | 8.5 |
| Pause after patrol destination (seconds) | 2 | 0.25 |

At the default starting aggression of 20, quiet patrol speed is 3.4, investigation speed 4.4, chase speed 6.5 and pause duration 1.65 seconds. Noise temporarily raises these values. Movement updates during travel; increased agitation shortens an existing pause without restarting it. Spawn begins patrol immediately. Maximum hunt uses agitation 100. The player walks at 5 and sprints at 10, so sprinting gains distance but walking cannot outrun pursuit. Existing stamina and sustained concealment rules remain in force.

Patrol destinations come from the reachable room graph, independent of the player's position. Room and floor entry history is recorded during every active behavior. Unvisited rooms are preferred, then least recently visited rooms, with random ties. After three completed patrol destinations on one floor, the next destination favors the least recently visited reachable other floor. A chosen destination survives intermediate rooms and uses existing door and stair traversal. Arrival retains the 30% chance to switch off a room light.

Failed destinations are excluded for 15 Playing seconds, including failures to sample a room point, navigate, open a door, or make progress. Another destination is selected; if none remains, the Monster moves locally. Pause freezes noise decay and failure cooldowns. `GameManager.ResetAgitation()` also clears noise and patrol memory through `MonsterController.ResetActivity()`; disabling or rebinding a Monster clears its transient state too. Ordinary investigation expiry clears only its target, preserving the decaying noise bonus.

`MonsterActivityTests` covers noise/baseline behavior, movement and pauses, branching/cyclic/disconnected patrol graphs, floor preference, visits during pursuit, destination retention, failure cooldowns and authored assets. Existing pursuit, agitation, navigation and audio suites remain regression coverage. Generated-mansion door/stair traversal and human encounter-frequency testing complement these fixtures.

### Hybrid escalation: familiar actions become dangerous

Aggression also modifies each action's base noise profile when a Monster receives an event. Hearing radius interpolates from a 0.1x multiplier at aggression 0 to 1.6x at 100. Strength and investigation duration each interpolate from 0.125x to 2x. The default starting aggression of 20 therefore gives 0.4x radius and 0.5x strength/duration. These ranges are configurable in `MonsterDefinition.Activity`.

| Playing time | Default aggression | Sprint hearing radius | Sprint strength | Investigation seconds |
| --- | ---: | ---: | ---: | ---: |
| Start | 20 | 30 | 0.5 | 2.5 |
| 5 minutes | 40 | 52.5 | 0.875 | 4.375 |
| 10 minutes | 60 | 75 | 1.25 | 6.25 |
| 15 minutes | 80 | 97.5 | 1.625 | 8.125 |

Early sprinting causes a small, brief response unless the Monster is close, encouraging a false sense of safety. Later the same sprint is heard farther away, produces more transient agitation, and sustains a longer investigation. Brewing and walking retain their distinct base profiles and receive the same aggression multipliers. There is no immunity or guaranteed safe opening: sight and nearby sounds remain dangerous.

Scaling uses current aggression, including ritual effects, rather than current agitation. Noise cannot amplify its own hearing radius or strength through feedback. Each listener resolves the event independently without modifying shared ScriptableObjects. Effective strength and duration are snapshotted when heard; aggression changes affect subsequent pulses, not a timer already running. Stronger-sound priority compares effective heard strengths. Repeated equal/stronger sounds refresh the timer without stacking it, and all existing pause, chase priority and reset rules remain intact.

Baseline agitation still raises activity over the run, patrol coverage remains active from the start, and the 20-minute maximum hunt deadline is unchanged. The hearing/duration scaling still resolves at maximum aggression, but maximum hunt takes priority over noise targets.

## Monster animation pace and attack reach

Monster locomotion playback uses measured horizontal speed times 0.15, bounded to 0.5-1.1x. Early chase at 6.5 units/second plays at about 0.975x; maximum chase at 8.5 stays capped at 1.1x. Idle and attack playback remain at 1x, keeping the attack wind-up independent of movement speed. The Monster's model scale and navigation speeds are unchanged.

An attack can start within 3 units of the player's collider edge. Impact occurs after 0.35 seconds and permits 2 additional units of swing follow-through. A small backward step or sidestep during wind-up therefore remains in reach; escaping far enough still avoids damage. The larger impact range cannot initiate an attack. Impact still rechecks current distance, floor and unobstructed body sight, preventing hits through walls, closed doors or another floor. The existing one-second attack/recovery cycle and single-hit consumption remain in force.

`MonsterCombatTests` verifies bounded animation rates on the authored prefab, normal idle/attack playback, attack initiation versus impact reach, short-step hits, larger escapes, wall/floor obstruction and pause behavior.
