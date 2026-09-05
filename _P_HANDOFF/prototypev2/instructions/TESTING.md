# Testing Procedure & Verification Matrix

## 1. Verified on Linux / Python
- [x] **Model 1 Export**: Exported `runs/detect/plant_localizer_model1_v1_tuned_e40_img640/weights/best.pt` to `flora_localizer_v1_tuned.onnx`. Verified ONNX Runtime input `[1, 3, 640, 640]` and output `[1, 5, 8400]`.
- [x] **Model 2 Copy & SHA256**: Copied `unity_delivery/model2/plant_classifier_model2.onnx` to `flora_species_classifier_v6_autonomous.onnx`. SHA256 matches `b7f2d0e1...`.
- [x] **Reference Inference**: Executed `prototypev2/integration/reference_inference.py` on test crops. Verified end-to-end execution.

---

## 2. Requires Unity Editor Verification (On Windows / macOS Machine)
- [ ] **Asset Import**: Import `prototypev2/` folder into Unity Editor.
- [ ] **C# Compilation**: Verify `PlantPipelineManager.cs` compiles without errors.
- [ ] **Sentis Worker Initialization**: Verify Sentis GPU workers load `flora_localizer_v1_tuned.onnx` and `flora_species_classifier_v6_autonomous.onnx`.
- [ ] **Play Mode Webcam Test**: Run in Play mode with live webcam feed. Verify Model 1 detects plants, crops ROI, and passes crop to Model 2.
- [ ] **AR Card UI Display**: Verify target species label and confidence percentage display on UI panel when accepted ($\ge 0.50$).

---

## 3. Requires Physical Mobile Device Verification
- [ ] **Android Build**: Build APK for Android device.
- [ ] **Real Camera Latency**: Measure real-time FPS and latency on target smartphone hardware.
- [ ] **Non-Plant Rejection**: Verify that pointing camera at non-plant objects (desks, walls, keyboards) returns `NO_PLANT` or `UNKNOWN_PLANT`.
