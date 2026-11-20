# Player head visibility

The player uses generated body and head meshes sharing the original animated skeleton. The Head bone stays at its imported unit scale. Each character variant owns its head renderers as children, so activating a variant activates its matching head and shadow together.

- Body: normal layer, visible and casting shadows.
- Head Visible: PlayerHead layer, normal rendering with shadow casting disabled.
- Head Shadow: normal player layer, ShadowsOnly, sharing the head mesh.

Only the player camera excludes PlayerHead. Keep this layer included in external cameras, future mirror/reflection cameras, and reflection probe culling masks. When copying the player camera to create a reflection camera, explicitly add PlayerHead back to the copied mask. Screen-space reflections cannot recover excluded geometry; this change does not implement mirrors.

The shadow renderer stays on a layer included by the player camera and lights. Do not move it onto PlayerHead or disable it to hide the head. The Animator uses AlwaysAnimate and the skinned renderers update offscreen so shadows and reflections remain animated independently of the owner's view.

After changing the source model, run Tools > Player > Rebuild Head Visibility Meshes. PlayerHeadMeshBuilder generates persistent meshes in Assets/Generated/PlayerMeshes without editing the imported model. It preserves vertex attributes, skinning, blend shapes, submeshes, materials, and bounds. A triangle belongs to the head if any vertex has more than 0.001 combined weight from Head or its descendants (eyes/jaw included); boundary triangles are kept whole. Rebuilding updates existing mesh assets in place and retains their GUIDs.

Run PlayerHeadVisibilityTests in the Unity Edit Mode test runner to validate classification, mesh partitions, all ten prefab variants, layer/shadow configuration, shared skeletons, and head scale across animation clips. Visually recheck extreme pitch, head bob, running, local/directional shadows, and an external camera after model changes.

## Verified result

Verified in Unity 6000.3.28f1 on 2026-11-20: all three PlayerHeadVisibilityTests passed, including all ten variants and animation clip samples; the existing PlayerGaspAudioTests Play Mode test also passed. Compilation completed without errors.

An isolated runtime fixture produced an external view identical pixel-for-pixel to the original unsplit mesh. Directional and point-light comparisons confirmed that the head shadow remains present with PlayerHead excluded by the camera. Ten views spanning pitch, forward offset, and bob extremes showed no head geometry in the owner view. A realtime reflection probe captured the complete head. Realtime probes are disabled in the current project quality settings; verification temporarily enabled them and restored the setting afterward. No mirror feature or permanent reflection settings were added.

## First-person eye placement

The HeadJoint pivot stays 0.9 units forward of the player root, within the controller capsule's radius of 1. The camera remains at the pivot origin, so pitching does not rotate an additional camera offset through the chest. PlayerController maintains this forward position both while moving and while idle; head bob changes only its height. Previously the camera returned to z=0 when movement stopped, exposing the open neck and torso interior when looking down. The 0.9 offset also clears the forward lean in locomotion poses. The camera near clip is 0.05, keeping its near plane inside the collision capsule. PlayerCameraPlacementTests covers idle, movement, stopping, pitch, bob, saved prefab placement, and near-plane clearance.

