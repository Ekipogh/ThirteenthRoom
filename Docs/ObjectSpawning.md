# Mansion object dependencies

Rooms request renewable world objects through their **Object Dependencies** list. Each entry references a `SpawnObjectDefinition` and a positive mansion-wide minimum quantity. Requests for the same definition share sources: the highest quantity wins.

Create definitions through **Assets > Create > Mansion > Spawn Object Definition**. Assign a prefab and its authored Small, Medium, or Large size. Sizes are fit contracts, not automatic bounds checks.

Add an `ObjectSpawnPoint` under a room prefab, outside any wall/door hierarchy that changes activity at runtime. Set its maximum size and allowed definitions. Each point holds one object; larger points accept smaller allowed objects. An optional default fills the point after all required objects are assigned. Required objects may replace defaults. Inactive or disabled points are excluded. Selected gizmos illustrate size tiers, and the Inspector reports invalid references and defaults.

The point is the placement pivot: objects use its position and rotation and retain the prefab's local scale. Keep point scale at one unless scaling the entire placement is intentional. Keep nested collectible item points on the object prefab if needed; world objects spawn before navigation and initial inventory items.

Generation plans every room before creating the entrance, doors, furniture, or navigation. Unique rooms stay fixed; generic choices can change to provide missing compatible points. Newly selected rooms can introduce further requirements. Matching reassigns flexible points to avoid starving restricted objects. Provider backtracking is limited to 4096 search visits per layout, with up to 50 layouts; failure reports the unsatisfied object and prevents game startup. A search-limit failure does not prove a configuration mathematically impossible.

Both fixed-shape Garden variants provide four Medium plant points with a default Plant. The Laboratory requires one Plant. An office can satisfy that dependency if a designer adds a compatible point; no room name or Garden-specific rule is used.

Validation: run the `Mansion.ObjectDependencies.Editor.Tests` assembly in Edit Mode. It covers point sizes, defaults, occupancy, shared quantities, competing requirements, provider selection, and 120 seeded plans using the saved GameScene configuration in a private preview scene. Existing Laboratory Play Mode tests cover brewing; live generation should additionally verify harvesting and navigation with doors opened.
