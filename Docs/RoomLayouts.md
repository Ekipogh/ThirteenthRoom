# Room layouts

The themed rooms use authored footprints and furnishings within the existing 37.5 × 37.5 cell and 13.75 floor spacing. Prefab GUIDs, root transforms, room types, door anchors, and doorway assemblies remain compatible with the generator. No runtime layout randomization or generator API changes were introduced.

## Corner and live Editor rendering correction — 22 November 2026

The follow-up screenshots exposed two separate defects missed by the earlier checks. Protruding wall corners needed mitered joins between their inward-offset faces. Generated mesh updates also retained stale GPU buffers in the live Scene View: CPU geometry and preview renders appeared correct while storage and utility displayed missing triangles and incorrect material regions.

- Rebuilt all 17 authored shells with mitered wall/trim joins and closed corner collision. Door anchors, doorway assemblies, identities, furnishings, and generator placement remain unchanged in this pass.
- Replaced mesh CopySerialized updates with explicit vertex/index/channel updates and UploadMeshData, preserving existing asset references. Forced reimport and inspected the real Editor Scene View for storage, utility, and the library protruding corner.
- New RoomBoundaryChecks reproduced 1,170 diagonal corner failures before repair. After repair, 210 corners pass 3,780 front-facing diagonal probes, including Kitchen, Entrance, and Landing. Also passed 9,137 ceiling probes and 640 doorway seam probes. These CPU geometry checks are separate from the actual Editor rendering inspection.
- Updated free-space wall probes pass 2,700 samples; 1,949 floor and 6,118 wall triangles retain source UV density. All 142 saved-prefab navigation paths and 18 Play Mode interaction fixtures (33 targets) pass.
- A three-floor generated mansion (seed 1337, 76 rooms, all four rotations) passes 150 navigation paths, 146 horizontal doorway alignments, 74 switch checks, 103 interaction rays, and 72 passable-door checks. The monster was held stationary for interaction diagnostics. Console errors: zero. Earlier multi-seed validation is recorded below; this correction reran seed 1337.

Run RoomBoundaryChecks.Run from AgentScripts/RoomBoundaryChecks.cs in Edit Mode. The repeatable builder now also exposes RebuildShells to repair architecture without rearranging furnishings.

Actual Editor captures: [Storage](RoomLayouts/StorageEditor.png) · [Utility](RoomLayouts/UtilityEditor.png) · [Library corner](RoomLayouts/LibraryCornerEditor.png).

## Screenshot repair pass — 22 November 2026

The earlier navigation and bounds checks missed visible construction and furnishing defects. This pass replaces the shared construction method and checks rendered mesh coverage and native UV density directly.

- All 17 reshaped shells now use cropped source geometry at its original scale, with interpolated source UVs, rather than scaled wall/floor strips. Lower walls, upper walls, and trim meet at their native offsets. New meshes are serialized in `Assets/Generated/RoomLayouts`; these assets and their metadata must accompany the prefabs.
- Storage, utility, and cellar now use their full square cell footprint, eliminating the small wall jogs beside doorways. Door positions and room identities are unchanged. Floor, wall, and ceiling collision remain fitted to the room volume.
- **B_Library:** removed the duplicate inner bookcases, aligned the remaining three, rearranged the desks/chairs and reading area, cleared loose books from furniture, and registered three desk-supported item spawn nodes. Each spawn has a clear 0.5-unit desktop footprint rather than intersecting the paper stacks.
- **B_RitualRoom:** moved the bookcases against the rear wall to open the blocked corner and moved the music-box dresser with its supported objects clear of the wall.
- **CN_Office:** replaced the incompatible corner insert with two complete, filled bookcase assemblies. Removed the wine rack blocking the secondary desk, straightened the main desk/chair arrangement, mounted the curtain against the wall, and placed the safe against the entrance-side wall with clear bookshelf access.
- **CR_Corridor:** moved the console, plate, mirror, and shelf contents out of the wall, mounted the composite painting flush to the side wall, and preserved the narrow passage. Short corner pieces retain native texture density through cropping.
- Reinspected overview and player-height images for all 20 themed/core prefabs, plus 12 focused views of the reported corners and furnishings. Review lighting is supplementary diagnostic lighting.

