# FORENSIC EVALUATION & HANDOFF REPORT: MODEL 2 V6 DATA-FIRST

> [!WARNING]
> **PRODUCTION STATUS**: **V6 IS NOT CERTIFIED PRODUCTION-READY AND IS NOT UNIVERSALLY ACCURATE.**
> - **[Verified Fact]**: Monolithic ONNX packaging resolves the catastrophic Unity Sentis 2.6.1 external-data loader bug, enabling stable execution.
> - **[Measured Result]**: Isolated mathematical parity between Python ONNX Runtime (CPU) and Unity Sentis 2.6.1 (`GPUCompute`) achieves $100.0\%$ Top-1 agreement with maximum absolute difference of $3.28 \times 10^{-7}$.
> - **[Measured Result]**: Physical device testing against plant imagery displayed on a secondary phone screen revealed dynamic classification instability with transitions between `unknown_plant` and `05_caryota_urens` at specialist confidences of **~62% to 86%**.
> - **[Verified Fact]**: The Solarpunk 3D AR card spawned and remained stably spatially anchored at its native ARAnchor point during these transitions, with its content updating dynamically as classifier and state changed.
> - **[Unverified Hypothesis]**: The hypothesis that secondary display refresh rates, pixel lattices, or optical Moiré patterns are the root cause triggering the *Caryota urens* feature extractor remains an unverified hypothesis pending controlled optical ablation.
> - **[Verified Fact]**: In-situ validation across all 9 target native species under diverse outdoor lighting and arboretum conditions has not been performed; universal accuracy cannot be claimed.

---

## Epistemological Categorization Taxonomy
To maintain forensic rigor and eliminate ambiguity, every finding, measurement, and technical statement in this report is explicitly classified into one of the following four epistemological tiers:
1. **`[Verified Fact]`**: Directly observed, deterministic system behavior verified through code audit, version control records, cryptographic hashes, or system architecture contracts.
2. **`[Measured Result]`**: Quantitative empirical data recorded via instrumented benchmarks, high-precision timers, or unaltered device telemetry logs.
3. **`[Suspected Cause]`**: Plausible technical explanations supported by partial empirical evidence or known mechanisms, but where confounding factors have not been fully isolated.
4. **`[Unverified Hypothesis]`**: Plausible conjectures or working hypotheses that have **not** been experimentally isolated or proven by controlled ablation.

---

## 1. Executive Summary & Context

This package delivers the complete, unedited forensic telemetry, benchmark data, on-device logs, physical device screenshots, and raw artifact outputs evaluated by the Team Cero engineering team for the **Model 2 V6 Data-First Specialist Classifier** (`plant_classifier_model2_v6.onnx` / `flora_species_classifier_v6_datafirst.onnx`).

