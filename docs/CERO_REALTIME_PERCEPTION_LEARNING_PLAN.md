# Cero — Real-Time Perception → AR Learning Plan

## Purpose

This is a just-in-time learning sequence for the XR lead. Learn the smallest useful concept, build a tiny experiment, observe it, and connect the result to Cero.

Do not turn this into a generic Unity course.

## Phase 0 — Full pipeline mental model

```text
REAL WORLD
    ↓
PHONE CAMERA
    ↓
CAMERA FRAMES
    ↓
ML PERCEPTION
    ↓
DETECTION / CLASSIFICATION
    ↓
CONFIDENCE + STATE
    ↓
UNITY CLIENT
    ↓
SPATIAL / AR PRESENTATION
    ↓
USER EXPERIENCE
```

Camera frame rate, ML inference rate and Unity rendering rate are separate.

## Phase 1 — Camera as data

Learn:
- device camera vs Unity camera
- ARCameraManager
- AR Foundation camera access
- frame/image data
- CPU vs GPU image paths
- resolution
- pixel formats
- frame cadence
- why copying frames can be expensive

Target concept:

> The camera is a time-varying source of image data that can also be rendered to the user.

Tiny experiment:
- inspect current AR camera path
- access/inspect one camera image/frame path
- understand the API/data type exposed

## Phase 2 — ML input representation

Learn:
- image tensor
- channels
- RGB/RGBA
- resize
- normalization
- input shape
- preprocessing
- inference output

Target concept:

> Neural networks consume numerical representations of images.

Tiny experiment:
- feed one test image to a minimal model/stub
- inspect preprocessing
- log input type/shape

## Phase 3 — Classification vs detection vs segmentation

Classification:
> What is in this image?

Detection:
> What objects are present and where?

Segmentation:
> Which pixels belong to the plant?

Tracking:
> Is this the same detected object across frames?

Cero must decide which combination fits the desired AR UX.

Do not assume classification provides localization.

## Phase 4 — Real-time inference cadence

Learn:
- inference frequency
- frame skipping
- throttling
- asynchronous inference
- latency
- stale results
- confidence smoothing
- temporal stability

Example only:

```text
Camera: 30 FPS
ML: 5–15 FPS
Unity render: ~30+ FPS
```

Do not treat these as locked values.

## Phase 5 — Perception state

Do not let UI respond directly to one raw prediction.

Conceptual state machine:

```text
NO_TARGET
    ↓
CANDIDATE
    ↓
CONFIRMED
    ↓
TRACKED
    ↓
LOST / UNCERTAIN
    ↓
NO_TARGET
```

Questions:
- confidence threshold?
- consecutive observations?
- what counts as confirmed?
- what happens on a brief confidence drop?
- how long can previous state persist?
- how is flicker avoided?

## Phase 6 — Coordinate systems

Learn:
- image coordinates
- screen coordinates
- camera coordinates
- world coordinates
- raycasts
- planes
- anchors
- camera pose
- projection

Core concept:

```text
ML:
"I see the plant here in the image."

AR:
"Here is how that observation relates to physical space."
```

Never directly treat image pixels as Unity world coordinates.

## Phase 7 — Fake end-to-end perception experiment

Create a fake detector:

```text
FakeDetector
    ↓
species_id = neem
confidence = 0.94
detection = active
    ↓
Unity state
    ↓
AR label / contextual UI
```

Purpose: prove detection-state → AR-presentation independently.

## Phase 8 — Replace fake detector

Replace only the perception source:

```text
FakeDetector
   ↓
Real ML model
```

Keep the downstream detection contract stable.

## Phase 9 — Spatial localization

Determine what the ML system can provide:
- bounding box
- mask
- keypoint
- center
- other localization

Then evaluate how Cero turns that into AR presentation.

Possible approaches:
- screen-space augmentation
- world-space augmentation
- hybrid spatialization

Choose based on UX and reliability, not convenience.

## Phase 10 — Optional 3D asset

Only evaluate a plant model after the perception-to-augmentation path works.

A 3D model is:
- optional augmentation content
- not the identity source
- not the core AR requirement

## Phase 11 — Architecture decision

Only after the above should the team lock:
- on-device inference
- remote inference
- hybrid inference

Evaluate:
- target Android hardware
- latency
- network dependence
- privacy
- model size
- battery/thermal behavior
- development effort
- deployment reliability

## Exit condition

The XR lead should be able to explain:
1. where the camera frame originates;
2. how it becomes ML input;
3. what the model returns;
4. what confidence means;
5. classification vs detection vs segmentation vs tracking;
6. temporal stability;
7. image/screen/world coordinates;
8. how detection drives AR UI;
9. where Unity ownership ends;
10. why no Scan button is required.
