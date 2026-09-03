# Cero — Current Decisions & Lessons Learned

## Decision 001 — Real-time perception

### Current decision
The intended user experience is continuous/live perception, not manual image capture.

### Reason
The central product idea is that the user points the camera naturally at the environment and Cero responds when a supported plant appears.

### Consequence
A manual button-triggered image-capture workflow may still be used as a temporary development mock, but it is not the target product architecture.

---

## Decision 002 — 3D plant models are optional

The AR experience is defined by **contextual augmentation**, not by showing a digital plant.

3D models are content assets that may be used when they add explanatory value.

---

## Decision 003 — Separate perception from presentation

Preferred architecture:

```text
ML / service result
        ↓
stable structured contract
        ↓
Unity state
        ↓
AR / UX presentation
```

Do not let the ML system directly control Unity visuals.

---

## Decision 004 — Learn with isolated experiments

The development sequence is:

```text
Understand
   ↓
Inspect
   ↓
Hypothesize
   ↓
Minimal experiment
   ↓
Verify
   ↓
Device test
   ↓
Record
```

Each experiment should isolate one variable where practical.

---

## Decision 005 — Keep asset pipeline explicit

Source asset, imported asset, authored prefab, and template integration are separate stages.

The Omnitrix experiment demonstrated this pipeline successfully.

---

## Decision 006 — Confidence is not truth

Model confidence is evidence produced by the model, not proof.

The eventual product must distinguish:
- confident recognition
- uncertain recognition
- no usable detection

Exact thresholds should come from evaluation of the actual model.

---

## Lessons from failed AR object experiments

### Rob_Walk
The imported asset contained an enabled Camera. This altered the AR scene and interfered with selection/rotation. The lesson is to inspect source import contents before assuming a visual asset is safe for AR.

### MH_Sam_full
Adding a minimal collider/Rigidbody/XRGrabInteractable/ARTransformer stack was not enough to reproduce all template UX characteristics. The lesson is that correct component lists alone are not equivalent to template parity.

### Omnitrix
A clean parent runtime root wrapping the imported FBX hierarchy, with explicit AR interaction components and template integration, successfully produced a usable AR object on a physical Android device.

The lesson is that prefab authoring should provide a stable runtime interface around source content.

## Current unresolved technical questions

1. How exactly should camera frames be acquired for ML?
2. What is the appropriate model type?
3. What inference cadence is practical?
4. How should temporal stability be handled?
5. How should image-space detection map to AR/world-space presentation?
6. Should perception run on-device, remotely, or hybrid?
7. What is the minimum stable contract between P/S and Unity?
8. Which contextual AR presentation is most useful to the user?
