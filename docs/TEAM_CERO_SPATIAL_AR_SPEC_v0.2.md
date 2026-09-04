# Team Cero --- Spatial AR Specification v0.2

**Project:** Team Cero --- Native Species Field Intelligence App\
**Target:** Unity 6 / AR Foundation / Android\
**Status:** Engineering specification --- pre-implementation\
**Basis:** Current production spatial AR audit and existing Team Cero
requirements\
**Purpose:** Define the smallest safe architecture for genuine
world-locked AR species visualization without changing the validated ML
pipeline.

------------------------------------------------------------------------

## 0. Non-Negotiable Principles

This specification is the engineering contract for the next
implementation step.

### MUST

-   Preserve the validated production ML pipeline.
-   Preserve `Pv002Pipeline.MapBoxToScreen`.
-   Separate ML detection, spatial acquisition, and AR presentation.
-   Create a spatial/world reference **once**, after sufficient
    detection stabilization.
-   Keep the card's **position world-locked** after spatial acquisition.
-   Allow card **rotation** to adapt independently for readability.
-   Maintain exactly one active production species card.
-   Use genuine AR Foundation spatial tracking where available.
-   Treat inability to acquire a meaningful spatial location as a real
    state.
-   Make the card readable in realistic lighting conditions.
-   Keep the six official species-information fields available.

### MUST NOT

-   Do not modify Model 1 preprocessing.
-   Do not modify Model 1 → Model 2 coordinate reconstruction.
-   Do not add a speculative vertical flip.
-   Do not lower the Model 2 threshold merely to increase visible
    detections.
-   Do not continuously move the card from the detection bbox after
    anchoring.
-   Do not use a fixed camera-relative `1.0 m` position as if it were a
    spatial anchor.
-   Do not keep `ExperimentalARTrackingCard` as a second competing
    production card system.
-   Do not dynamically `AddComponent<ExperimentalARTrackingCard>()` for
    production presentation.
-   Do not instantiate a new card on every detection frame.
-   Do not combine detection, spatial acquisition, and presentation
    tracking into one per-frame loop.

------------------------------------------------------------------------

# 1. Current Validated Boundary

The existing production ML chain is considered **out of scope for
modification**.

``` text
Camera
  ↓
Pv002Pipeline
  ↓
M1 detection
  ↓
M2 classification
  ↓
DetectionResult
  ↓
MapBoxToScreen()
  ↓
screen-space bounding box
```

The current production audit found the detector-to-screen mapping
geometrically valid.

Therefore the spatial system begins **after the validated screen-space
DetectionResult**.

------------------------------------------------------------------------

# 2. Target Architecture

The target architecture is:

``` text
┌───────────────────────┐
│      ML DETECTION     │
│                       │
│ Pv002Pipeline         │
│ M1 + M2               │
└───────────┬───────────┘
            │
            │ DetectionResult
            ▼
┌───────────────────────┐
│   SCREEN REFERENCE    │
│                       │
│ bbox → chosen point   │
└───────────┬───────────┘
            │
            │ screen point
            ▼
┌───────────────────────┐
│ SPATIAL ACQUISITION   │
│                       │
│ AR ray / AR trackable │
│ → stable world pose  │
└───────────┬───────────┘
            │
            │ world pose / anchor
            ▼
┌───────────────────────┐
│   AR PRESENTATION      │
│                       │
│ 3D card + tether       │
│ text + visual state    │
└───────────────────────┘
```

The critical ownership rule is:

> **ML owns identity and confidence. Spatial acquisition owns world
> position. Presentation owns appearance.**

------------------------------------------------------------------------

# 3. Screen Reference Point

## 3.1 Existing behavior

The current production system passes:

``` csharp
result.boundingBox.center
```

as the screen reference point.

This is already geometrically consistent with the detector output.

## 3.2 Preferred spatial reference

For a plant, the bbox center is not necessarily the best physical
reference.

A plant's bounding box can contain:

-   leaves,
-   empty background,
-   sky,
-   table/ground,
-   stems,
-   unrelated pixels.

The preferred candidate for spatial acquisition is therefore:

``` text
BOTTOM-CENTER OF DETECTION BBOX
```

Conceptually:

``` text
┌─────────────────────┐
│                     │
│      PLANT          │
│     / | \           │
│    /  |  \          │
│                     │
└────────●────────────┘
         ↑
   bottom-center
```

