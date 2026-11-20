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
