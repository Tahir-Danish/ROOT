# FloraSense Prototype V2 — Model Selection Rationale

## Executive Summary
This document records the forensic audit rationale for selecting **Model 1 V1 Tuned** and **Model 2 V6 Autonomous** as the production models for FloraSense Prototype V2.

---

## 1. Selected Production Models

### Model 1: Flora Localizer V1 Tuned
- **Canonical ID**: `M1_V1_TUNED`
- **Source PyTorch Checkpoint**: `runs/detect/plant_localizer_model1_v1_tuned_e40_img640/weights/best.pt`
- **Source SHA256**: `76a34a1034af350da179d33d423c2d984fc65cacb9bb4c8ea91149be5caf17dd`
- **Prototype ONNX Asset**: `prototypev2/models/flora_localizer_v1_tuned.onnx`
- **Exported ONNX SHA256**: `3c1f841c0da136cc8d4e45d06981b5530e9938d57fb38d524bb8c3ac20ad5d4e`
- **Why Selected**: M1 V1 Tuned achieved the strongest overall performance on the 376-image external evaluation set (240 plant-present, 136 non-plant):
  - **98.75%** plant recall @ confidence 0.10
  - **90.83%** plant recall @ confidence 0.25
  - **75.00%** plant recall @ confidence 0.40
  - ~6.2 ms measured inference latency on CPU (YOLO11n architecture)

> [!IMPORTANT]
> `unity_delivery/model1/plant_localizer_model1.onnx` was NOT used for Prototype V2 because SHA256 auditing proved that asset was an older Model 1 V3 ONNX export. `flora_localizer_v1_tuned.onnx` was newly exported directly from `runs/detect/plant_localizer_model1_v1_tuned_e40_img640/weights/best.pt`.

---

### Model 2: Flora Species Classifier V6 Autonomous
- **Canonical ID**: `M2_V6_AUTONOMOUS`
- **Source PyTorch Checkpoint**: `runs/classify/plant_classifier_model2_v6_autonomous/best.pth`
- **Source SHA256**: `2482ff7bbde6ef3e618de674c2b48639e4b511bd5ce31faaba2b2cc6b888b72c`
- **Prototype ONNX Asset**: `prototypev2/models/flora_species_classifier_v6_autonomous.onnx`
- **Verified ONNX SHA256**: `b7f2d0e1850d71d4d7158cb598b215db109df35bbf53ebed2416e087bee1dfb3`
- **Why Selected**: M2 V6 Autonomous provided the best practical balance between genuine target recognition and out-of-distribution (OOD) non-target rejection:
  - **42.96%** target top-1 accuracy without threshold
  - **29.63%** target accuracy @ confidence >= 0.50
  - **18.67%** combined non-target False Acceptance Rate (FAR)
  - **85.29%** non-plant rejection
  - **62.77%** full end-to-end pipeline accuracy

---

## 2. Explicit Rejection of Model 2 V7 ArcFace

- **Candidate**: Model 2 V7 ArcFace (`runs/classify/plant_classifier_model2_v7_arcface_autonomous/best.pth`)
- **Reason for Rejection**:
  - Although V7 achieved slightly higher target acceptance at the 0.50 threshold (32.59%), it suffered from severe out-of-distribution overconfidence.
  - V7 produced a **43.98% combined non-target False Acceptance Rate** (accepting almost half of non-target plants).
  - V7's full end-to-end pipeline accuracy collapsed to **47.61%** (compared to V6's 62.77%).
  - V7 is **NOT** acceptable as a production default.