Validation after the repairs:

- 1,949 floor triangles and 4,882 wall triangles match source UV density; numerically degenerate clipping slivers are excluded from the area-ratio measurement.
- 3,570 visible-mesh probes passed at wall ends, corners, and lower/upper wall heights. These test the actual opaque mesh triangles, independently of collision boxes.
- The three library spawn nodes are registered with `Room.GetItemSpawnPoints`, supported by desks, and clear of desktop decorations. The office corner book inserts are contained by their supporting bookcase frames.
- All 142 saved-prefab navigation paths passed. All 18 themed Play Mode fixtures passed their 33 active interaction-target checks at the existing three-unit range.
- The three-floor generated mansion passed 150 directed connection paths, 146 horizontal doorway alignments, and 100 active interaction-ray checks. A second, two-floor layout passed 100 directed connection paths. Monster movement was held still during the diagnostic interaction checks.

Run the added Edit Mode regression check with:

```powershell
unity command script run --caller plugin --skill unity-cli --project-path H:/development/ThirteenthRoom --file AgentScripts/RoomVisualChecks.cs --entry RoomVisualChecks.Run
```

`AgentScripts/RepairRoomLayouts.cs` contains the repeatable editor-only mesh/furnishing builder. It preserves mesh asset references when regenerating and saves backup prefabs under `Temp/RoomLayoutRepairBackup` before its first write.

[Focused repair views](RoomLayouts/RepairCloseups.png) · [Updated room overviews](RoomLayouts/Overview.png)


## Layouts

| Rooms | Layout and contents |
|---|---|
| B_Lavatory | 11 × 11.75 personal restroom beside its original entrance; one toilet, basin, mirror, and toilet roll. Communal fixtures and partitions removed; smaller overhead fixture. |
| B_Library, B_RitualRoom | Recessed chambers and entrance passages; bookcase/reading arrangements and a separate ritual preparation area. The ritual book sits near the table edge, within interaction range. |
| CN_Office | Offset study with connecting entrance passages, desk, shelves, consultation furnishings, and accessible safe. |
| CN_Storage, CN_UtilityRoom, J_Cellar | Large chambers retained, with organized shelving, service equipment, supplies, and clear circulation. |
| CR_Corridor | Straight 9 × 37.5 corridor; wall displays and sparse furnishings replace the sitting-room inventory. |
| CR_Crypt | Connecting aisle, recessed tomb bays, and memorial alcove. |
| CR_Gallery | Elongated gallery with alternating display recesses. |
| CR_Laboratory | Connecting aisle and offset work bays; original laboratory interactions retained. |
| I_DinningHall | Spacious banquet chamber with recessed corners and four entrance approaches. |
| I_GardenRoom, J_GardenRoom | Distinct planted footprints around central fountains. Access space is reserved around interactive plants. All existing doorway assemblies, including the junction garden's unused north assembly, remain. |
| J_Bedroom | Recessed sleeping and reading areas beside connecting circulation. |
| J_DungeonRoom | Recessed holding areas, connected passage, and guard station. The original inactive electric switch remains inactive. |
| J_MusicRoom | Offset performance chamber, listening furnishings, and connecting passages. |
| CN_Kitchen | Reference shape and furnishings retained. MonsterPoint moved to (5, 0, -2.5), onto accessible floor: its old position could sample a disconnected downstairs ceiling in multi-floor mansions. |
| Landing, Entrance | Original shapes and furnishings retained as architectural anchors. Their existing doorway, wallpaper, trim, and fixture language is reused by the themed rooms. |

The new shells have fitted physical colliders and compound room-tracking triggers. Floors and ceilings follow the usable footprint, leaving unused cell space outside the room. Doorway exterior faces retain their original shared seam; the kitchen's existing exterior wall seam is also retained. New shell geometry and furnishings stay inside the cell limits.

## Verification

Performed in Unity 6000.3.28f1:

