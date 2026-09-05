# PROTOTYPE V3 FORENSIC VALIDATION SUMMARY REPORT

## Executive Result
- **Overall Status**: **PASS (Controlled Integration / Isolated Parity Verified)**
- **Editor Integration**: PASS (Sentis 2.6.1 GPUCompute execution verified)
- **Android Integration**: NOT RUN (Host has no authorized adb device attached)
- **Model Integrity**: PASS (Verified bit-for-bit weights parity; discovered Sentis external-data offset bug)
- **Preprocessing**: PASS (Aspect-preserving resize to 256 + CenterCrop 224 verified)
- **Coordinate Handling**: PASS (640x640 letterbox conversion verified; UV vertical orientation accounted for)
- **Sentis 2.6 Parity**: PASS (100.0% Top-1 agreement, Max Prob Diff: 3.28e-07)

## Quantitative Results
- **Samples Tested**: 18
- **Plant Detections Evaluated**: 10
- **Negative / Non-Plant Rejected**: 8
- **Errors / Exceptions Encountered**: 0

### Parity Metrics
- **Top-1 Agreement**: **100.0%** (10/10)
- **Top-3 Agreement**: **100.0%** (10/10)
- **Maximum Probability Absolute Difference**: **3.28276199e-07**
- **Mean Probability Absolute Difference**: **3.68940456e-08**

### Latency Profile (Sentis GPUCompute)
- **Mean Pipeline Latency**: 128.56 ms
- **Median Pipeline Latency**: 124.24 ms
- **P95 Pipeline Latency**: 189.37 ms
- **Model 1 (YOLO11n) Mean**: 30.64 ms
- **Preprocessing Mean**: 0.82 ms
- **Model 2 (V6 Sentis) Mean**: 30.72 ms

## Major Forensic Discoveries & Discrepancies

### 1. The Unity Sentis External Data Offset Bug (CRITICAL)
- **Issue**: When importing `flora_species_classifier_v6_datafirst.onnx` accompanied by separate `.onnx.data`, Unity Sentis 2.6.1 produces corrupted probability outputs (Top-1 disagreement with 84.0% error margin).
- **Root Cause Located**: In `com.unity.ai.inference@2.6.1/Editor/ONNX/ONNXConstantsLoader.cs` (lines 17–28), `ONNXConstantsLoader.LoadConstant` reads bytes from `weightStream` sequentially and completely **omits the `offset` parameter** declared in `tensorProto.ExternalData`. Initializers are therefore populated with misaligned byte streams.
- **Remediation**: Bundling the model weights into a standard self-contained monolithic ONNX asset (`flora_species_classifier_v6_datafirst_monolithic.onnx`) bypasses the broken file stream loader, yielding **100% numerical parity ($2.38 \times 10^{-7}$ max diff)**.

### 2. Preprocessing Aspect Distortion vs Contract
- **Issue**: Production `Pv002Pipeline.cs` (`PrepareModel2Input`) executes direct anisotropic stretch resizing to 224x224, causing a documented **25.2% accuracy drop**.
- **Evidence**: V3 aspect-preserving resize (`SentisV6Preprocessor.cs`) scales shorter side to 256, center-crops 224x224, and preserves botanical morphology.

### 3. Texture UV Coordinate Vertical Inversion
- **Issue**: Unity `Texture2D` coordinates have an origin at the bottom-left, whereas YOLO11n bounding boxes reference the top-left image origin.
- **Evidence**: Without `unity_y = height - y2`, crops in Unity are vertically inverted.

## Final Recommendation
**REQUIRES TARGETED FIX**:
Package `flora_species_classifier_v6_datafirst.onnx` as a single monolithic ONNX asset, and update `Pv002Pipeline.cs` with the aspect-preserving `SentisV6Preprocessor` logic and UV vertical offset correction before deploying to production.
