# Monster pursuit and aggression

`MonsterDefinition.Aggression` configures each monster. Existing definitions and unassigned controllers use the same defaults. The run state lives in `GameManager.Aggression`; it is independent of score. No difficulty menu is introduced.

## Defaults

Aggression begins at 20, reaches 80 after 900 Playing seconds, and enters a latched maximum hunt after 1200 Playing seconds. `TimeIncrease` is an additive curve in seconds (default 0 to +80 over 1200 seconds). `EndgameSeconds` independently controls the deadline. Ordinary modifiers cannot produce a maximum hunt: values clamp to 0–99 until a deadline or explicit override activates it. Pausing and Game Over freeze elapsed time and event expiry. Restart clears run state.

Tracking grace scales from 3 to 8 seconds across aggression 0–99, and search duration from 8 to 20 seconds. Search time starts on arrival at the remembered location; unreachable travel expires after max(10, search duration × 5) seconds. An unseen player beyond 50 units starts a search after 2 seconds. During tracking grace the monster knows the current position; after grace expires it remembers only the last tracked position. Reacquisition resets both concealment clocks.

Sight tests head, torso and two lateral body points based on CharacterController bounds. Every point must be obstructed before concealment time accumulates. Range and initial field of view still apply. A lamp or railing obstructing just one ray is insufficient. Full-body cover held continuously can allow escape. Darkness itself and dedicated hiding interactions are not part of this change. Attacks require clear head or torso sight, reach and floor checks, rechecked at impact.

Every currently failed ritual adds +5. Restoring one removes its contribution and applies a shared −10 effect for 30 Playing seconds. Further restorations refresh that effect; duplicate completion calls do not. Destroying a ritual removes its failure contribution without granting restoration calm. Generated rituals are bound at run startup; dynamically created rituals should call `BindAggressionOwner(gameManager)`.

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
