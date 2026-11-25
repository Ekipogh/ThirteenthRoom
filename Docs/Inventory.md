# Inventory and usable items

The player prefab includes `PlayerItemController` and `InventoryUI`. The UI builds a Canvas/TMP inventory and a selected-item HUD at runtime, using the existing Input System and EventSystem.

## Controls

| Control | Action |
| --- | --- |
| I / Tab | Toggle inventory |
| Escape / Close | Close inventory |
| Drag item to active slot | Assign shortcut; keep the inventory stack |
| Right-click active slot | Clear shortcut |
| 1-9 | Select active slot |
| Mouse wheel | Cycle slots during gameplay; scroll the grid while inventory is open |
| Left click | Use the selected potion or fire the gun |
| R | Load the selected gun from reserve bullets |
| F | Existing torch toggle |

Opening the inventory stops player movement, look, interactions, and item use. It cancels held interactions and releases the cursor. The mansion continues running. Death closes inventory and leaves the cursor available for the game-over UI.

Ordinary items stack by definition. Target-specific keys remain separate. Internal target IDs are hidden. Inventory cells and active slots show an optional detail line: safe keys retain their matching safe number (for example, `Safe #2`). Other items can supply `ItemDefinition.InventoryDetail`; instance-specific details override it. Blank details reserve no extra line, and long details use single-line ellipsis. Active slots reference stacks, never move stock, and clear when that stack is exhausted. Reassigning the same stack moves its shortcut.

## Item tuning

Use settings are serialized on `ItemDefinition`; loaded ammunition belongs to the player's inventory, not the shared asset.

- Gun: six-round capacity, starts empty, instant reload, 0.5-second shot cooldown, 25-metre camera-centered hitscan, three-second stun.
- Gunshot agitation: `Assets/UI/Inventory/GunshotAgitation.asset`, initially radius 30, strength 1, investigation five seconds. Existing aggression scaling still applies.
- Bullets: `AmmoDefinition` plus `Ammo.prefab`, granting six bullets per pickup. Two explicit ammo entries are included in `GameScene`'s initial item pool. They use the same Valuable spawn-point category as the existing gun. Any-category item points also accept them.
- Potion: restores 50 stamina, capped at 100. Full stamina preserves the potion. Laboratory brewing continues to produce the same potion definition.

Shots consume loaded rounds on hits and misses. Walls stop the shot. The monster's `ShotHitbox` is a trigger, so it can receive shots without creating new physical blocking geometry. Shots explicitly include monster triggers and skip unrelated triggers; see Unity's [QueryTriggerInteraction reference](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/querytriggerinteraction).

Stun cancels pending attack damage, stops navigation and door interaction, and suspends animation. Further hits refresh the duration. Recovery uses normal perception or the remembered search position; maximum hunt resumes under its existing rules.

## Validation

- `InventoryTests` exercises stacking, targeted keys, shortcut movement/depletion, reload capacity and ownership, shot hits/misses/obstructions/noise, stun cancellation/recovery, potion consumption, inventory input blocking, held-interaction cancellation, keyboard/wheel controls, UI drag/drop, and six-bullet pickup quantities. Gun tests instantiate the authored monster prefab.
- Full Editor and Play Mode suites passed. Live scene checks collected the generated gun from an opened safe and shot the actual spawned monster. Two ammo pickups were present.
- Inventory and HUD reviewed in actual 16:9 and 16:10 Game Views. Screenshots and run evidence are under `Temp/UIReview` and `Temp/InventoryValidation`.
- No standalone player build was tested. No save system, held weapon model, lethal damage, or reload animation is added.

Restore point for the pre-change scripts, player prefab, scene and item definitions: `Temp/restore/inventory-20261125`. The original monster prefab is backed up there separately as `Monster.prefab`.

Existing safe geometry still emits negative-scale BoxCollider warnings during mansion generation; this feature does not modify that geometry.