### Package Directory Structure
```
V6_FORENSIC_HANDOFF_TO_P/
├── HANDOFF_TO_P.md                               (This forensic report)
├── metadata/
│   ├── model_hashes.json                         (SHA256, byte sizes, GUIDs for all ONNX assets)
│   ├── class_mapping.json                        (Authoritative 10-class mapping contract)
│   └── model_contract.json                       (V3 architecture and preprocessing contract)
├── run1_isolated_parity_v3/                      (Controlled Editor & PyTorch/ORT Parity Evaluation)
│   ├── SUMMARY.md                                (Executive summary of isolated benchmark)
│   ├── RUN_METADATA.json                         (Test run parameters, environment, commit tags)
│   ├── PARITY_COMPARISON.csv                     (Side-by-side numerical comparison per sample)
│   ├── INFERENCE_RESULTS.csv                     (Full sample-by-sample tabular outputs)
│   ├── INFERENCE_RESULTS.jsonl                   (Detailed per-inference JSONL telemetry)
│   ├── MODEL_OUTPUTS.jsonl                       (Raw probability vectors for all 10 classes)
│   ├── PERFORMANCE.csv                           (Latency breakdown: M1, crop, M2, total)
│   ├── PREPROCESSING_RECORDS.jsonl               (Crop coordinates, tensor min/max/mean, aspect ratio)
│   ├── ERRORS.csv                                (Logged exceptions and anomalies)
│   └── crop_samples/                             (Input crops SMP_0001 through SMP_0017)
├── run2_live_android_device/                     (Physical Motorola Edge 60 Fusion On-Device Evaluation)
│   ├── ANDROID_V6_FORENSIC_EVALUATION.md         (Comprehensive on-device forensic analysis)
│   ├── android_v6_summary.json                   (Statistical summary of on-device telemetry)
│   ├── android_v6_samples.jsonl                  (38 chronological on-device inference cycles)
│   ├── raw_android_logcat.log                    (Raw unedited 1.53 MB ADB logcat from application session)
│   └── screenshots/                              (Full-resolution on-device PNG captures)
│       ├── screenshot_01_initial_detection.png   (Live camera frame, M1 bounding box, Solarpunk card)
│       └── screenshot_02_anchored_eval.png       (Printout testing showing persistent spatial anchor)
└── field_observations/                           (Physical Device Observations: Secondary Screen & Cards)
    ├── FIELD_OBSERVATIONS_INDEX.md               (Catalog of observations, image hashes, and metrics)
    ├── media_1788592402311.png                   (Unconfirmed target acquisition, M1 confidence 58.9%)
    ├── media_1788592529128.png                   (Spatial AR card displaying Unknown Species / Pending)
    ├── media_1788592550131.png                   (Framing plant imagery displayed on secondary screen)
    ├── media_1788592574641.png                   (Spatial card tethered to anchored specimen during motion)
    ├── media_1788592628768.png                   (Detection visualizer overlay on secondary screen target)
    ├── media_1788592662936.png                   (Host logcat terminal capturing perception state updates)
    ├── media_1788592697318.png                   (Spatial reticle and tether alignment during acquisition)
    ├── media_1788592741418.png                   (Solarpunk glass chassis close-up and typography audit)
    ├── media_1788592786696.png                   (Landscape orientation test framing secondary screen)
    ├── media_1788592836531.png                   (UI Bridge Online status scan over background clutter)
    └── media_1788592885495.png                   (Unaltered verification capture matching media_1788592628768)
```

---

## 2. What Was Actually Tested

1. **Run 1: Isolated Parity & Preprocessing Benchmark (Unity Editor / PC Host)**:
   - **[Verified Fact]**: Evaluated 18 distinct test scenarios (10 positive plant frames with valid detections, 8 negative/non-plant frames, across landscape, portrait, inverted, and mirrored orientations).
   - **[Verified Fact]**: Executed side-by-side identical input crops through Python ONNX Runtime (CPU) vs. Unity Sentis 2.6.1 (`GPUCompute` backend on Direct3D12).
   - **[Verified Fact]**: Validated the V6 aspect-preserving preprocessing contract (resize shorter side to 256, center crop 224×224, ImageNet normalization, float32 NCHW).
   - **[Verified Fact]**: Evaluated both the split external-data ONNX asset (`flora_species_classifier_v6_datafirst.onnx` + `.data`) and the self-contained monolithic ONNX asset (`flora_species_classifier_v6_datafirst_monolithic.onnx`).
2. **Run 2: Live On-Device Execution (Motorola Edge 60 Fusion Hardware)**:
   - **[Verified Fact]**: Deployed release production APK (`TeamCero_V6_Production.apk`, IL2CPP arm64-v8a) to physical Motorola Edge 60 Fusion (Android 16 / API 36).
   - **[Verified Fact]**: Tested real-time camera stream acquisition ($640 \times 480$ RGBA32 conversion).
   - **[Verified Fact]**: Evaluated the complete real-time pipeline: Model 1 Localizer (YOLO11n) $\to$ NMS $\to$ Aspect-Preserving Crop Preprocessing $\to$ Model 2 Sentis (`GPUCompute` on Snapdragon Adreno GPU) $\to$ Perception State Machine $\to$ ARUX Spatial Candidate Confirmation Gate $\to$ Native ARAnchor attachment $\to$ Solarpunk 3D UI presentation.
   - **[Measured Result]**: Sampled 38 continuous real-world pipeline cycles pointing at physical background clutter, desk surfaces, and printed botanical reference sheets.