The reason is geometric rather than semantic:

> For plants growing from a surface, the lower portion of the detection
> is more likely to project toward the plant's base / supporting surface
> than the bbox center.

## 3.3 Important qualification

Bottom-center is the **preferred candidate**, not an assumption that it
will always hit the correct physical location.

The implementation must validate this against actual AR behavior.

If evidence shows that center produces more reliable spatial acquisition
for the target environment, the spatial reference policy may be revised.

This decision must be data-driven.

------------------------------------------------------------------------

# 4. Screen → Spatial Acquisition

The spatial acquisition layer receives a screen point and attempts to
derive a meaningful world pose.

Preferred chain:

``` text
screen point
    ↓
camera ray
    ↓
ARRaycastManager
    ↓
valid AR trackable / plane hit
    ↓
world pose
    ↓
ARAnchor
```

The current project already has an `ARRaycastManager` on the XR Origin.

The project currently has **no `ARAnchorManager`**.

The future implementation may add the required anchor-management
capability at the smallest safe scene boundary.

------------------------------------------------------------------------

# 5. No-Hit Policy

The current system uses:

``` csharp
ray.GetPoint(1.0f)
```

when AR plane raycasting fails.

That behavior must **not** be retained as the production spatial
placement policy.

Why:

``` text
No plane hit
     ↓
1 m in front of camera
     ↓
camera moves
     ↓
card moves
```

This creates the appearance of AR while not providing a stable world
reference.

## Required behavior

If spatial acquisition cannot produce a sufficiently reliable world
pose:

``` text
Detection exists
       ↓
Spatial acquisition failed
       ↓
NO SPATIAL CARD YET
```

The system may expose a clear acquisition state in the UI if useful, but
it must not silently pretend that a camera-relative fallback is
world-locked.

------------------------------------------------------------------------

# 6. Spatial Acquisition Lifecycle

The lifecycle must be stateful rather than frame-driven.

``` text
DETECTION
    ↓
STABILIZATION
    ↓
SPATIAL ACQUISITION
    ↓
ANCHOR CREATED
    ↓
CARD PRESENTATION
    ↓
WORLD TRACKING
```

## 6.1 Detection

ML produces:

-   species ID
-   confidence
-   status
-   bounding box

No world position is created here.

## 6.2 Stabilization

The system should require sufficiently stable detection before
committing to a world location.

Existing state-machine confirmation behavior may be reused as the
starting trigger, but the spatial layer must not assume that every raw
detection is suitable for anchoring.

## 6.3 Spatial acquisition

Once detection is sufficiently stable:

1.  Calculate the selected screen reference.
2.  Perform AR spatial acquisition.
3.  Validate the hit.
4.  Create/bind the world anchor.
5.  Create or activate the production card.
6.  Bind the card to the spatial reference.

## 6.4 World tracking

After the anchor is established:

``` text
Detection updates:
    identity
    confidence
    content
    visibility/state

Detection does NOT update:
    anchor position
```

This is the central anti-jitter rule.

------------------------------------------------------------------------

# 7. Anchor Ownership

The spatial anchor must have a single clear owner.

Recommended conceptual hierarchy:

``` text
AR Session
└── XR Origin
    └── Spatial Anchor
        └── AR Species Card
```

The card's world position should be derived from the anchor, not from
the camera.

Once established:

``` text
Anchor position = authoritative spatial position
Card position    = anchor + presentation offset
```

The presentation offset may be configurable.

------------------------------------------------------------------------

# 8. Card Position vs Card Rotation

These are independent systems.

## Position

Position is:

**WORLD-LOCKED**

After anchoring:

``` text
Card XYZ
   ↑
Anchor XYZ
```

The card must not follow:

``` text
Camera XYZ
BoundingBox center
Detection jitter
```

## Rotation

Rotation may remain adaptive for readability.

The existing `Billboard.cs` is a candidate reusable component because it
already performs damped camera-facing orientation.

However:

> Billboard rotation must never modify the card's world position.

The desired result is:

``` text
        Camera
          👁
          │
          │ viewing
          ▼

      ┌───────────┐
      │ AR CARD   │
      └───────────┘
           │
           │ fixed world position
           ●
         Anchor
```

The card can turn toward the viewer while remaining spatially fixed.

------------------------------------------------------------------------

# 9. Card Presentation Geometry

The card must read as a **3D spatial object**, not as a tiny flat
overlay pretending to be AR.

