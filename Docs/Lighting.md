# Lighting direction and bake audit

The mansion uses deliberately dim, localized light. Existing switches start rooms dark; fuses and monster interactions remain responsible for power loss. No new fuel or battery rules were added.

## Saved tuning
- Consolidated chandelier (6 to 1), ceiling and wall fixtures (2 to 1), and candle stick (5 to 1) light components. Bulb meshes and switch roots remain intact; unused Light components are disabled.
- Electrical fixtures: combined fixture intensity (chandelier 30, paired fixtures 10, single fixtures 5), range 28 m (originally intensity 5 and range 50 m; the initial optimization to 2.5 / 12 m was too weak for the room scale). Candle/decorative lights: range capped at 5 m. Short-range lights retain hard shadows to reduce illumination through walls; point-light shadow tier is low.
- Torch prefab: intensity 30, range 18 m, hard shadows.
- Scene: flat cool ambient RGB (0.16, 0.18, 0.22), reflection intensity 0.1, moon intensity 0.06 with a cool tint.
- PC and Mobile pipeline assets: shadow distance 35 m, 2 cascades, additional-light shadow atlas 2048 instead of PC 4096. Platform shadow-support switches remain as configured.

## Baking decision
No active gameplay lights were baked. MansionGenerator instantiates and rotates rooms at runtime. LightObject, LightSwitchInteractable, FuseBox and monster behavior switch fixture illumination; RitualCandle changes ritual lights, and the player carries a movable torch. Baked or mixed indirect illumination would remain after these sources turn off. Ordinary scene lightmaps also require a separate binding/relocation solution for generated rooms.

A future fixed-layout scene could bake permanently lit architectural accents and static geometry, with probes for moving actors. A modular room bake pipeline would need to carry lightmap textures, renderer indices/offsets and probes with room instances, handle rotation and seams, and exclude every switchable source. No such pipeline is currently implemented, so baking the current mansion is not a safe optimization.

## Verification (2026-11-19)
Editor Play Mode generated 51 rooms. Time.frameCount advanced from 2241 to 5385. There were 555 light components and 198 enabled components after fixture consolidation. All 158 LightObject toggle and power-off checks passed, with original states restored. All runtime lights were realtime; no point light exceeded the then-configured 12 m. Torch-off and torch-on Game view captures were visually inspected. No gameplay console errors were present; project warnings remain. This is functional and visual verification, not a GPU/FPS benchmark. Play Mode was stopped afterward.

Backups of touched existing assets are stored outside the project at C:/Users/ekipo/OneDrive/Documents/ChatGPT/ThirteensRoom/LightingBackup, preserving relative paths. Captures and audit scripts are beside that backup.

## Ambient readability correction (2026-11-19)
The initial ambient floor was too dark to locate wall switches. Raised flat ambient RGB from (0.018, 0.024, 0.038) to (0.16, 0.18, 0.22), retaining the cool tint, moon and localized light settings. Visually checked the user's current dark-room view in Play Mode: switch silhouette, wallpaper, doorway and floor are now visible, with clear contrast against the powered room. Saved to GameScene after stopping Play Mode, then restarted the session.

## Powered room coverage correction (2026-11-19)
The 12 m range and 2.5 intensity did not reach across 37.5 m rooms, especially from chandeliers mounted nearly 12 m above the floor. Electrical point lights now use 28 m range and combined fixture intensity (chandelier 30, paired fixtures 10, single fixtures 5), preserving one enabled light per consolidated fixture, hard shadows, and the readable ambient baseline. Candle and torch settings are unchanged.

Retained chandelier light is positioned 0.25 m below the model bounds to avoid its opaque body blocking illumination. A fixed camera in CR_Gallery was captured with room lights off and on, with torch disabled and Play Mode paused. Visually confirmed stronger floor and wall illumination when powered. Disabled redundant components remain disabled; ambient baseline stays (0.16, 0.18, 0.22).

## Dungeon firelight — 22 November 2026

J_DungeonRoom is lit exclusively by its four existing wall torches. Removed the unused wall switch. Torch Lights remain outside LightObject electrical control, so FuseBox and electrical switch operations cannot extinguish them. No global electrical-light behavior was changed.

Each torch now uses realtime warm amber light (RGB 1, 0.57, 0.25), base intensity 12 and range 22, with hard shadows to keep light contained. The previous intensity 1/range 5 could not reach the floor from the 8-unit mounting height. The intended result is readable, localized firelight with darker areas than a powered room; the scene ambient settings remain intact.

TorchLightFlicker layers two smooth noise frequencies with unique authored phases. Intensity varies around the authored baseline within an 18% envelope, avoiding abrupt random flashes and synchronized pulses. Disabling the component restores its baseline. Existing flame effects remain.

Validation: inspected fixture-only dungeon and powered-storage prefab renders without diagnostic lights. In Play Mode, 586 advancing frames over 3 seconds confirmed all four lights animate (observed intensity 9.957–13.335). All torches remained active through fuse off/on while five electrical fixtures lost/regained power; disabling flicker restored intensity 12. Console errors: zero. This is visual/function verification, not a GPU performance benchmark.

Run AgentScripts/DungeonTorchChecks.cs with script eval in Play Mode; its real-frame result is written to Temp/DungeonTorchRuntime.txt. [Dungeon lighting preview](RoomLayouts/DungeonTorchLighting.png).

## Fixture emission and switch audit — 22 November 2026

Audited all 25 Building room prefabs. Connected six functional lamps (Landing console, two bedroom lamps, two office lamps and ritual-room floor lamp) to Objects/Lights using LightObject, so room switches, fuse boxes, and existing electrical gameplay operate them together. Added modest local point lights where missing (intensity 3, range 12, hard shadows). The office desk lamp's nested light was replaced with an owned switched light because reparenting its inherited light did not persist through prefab save/reload.

Made 13 decorative renderers non-emissive, including both library desk lamps, unused candle stands and dining candelabra. Library desk lamp Light components are disabled. Generated non-emissive material variants are local to these fixture props; the shared mansion atlas and glass materials remain intact elsewhere.

LightObject now applies emission state to every renderer under its model, including glass children. RitualCandle synchronizes its model emission with LightCandle/ExtinguishCandle. Dungeon torches retain independent firelight and fuse immunity.

Play Mode validation: all 25 prefabs, 83 electrical fixtures through switch off/on and fuse off/on, eight ritual candles, and 13 non-emissive decorative renderers passed. The check requires actual enabled positive-output Light components, not emission alone. Inspected Landing lamp off/on fixture renders: the glass is dark when off and the console/floor are illuminated when on. Console errors: zero.

Repeat using AgentScripts/RoomFixtureChecks.cs entry RoomFixtureChecks.Run in Play Mode. [Lamp off](RoomLayouts/LandingLampOff.png) · [Lamp on](RoomLayouts/LandingLampOn.png).
