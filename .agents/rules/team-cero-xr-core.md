# Team Cero XR Core — Updated Rules

## Mission

Build a reliable AR + AI field-intelligence experience for native plants.

Core experience:

**Live camera → perception → identification/confidence → contextual AR augmentation → exploration → Botanical Guide → conservation.**

A manual Scan/capture workflow is not the intended product interaction.

## Context first

Before any meaningful change:
1. understand the product outcome;
2. read relevant project docs;
3. inspect live Unity state;
4. identify responsible layer;
5. separate fact from hypothesis;
6. select the smallest safe experiment.

## Technical source hierarchy

Prefer:
1. official Unity / XRI / AR Foundation / Google ARCore documentation;
2. actual package/template source code;
3. live Team Cero project;
4. controlled experiments;
5. agent inference.

Do not silently promote inference to fact.

## Technical references

- `docs/UNITY_XR_FUNDAMENTALS.md`
- `docs/MOBILE_AR_TEMPLATE_REVERSE_ENGINEERING.md`
- `docs/AR_OBJECT_CONTRACT.md`
- `docs/PROJECT_CONTEXT.md`
- `docs/PRODUCT_SPEC.md`
- `docs/XR_ARCHITECTURE.md`
- `docs/WORKING_STATE.md`
- `docs/AGENT_CONTEXT_AND_WORKFLOW.md`
- `docs/CERO_REALTIME_PERCEPTION_LEARNING_PLAN.md`
- `docs/TECHNICAL_CHALLENGES_AND_DECISIONS.md`
- `docs/AGENT_TASK_PROTOCOL.md`

## Investigation safety

Read-only means:
- no scene edits
- no prefab edits
- no asset edits
- no package changes
- no settings changes
- no scripts
- no reports unless requested

## Existing-system preference

Reuse/extend the current Mobile AR Template and installed packages.

Do not add packages or replace architecture without explicit technical justification.

## Experiment discipline

```text
Understand → Inspect → Hypothesize → Minimal experiment → Verify → Device test → Record
```

Isolate variables.

## Real-time perception

The desired product loop is:
```text
live camera → perception → detection state → AR augmentation
```

Do not confuse classification, detection, segmentation and tracking.

Do not treat image coordinates as world coordinates without a mapping step.

## Asset discipline

Treat source asset, imported asset, authored prefab and template integration as separate stages.

Imported 3D content is not automatically an AR object.

## Verification discipline

Always distinguish editor, build and device verification.

## Product discipline

First objective is one plant end-to-end.

Do not expand to seven species or optional innovation until the core vertical slice works.

A 3D plant model is optional. Contextual AR augmentation is core.

## Core sentence

> Cero continuously perceives the world, recognizes a plant, and augments the user's understanding of it.
