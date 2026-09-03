# Team Cero — Agent Context & Development Workflow

## Purpose

This document establishes the working context for AI agents after the foundational Unity/XR experimentation phase.

## Project intent

Team Cero is building an **AR + AI field-intelligence experience for native plants at VIT Vellore**.

The product is not merely a plant-identification app and not merely an AR visualizer. It is intended to act as a smartphone-based biodiversity field assistant that helps a person:

**Encounter → Detect/Identify → Understand → Explore → Ask → Conserve**

Mandatory pillars:
- AI plant identification
- AR contextual augmentation
- AI Botanical Guide
- Conservation awareness

The first engineering target is deliberately narrow:

> **One plant must work end-to-end before the project expands.**

## Critical product clarification: real-time perception

Cero is **not** intended to use a Scan button followed by a still-image capture.

The intended interaction model is:

```text
Live camera feed
      ↓
Continuous / appropriately optimized ML perception
      ↓
Detection / classification state
      ↓
Stable identity + confidence
      ↓
Immediate contextual AR augmentation
```

When a supported plant appears in front of the camera and the system reaches the required confidence/stability, the interface should respond without requiring the user to manually capture a photo.

The exact inference cadence is an engineering decision. The product requirement is the real-time interaction model, not a promise that every camera frame must be inferred.

## AR is not synonymous with a 3D plant model

A 3D plant model is **optional**.

The essential AR value is that the real plant visible in the camera feed becomes more understandable through contextual digital information.

Possible outputs:
- species label
- contextual callouts
- spatial annotations
- information cards
- visual highlighting
- ecological/conservation overlays
- interactive hotspots
- optional 3D representation

The real plant remains the subject.

## What has been proven so far

### Environment
- Unity is the active XR implementation path.
- Android-first.
- Unity MCP is connected to Antigravity.
- Project is based heavily on Unity's Mobile AR Template.
- AR Foundation 6.5.0.
- XR Interaction Toolkit 3.5.1.
- Unity 6.5.x generation.

### Experiments
1. Baseline AR project built and ran on Android.
2. Default template objects could be placed and manipulated.
3. Imported models initially spawned without automatically inheriting template interaction behavior.
4. Rob_Walk exposed an embedded-camera/source-import problem.
5. MH_Sam_full demonstrated the difference between visual content and authored interaction behavior.
6. Deep reverse-engineering was performed on the Mobile AR Template, including serialized configuration and relevant source code.
7. `AR_OBJECT_CONTRACT.md` was created to formalize AR prefab requirements.
8. An Omnitrix FBX pipeline was created and integrated into the template.
9. On a physical Android device, the Omnitrix demonstrated placement, selection, movement, rotation, scaling and deletion.
10. The Omnitrix experiment established a reusable asset → import → prefab → template-integration pipeline.

These were learning experiments, not product features.

## Core technical understanding gained

Treat these as separate stages:

```text
Source asset
    ↓
Importer
    ↓
Imported Unity asset/hierarchy
    ↓
Authored prefab
    ↓
Behaviour/components + references
    ↓
Template integration
    ↓
Runtime AR object
```

Do not collapse them into "model import."

## Mobile AR Template understanding

The current understanding includes:
- AR Session / XR Origin / AR camera provide AR runtime foundation.
- ARPlaneManager handles detected planes.
- Screen Space Ray Interactor participates in touch-based spatial interaction.
- ARInteractorSpawnTrigger and ObjectSpawner participate in spawning.
- XR Interaction Toolkit provides interactable behavior.
- ARTransformer contributes AR-aware transform manipulation.
- Object menu UI and ObjectSpawner configuration are separate concerns.
- Menu icons are separate UI assets/configuration from runtime 3D prefabs.
- Template prefabs contain important configuration/references beyond a small set of components.

## New development philosophy

The project is no longer in a "try random Unity modifications" phase.

Preferred loop:

```text
UNDERSTAND
    ↓
INSPECT
    ↓
FORM HYPOTHESIS
    ↓
MINIMAL CHANGE / EXPERIMENT
    ↓
VERIFY
    ↓
REAL DEVICE TEST
    ↓
RECORD RESULT
    ↓
UPDATE UNDERSTANDING
```

For ambiguous problems:

```text
Observed fact
    ↓
Possible explanation(s)
    ↓
Evidence needed
    ↓
Smallest discriminating experiment
    ↓
Result
    ↓
Confirmed / rejected hypothesis
```

Do not jump directly from symptom to fix.

## Agent reasoning requirements

Before any meaningful technical change:
1. Re-read relevant project docs.
2. Inspect the live Unity project through MCP.
3. Identify the layer where the issue lives.
4. Separate observed facts from inference.
5. Preserve working systems unless evidence says otherwise.
6. Make the smallest change that tests the hypothesis.
7. Verify editor state.
8. Test on device when runtime behavior is involved.
9. Continue only after the result is understood.

Never treat a prior agent conclusion as authoritative merely because it sounds confident.

## Read-only means read-only

When explicitly investigative/read-only:
- no scene edits
- no prefab edits
- no asset edits
- no package changes
- no settings changes
- no scripts
- no generated reports/notes unless explicitly requested

Report findings and stop.

## Source hierarchy

Use this order:
1. Official Unity/XRI/AR Foundation/Google ARCore documentation.
2. Actual package/template source code.
3. Actual Team Cero project state.
4. Controlled experiments.
5. Agent inference.

Label important claims where useful:
`[DOCUMENTED]`, `[OBSERVED]`, `[INFERRED]`, `[UNVERIFIED]`.

## Documentation principle

Documentation should capture:
- intent
- requirements
- architecture
- decisions
- verified technical understanding
- reusable workflows

Do not maintain brittle inventories of changing GameObjects unless architecturally important.

The live Unity project remains the implementation source of truth.

## Current learning goal

Move from XR infrastructure learning to **Cero perception-to-augmentation**:

```text
Camera
  ↓
Frame / image data
  ↓
ML perception
  ↓
Detection / classification result
  ↓
Client-side state
  ↓
AR augmentation
```

Use tiny experiments before connecting the real system.

## Team boundary

- **You:** Unity/XR client and user experience.
- **S:** software/API/service/data infrastructure.
- **P:** ML model, inference, evaluation, confidence behavior.
- **M:** verified botanical and conservation knowledge.

The boundary is collaborative; the interface contract should be explicit.