3. **Field Observations: Secondary Phone-Screen Target Evaluation**:
   - **[Verified Fact]**: Tested physical presentation of botanical reference imagery displayed on another mobile phone screen held in front of the Motorola test device.
   - **[Verified Fact]**: Tested AR spatial tracking behavior, anchor stability, and UI card response when observing dynamic transitions between `unknown_plant` and specialist candidate classifications.

---

## 3. What Was Measured

- **Mathematical Parity**: Maximum absolute probability difference, mean absolute probability difference, Top-1 categorical agreement, and Top-3 categorical agreement between reference Python ORT and Unity Sentis.
- **Preprocessing Latency**: High-precision stopwatch timing for bounding box extraction, short-side aspect scaling, center crop indexing, and ImageNet normalization.
- **Inference Latency**:
  - In-Editor: Sentis `GPUCompute` execution time on Direct3D12.
  - On-Device: Hardware `GPUCompute` execution time on Snapdragon Adreno GPU.
- **End-to-End Pipeline Latency**: Model 1 execution, NMS postprocessing, crop preprocessing, Model 2 inference, state machine dispatch, and total frame turnaround time.
- **Categorical Output Distributions**: Top-1 species ID, Top-1 confidence score, acceptance/rejection decisions against the $0.50$ threshold.
- **AR Spatial Integration**: Candidate observation count, spatial jump distance, confirmation duration, raycast hit distance, and native ARAnchor tracking stability.

---

## 4. What Passed

1. **Monolithic Model Mathematical Parity**:
   - **[Measured Result]**: **Top-1 Categorical Agreement**: **100.0%** (10/10 evaluated crops).
   - **[Measured Result]**: **Top-3 Categorical Agreement**: **100.0%** (10/10 evaluated crops).
   - **[Measured Result]**: **Maximum Absolute Probability Difference**: **$3.28 \times 10^{-7}$** (attributable to standard single-precision floating point rounding).
   - **[Measured Result]**: **Mean Absolute Probability Difference**: **$3.69 \times 10^{-8}$**.
   - *Evidence*: [`run1_isolated_parity_v3/PARITY_COMPARISON.csv`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/PARITY_COMPARISON.csv)
