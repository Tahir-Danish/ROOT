# Cero — Team Workflow & Agent Method

## Why this exists

The project now uses AI agents heavily. Their value depends on them understanding the project's intent and the layer they are changing.

The goal is not maximum autonomous activity.

The goal is **maximum useful progress with controlled risk**.

## Before every meaningful task

The agent should establish:

1. What user/product outcome is actually wanted?
2. Which layer owns the problem?
3. What do the project documents say?
4. What does the live project actually contain?
5. Which facts are observed?
6. Which conclusions are inferred?
7. What is the smallest experiment/change that can answer the question?

## Layer model

```text
L0 Product intent
L1 Product architecture
L2 Source asset/import
L3 Unity asset/hierarchy
L4 Prefab
L5 Template integration
L6 Runtime interaction
L7 Camera/perception input
L8 ML inference
L9 API/software integration
L10 Knowledge/data
L11 Device performance/deployment
```

Find the lowest relevant layer before changing a higher layer.

## Investigation mode

Read-only means:
- inspect
- read docs
- inspect source
- collect evidence
- report
- STOP

No opportunistic fixes.

## Implementation mode

```text
Inspect
  ↓
Plan
  ↓
Change minimally
  ↓
Verify editor
  ↓
Compile/build
  ↓
Device test if needed
  ↓
Report exact changes
```

## Hypothesis discipline

Use:

> "X is the leading hypothesis because..."

instead of:

> "X is definitely the cause."

unless evidence supports certainty.

## Product-context rule

Never optimize a technical implementation in a way that silently changes the intended user experience.

For example:
- do not replace real-time perception with a Scan button simply because it is easier;
- do not make a 3D model mandatory because it makes AR placement easier;
- do not hardcode botanical claims inside UI code because it is faster.

## Documentation rule

When an experiment changes our architectural understanding:
- record the finding;
- distinguish fact from inference;
- update the appropriate document;
- do not convert one asset's quirk into a universal rule without evidence.

## Human verification

AI-generated:
- code
- technical conclusions
- botanical research
- ML experiments

must be reviewed by the accountable human.

## Current learning strategy

The XR lead will learn through small Cero-specific experiments:

```text
Camera
  ↓
Frame / image data
  ↓
ML input
  ↓
Detection
  ↓
Confidence / temporal state
  ↓
Spatialization
  ↓
AR augmentation
```

The learning target is practical competence sufficient to make architecture and implementation decisions for Cero, not generic mastery of every XR topic.
