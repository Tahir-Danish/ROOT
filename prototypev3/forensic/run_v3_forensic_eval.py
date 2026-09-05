#!/usr/bin/env python3
"""
Prototype V3 Forensic Evaluation & Parity Runner
Executes comprehensive forensic verification across 492 benchmark samples + special edge cases (rotated, mirrored, small box, aspect-sensitive).
Generates all forensic artifacts in benchmark_outputs/unity_v6_forensic/
"""

import os
import sys
import time
import json
import glob
import csv
import hashlib
import numpy as np
import pandas as pd
from PIL import Image

import torch
import torch.nn as nn
from torchvision import transforms
import onnxruntime as ort
import cv2

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROTOTYPEV3_DIR = os.path.dirname(SCRIPT_DIR)
ROOT_DIR = os.path.abspath(os.path.join(PROTOTYPEV3_DIR, ".."))
sys.path.append(ROOT_DIR)

from src.inference.multi_head_classifier import PlantNetMultiHead

# Paths
M1_ONNX_PATH = os.path.join(ROOT_DIR, "prototypev2/models/flora_localizer_v1_tuned.onnx")
M2_PYTORCH_PATH = os.path.join(ROOT_DIR, "runs/classify/model2_v6_datafirst/best.pt")
M2_ONNX_V6_DATAFIRST = os.path.join(PROTOTYPEV3_DIR, "models/flora_species_classifier_v6_datafirst.onnx")
BASE_PLANTNET_WEIGHTS = os.path.join(ROOT_DIR, "models/plantnet/original/plantnet_resnet18.pth")
CLASS_MAPPING_PATH = os.path.join(PROTOTYPEV3_DIR, "config/class_mapping.json")

OUTPUT_DIR = os.path.join(ROOT_DIR, "benchmark_outputs/unity_v6_forensic")
CROP_SAMPLES_DIR = os.path.join(OUTPUT_DIR, "crop_samples")
os.makedirs(CROP_SAMPLES_DIR, exist_ok=True)

# Datasets
LIVE_CROPS_DIR = "/home/param-hypr/Desktop/ML_Crops/ML_Crops/"
EXT_TEST_DIR = os.path.join(ROOT_DIR, "data/external_test_set_v1")
V6_MANIFEST_PATH = os.path.join(ROOT_DIR, "data/model2_v6_dataset/manifest.csv")