2. **On-Device Preprocessing Efficiency**:
   - **[Measured Result]**: The direct-mapping aspect-preserving formula (short side 256, center crop 224×224) executes in **1.0 to 2.0 ms** on the Motorola Edge 60 Fusion with **zero heap allocations** inside the inner processing loop.
   - *Evidence*: [`run2_live_android_device/android_v6_summary.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/android_v6_summary.json)
3. **Sentis GPUCompute Stability on Mobile Hardware**:
   - **[Measured Result]**: Model 2 Sentis 2.6.1 executed consistently in **215–251 ms** on the physical device with **zero crashes, zero shader compilation exceptions, and zero NaN/Inf outputs**.
   - *Evidence*: [`run2_live_android_device/raw_android_logcat.log`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/raw_android_logcat.log)
4. **General Background Clutter Rejection**:
   - **[Measured Result]**: 100% of non-plant background clutter frames (37/37 cycles) were rejected as `unknown_plant` with confidences ranging from $0.27$ to $0.70$, preventing false-positive activations on household items.
   - *Evidence*: [`run2_live_android_device/android_v6_samples.jsonl`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/android_v6_samples.jsonl)
5. **AR Spatial Card Anchoring & Persistence**:
   - **[Verified Fact]**: The Solarpunk 3D AR card spawned upon candidate confirmation and remained rigidly anchored in 3D world space at its original native ARAnchor point.
   - **[Verified Fact]**: Moving and rotating the camera around the target maintained spatial locking without card duplication, jitter, or anchoring drift.
   - *Evidence*: [`run2_live_android_device/screenshots/screenshot_02_anchored_eval.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/screenshots/screenshot_02_anchored_eval.png), [`field_observations/media_1788592574641.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592574641.png)

---

## 5. What Failed & Critical Vulnerabilities

1. **Unity Sentis 2.6.1 External Data Loader Bug**:
   - **[Measured Result]**: The original split ONNX model package (`flora_species_classifier_v6_datafirst.onnx` with external `.data`) **fails catastrophically in Unity Sentis 2.6.1**, resulting in an **84.0% error margin** and complete Top-1 output corruption.
   - **[Verified Fact]**: In `com.unity.ai.inference@2.6.1/Editor/ONNX/ONNXConstantsLoader.cs` (lines 17–28), Unity's loader reads weights sequentially starting at file offset 0 and completely ignores the explicit `offset` field declared in `tensorProto.ExternalData`.
   - **[Verified Fact]**: Repacking the model into a standard monolithic ONNX file with embedded `raw_data` completely resolved the issue, recovering full mathematical parity.
   - *Evidence*: [`metadata/model_hashes.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/metadata/model_hashes.json), [`run1_isolated_parity_v3/SUMMARY.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/SUMMARY.md)

2. **Secondary Phone-Screen $\leftrightarrow$ Caryota urens / Unknown Plant Classification Instability**:
   - **[Measured Result]**: When framing plant imagery displayed on a secondary phone screen, the pipeline exhibited rapid alternating transitions between `unknown_plant` and **`05_caryota_urens`** (Class 4).
   - **[Measured Result]**: During periods where `05_caryota_urens` was selected, the specialist classifier output confidences in the range of **~62% to 86%**, easily surpassing the $0.50$ acceptance threshold.
   - **[Verified Fact]**: The physical target was not a live botanical specimen, but plant imagery displayed on a secondary smartphone display.
   - **[Verified Fact]**: The Solarpunk 3D AR card remained stably spatially anchored at its physical target position throughout these classification flips.
   - **[Verified Fact]**: The AR card content dynamically updated in real time as the classifier output changed, transitioning between "Unknown Species / Data Unavailable / Identification Pending" and species metadata without breaking spatial tracking or creating duplicate cards.
   - **[Suspected Cause]**: Visual similarity between the repetitive texture of dense foliage in the displayed imagery and the characteristic bipinnate leaflet structure of *Caryota urens*.
   - **[Unverified Hypothesis]**: The hypothesis that high-frequency display refresh rates, pixel grid lattices, or optical Moiré patterns are the root cause triggering the *Caryota urens* feature extractor remains an unverified hypothesis.
   - *Detailed Analysis in Section 7.*
   - *Evidence*: [`field_observations/FIELD_OBSERVATIONS_INDEX.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/FIELD_OBSERVATIONS_INDEX.md), [`field_observations/media_1788592529128.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592529128.png), [`field_observations/media_1788592550131.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592550131.png), [`run1_isolated_parity_v3/crop_samples/SMP_0003_crop.jpg`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/crop_samples/SMP_0003_crop.jpg)

---

## 6. What Was Not Measured (Telemetry Blind Spots)

To preserve mobile runtime performance and eliminate garbage collection overhead on the test device, `Pv002Pipeline.cs` intentionally omits non-essential debug telemetry during production runs:

1. **On-Device Top-2 and Top-3 Categorical Probabilities**:
   - **[Verified Fact]**: Only the winning `maxIndex` and `maxProb` are extracted in production C# code. Secondary and tertiary class confidences are not sorted or logged at runtime on Android.
2. **On-Device Full 10-Class Probability Array**:
   - **[Verified Fact]**: Complete probability vectors are captured in Run 1 (isolated benchmark), but are not streamed over Android logcat during live camera sessions.
3. **On-Device Preprocessed Float32 Tensor Statistics**:
   - **[Verified Fact]**: Min, max, and mean tensor statistics are not computed during live AR rendering to maintain zero heap allocations.
4. **Physical In-Situ Validation Across All 9 Specialist Species**:
   - **[Verified Fact]**: Testing was conducted in indoor laboratory environments using physical foliage, printed reference specimens, and secondary phone screens. Controlled field testing on live wild specimens (*Alstonia scholaris*, *Mimusops elengi*, *Strobilanthes kunthiana*, etc.) in natural outdoor lighting remains unperformed.

---

## 7. Forensic Deep-Dive: Secondary Phone-Screen Target Evaluation & Caryota Transitions

During physical evaluation on the Motorola Edge 60 Fusion, a notable interaction between the V6 classifier and secondary phone-screen imagery was observed:

### Empirical Observations
1. **Target Modality**:
   - Plant imagery displayed on another mobile phone screen was held in front of the Motorola test device's camera.
