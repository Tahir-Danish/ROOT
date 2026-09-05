# Troubleshooting Guide

## 1. ONNX Model Not Loading / Sentis Error
- **Symptom**: Unity console throws `CompileFailed` or `InvalidModelException`.
- **Solution**: Ensure Unity Sentis package is version `2.1.0` or higher. Re-import the ONNX file by right-clicking `flora_localizer_v1_tuned.onnx` -> `Reimport`.

---

## 2. Input Tensor Dimension Mismatch
- **Symptom**: Model 1 throws tensor shape error.
- **Solution**: Verify Model 1 input shape is `[1, 3, 640, 640]` and Model 2 input shape is `[1, 3, 224, 224]`. Check that `PlantPipelineManager.cs` resizes textures correctly.

---

## 3. BGR / RGB Color Mismatch
- **Symptom**: Model 2 classification confidence drops unexpectedly on valid plants.
- **Solution**: Ensure pixel values are passed in RGB order, not BGR. In `PlantPipelineManager.cs`, `TextureFormat.RGB24` guarantees RGB byte ordering.

---

## 4. DroidCam Green-Screen Issue
- **Symptom**: Live camera stream appears completely green or black in Unity Play mode.
- **Solution**: Close Unity Editor, launch DroidCam Client app on PC, connect to phone camera, verify video feed works in DroidCam, then reopen Unity Editor and press Play.

---

## 5. Incorrect Model Filename Reference
- **Symptom**: `FileNotFoundException` or null reference in `PlantPipelineManager`.
- **Solution**: Verify Inspector fields point to `flora_localizer_v1_tuned.onnx` and `flora_species_classifier_v6_autonomous.onnx`.
