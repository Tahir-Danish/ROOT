# Cero — Technical Challenges, Hypotheses & Decision Log

This document records the major technical questions being approached. It does not prescribe answers before evidence exists.

## 1. Camera → ML input
Question: how do we obtain camera imagery efficiently enough for real-time inference?

Unknowns:
- exact image API/path
- CPU vs GPU route
- conversion cost
- pixel formats
- resolution
- inference cadence

Do not assume every rendered frame should be copied to CPU memory.

## 2. ML model type
Question: classification, detection, segmentation, or combination?

Why it matters:
The AR UI may need localization, which classification alone may not provide.

## 3. Temporal stability
Question: how do we avoid rapidly changing predictions?

Possible mechanisms:
- confidence threshold
- consecutive-frame confirmation
- moving average
- exponential smoothing
- temporal voting
- tracked-object state

Choose after observing the model.

## 4. Screen → world mapping
Question: how does image-space detection become spatial AR?

Potential inputs:
- bounding box center
- segmentation centroid
- camera ray
- AR raycast
- plane
- depth
- anchor

No universal answer.

## 5. On-device vs remote inference

### On-device
```text
Camera → Unity → local inference → AR
```

### Remote
```text
Camera → app → API → ML → app → AR
```

### Hybrid
A combination where useful.

Decision factors:
- latency
- connectivity
- privacy
- compute
- model size
- battery/thermal
- cost
- reliability
- implementation time

## 6. Detection-to-UI ownership

ML should not directly control Unity visuals.

Preferred:

```text
ML/service result
      ↓
stable data contract
      ↓
Unity state
      ↓
UX / AR presentation
```

## 7. Asset pipeline

Established:

```text
Source asset
→ Import
→ clean/authored prefab
→ AR object contract
→ template integration
```

Known lessons:
- source assets can include unwanted cameras/lights
- transforms may come from source coordinates
- texture mapping may require explicit work
- imported animation clips do not imply runtime Animator configuration

## 8. Confidence is evidence, not truth

Confidence should drive product states:
- confident
- uncertain
- unknown/unrecognized

Thresholds should be empirically chosen using the actual model.

## 9. Meaning of real-time

Real-time means the experience reacts to the live scene without requiring manual capture.

It does not automatically mean:
- every frame is inferred
- every result is shown
- results persist forever
- remote inference is mandatory

## 10. Experiment sequencing

```text
Camera understanding
    ↓
ML input understanding
    ↓
Fake detection
    ↓
AR presentation
    ↓
Temporal state
    ↓
Real model
    ↓
Spatial localization
    ↓
Inference architecture
    ↓
Optimization
```

## 11. Failure handling

For each experiment record:

```text
Hypothesis
Expected result
Actual result
What it disproves
What remains true
Next smallest experiment
```

## 12. Performance principle

Measure before optimizing.

Establish:
- correctness
- latency
- responsiveness
- thermal/battery behavior
- network requirements

Then optimize the measured bottleneck.
