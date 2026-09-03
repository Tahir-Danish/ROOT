# Team Cero XR Core — Recovery Candidate

> This file is a recovery candidate. Review it against any surviving local rule before replacing an existing rule.

## Mission

Build a reliable AR + AI field-intelligence experience for native plants.

Current product loop:

**Live camera → perception → identification/confidence → contextual AR augmentation → exploration → Botanical Guide → conservation.**

Manual Scan/capture is not the target product interaction.

## Context before implementation

Before a meaningful change:
1. read relevant project docs;
2. inspect live Unity state;
3. identify responsible layer;
4. separate facts from hypotheses;
5. choose the smallest safe experiment.

## Source hierarchy

1. Official Unity/XRI/AR Foundation/Google ARCore documentation.
2. Actual package/template source.
3. Live Team Cero project.
4. Controlled experiments.
5. Agent inference.

## Experiment discipline

```text
Understand → Inspect → Hypothesize → Minimal experiment → Verify → Device test → Record
```

## Existing-system preference

Preserve working Mobile AR Template architecture.

Do not add packages or replace systems without technical justification.

## Layer discipline

Treat these as separate:
- source asset
- import
- Unity asset
- prefab
- template integration
- runtime
- perception
- API
- knowledge/data

## Real-time perception

The desired experience is:
```text
live camera → perception → detection state → AR augmentation
```

Do not confuse:
- classification
- detection
- segmentation
- tracking

Do not treat image coordinates as world coordinates without an explicit mapping.

## Verification

Distinguish:
- editor verified
- build verified
- physical-device verified

## Product discipline

One plant end-to-end first.

A 3D model is optional; contextual AR is core.

## Core sentence

> Cero continuously perceives the world, recognizes a plant, and augments the user's understanding of it.