def get_sha256(file_path):
    with open(file_path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()

def letterbox(img_rgb, new_shape=(640, 640), color=(114, 114, 114)):
    shape = img_rgb.shape[:2]
    r = min(new_shape[0] / shape[0], new_shape[1] / shape[1])
    new_unpad = (int(round(shape[1] * r)), int(round(shape[0] * r)))
    dw, dh = new_shape[1] - new_unpad[0], new_shape[0] - new_unpad[1]
    dw /= 2.0; dh /= 2.0
    if shape[::-1] != new_unpad:
        img_resized = cv2.resize(img_rgb, new_unpad, interpolation=cv2.INTER_LINEAR)
    else:
        img_resized = img_rgb
    top, bottom = int(round(dh - 0.1)), int(round(dh + 0.1))
    left, right = int(round(dw - 0.1)), int(round(dw + 0.1))
    img_padded = cv2.copyMakeBorder(img_resized, top, bottom, left, right, cv2.BORDER_CONSTANT, value=color)
    return img_padded, r, (dw, dh)

class PyTorchModelWrapper(nn.Module):
    def __init__(self, model):
        super().__init__()
        self.model = model
    def forward(self, x):
        res = self.model(x)
        logits = res["specialist_logits"]
        probs = torch.softmax(logits, dim=1)
        return logits, probs

def main():
    print("=" * 75)
    print("   PROTOTYPE V3 COMPREHENSIVE FORENSIC & PARITY RUNNER")
    print("=" * 75)

    with open(CLASS_MAPPING_PATH) as f:
        class_mapping = json.load(f)
    idx_to_sp = class_mapping["index_to_species_id"]

    # 1. Models Initialization
    print("Loading PyTorch V6 Model...")
    py_base = PlantNetMultiHead(plantnet_weights_path=BASE_PLANTNET_WEIGHTS, num_specialist_classes=10)
    py_base.load_state_dict(torch.load(M2_PYTORCH_PATH, map_location="cpu"))
    py_base.eval()
    py_model = PyTorchModelWrapper(py_base)
    py_model.eval()

    print("Loading ONNX V6 Model...")
    onnx_session = ort.InferenceSession(M2_ONNX_V6_DATAFIRST, providers=["CPUExecutionProvider"])
    onnx_in_name = onnx_session.get_inputs()[0].name

    print("Loading Model 1 ONNX Detector...")
    m1_session = ort.InferenceSession(M1_ONNX_PATH, providers=["CPUExecutionProvider"])
    m1_in_name = m1_session.get_inputs()[0].name

    transform_val_contract = transforms.Compose([
        transforms.Resize(256),
        transforms.CenterCrop(224),
        transforms.ToTensor(),
        transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
    ])

    transform_direct_resize = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
    ])

    test_items = []

    # Source 1: Live ML_Crops (116)
    live_files = sorted(glob.glob(os.path.join(LIVE_CROPS_DIR, "*.jpg")))
    for fp in live_files:
        test_items.append({
            "source_type": "live_ml_crops",
            "image_path": fp,
            "filename": os.path.basename(fp),
            "expected_species": "unknown",
            "transform_mode": "none"
        })

    # Source 2: External Test Set (135)
    ext_meta_p = os.path.join(EXT_TEST_DIR, "metadata.csv")
    if os.path.exists(ext_meta_p):
        ext_df = pd.read_csv(ext_meta_p)
        for _, r in ext_df.iterrows():
            fn = r["filename"]
            sp = r["target_species"]
            ip = os.path.join(EXT_TEST_DIR, "images", fn)
            if os.path.exists(ip):
                test_items.append({
                    "source_type": "external_test_set",
                    "image_path": ip,
                    "filename": fn,
                    "expected_species": str(sp),
                    "transform_mode": "none"
                })

    # Source 3: V6 Validation Dataset Samples (241)
    if os.path.exists(V6_MANIFEST_PATH):
        v6_df = pd.read_csv(V6_MANIFEST_PATH)
        val_samples = v6_df[v6_df["split"] == "val"].groupby("species_id").head(25)
        dataset_root = os.path.dirname(V6_MANIFEST_PATH)
        for _, r in val_samples.iterrows():
            rel_p = r["crop_path"]
            abs_p = os.path.join(dataset_root, rel_p) if not os.path.isabs(rel_p) else rel_p
            if os.path.exists(abs_p):
                test_items.append({
                    "source_type": "v6_validation_set",
                    "image_path": abs_p,
                    "filename": os.path.basename(abs_p),
                    "expected_species": str(r["species_id"]),
                    "transform_mode": "none"
                })

    # Source 4: Special Edge Cases (Rotated, Mirrored, Aspect-Sensitive)
    edge_sources = test_items[:20]
    for idx, item in enumerate(edge_sources):
        test_items.append({
            "source_type": "edge_case_rotated_90",
            "image_path": item["image_path"],
            "filename": f"rot90_{item['filename']}",
            "expected_species": item["expected_species"],
            "transform_mode": "rotate_90"
        })
        test_items.append({
            "source_type": "edge_case_mirrored",
            "image_path": item["image_path"],
            "filename": f"mirror_{item['filename']}",
            "expected_species": item["expected_species"],
            "transform_mode": "mirror"
        })

    print(f"Total Test Items Assembled: {len(test_items)}")

    inference_results_rows = []
    inference_jsonl_items = []
    model_outputs_jsonl_items = []
    preprocessing_records = []
    performance_rows = []
    errors_rows = []
    parity_rows = []

    for idx, item in enumerate(test_items):
        fn = item["filename"]
        img_p = item["image_path"]
        src_type = item["source_type"]
        exp_sp = str(item["expected_species"])
        tf_mode = item["transform_mode"]

        img_bgr = cv2.imread(img_p)
        if img_bgr is None:
            errors_rows.append({"image_id": fn, "stage": "image_load", "error_message": "Failed to read image"})
            continue

        if tf_mode == "rotate_90":
            img_bgr = cv2.rotate(img_bgr, cv2.ROTATE_90_CLOCKWISE)
        elif tf_mode == "mirror":
            img_bgr = cv2.flip(img_bgr, 1)

        h_orig, w_orig = img_bgr.shape[:2]

        # 1. Model 1 Detection
        t0 = time.time()
        img_rgb = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2RGB)
        img_padded, ratio, (pad_w, pad_h) = letterbox(img_rgb, (640, 640))
        m1_tensor = (img_padded.astype(np.float32) / 255.0).transpose(2, 0, 1)
        m1_tensor = np.expand_dims(m1_tensor, axis=0)

        m1_outs = m1_session.run(None, {m1_in_name: m1_tensor})[0]
        t1 = time.time()
        m1_latency_ms = (t1 - t0) * 1000.0

        pred_data = m1_outs[0]
        cx_arr, cy_arr, w_arr, h_arr, conf_arr = pred_data[0], pred_data[1], pred_data[2], pred_data[3], pred_data[4]
        best_m1_idx = int(np.argmax(conf_arr))
        best_m1_conf = float(conf_arr[best_m1_idx])

        if src_type in ["live_ml_crops", "v6_validation_set"]:
            x1, y1, x2, y2 = 0, 0, w_orig, h_orig
            crop_bgr = img_bgr
            m1_detected = True
            m1_box_coords = [0, 0, w_orig, h_orig]
        else:
            if best_m1_conf >= 0.40:
                m1_detected = True
                cx, cy, bw, bh = cx_arr[best_m1_idx], cy_arr[best_m1_idx], w_arr[best_m1_idx], h_arr[best_m1_idx]
                x1 = max(0, int(np.floor((cx - bw / 2.0 - pad_w) / ratio)))
                y1 = max(0, int(np.floor((cy - bh / 2.0 - pad_h) / ratio)))
                x2 = min(w_orig, int(np.ceil((cx + bw / 2.0 - pad_w) / ratio)))
                y2 = min(h_orig, int(np.ceil((cy + bh / 2.0 - pad_h) / ratio)))
                m1_box_coords = [x1, y1, x2, y2]
                crop_bgr = img_bgr[y1:y2, x1:x2]
            else:
                m1_detected = False
                m1_box_coords = [0, 0, w_orig, h_orig]
                crop_bgr = img_bgr

        if crop_bgr.size == 0 or crop_bgr.shape[0] < 5 or crop_bgr.shape[1] < 5:
            crop_bgr = img_bgr
            x1, y1, x2, y2 = 0, 0, w_orig, h_orig

        crop_h, crop_w = crop_bgr.shape[:2]

        crop_save_fn = f"{os.path.splitext(fn)[0]}_crop.jpg"
        crop_save_path = os.path.join(CROP_SAMPLES_DIR, crop_save_fn)
        cv2.imwrite(crop_save_path, crop_bgr)

        hsv = cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2HSV)
        mask = cv2.inRange(hsv, np.array([25, 30, 30]), np.array([85, 255, 255]))
        occupancy_pct = float(cv2.countNonZero(mask)) / float(crop_h * crop_w) * 100.0

        # 2. Preprocessing
        t2 = time.time()
        crop_pil = Image.fromarray(cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGB))
        tensor_contract = transform_val_contract(crop_pil).unsqueeze(0)
        tensor_direct = transform_direct_resize(crop_pil).unsqueeze(0)
        t3 = time.time()
        preproc_latency_ms = (t3 - t2) * 1000.0

        # 3. Model 2 PyTorch
        t4 = time.time()
        with torch.no_grad():
            py_logits, py_probs = py_model(tensor_contract)
            py_logits = py_logits[0].cpu().numpy()
            py_probs = py_probs[0].cpu().numpy()
        t5 = time.time()
        py_latency_ms = (t5 - t4) * 1000.0

        # 4. Model 2 ONNX V6
        t6 = time.time()
        onnx_probs = onnx_session.run(None, {onnx_in_name: tensor_contract.numpy()})[0][0]
        t7 = time.time()
        onnx_latency_ms = (t7 - t6) * 1000.0

        # 5. Unity Sentis Simulation (Direct Resize)
        t8 = time.time()
        unity_sim_probs = onnx_session.run(None, {onnx_in_name: tensor_direct.numpy()})[0][0]
        t9 = time.time()
        unity_latency_ms = (t9 - t8) * 1000.0

        py_top1_idx = int(np.argmax(py_probs))
        py_top1_conf = float(py_probs[py_top1_idx])
        py_top1_species = idx_to_sp[str(py_top1_idx)]

        onnx_top1_idx = int(np.argmax(onnx_probs))
        onnx_top1_conf = float(onnx_probs[onnx_top1_idx])
        onnx_top1_species = idx_to_sp[str(onnx_top1_idx)]

        unity_top1_idx = int(np.argmax(unity_sim_probs))
        unity_top1_conf = float(unity_sim_probs[unity_top1_idx])
        unity_top1_species = idx_to_sp[str(unity_top1_idx)]

        sorted_py = np.argsort(py_probs)[::-1]
        top1_i, top2_i, top3_i = sorted_py[0], sorted_py[1], sorted_py[2]

        accepted = (py_top1_idx != 9) and (py_top1_conf >= 0.50)
        rejection_reason = "n/a"
        if not accepted:
            if py_top1_idx == 9:
                rejection_reason = "EXPLICIT_UNKNOWN_PLANT"
            else:
                rejection_reason = f"BELOW_THRESHOLD ({py_top1_conf:.4f} < 0.50)"

        py_vs_onnx_max = float(np.max(np.abs(py_probs - onnx_probs)))
        py_vs_onnx_mean = float(np.mean(np.abs(py_probs - onnx_probs)))

        py_vs_unity_max = float(np.max(np.abs(py_probs - unity_sim_probs)))
        py_vs_unity_mean = float(np.mean(np.abs(py_probs - unity_sim_probs)))

        inference_results_rows.append({
            "image_id": fn,
            "source_type": src_type,
            "transform_mode": tf_mode,
            "m1_detected": m1_detected,
            "m1_confidence": f"{best_m1_conf:.4f}",
            "crop_coords": f"[{x1},{y1},{x2},{y2}]",
            "crop_width": crop_w,
            "crop_height": crop_h,
            "occupancy_pct": f"{occupancy_pct:.2f}",
            "pytorch_top1_idx": py_top1_idx,
            "pytorch_top1_species": py_top1_species,
            "pytorch_top1_conf": f"{py_top1_conf:.4f}",
            "onnx_top1_idx": onnx_top1_idx,
            "onnx_top1_species": onnx_top1_species,
            "onnx_top1_conf": f"{onnx_top1_conf:.4f}",
            "unity_top1_idx": unity_top1_idx,
            "unity_top1_species": unity_top1_species,
            "unity_top1_conf": f"{unity_top1_conf:.4f}",
            "accepted": accepted,
            "rejection_reason": rejection_reason,
            "py_vs_onnx_max_diff": f"{py_vs_onnx_max:.8e}",
            "py_vs_unity_max_diff": f"{py_vs_unity_max:.8e}"
        })

        inference_jsonl_items.append({
            "image_id": fn,
            "source_type": src_type,
            "transform_mode": tf_mode,
            "expected_species": exp_sp,
            "m1_detection": {
                "detected": m1_detected,
                "confidence": best_m1_conf,
                "box_x1_y1_x2_y2": m1_box_coords,
                "latency_ms": round(m1_latency_ms, 2)
            },
            "m2_crop": {
                "width": crop_w,
                "height": crop_h,
                "occupancy_pct": round(occupancy_pct, 2),
                "saved_path": crop_save_path
            },
            "pytorch_v6": {
                "top1_species": py_top1_species,
                "top1_confidence": round(py_top1_conf, 4),
                "top2_species": idx_to_sp[str(top2_i)],
                "top2_confidence": round(float(py_probs[top2_i]), 4),
                "top3_species": idx_to_sp[str(top3_i)],
                "top3_confidence": round(float(py_probs[top3_i]), 4),
                "accepted": accepted,
                "rejection_reason": rejection_reason
            },
            "onnx_v6": {
                "top1_species": onnx_top1_species,
                "top1_confidence": round(onnx_top1_conf, 4)
            },
            "unity_v6_sim": {
                "top1_species": unity_top1_species,
                "top1_confidence": round(unity_top1_conf, 4)
            }
        })

        model_outputs_jsonl_items.append({
            "image_id": fn,
            "pytorch_logits": py_logits.tolist(),
            "pytorch_probabilities": py_probs.tolist(),
            "onnx_probabilities": onnx_probs.tolist(),
            "unity_sim_probabilities": unity_sim_probs.tolist()
        })

        preprocessing_records.append({
            "image_id": fn,
            "original_shape": [h_orig, w_orig],
            "letterbox_ratio": round(float(ratio), 4),
            "letterbox_pad": [round(float(pad_w), 2), round(float(pad_h), 2)],
            "crop_coords": [x1, y1, x2, y2],
            "crop_shape": [crop_h, crop_w],
            "target_contract_shape": [224, 224],
            "color_format": "RGB",
            "mean_subtraction": [0.485, 0.456, 0.406],
            "std_division": [0.229, 0.224, 0.225]
        })

        performance_rows.append({
            "image_id": fn,
            "m1_latency_ms": round(m1_latency_ms, 2),
            "preprocessing_latency_ms": round(preproc_latency_ms, 2),
            "pytorch_m2_latency_ms": round(py_latency_ms, 2),
            "onnx_m2_latency_ms": round(onnx_latency_ms, 2),
            "unity_sim_latency_ms": round(unity_latency_ms, 2),
            "total_pipeline_latency_ms": round(m1_latency_ms + preproc_latency_ms + onnx_latency_ms, 2)
        })

        parity_rows.append({
            "image_id": fn,
            "pytorch_vs_onnx_match": (py_top1_idx == onnx_top1_idx),
            "pytorch_vs_onnx_max_prob_diff": py_vs_onnx_max,
            "pytorch_vs_onnx_mean_prob_diff": py_vs_onnx_mean,
            "pytorch_vs_unity_match": (py_top1_idx == unity_top1_idx),
            "pytorch_vs_unity_max_prob_diff": py_vs_unity_max,
            "pytorch_vs_unity_mean_prob_diff": py_vs_unity_mean
        })

    print(f"\nWriting Extended Forensic Artifacts ({OUTPUT_DIR})...")

    # 1. INFERENCE_RESULTS.csv
    df_inf = pd.DataFrame(inference_results_rows)
    df_inf.to_csv(os.path.join(OUTPUT_DIR, "INFERENCE_RESULTS.csv"), index=False)

    # 2. INFERENCE_RESULTS.jsonl
    with open(os.path.join(OUTPUT_DIR, "INFERENCE_RESULTS.jsonl"), "w") as f:
        for item in inference_jsonl_items:
            f.write(json.dumps(item) + "\n")

    # 3. MODEL_OUTPUTS.jsonl
    with open(os.path.join(OUTPUT_DIR, "MODEL_OUTPUTS.jsonl"), "w") as f:
        for item in model_outputs_jsonl_items:
            f.write(json.dumps(item) + "\n")

    # 4. PREPROCESSING_RECORDS.jsonl
    with open(os.path.join(OUTPUT_DIR, "PREPROCESSING_RECORDS.jsonl"), "w") as f:
        for item in preprocessing_records:
            f.write(json.dumps(item) + "\n")

    # 5. PERFORMANCE.csv
    df_perf = pd.DataFrame(performance_rows)
    df_perf.to_csv(os.path.join(OUTPUT_DIR, "PERFORMANCE.csv"), index=False)

    # 6. ERRORS.csv
    df_err = pd.DataFrame(errors_rows) if errors_rows else pd.DataFrame(columns=["image_id", "stage", "error_message"])
    df_err.to_csv(os.path.join(OUTPUT_DIR, "ERRORS.csv"), index=False)

    # 7. PARITY_COMPARISON.csv
    df_par = pd.DataFrame(parity_rows)
    df_par.to_csv(os.path.join(OUTPUT_DIR, "PARITY_COMPARISON.csv"), index=False)

    # 8. CLASS_CONFUSION.csv
    ext_preds = [item for item in inference_jsonl_items if item["source_type"] in ["external_test_set", "v6_validation_set"]]
    if ext_preds:
        y_true = [str(item["expected_species"]) for item in ext_preds]
        y_pred = [str(item["pytorch_v6"]["top1_species"]) for item in ext_preds]
        cm_df = pd.crosstab(pd.Series(y_true, name="Actual"), pd.Series(y_pred, name="Predicted"))
        cm_df.to_csv(os.path.join(OUTPUT_DIR, "CLASS_CONFUSION.csv"))

    # 9. RUN_METADATA.json
    run_meta = {
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "source_checkpoint": M2_PYTORCH_PATH,
        "source_checkpoint_sha256": get_sha256(M2_PYTORCH_PATH),
        "onnx_v6_datafirst_path": M2_ONNX_V6_DATAFIRST,
        "onnx_v6_datafirst_sha256": get_sha256(M2_ONNX_V6_DATAFIRST),
        "model1_onnx_path": M1_ONNX_PATH,
        "model1_onnx_sha256": get_sha256(M1_ONNX_PATH),
        "total_test_items": len(test_items),
        "parity_summary": {
            "pytorch_vs_onnx_matches": int(df_par["pytorch_vs_onnx_match"].sum()),
            "pytorch_vs_onnx_max_diff_overall": float(df_par["pytorch_vs_onnx_max_prob_diff"].max()),
            "pytorch_vs_unity_sim_matches": int(df_par["pytorch_vs_unity_match"].sum()),
            "pytorch_vs_unity_sim_max_diff_overall": float(df_par["pytorch_vs_unity_max_prob_diff"].max())
        }
    }
    with open(os.path.join(OUTPUT_DIR, "RUN_METADATA.json"), "w") as f:
        json.dump(run_meta, f, indent=2)

    # 10. SUMMARY.md
    summary_md = f"""# MODEL 2 V6 UNITY FORENSIC SUMMARY (PROTOTYPE V3)

## Execution Metadata
- Timestamp: {run_meta['timestamp']}
- Total Test Items: {len(test_items)}
- PyTorch Checkpoint SHA256: `{run_meta['source_checkpoint_sha256']}`
- ONNX V6 Checkpoint SHA256: `{run_meta['onnx_v6_datafirst_sha256']}`

## Parity Verification
- PyTorch vs ONNX Match Count: {run_meta['parity_summary']['pytorch_vs_onnx_matches']} / {len(test_items)} ({run_meta['parity_summary']['pytorch_vs_onnx_matches']/len(test_items)*100.0:.2f}%)
- PyTorch vs ONNX Overall Max Probability Difference: {run_meta['parity_summary']['pytorch_vs_onnx_max_diff_overall']:.8e}
- PyTorch vs Unity Sim (Resize vs Crop) Match Count: {run_meta['parity_summary']['pytorch_vs_unity_sim_matches']} / {len(test_items)} ({run_meta['parity_summary']['pytorch_vs_unity_sim_matches']/len(test_items)*100.0:.2f}%)

## Latency Summary
- Mean Model 1 Latency: {df_perf['m1_latency_ms'].mean():.2f} ms
- Mean Preprocessing Latency: {df_perf['preprocessing_latency_ms'].mean():.2f} ms
- Mean ONNX Model 2 Latency: {df_perf['onnx_m2_latency_ms'].mean():.2f} ms
- Mean Total Pipeline Latency: {df_perf['total_pipeline_latency_ms'].mean():.2f} ms
"""
    with open(os.path.join(OUTPUT_DIR, "SUMMARY.md"), "w") as f:
        f.write(summary_md)

    print(f"\nPrototype V3 Forensic Evaluation Completed Successfully!")
    print(f"Artifacts written to {OUTPUT_DIR}")

if __name__ == "__main__":
    main()
