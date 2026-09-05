# Model Usage Specification

## 1. Model 1 (Plant Localizer)
- **Asset**: `models/flora_localizer_v1_tuned.onnx`
- **Task**: Object Detection (`plant`)
- **Input**: `images` tensor `[1, 3, 640, 640]` RGB float32 normalized to `[0.0, 1.0]`.
- **Output**: `output0` tensor `[1, 5, 8400]` containing bounding box center coordinates `(cx, cy, w, h)` in 640x640 letterbox space and plant confidence score.
- **Post-Processing**: Filter anchors where `conf >= 0.40`. Convert letterboxed bounds back to original frame pixels `(x1, y1, x2, y2)`.

---

## 2. Model 2 (Species Identifier)
- **Asset**: `models/flora_species_classifier_v6_autonomous.onnx`
- **Task**: 10-Class Specialist Species Classification
- **Input**: `image` tensor `[1, 3, 224, 224]` RGB float32 ImageNet normalized.
- **Output**: `probabilities` tensor `[1, 10]` Softmax probabilities.
- **Decision Rule**: Accept species if `top1_idx != 9` AND `top1_probability >= 0.50`. Otherwise return `UNKNOWN_PLANT`.