The existing `ARSpeciesCard.prefab` should be treated as the
presentation starting point.

Desired conceptual structure:

``` text
Plant / Anchor ●
                                                ╲ 3D tether
                  ╲
             ┌───────────────┐
             │ SCIENTIFIC    │
             │ Common Name   │
             │               │
             │ Family        │
             │ Native Region │
             │ Ecology       │
             │ Conservation  │
             └───────────────┘
```

The exact visual styling is intentionally secondary to the spatial
behavior.

------------------------------------------------------------------------

# 10. Tether / Leader Line

The tether should be genuine 3D geometry.

Let:

``` text
A = plant/world anchor
B = card world anchor
```

Then:

``` text
direction = B - A
length    = |B - A|
midpoint  = (A + B) / 2
```

The tether is placed at `midpoint`, aligned along `direction`, with its
length equal to `|B-A|`.

Conceptually:

``` text
A ●────────────────────● B
  plant                card
       3D tether
```

The tether is presentation geometry only.

It must not become the source of spatial tracking.

------------------------------------------------------------------------

# 11. Card Offset

The card should generally not sit directly on the plant geometry.

Instead:

``` text
Plant anchor
     ●
             \ small controlled offset
                 ▣ Card
```

The offset must be:

-   deterministic;
-   world-space;
-   bounded;
-   visually connected to the anchor;
-   independent of camera-distance hacks.

A future implementation should choose the offset after testing
readability and occlusion.

Do not introduce arbitrary large offsets to make the card visible.

------------------------------------------------------------------------

# 12. Official Information Requirements

The AR visualization must support the six required contextual
information fields:

1.  **Scientific Name**
2.  **Common Name**
3.  **Plant Family**
4.  **Native / Native Region**
5.  **Ecological Importance**
6.  **Conservation Status**

The information hierarchy should prioritize:

``` text
Scientific / Common identity
        ↓
Family / Native region
        ↓
Ecological importance
        ↓
Conservation status
```

The card should remain readable without requiring tiny text.

------------------------------------------------------------------------

# 13. Readability Requirements

The card is intended for real camera-view use.

Therefore:

-   use strong contrast;
-   use sufficiently large typography;
-   use a mostly opaque backing;
-   avoid thin low-contrast text;
-   avoid excessive transparency;
-   avoid packing all six fields into microscopic type;
-   maintain readable dimensions as the user approaches/recedes;
-   ensure the card remains legible under both outdoor and indoor
    conditions.

Distance scaling must not undermine readability.

The existing `ARSpeciesCard` distance-scaling behavior should therefore
be audited during implementation rather than assumed to be acceptable.

------------------------------------------------------------------------

# 14. Detection State vs Spatial State

These must not be conflated.

Example:

``` text
Detection:
CONFIRMED / identified / 82%

Spatial:
ANCHOR_LOCKED
```

Later:

``` text
Detection:
temporarily lost

Spatial:
ANCHOR_LOCKED
```

The card does not necessarily need to teleport or be destroyed
immediately.

Likewise:

``` text
Detection:
confidence 0.74

Spatial:
existing anchor
```

does not automatically mean:

``` text
destroy anchor
destroy card
recreate card
```

Temporal state policy should be deliberate.

------------------------------------------------------------------------

# 15. Temporary Detection Loss

A temporary ML dropout should not immediately cause spatial-card
destruction.

Preferred behavior:

``` text
Anchor locked
     ↓
Detection temporarily lost
     ↓
short grace period
     ↓
card may remain / transition state
     ↓
detection returns
     ↓
same anchor continues
```

The exact timeout should be determined during implementation/testing.

The key requirement is:

> **Do not recreate the spatial object merely because one inference
> frame failed.**

------------------------------------------------------------------------

# 16. Confidence Fluctuation

The current production system has a meaningful threshold discrepancy:

``` text
Pv002Pipeline acceptance: >= 0.50
State-machine confirmation: >= 0.75
```

This contributed to the previous #008 state flapping.

The spatial implementation must avoid creating/destroying/repositioning
the card on every threshold crossing.

The correct solution is a stable state transition policy, not repeated
transform changes.

Any modification to the existing confirmation thresholds must be treated
as a separate decision and must not be smuggled into the spatial
implementation.

------------------------------------------------------------------------

# 17. Multiple Plants

