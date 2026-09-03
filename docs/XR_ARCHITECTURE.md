# Team Cero — XR Architecture

## Current stack

- Unity 6.x; current observed version: 6.5.0f1
- AR Foundation; observed package version: 6.5.0
- XR Interaction Toolkit; observed package version: 3.5.1
- Android-first handheld AR
- Unity MCP + Antigravity for development assistance
- Unity Sentis observed installed at 2.6.1; currently unused in the reported project state

## Current observed implementation

The project is heavily based on the Unity Mobile AR Template.

Observed template responsibilities:
- AR camera/tracking via AR Foundation components;
- plane detection via AR Plane Manager;
- screen-space AR interaction via Screen Space Ray Interactor;
- object spawning via XRI `ARInteractorSpawnTrigger` + `ObjectSpawner`;
- object menu/selection via the template's existing UI/menu system.

The current project has zero custom C# scripts in `Assets` according to the Antigravity onboarding report. Custom behavior so far has mainly been prefab/scene configuration.

## Core conceptual pipeline

Camera
→ AR tracking/session
→ plane detection
→ screen-space interaction/raycast
→ object selection/spawn
→ manipulation
→ contextual AR presentation

## ML boundary

ML produces a structured result, conceptually:

```json
{
  "species_id": "SP01",
  "confidence": 0.91,
  "status": "identified"
}
```

Unity consumes the result. Training/evaluation logic stays outside the XR layer.

## Architecture principles

- preserve template systems while they satisfy requirements;
- custom code should be introduced only where it creates project-specific value;
- no duplicate placement/manipulation framework unless the existing template is proven insufficient;
- keep data, ML, UI, and XR loosely coupled.

## Current experiment state

The existing Object Spawner has been successfully configured to spawn Team Cero test prefabs including:
- `MH_Sam_full`
- `Pringles_Cans`
- `Rob_Walk`

These are playground assets, not product content.

Important observed limitation: imported custom prefabs can spawn successfully without automatically inheriting the template's menu iconography or manipulation behavior. Treat visual assets and interaction/configuration as separate concerns.