2. **Observed Transitions**:
   - **[Measured Result]**: The pipeline alternated between two primary states:
     - **State A (`unknown_plant`)**: Specialist classifier assigned Top-1 to class 9 (`unknown_plant`) or all classes fell below the acceptance threshold.
     - **State B (`05_caryota_urens`)**: Specialist classifier assigned Top-1 to class 4 (`05_caryota_urens`) with confidences measured between **~62% and 86%**.
3. **AR Card & Tracking Behavior**:
   - **[Verified Fact]**: When the target was initially acquired, the 3D Solarpunk AR card spawned and anchored to a native ARAnchor generated from AR Foundation plane/feature raycasts.
   - **[Verified Fact]**: As the device translated and rotated around the target phone screen, the AR card remained rigidly locked to its 3D world anchor without spatial drift or popping.
   - **[Verified Fact]**: When the classification output flipped between `unknown_plant` and `05_caryota_urens`, the card content updated dynamically:
     - During `unknown_plant`: Displayed "Unknown Species / Data Unavailable / Identification Pending" with amber status indicator.
     - During `05_caryota_urens`: Displayed *Caryota urens* botanical profile with teal confirmed indicator.
     - The perception state machine updated the existing card instance in place; no duplicate cards were instantiated.
4. **Corroborating Evidence from Isolated Parity Run (`SMP_0003`)**:
   - **[Measured Result]**: In sample `SMP_0003` ([`run1_isolated_parity_v3/crop_samples/SMP_0003_crop.jpg`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/crop_samples/SMP_0003_crop.jpg)), a photograph of a generic shade tree displayed on a screen produced:
     - `05_caryota_urens`: $38.95\%$ (Top-1)
     - `unknown_plant`: $24.85\%$ (Top-2)
     - `07_strobilanthes_kunthiana`: $17.11\%$ (Top-3)
   - While `SMP_0003` fell below the $0.50$ threshold ($38.95\% < 50.0\%$), the live phone screen presentation pushed confidences well above $0.50$ (up to $86\%$).

### Rigorous Epistemological Audit of Causal Claims
- **[Verified Fact]**: The physical target was a secondary digital phone screen displaying plant imagery.
- **[Measured Result]**: Specialist confidences of ~62% to 86% were recorded for `05_caryota_urens` on secondary phone-screen imagery.
- **[Suspected Cause]**: The model's feature extractor may be associating repetitive fine foliage textures (leaflets, branchlets) with the bipinnate fishtail structure of *Caryota urens*, indicating insufficient training regularization against general non-target foliage.
- **[Unverified Hypothesis]**: The hypothesis that high-frequency display refresh rates (e.g. 60Hz/120Hz PWM flicker), subpixel RGB lattices, or optical Moiré interference patterns are the root cause triggering the *Caryota urens* classification is **strictly an unverified hypothesis**. This has not been experimentally proven because no controlled optical ablation (such as testing identical images on matte paper vs. high-DPI OLED vs. varying camera shutter speeds) has been conducted to isolate the display medium from the visual content.

---

## 8. Evidence File Traceability Matrix

Every statement, finding, and metric in this report is directly traceable to the archived evidence files:

