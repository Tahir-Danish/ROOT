# UNITY V6 INTEGRATION FORENSIC LOGGING SPECIFICATION (PROTOTYPE V3)

This document establishes the mandatory forensic logging contract for Unity engineers integrating **Team Cero Model 2 V6 Data-First** (`flora_species_classifier_v6_datafirst.onnx`) into Unity using Unity Sentis.

---

## 1. PREPROCESSING CONTRACT (MANDATORY)

To prevent the **25.2% accuracy degradation** caused by direct $224 \times 224$ texture stretching, Unity Sentis preprocessing MUST execute the exact aspect-preserving pipeline:

```
Camera Frame
    ↓
Bounding Box Crop [x1, y1, x2, y2]
    ↓
Resize shorter dimension to 256px (preserving aspect ratio)
    ↓
CenterCrop 224×224
    ↓
Convert RGB float32 [0.0, 1.0]
    ↓
ImageNet Normalization:
    R_norm = (R - 0.485) / 0.229
    G_norm = (G - 0.456) / 0.224
    B_norm = (B - 0.406) / 0.225
    ↓
TensorFloat [1, 3, 224, 224] (NCHW)
```

---

## 2. PER-SAMPLE FORENSIC LOGGING REQUIREMENTS

For **every single inference sample**, Unity must record and export the following fields in JSONL format:

### A. RUN METADATA
- `test_run_id`: Unique GUID string per execution session.
- `timestamp`: UTC ISO-8601 string.
- `unity_version`: Unity Editor / Runtime version string (e.g. `6000.0.32f1`).
- `sentis_version`: Unity Sentis package version (e.g. `2.1.0-exp.1`).
- `platform`: `Android`, `iOS`, `Editor`, etc.
- `device_model`: Hardware device identification string.
- `backend_type`: Sentis `BackendType` (`GPUCompute`, `CPU`, `PixelShader`).
- `model_filename`: `flora_species_classifier_v6_datafirst.onnx`.
- `model_sha256`: `332e0ef013ca846000c154bbaf194dafff1edffa3d8a64308fad27b4b5e17dbe`.

### B. CAMERA / FRAME DATA
- `frame_id`: Sequential integer.
- `source_width`: Original frame width in pixels.
- `source_height`: Original frame height in pixels.
- `camera_orientation`: `LandscapeLeft`, `Portrait`, etc.
- `rotation_degrees`: 0, 90, 180, or 270.
- `is_mirrored`: Boolean indicating front-facing mirror state.
- `texture_format`: E.g. `RGBA32`, `RGB24`, `YUV420p`.

### C. MODEL 1 DETECTION
- `m1_detected`: Boolean.
- `m1_confidence`: Float detection confidence score $[0.0, 1.0]$.
- `m1_box_raw`: Bounding box array $[cx, cy, w, h]$ in $640 \times 640$ letterboxed space.
- `m1_box_pixels`: Bounding box array $[x1, y1, x2, y2]$ in original image pixels.
- `crop_width`: Bounding box crop width.
- `crop_height`: Bounding box crop height.
- `crop_occupancy_pct`: Estimated foliage pixel occupancy percentage.

### D. MODEL 2 PREPROCESSING
- `crop_original_aspect`: Float aspect ratio ($\text{width} / \text{height}$).
- `aspect_resize_width`: Width after scaling shorter dimension to 256.
- `aspect_resize_height`: Height after scaling shorter dimension to 256.
- `centercrop_x1_y1`: Top-left offset of $224 \times 224$ center crop within $256$ scaled image.
- `color_format`: `"RGB"`.
- `tensor_layout`: `"NCHW"`.
- `tensor_shape`: `[1, 3, 224, 224]`.
- `tensor_min`: Minimum normalized float value in tensor.
- `tensor_max`: Maximum normalized float value in tensor.
- `tensor_mean`: Mean normalized float value in tensor.
- `texture_vertically_flipped`: Boolean indicating UV inversion handling.

### E. MODEL 2 INFERENCE
- `inference_latency_ms`: Execution duration in milliseconds.
- `probabilities`: 10-element float array of specialist species probabilities.
- `top1_index`: Integer class index $[0..9]$.
- `top1_species`: String species identifier from authoritative mapping.
- `top1_confidence`: Top-1 probability score.
- `top2_species`: String species identifier for second highest prediction.
- `top2_confidence`: Top-2 probability score.
- `accepted`: Boolean (`top1_index != 9 AND top1_confidence >= 0.50`).
- `rejection_reason`: `"NONE"`, `"EXPLICIT_UNKNOWN_PLANT"`, or `"BELOW_CONFIDENCE_THRESHOLD"`.

### F. END-TO-END LATENCY TIMING
- `t_camera_acquisition_ms`: Latency to capture texture.
- `t_m1_inference_ms`: Latency to execute Model 1 YOLO11n worker.
- `t_crop_extraction_ms`: Latency to compute coordinates and blit ROI.
- `t_preprocessing_ms`: Latency to scale, crop, and normalize tensor.
- `t_m2_inference_ms`: Latency to execute Model 2 Sentis worker.
- `t_total_pipeline_ms`: Total end-to-end latency.

### G. ERRORS AND ANOMALIES
- `has_error`: Boolean.
- `error_type`: `"NONE"`, `"MODEL1_FAILURE"`, `"INVALID_CROP"`, `"TENSOR_SHAPE_MISMATCH"`, `"NAN_PROBABILITY"`.
- `error_message`: Detailed exception text if applicable.

### H. FORENSIC SAMPLE ARTIFACTS
For every evaluation run, Unity must output:
1. `crop_samples/{image_id}_crop.jpg`: Raw unnormalized Model 2 crop image immediately prior to tensor creation.
2. `INFERENCE_RESULTS.jsonl`: Complete per-sample machine-readable log.
3. `RUN_METADATA.json`: System and run summary.
4. `SUMMARY.md`: Human-readable evaluation overview.

---

## 3. AUTHORITATIVE 10-CLASS MAPPING CONTRACT

```
0 -> 01_alstonia_scholaris
1 -> 02_justicia_adhatoda
2 -> 03_acalypha_indica
3 -> 04_mimusops_elengi
4 -> 05_caryota_urens
5 -> 07_strobilanthes_kunthiana
6 -> 08_plumeria_alba
7 -> 09_bougainvillea_spectabilis
8 -> 10_gardenia_jasminoides
9 -> unknown_plant
```
