# OUTPOST 09 — Canyon map, revision 01

## Reference and intent
The supplied concept image defines the composition: three elevated outposts on red sandstone mesas, five invasion roads, bridges over a deep river canyon and a final fortress gate. This revision uses generated mesh geometry and materials, not the photograph-level models shown in the reference. No purchased assets are required.

## Launch and editing
- Open SampleScene and press Play: the new map replaces the legacy generated flat arena.
- To inspect the whole map before playing: Tools > OUTPOST 09 > Open Canyon Preview.
- To create an editable saved scene: Tools > OUTPOST 09 > Generate Editable Canyon Scene.
- The saved scene is Assets/Outpost09/Scenes/CanyonPreview.unity. Generated material, texture and mesh assets go into a timestamped Assets/Outpost09/Generated/Canyon_* folder. Commit these assets and their .meta files with the scene.
- Play from the saved scene reuses its existing map instead of constructing a duplicate.
- The generator asks to save any currently modified scene before replacing the open scene. Earlier generated art asset folders are preserved.

## Coordinates (metres)
+Z points from the gate towards the invasion front. All route points are road SURFACE positions; enemy body clearance is added by combat code.

| Location | X | Y | Z | Role |
|---|---:|---:|---:|---|
| A: Ridge Watch | -25 | 18 | 4 | Left switchbacks and elevated D approach |
| B: Canyon Heart | 0 | 12 | 26 | Forward central overlook; central road skirts the mesa |
| C: East Bastion | 26 | 15 | 3 | East bridge and C/E approaches |
| Final Gate | 0 | 3 | -24 | Five routes converge at (0, 3, -20) |
| Spawn A | -38 | 3 | 65 | Left outer road |
| Spawn B | -3 | 1 | 72 | Central winding road |
| Spawn C | 39 | 3 | 59 | Eastern arch and bridge |
| Spawn D | -47 | 8 | 21 | High western flank |
| Spawn E | 46 | 7 | 3 | High eastern flank |

The A/B/C table entries are station centre reference positions (`StationCenters`). Platform deck is 1.65m below that reference. The actual camera (`Stations`) is 4.65m towards its AimPoint and 0.35m above the reference, on a narrow firing balcony. Main deck diameter is 7.6m (previously 12m). The balcony ends 0.5m beyond the camera, with no front handrail. Rails, cabins and supplies remain behind the firing position. The supporting mesas are narrower too, so their front faces do not block a downward shot. Enemy routes avoid passing through the elevated station mesas. They are deterministic waypoint paths, not a NavMesh or dynamic crowd avoidance system.

## Graphics implemented
- Ring-based cliff meshes with eroded silhouette and horizontal sediment ledges.
- Generated sandstone/sand albedo and micro normal textures, shared materials.
- Recessed river basin, animated waterfall ribbons and surface ripples.
- Metal bridges with lower girders, diagonal bracing, rail posts and amber edge markers.
- Circular armored platforms, cyan landing rings, utility cabins, storage cases, antennas and sensor/comms props.
- Gate forecourt, wall buttresses, two armored doors and a lit central seam.
- Warm low sunlight, cool fill light, distance haze, bloom, ACES tone mapping and vignette.
- Red spawn rings, warning beacons and alien crystal clusters outside the roads.
- Optional URP runtime quality clone: 4x MSAA and increased shadow range; shared project rendering assets remain intact.

## Team handoff
The map owner maintains `Scripts/Map/`, the canyon shaders and resources, and `Editor/OutpostCanyonTools.cs`. Edit station positions, spawn points and waypoint geometry in `Scripts/Map/OutpostMapLayout.cs`.

Combat reads station positions, aim points, gate position and routes through `Scripts/Contracts/OutpostBattlefield.cs`. Combat owns route following, enemy and weapon appearance, and player aiming. Gameplay owns the spawn schedule, rewards, menu and HUD. See the project root `TEAM.md` for team ownership and integration.

## Validation
C# runtime and Editor scripts compiled against installed Unity 6000.6.7f1 / URP 17.6 assemblies using Roslyn. This is source/API compatibility checking, not an Editor import/render or gameplay test.

Additional coordinate checks passed: all five routes terminate within 5m of the gate; segment rise/run stays below 0.5; every road centre stays at least 11.33m from an outpost mesa centre. These are geometric checks, not a substitute for collision/sightline tests in Play mode.

Unity launch was not approved in this session. Shader import, water appearance, generated scene saving, camera sightlines, performance and a full combat run still need in-Editor checking. Tools > OUTPOST 09 menus provide that next step. CLI entry point OutpostCanyonTools.ValidateAndCapture constructs the map and captures four views when Unity execution is available.
