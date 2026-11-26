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
| Mouse wheel | Cycle slots during gameplay; scroll the item list while inventory is open |
| Left click | Use the selected potion or fire the gun |
| R | Load the selected gun from reserve bullets |
| F | Existing torch toggle |

Opening the inventory stops player movement, look, interactions, and item use. It cancels held interactions and releases the cursor. The mansion continues running. Death closes inventory and leaves the cursor available for the game-over UI.

Ordinary items stack by definition. Target-specific keys remain separate. Internal target IDs are hidden. Inventory rows show an optional detail line; compact active slots show the same detail when hovered: safe keys retain their matching safe number (for example, `Safe #2`). Other items can supply `ItemDefinition.InventoryDetail`; instance-specific details override it. Blank details reserve no extra line, and long details stay on a single clipped line. Active slots reference stacks, never move stock, and clear when that stack is exhausted. Reassigning the same stack moves its shortcut.

## Minimal inventory presentation

The open inventory uses a translucent **POCKETS** list at the left and nine compact active slots along the bottom right. Rows retain item icons, stack quantities, and in-world details. A thin indicator marks the row assigned to the selected slot. Empty slots show only their number. Hovering an assigned slot reveals its name and detail above the row. Missing artwork uses a neutral initial marker rather than an empty icon area.

The gameplay selected-item HUD remains at bottom left when inventory is closed. The complete slot row appears only while inventory is open. A subtle scrollbar appears when the list exceeds the viewport.

`InventoryUI` exposes **Background Blur Radius** (8px at 1080p by default) and **Background Dimming** (0.18). The camera-specific `InventoryBackgroundBlur` marker lets `InventoryBlurFeature` enqueue a RenderGraph pass only for the owning player camera while its inventory is open. Both PC and mobile renderer assets reference the blur shader directly. The effect runs after scene post-processing and before overlay UI, with a half-resolution downsample and two separable blur passes. RenderGraph owns the transient textures; the renderer feature disposes its material. Closing, death, disabling UI, and scene destruction all stop the effect without pausing gameplay.

Redesign captures: `Temp/UIReview/MinimalInventory`. Test evidence: `Temp/InventoryValidation/Minimal`. Restore point: `Temp/restore/inventory-minimal-20261126`.

## Item tuning

Use settings are serialized on `ItemDefinition`; loaded ammunition belongs to the player's inventory, not the shared asset.

- Gun: six-round capacity, starts empty, instant reload, 0.5-second shot cooldown, 25-metre camera-centered hitscan, three-second stun.
- Gunshot agitation: `Assets/SOInstances/Agitation/Gunshot.asset`, radius 250, strength 10, investigation 20 seconds. Existing aggression scaling still applies.
- Bullets: `AmmoDefinition` plus `Ammo.prefab`, granting six bullets per pickup. Two explicit ammo entries are included in `GameScene`'s initial item pool. They use the same Valuable spawn-point category as the existing gun. Any-category item points also accept them.
- Potion: restores 50 stamina, capped at 100. Full stamina preserves the potion. Laboratory brewing continues to produce the same potion definition.

Shots consume loaded rounds on hits and misses. Walls stop the shot. The monster's `ShotHitbox` is a trigger, so it can receive shots without creating new physical blocking geometry. Shots explicitly include monster triggers and skip unrelated triggers; see Unity's [QueryTriggerInteraction reference](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/querytriggerinteraction).

Stun cancels pending attack damage, stops navigation and door interaction, and plays the hit reaction at normal speed. Further hits refresh the duration. Recovery uses normal perception or the remembered search position; maximum hunt resumes under its existing rules.

## Generic item-use feedback

Each `ItemDefinition` has two independent, optional **Use Feedback** settings:

- **Use Sound** (`UseSound`): a clip played by the player's shared `ItemUseAudioSource` after successful use.
- **Use Agitation** (`UseAgitation`): an optional profile reported from the player's position to the existing monster-hearing system. A sound clip alone does not emit AI agitation; a noise profile can work without an audio clip or audio source.

