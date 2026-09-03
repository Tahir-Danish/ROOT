# Cero — Real-Time Pipeline: Conceptual Reference

## The central model

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
CONFIDENCE + TEMPORAL STATE
    ↓
UNITY CLIENT STATE
    ↓
SPATIALIZATION
    ↓
CONTEXTUAL AR
```

## Camera frame rate ≠ ML inference rate ≠ rendering rate

A device may capture frames continuously, while the ML model processes only selected frames, while Unity renders its experience independently.

Example only:

```text
Camera        30 FPS
ML             5–15 FPS
Unity render  ~30+ FPS
```

These numbers are not project requirements.

## Perception types

### Classification
"What is in this image?"

### Object detection
"What object is present, and where is it?"

### Segmentation
"Which pixels belong to the object?"

### Tracking
"Is this the same detected object over time?"

Cero may need more than classification if AR information is expected to follow the plant.

## Coordinate systems

ML may output image/screen coordinates.

AR works with camera/world coordinates.

They are not interchangeable.

Conceptually:

```text
ML:
"I see the plant here in the image."

AR:
"Here is how that observation relates to physical space."
```

## Temporal state

A practical perception state machine may look like:

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

This avoids UI flicker from isolated noisy predictions.

Exact rules are TBD.

## Fake-detector strategy

Before real ML integration, create:

```text
FakeDetector
   ↓
species_id + confidence + detection
   ↓
Unity
   ↓
AR augmentation
```

Then replace only the detector:

```text
Real ML
   ↓
same contract
   ↓
same Unity AR
```

This isolates perception problems from Unity UX problems.

## 3D is optional

The AR interface may use:
- screen-space UI
- world-space annotations
- markers
- callouts
- info cards
- highlights
- optional 3D content

Do not treat a plant mesh as the system's identity or knowledge source.

## Inference architecture

Potential:

```text
On-device:
Camera → Unity → local ML → AR
```

or:

```text
Remote:
Camera → App → API → ML → App → AR
```

or hybrid.

The choice is not yet fixed.