The first implementation should prioritize **one stable active spatial
target** unless the existing product scope explicitly requires
simultaneous multi-plant cards.

The system must not accidentally create multiple cards because multiple
detections arrive.

At minimum:

``` text
One active target
    ↓
One active anchor
    ↓
One active card
```

Multi-target support should be a deliberate future extension, not an
accidental side effect.

------------------------------------------------------------------------

# 18. ExperimentalARTrackingCard

The current audit established that `ExperimentalARTrackingCard`:

-   is dynamically added by `DetectionVisualizer`;
-   instantiates its own `ARSpeciesCard`;
-   performs its own raycast;
-   has its own camera fallback;
-   continuously Lerps its card position;
-   has no anchor logic.

It therefore **must not remain as a second production spatial-card
controller**.

The future implementation should remove/disable this competing path at
the appropriate production boundary.

The goal is:

``` text
ONE detection event
      ↓
ONE spatial acquisition
      ↓
ONE anchor
      ↓
ONE card
```

------------------------------------------------------------------------

# 19. `DetectionVisualizer`

`DetectionVisualizer` currently has a production-side responsibility
beyond visualization because it dynamically injects
`ExperimentalARTrackingCard`.

That coupling should be removed from the final spatial architecture.

The visualizer may continue to provide diagnostic/debug visualization
where useful, but it should not silently create a second production
AR-card controller.

------------------------------------------------------------------------

# 20. AR Foundation Components

Current audited state:

  -----------------------------------------------------------------------
  Component               Current status          Future role
  ----------------------- ----------------------- -----------------------
  AR Session              Present                 Tracking lifecycle

  XR Origin               Present                 AR world coordinate
                                                  root

  ARRaycastManager        Present                 Spatial acquisition

  ARPlaneManager          Present/passive         Supplies trackable
                                                  planes

  ARAnchorManager         **Absent**              Candidate required for
                                                  persistent anchor
                                                  lifecycle

  ARAnchor                **Absent**              Future world-lock
                                                  reference

  ARCameraManager         Present                 Camera/ML input
  -----------------------------------------------------------------------

The implementation should add only the minimum missing capability
required for true anchoring.

------------------------------------------------------------------------

# 21. No Per-Frame Position Updates

This is a hard invariant.

After anchor creation:

``` text
FOR EACH DETECTION FRAME:
    update identity
    update confidence
    update card content
    update visibility/state

DO NOT:
    recompute world position
    recalculate camera-ray fallback
    Lerp card toward bbox
    recreate anchor
    recreate card
```

The anchor subsystem is responsible for spatial tracking.

------------------------------------------------------------------------

# 22. What Caused #008

The previous #008 failure can now be explained structurally:

``` text
Two card controllers
      +
camera-relative fallback
      +
per-frame movement
      +
confidence state flapping
      +
runtime component injection
      =
unstable spatial presentation
```

The new architecture explicitly removes these failure modes.

------------------------------------------------------------------------

# 23. Acceptance Tests

The implementation is not considered successful merely because the card
appears.

It must pass the following behavioral tests.

## Test A --- Stationary plant

1.  Detect plant.
2.  Establish spatial anchor.
3.  Card appears.
4.  Hold phone still.

**Expected:** card remains stable.

## Test B --- Move phone sideways

1.  Anchor a plant.
2.  Move phone left/right without moving plant.

**Expected:** card remains at the same world location relative to the
plant.

## Test C --- Move closer/farther

1.  Anchor plant.
2.  Walk/lean toward and away from it.

**Expected:** card remains spatially associated with the same location.

## Test D --- Rotate around plant

1.  Anchor plant.
2.  Move around the plant.

**Expected:** card position remains world-locked; orientation may adapt
for readability.

## Test E --- Temporary detection loss

1.  Anchor plant.
2.  Temporarily obstruct/lose detection.

**Expected:** no immediate card recreation or teleportation.

## Test F --- Confidence fluctuation

1.  Observe confidence crossing the classification/confirmation
    boundary.

**Expected:** no rapid card duplication, destruction/recreation, or
position jumping.

## Test G --- New plant

1.  Lose the old target deliberately.
2.  Detect another plant.

**Expected:** old spatial target is handled according to lifecycle
policy; new target gets its own valid acquisition.

## Test H --- No plane hit

Aim at foliage where no suitable AR plane is available.

**Expected:** system does not pretend that a 1 m camera-relative card is
spatially anchored.

