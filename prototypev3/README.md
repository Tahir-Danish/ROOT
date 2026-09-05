# PROTOTYPE V3 — CONTROLLED UNITY V6 INTEGRATION & FORENSIC BRANCH

Welcome to **Prototype V3** for Team Cero — Plant Localizer.

Prototype V3 is an isolated, controlled integration and forensic verification package for **Model 2 V6 Data-First** (`flora_species_classifier_v6_datafirst.onnx`).

---

## 1. Directory Structure

```
prototypev3/
├── README.md                                # Package documentation
├── models/
│   └── flora_species_classifier_v6_datafirst.onnx  # Verified ONNX V6 model (SHA256: 332e0ef0...)
├── config/
│   ├── class_mapping.json                   # Authoritative 10-class species mapping contract
│   └── model_contract.json                  # Complete pipeline input/output contract
├── integration/
│   ├── PlantPipelineManagerV3.cs            # Unity Sentis V3 MonoBehaviour Pipeline Manager
│   ├── SentisV6Preprocessor.cs              # Aspect-preserving Resize(256)->CenterCrop(224) C# preprocessor
│   ├── SentisV6ForensicLogger.cs            # C# machine-readable JSONL & CSV per-sample logger
│   └── reference_inference_v3.py            # CPython Sentis reference inference pipeline
└── forensic/
    └── UNITY_V6_INTEGRATION_FORENSIC_LOGGING.md  # Detailed forensic logging contract (Sections A-H)
```

---

## 2. Key Integration Guidelines

1. **Isolation**: Prototype V3 exists independently of `prototypev2` and `Assets/Scripts/Detector/Pv002Pipeline.cs`.
2. **Explicit Preprocessing**: Must use `SentisV6Preprocessor.cs` (`Resize(256) -> CenterCrop(224)`) instead of direct 224x224 texture stretching.
3. **Forensic Logging**: Record per-sample CSV and JSONL records for all integration test runs using `SentisV6ForensicLogger.cs`.
4. **Authoritative 10-Class Contract**: Preserved in `config/class_mapping.json`.
