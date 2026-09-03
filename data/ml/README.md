# ML Dataset Instructions

This directory defines the structure for the plant-identification dataset. It does not contain a trained model or a real image dataset yet.

## Required workflow

1. Choose exactly 7+ MVP species.
2. Assign stable `species_id` values in `class_manifest.json`.
3. Verify species facts independently before adding them to presentation data.
4. Build an image manifest with stable IDs, source/license metadata, and train/validation/test splits.
5. Keep evaluation images separate from training images.
6. Record data quality issues explicitly.
7. Never use an image source in the app without checking its license/usage terms.

## Minimum useful metadata

- image_id
- path or source URI
- species_id
- split
- source
- license
- quality_notes
- verified

## ML/XR contract

Inference should return `species_id`, `confidence`, and `status`. See `docs/AI_ML_CONTRACT.md`.