## Test I --- Readability

Test under:

-   bright outdoor light;
-   indoor light;
-   near distance;
-   moderate distance.

**Expected:** six information fields remain meaningfully readable.

## Test J --- Duplication

Run repeated detections.

**Expected:** exactly one production card instance for the active
target.

------------------------------------------------------------------------

# 24. Evidence Requirements

Every implementation decision should be traceable to one of:

1.  Existing production code.
2.  Existing project requirements.
3.  AR Foundation runtime evidence.
4.  Explicit acceptance-test evidence.

Do not introduce speculative fixes simply because they "usually work" in
AR applications.

------------------------------------------------------------------------

# 25. Implementation Boundary

The eventual implementation should be as small as possible.

Conceptually:

``` text
Existing:
Pv002Pipeline
      ↓
DetectionResult
      ↓
AutoARCardTrigger
      ↓
ARUXManager

Change:
                    ┌────────────────────────┐
                    │ Spatial acquisition    │
                    │ + anchor lifecycle     │
                    └───────────┬────────────┘
                                ↓
                         ARSpeciesCard
```

Likely areas requiring modification:

-   `ARUXManager` --- spatial acquisition / card lifecycle boundary.
-   `SampleScene` / XR Origin --- minimum required anchor-management
    capability.
-   `DetectionVisualizer` --- remove production dynamic injection of the
    experimental card.
-   `ExperimentalARTrackingCard` --- remove from the production path
    rather than layering it into the new system.
-   Potentially the card presentation/tether components, but only after
    the spatial behavior is proven.

`Pv002Pipeline.cs` should remain unchanged unless new evidence
specifically demonstrates an ML-side problem.

------------------------------------------------------------------------

# 26. Implementation Order

Do not implement everything simultaneously.

Use this sequence:

### Phase 1 --- Spatial correctness

``` text
Detection
  ↓
screen reference
  ↓
AR raycast
  ↓
anchor
  ↓
static card
```

Prove that the card is world-locked.

### Phase 2 --- Lifecycle stability

Test:

-   confidence fluctuations;
-   temporary detection loss;
-   repeated detections;
-   new target.

### Phase 3 --- Presentation

Only after spatial correctness is proven:

-   card sizing;
-   typography;
-   six-field hierarchy;
-   tether;
-   billboard orientation;
-   polish.

### Phase 4 --- Field validation

Test outdoors and indoors.

This ordering is deliberate.

**A beautiful card attached to a moving camera is still a failed AR
implementation.**

------------------------------------------------------------------------

# 27. Definition of Done

The spatial AR work is considered complete only when all of the
following are true:

-   [ ] One production AR-card controller exists.
-   [ ] `ExperimentalARTrackingCard` no longer competes with production
    presentation.
-   [ ] No camera-relative 1 m fallback is presented as spatial
    anchoring.
-   [ ] A valid world reference is acquired before spatial presentation.
-   [ ] The card's position is world-locked after acquisition.
-   [ ] Detection jitter does not move the anchored card.
-   [ ] Confidence fluctuation does not create duplicate cards.
-   [ ] Temporary detection loss does not immediately recreate the card.
-   [ ] Card rotation can adapt independently of world position.
-   [ ] Tether is genuine 3D geometry.
-   [ ] Official six information fields are represented.
-   [ ] Card remains readable in field conditions.
-   [ ] ML pipeline remains unchanged.
-   [ ] Stationary/movement/rotation acceptance tests pass.
-   [ ] No-hit behavior is honest and stable.
-   [ ] The implementation remains small enough to diagnose if field
    behavior fails.

------------------------------------------------------------------------

# 28. Final Engineering Principle

The final system should behave like this:

``` text
             CAMERA
                │
                ▼
        ┌───────────────┐
        │ ML DETECTION  │
        └───────┬───────┘
                │
             2D bbox
                │
                ▼
        ┌───────────────┐
        │ SPATIAL QUERY │
        └───────┬───────┘
                │
          world pose
                │
                ▼
             ANCHOR
                │
        ┌───────┴────────┐
        │                │
        ▼                ▼
     3D CARD           TETHER
        │
        ▼
   READABLE INFO

After anchor creation:

ML may change WHAT is shown.

AR tracking may change WHERE the anchor is tracked.

But detection must NOT continuously decide WHERE the card lives.
```

**That separation is the core of Spatial AR v0.2.**