| Finding / Telemetry Item | Category | Primary Evidence File | Supporting Evidence File |
| :--- | :--- | :--- | :--- |
| **Model Weight Bit-for-Bit Parity** | `[Verified Fact]` | [`metadata/model_hashes.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/metadata/model_hashes.json) | [`run1_isolated_parity_v3/RUN_METADATA.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/RUN_METADATA.json) |
| **Sentis External-Data Bug Root Cause** | `[Verified Fact]` | [`run1_isolated_parity_v3/SUMMARY.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/SUMMARY.md) (Section 1) | [`run1_isolated_parity_v3/ERRORS.csv`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/ERRORS.csv) |
| **Sentis External-Data Error Margin ($84.0\%$)** | `[Measured Result]` | [`run1_isolated_parity_v3/SUMMARY.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/SUMMARY.md) | [`metadata/model_hashes.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/metadata/model_hashes.json) |
| **Monolithic Mathematical Parity ($100\%$, $3.28 \times 10^{-7}$)** | `[Measured Result]` | [`run1_isolated_parity_v3/PARITY_COMPARISON.csv`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/PARITY_COMPARISON.csv) | [`run1_isolated_parity_v3/INFERENCE_RESULTS.jsonl`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/INFERENCE_RESULTS.jsonl) |
| **Preprocessing Contract (256 Short / 224 Center / ImageNet)** | `[Verified Fact]` | [`run1_isolated_parity_v3/PREPROCESSING_RECORDS.jsonl`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/PREPROCESSING_RECORDS.jsonl) | [`metadata/model_contract.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/metadata/model_contract.json) |
| **Isolated Screen-Crop Bias (`SMP_0003` $\to$ Caryota $38.95\%$)** | `[Measured Result]` | [`run1_isolated_parity_v3/MODEL_OUTPUTS.jsonl`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/MODEL_OUTPUTS.jsonl) (Sample 3) | [`run1_isolated_parity_v3/crop_samples/SMP_0003_crop.jpg`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run1_isolated_parity_v3/crop_samples/SMP_0003_crop.jpg) |
| **On-Device Preprocessing Latency ($1.0–2.0 \text{ ms}$)** | `[Measured Result]` | [`run2_live_android_device/android_v6_summary.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/android_v6_summary.json) | [`run2_live_android_device/raw_android_logcat.log`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/raw_android_logcat.log) |
| **On-Device Inference Latency ($215–251 \text{ ms}$)** | `[Measured Result]` | [`run2_live_android_device/android_v6_summary.json`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/android_v6_summary.json) | [`run2_live_android_device/ANDROID_V6_FORENSIC_EVALUATION.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/ANDROID_V6_FORENSIC_EVALUATION.md) |
| **Background Clutter Rejection ($100\%$ Unknown)** | `[Measured Result]` | [`run2_live_android_device/android_v6_samples.jsonl`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/android_v6_samples.jsonl) | [`run2_live_android_device/screenshots/screenshot_01_initial_detection.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/screenshots/screenshot_01_initial_detection.png) |
| **Secondary Phone Screen Plant Imagery Presentation** | `[Verified Fact]` | [`field_observations/FIELD_OBSERVATIONS_INDEX.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/FIELD_OBSERVATIONS_INDEX.md) | [`field_observations/media_1788592550131.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592550131.png) |
| **Observed Unknown $\leftrightarrow$ Caryota Transitions (~62–86%)** | `[Measured Result]` | [`field_observations/FIELD_OBSERVATIONS_INDEX.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/FIELD_OBSERVATIONS_INDEX.md) | [`field_observations/media_1788592662936.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592662936.png) |
| **AR Card Spatial Anchoring Stability (Zero Drift)** | `[Verified Fact]` | [`field_observations/media_1788592574641.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592574641.png) | [`run2_live_android_device/screenshots/screenshot_02_anchored_eval.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/run2_live_android_device/screenshots/screenshot_02_anchored_eval.png) |
| **Dynamic Card UI Content Updating on State Transitions** | `[Verified Fact]` | [`field_observations/media_1788592529128.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592529128.png) | [`field_observations/media_1788592741418.png`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/field_observations/media_1788592741418.png) |
| **Display Refresh / Pixel Lattice Root Cause** | `[Unverified Hypothesis]` | [`HANDOFF_TO_P.md`](file:///D:/Unity/Projects/Team_Cero/V6_FORENSIC_HANDOFF_TO_P/HANDOFF_TO_P.md) (Section 7) | Requires controlled optical ablation |

---

## 9. Next Required Engineering Actions (For P)

1. **Deliver Monolithic ONNX Exports Only**:
   Discontinue split `.onnx` + `.onnx.data` distribution for Unity Sentis integration targets.
2. **Retrain / Fine-Tune with Screen and Hard-Negative Distractors**:
   Augment the training dataset with hard-negative images of plant foliage displayed on screens and printouts to suppress spurious activations on non-target vegetation.
3. **Calibrate Class-Specific Confidence Thresholds**:
   A uniform $0.50$ threshold is insufficient for deployment. Sensitive classes (*Caryota urens*, *Bougainvillea*) require tighter acceptance thresholds ($\ge 0.70$), while well-isolated classes can operate at $0.50$.
4. **Field Validation Campaign**:
   Validate against genuine botanical specimens in authentic outdoor daylight and arboretum settings before requesting production sign-off.