- Reloaded all 18 themed prefabs: identity, topology, door transforms, geometry bounds, tracking bounds, nested prefab references, meshes, materials, and missing-script checks passed.
- Baked isolated navigation using the game's humanoid settings (radius 0.9, height 5.5) and 0.1 voxel size: 142 paths between door and floor-spawn markers passed, plus interaction approach checks.
- Play Mode fixtures for all 18 themed prefabs passed 33 active interaction-target ray checks, using the actual player interaction mask, obstruction logic, three-unit range, and 4.8 eye height. Fixtures were destroyed afterward.
- Generated layouts requested with seeds 42 and 2026 passed 100 directed connection paths each; seed 1337 with three floors passed 150. All four quarter-turn orientations were exercised. Prefab choices can vary with runtime random-state consumption.
- The final three-floor run passed 146 horizontal doorway alignments, 89 active interaction-ray checks, 75 light-switch checks, and 72 passable generated doors, with no reported failures. Monster movement was held still during these diagnostic interaction checks.
- Five keyed safes completed their opening animation and five pickups entered the player's inventory in Play Mode.
- Overview and player-height renders of all 20 themed/core room prefabs were visually inspected. These are diagnostic prefab renders with supplementary review lighting, rather than captures of final gameplay exposure. This verification does not claim a manual walking tour or monster pursuit through every room.
- Editor returned to Edit Mode; temporary imports and fixtures were removed and background-running overrides reverted.

## Repeat the checks

Run from a connected Editor for this project:

```powershell
unity command script run --caller plugin --skill unity-cli --project-path H:/development/ThirteenthRoom --file AgentScripts/RoomLayoutChecks.cs --entry RoomLayoutChecks.Run
```

Run the interaction check in a ticking Play Mode session with a PlayerInteractor present:

```powershell
unity command script run --caller plugin --skill unity-pipeline --project-path H:/development/ThirteenthRoom --file AgentScripts/RoomInteractionChecks.cs --entry RoomInteractionChecks.Run
```

The baseline records in `AgentScripts/RoomLayoutBaseline` capture the pre-edit doorway transforms, topology, and prefab identity. The checks operate on temporary instances and do not save changes to room assets.

## Review images

[Room overviews](RoomLayouts/Overview.png) · [Player-height views](RoomLayouts/PlayerViews.png)

Remaining warnings are inherited missing optional item-spawn containers and a negatively scaled collider in the office safe. Diagnostic runs that switch on an entire multi-floor mansion also produce URP shadow-atlas pressure warnings. No console errors were reported. Unity's serialized empty YAML fields produce standard trailing-space notices in plain Git whitespace checks; checks pass with blank-at-eol excluded.




## Light-switch mounting audit — 22 November 2026

Audited all 25 light switches in Building room prefabs, including generic templates, Kitchen, Entrance, and Landing. Aligned each backplate to its actual opaque wall triangles with a 0.001-unit surface allowance. Retained switch rotation, height, wiring, colliders, and behavior. The redesigned rooms previously had approximately 0.14-unit gaps; several original switches were slightly embedded.

Saved-prefab reload checks pass at nine points across every backplate. Repeat with RoomSwitchMounting.Run from AgentScripts/RoomSwitchMounting.cs; Run(true) repairs placement. Backups are in Temp/SwitchMountingBackup. All 18 themed Play Mode interaction fixtures pass (32 active targets at the existing three-unit range). Console errors: zero. Actual Scene View capture remained stale during this audit, so visual confirmation is not claimed.

## J_GardenRoom conservatory revision — 22 November 2026

Replaced the uneven cross and narrow rear extension with a balanced chamber, short side and entrance approaches, and a full-width rear seating bay. Preserved all door anchors, prefab identity, room type, and existing doorway assemblies. Four perimeter beds retain their foliage and fern groupings. Three benches face the fountain; potted plants frame the garden without crowding the doors. Grounded props, moved both spawn markers to clear lanes, and refitted tracking triggers to the new footprint.

The permanent builder exposes RebuildGarden and retains this arrangement during full rebuilds. All 15 garden marker-to-marker navigation paths pass, along with the 18-room navigation suite, corner/ceiling/door-seam checks, and all 18 Play Mode interaction fixtures (32 active targets). Preview images were visually inspected using diagnostic lighting; no generated-mansion seed sweep was rerun for this single-room revision.

[Top-down prefab preview](RoomLayouts/GardenTop.png) · [Player-height prefab preview](RoomLayouts/GardenPlayer.png).