Successful potion drinking and gun firing use the same feedback helper. Rejected uses (full stamina, empty gun, cooldown, missing ownership, inactive gameplay, or an open inventory) produce neither new sound nor AI noise. Items without an implemented use effect do not count as successful uses. Removing/crafting/picking up an item does not play its use sound. Reloading retains its separate gun-specific sound and remains instant.

The potion uses `Assets/Thirdparty/Audio/41529__jamius__potiondrinklong.wav`. Its `UseAgitation` is intentionally unassigned: drinking is audible to the player without attracting monsters by default. Authors can assign a profile later. The final potion in a stack still plays its sound even when consuming it clears the selected shortcut.

The gun's existing clip and authored agitation profile are preserved. `FormerlySerializedAs` migrates `ShotSound` to `UseSound`, `ShotAgitation` to `UseAgitation`, and the player's `WeaponFireAudioSource` binding to `ItemUseAudioSource`. Game-over or disabling the item/audio controller stops the shared use source and reload source.

`ItemUseFeedbackTests` covers successful/rejected potion use, final-stack consumption, sound-only/noise-only/no-feedback configuration, disabled audio, blocked gameplay, and generic cleanup. Existing gun feedback tests continue to cover firing, reloads, hearing scaling, and hit animation. Evidence: `Temp/ItemUseValidation`. Restore point: `Temp/restore/item-use-feedback-20261126`.

## Gun audio and hit feedback

The gun definition's `UseSound` references `Assets/Audio/gunshot.wav` for firing and `Assets/Thirdparty/Audio/652631__percyfrench__revolver-reload.mp3` for reloading. The shared item-use source and independent reload source on the player retain its existing one-shot audio settings. Item-use sounds use one-shots so tails can overlap. Reload stays instant and its sound restarts on another successful transfer; it does not prevent firing. Rejected actions produce no additional audio or agitation. Game-over and disabling the item/audio controller stop the weapon sources.

Every fired bullet reports the authored gunshot agitation once, including misses and obstructed shots. Reloading reports no agitation. Existing aggression-based hearing scaling and pursuit priorities still apply.

`MonsterController.StunApplied` notifies animation on every accepted hit, including repeated hits during stun. The project-owned `Assets/Settings/MonsterGameplay.controller` copies the existing third-party locomotion/attack controller and adds `Getting Shot` (`Zombie@Damage01`, normal speed), followed by `Stunned Idle` (`Zombie@Idle01`) until the remaining stun expires. Each hit restarts the reaction and refreshes the three-second timer. Navigation/root motion stay stopped, attacks are cancelled, and the existing pain sound plays once per hit. Pause freezes animation and stun timing. Disable/game-over clear pending hit requests.

`GunFeedbackTests` covers playback/rejection/cleanup, overlapping shot tails, reload restarts, the authored agitation profile and scaling, and actual Animator/bone progression, interruption, repeat hits, pause and recovery. Evidence is in `Temp/GunFeedbackValidation`. A pre-change restore point is in `Temp/restore/gun-feedback-20261126`; the third-party Animator controller remains unchanged.

## Validation

- `InventoryTests` exercises stacking, targeted keys, shortcut movement/depletion, reload capacity and ownership, shot hits/misses/obstructions/noise, stun cancellation/recovery, potion consumption, inventory input blocking, held-interaction cancellation, keyboard/wheel controls, UI drag/drop, and six-bullet pickup quantities. Gun tests instantiate the authored monster prefab.
- Full Editor and Play Mode suites passed. Live scene checks collected the generated gun from an opened safe and shot the actual spawned monster. Two ammo pickups were present.
- Inventory and HUD reviewed in actual 16:9 and 16:10 Game Views. Screenshots and run evidence are under `Temp/UIReview` and `Temp/InventoryValidation`.
- No standalone player build was tested. No save system, held weapon model, lethal damage, or reload animation is added.

Restore point for the pre-change scripts, player prefab, scene and item definitions: `Temp/restore/inventory-20261125`. The original monster prefab is backed up there separately as `Monster.prefab`.

Existing safe geometry still emits negative-scale BoxCollider warnings during mansion generation; this feature does not modify that geometry.
