# Unity Sentis Setup Instructions

## 1. Prerequisites
- **Unity Engine**: Unity 6 (v6000.0.x) or Unity 2022.3 LTS.
- **Sentis Package**: `com.unity.sentis` version 2.1.0+.
- **Target Platform**: Android (ARM64) or Windows Standalone / Editor.

---

## 2. Package Verification
Open Unity Editor, navigate to **Window -> Package Manager**, select **Unity Registry**, search for **Sentis**, and ensure `2.1.0` or later is installed.
*(Note: If the exact Sentis version differs in your existing Unity project, check Package Manager in your project rather than assuming a fixed version).*

---

## 3. Importing Prototype Assets
1. Copy `prototypev2/` into `<Your_Unity_Project>/Assets/`.
2. Select `flora_localizer_v1_tuned.onnx` in the Unity Project Window and verify the Import Settings:
   - Model Type: ONNX
   - Compute Backend: GPU Compute
3. Select `flora_species_classifier_v6_autonomous.onnx` and verify import settings.

---

## 4. Hierarchy & Component Setup
1. In your AR Scene hierarchy, create an empty GameObject named `PlantPipelineManager`.
2. Attach `prototypev2/integration/PlantPipelineManager.cs`.
3. In the Inspector panel:
   - Set **Active Model 2 Variant**: `V6_PRIMARY`
   - Assign **Model 1 Asset**: `flora_localizer_v1_tuned.onnx`
   - Assign **Model 2 V6 Asset**: `flora_species_classifier_v6_autonomous.onnx`
   - Set **Model 1 Conf Threshold**: `0.40`
   - Set **Model 2 Conf Threshold**: `0.50`
   - Assign UI Text & AR Panel references.
