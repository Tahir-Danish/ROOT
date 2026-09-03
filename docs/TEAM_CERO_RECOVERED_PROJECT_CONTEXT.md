# Team Cero — Recovered Project Context

## Mission

Build an **AR + AI field intelligence experience for native plants at VIT Vellore**.

The intended application turns a smartphone into a biodiversity field assistant that helps users identify plants, understand them through AR and an AI botanical guide, and understand why native species matter for conservation.

This is not merely a plant-identification app.

## Mandatory pillars

1. AI Plant Identification
2. AR Species Visualization / Contextual AR
3. AI Botanical Guide
4. Conservation Awareness

The broader requirement is at least 7 plants, a working prototype, and user validation from at least 5 users.

## Current product direction

The earlier project documents used:

**Encounter → Scan → Identify → Understand → Explore → Ask → Conserve**

The current product decision refines this.

### Current workflow

**Encounter → Real-time Perception → Identification → Contextual AR → Explore → Ask → Conserve**

There is no required "Scan" button or still-image capture in the intended experience.

When a supported plant enters the live camera view, the perception system should detect/identify it in an appropriately optimized real-time loop. Once the result is sufficiently stable/confident, the AR layer responds.

## Core product idea

Cero is best understood as:

> **A real-time ML perception system with an augmented interface layered over the camera feed.**

The real plant remains the subject.

A 3D plant model is optional. AR value can come from:
- labels
- spatial annotations
- contextual callouts
- information cards
- highlighting
- ecological/conservation overlays
- interactive hotspots
- optional 3D representation

## One-plant vertical slice

The first engineering proof should be one plant end-to-end:

```text
LIVE CAMERA
    ↓
REAL-TIME ML PERCEPTION
    ↓
DETECTION / IDENTITY + CONFIDENCE
    ↓
CLIENT-SIDE STATE
    ↓
CONTEXTUAL AR AUGMENTATION
    ↓
VERIFIED PLANT INFORMATION
    ↓
BOTANICAL GUIDE
    ↓
CONSERVATION
```

Only after this is reliable should the system scale to 7+ species.

## Team ownership

### You — XR / technical lead

Own:
- overall product coherence
- Unity/XR
- camera and AR runtime
- spatial presentation
- AR interaction
- AR UI
- 3D assets/prefabs
- scan/perception experience as presented to the user
- identification-result UX
- contextual AR
- Explore experience
- Botanical Guide UI
- Conservation UI
- Android build/testing
- client-side integration

### S — Software / UI / integration

Own:
- software implementation
- application/service APIs
- backend
- database/data services
- model-serving interface
- AI-provider integration
- deployment
- service-side error handling
- integration contracts

### P — ML / AI

Own:
- plant dataset
- preprocessing
- model choice/training
- evaluation
- inference
- confidence behavior
- perception failure handling
- model improvements

### M — Plant / conservation

Own:
- species research
- scientific verification
- native status
- ecological information
- conservation information
- source verification
- image/reference collection
- testing/logistics support

## Boundary principle

> **S builds the pipes. P builds the brain. M supplies the knowledge. You build the world.**

The boundary is collaborative, not a wall.

S answers:
> Can the services communicate correctly?

You answer:
> Can Unity consume that result and turn it into the correct user experience?

## Core project principles

- Complete before clever.
- Vertical slices before isolated features.
- One owner per outcome.
- AI accelerates; humans verify.
- Do not fake functionality.
- Preserve working systems unless evidence says otherwise.
- Make the smallest change that can test a hypothesis.
- Connect systems early.

## Current technical direction

Android-first is the practical direction.

Unity + AR Foundation + MCP is the current active implementation path, but the original project context recorded Unity vs Unreal as a delivery-risk decision rather than a matter of loyalty.

MCP is development infrastructure, not part of the shipped product.

## Known Unity/XR learning milestones

The Team Cero Unity experiments established:
- baseline Android AR deployment works;
- Mobile AR Template object placement/manipulation works;
- source 3D models do not automatically inherit AR interaction behavior;
- imported source assets can contain dangerous components such as cameras;
- prefab authoring is separate from source asset import;
- the Omnitrix FBX was successfully turned into an AR-compatible prefab;
- the Omnitrix was integrated into the template and successfully tested on a physical Android device for placement, selection, movement, rotation, scaling and deletion;
- rotation and scaling feel can be improved later;
- menu icon generation/integration remains a small UI polish issue.

## Important architecture distinction

```text
SOURCE ASSET
    ↓
IMPORTER
    ↓
IMPORTED UNITY ASSET
    ↓
AUTHORED PREFAB
    ↓
AR / BEHAVIOUR CONFIGURATION
    ↓
TEMPLATE INTEGRATION
    ↓
RUNTIME
```

Do not treat "model imported" as equivalent to "AR object ready".

## Perception architecture is not yet fully locked

Still to be determined with S/P:
- classification vs detection vs segmentation
- temporal tracking/stability strategy
- screen-space vs world-space AR mapping
- on-device vs remote vs hybrid inference
- exact data/API contract

Those are decisions to be made from actual model, device and UX constraints.
