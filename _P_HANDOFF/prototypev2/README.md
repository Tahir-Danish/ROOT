# FloraSense Prototype V2 — Standalone Delivery Package

## 1. Prototype Information
- **Package Name**: FloraSense Prototype V2
- **Purpose**: Clean production prototype package containing ONLY the audited best plant localization and species classification models and Unity integration instructions.
- **Target Runtime**: Unity 6 / Unity Sentis (v2.1+ / ONNX)

---

## 2. Included Production Models

### Production Model 1 (Plant Localizer)
- **Filename**: `models/flora_localizer_v1_tuned.onnx`
- **Source Checkpoint**: `runs/detect/plant_localizer_model1_v1_tuned_e40_img640/weights/best.pt`
- **Source SHA256**: `76a34a1034af350da179d33d423c2d984fc65cacb9bb4c8ea91149be5caf17dd`
- **Exported ONNX SHA256**: `3c1f841c0da136cc8d4e45d06981b5530e9938d57fb38d524bb8c3ac20ad5d4e`
- **Architecture**: YOLO11n Object Detector (`plant` single class)
- **Input Tensor**: `images` `[1, 3, 640, 640]` RGB float32 `[0.0, 1.0]`
- **Output Tensor**: `output0` `[1, 5, 8400]` (`cx`, `cy`, `w`, `h`, `confidence`)
- **Operating Threshold**: Confidence Threshold = `0.40`

### Production Model 2 (Species Classifier)
- **Filename**: `models/flora_species_classifier_v6_autonomous.onnx`
- **Source Checkpoint**: `runs/classify/plant_classifier_model2_v6_autonomous/best.pth`
- **Source SHA256**: `2482ff7bbde6ef3e618de674c2b48639e4b511bd5ce31faaba2b2cc6b888b72c`
- **Verified ONNX SHA256**: `b7f2d0e1850d71d4d7158cb598b215db109df35bbf53ebed2416e087bee1dfb3`
- **Architecture**: ResNet18 Specialist Classifier (PlantNet MultiHead)
- **Input Tensor**: `image` `[1, 3, 224, 224]` RGB float32 ImageNet normalized:
  - Mean: `[0.485, 0.456, 0.406]`
  - Std: `[0.229, 0.224, 0.225]`
- **Output Tensor**: `probabilities` `[1, 10]` Softmax distribution over 10 classes
- **Operating Threshold**: Species Acceptance Threshold = `0.50`

---

## 3. Class Index Mapping (10 Classes)
| Index | Species ID | Common Name |
| :---: | :--- | :--- |
| `0` | `01_alstonia_scholaris` | Alstonia scholaris |
| `1` | `02_justicia_adhatoda` | Justicia adhatoda |
| `2` | `03_acalypha_indica` | Acalypha indica |
| `3` | `04_mimusops_elengi` | Mimusops elengi |
| `4` | `05_caryota_urens` | Caryota urens |
| `5` | `07_strobilanthes_kunthiana` | Strobilanthes kunthiana |
| `6` | `08_plumeria_alba` | Plumeria alba |
| `7` | `09_bougainvillea_spectabilis` | Bougainvillea spectabilis |
| `8` | `10_gardenia_jasminoides` | Gardenia jasminoides |
| `9` | `unknown_plant` | Unknown / Non-Target Plant |

---

## 4. Unity Integration Procedure
1. Copy `prototypev2/` into your Unity project's `Assets/` directory.
2. Ensure Unity Sentis package (`com.unity.sentis`) is installed in Unity Package Manager.
3. Add `prototypev2/integration/PlantPipelineManager.cs` to a GameObject in your AR scene.
4. Assign `flora_localizer_v1_tuned.onnx` to `model1Asset` and `flora_species_classifier_v6_autonomous.onnx` to `model2V6Asset`.
5. Refer to `instructions/UNITY_SETUP.md` for detailed inspector setup.

---

## 5. Safety Warning
> [!CAUTION]
> Historical checkpoints (V3, V4, V5, V7 ArcFace) MUST NOT replace these production models without re-running the complete 376-image external bake-off benchmark.
