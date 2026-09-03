# Team Cero — Working State Snapshot

This is a human/agent snapshot of what was observed during the early XR setup. It is NOT the live source of truth; revalidate with Unity MCP before relying on implementation details.

## Confirmed working

- Unity project opens successfully.
- Android build succeeds.
- AR runs on a real Android phone.
- Floor/plane tracking works.
- Basic objects can be placed in AR.
- 3D assets can be imported.
- Prefabs can be created.
- Multiple custom prefabs can be assigned to the existing Object Spawner and spawned.
- Unity MCP for Unity is installed locally and connected to Antigravity.

## Current observed project state

- Unity: 6.5.0f1
- AR Foundation: 6.5.0
- XR Interaction Toolkit: 3.5.1
- Sentis: 2.6.1 installed, reported unused so far
- Existing Mobile AR Template is the major implementation base
- No custom C# scripts currently in `Assets` according to onboarding inspection

## Current playground prefabs

- MH_Sam_full
- Pringles_Cans
- Rob_Walk

These are experimental assets only.

## Known issues / learning points

1. Template object menu icons are separate UI assets and do not automatically change when the spawned prefab changes.
2. A plain imported model/prefab does not automatically have every interaction/manipulation component/configuration used by the template's built-in objects.
3. The safest approach is to inspect a working template object and reproduce only the interaction configuration actually needed.
4. The previous onboarding pass was instructed to be read-only but still created an `onboarding_report.md`. Agent rules now explicitly prohibit artifact creation during read-only tasks unless requested.

## Revalidation rule

Before implementation, inspect the current Unity project. Do not assume the state described here is still true.
