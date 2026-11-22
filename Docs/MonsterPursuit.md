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

Noise is separate from run aggression. Sprinting uses strength 1, radius 75 (two room widths) and 5 seconds; brewing uses strength 2, radius 112.5 (three room widths) and 12 seconds. A room is 37.5 units wide; hearing is radial rather than a count of connected rooms. Edit the reusable profiles under `Assets/SOInstances/Agitation`. Walking uses its own `Walking` profile and emitter on the player, including movement behind the Monster outside its field of view. Walking, sprinting and brewing pulse immediately and every Playing second while active. Walking requires actual grounded movement with movement input, and stops while sprinting, stationary, airborne, blocked or disabled. Exhausted players moving at walking speed emit walking agitation. Sprint pulses require actual grounded movement and stamina; brewing stops on cancellation, completion, missing ingredients or disable. Audio playback does not drive these signals.

Hearing uses inclusive three-dimensional straight-line radius, without wall/floor attenuation. The Monster investigates the emitted position snapshot at travel speed, through existing doors/stairs, then searches within 3 units on arrival. The timer starts when heard and expires during travel too; unreachable destinations cannot prolong it. Stronger or equal events redirect and replace the remaining duration; weaker events are discarded. Sight, attacks and maximum hunt take priority and discard incoming sounds. Investigation may interrupt a remembered-location search, but cannot follow a silent moving player. Investigation does not activate chase vocals or player encounter breathing. Seeing the player from investigation starts the usual alert with its cooldown.

`GameManager.ReportAgitation(origin, profile)` reports a one-shot event to that run's registered Monsters. `AgitationEmitter.Emit()` is a no-argument UnityEvent hook using its transform and assigned profile. Generated room and player emitters bind during run startup; dynamically spawned emitters must call `BindOwner(gameManager)`. For a future gun, call `Emit()` once on successful discharge. For a future piano, call `TickContinuous(true, deltaTime)` while playing and `StopContinuous()` when it stops. Give these actions their own profiles; no piano or gun gameplay is implemented here.

Pausing and Game Over freeze investigation and reject reports. Restart calls `ResetAgitation()` and clears emitter cadence before reloading; Monster disable clears investigation and unregisters it. Invalid profiles/positions are ignored. `MonsterAgitationTests` covers hearing, timers, priorities, lifecycle, action integration and prefab configuration.
