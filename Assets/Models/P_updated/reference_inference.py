#!/usr/bin/env python3
"""
Reference ONNX Pipeline Implementation for Unity Developer
Supports both Model 2 V6 (Primary) and Model 2 V7 (Experimental).

Demonstrates the exact Model 1 -> Crop -> Model 2 contract using ONNXRuntime.
Outputs explicit detection coordinates, confidence, crop bounds, and species decision.
"""

import os
import sys
import json
import argparse
import cv2
import numpy as np
import onnxruntime as ort

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DELIVERY_DIR = os.path.dirname(SCRIPT_DIR)

MODEL1_ONNX = os.path.join(DELIVERY_DIR, "model1/plant_localizer_model1.onnx")
MODEL2_V6_ONNX = os.path.join(DELIVERY_DIR, "model2/plant_classifier_model2_v6.onnx")
MODEL2_V7_ONNX = os.path.join(DELIVERY_DIR, "model2/plant_classifier_model2_v7.onnx")
MODEL2_DEFAULT_ONNX = os.path.join(DELIVERY_DIR, "model2/plant_classifier_model2.onnx")
CONTRACT_PATH = os.path.join(SCRIPT_DIR, "model_contract.json")

def letterbox(img, new_shape=(640, 640), color=(114, 114, 114)):
    shape = img.shape[:2]
    r = min(new_shape[0] / shape[0], new_shape[1] / shape[1])
    new_unpad = (int(round(shape[1] * r)), int(round(shape[0] * r)))
    dw, dh = new_shape[1] - new_unpad[0], new_shape[0] - new_unpad[1]
    dw /= 2.0; dh /= 2.0
    if shape[::-1] != new_unpad:
        img = cv2.resize(img, new_unpad, interpolation=cv2.INTER_LINEAR)
    top, bottom = int(round(dh - 0.1)), int(round(dh + 0.1))
    left, right = int(round(dw - 0.1)), int(round(dw + 0.1))
    img = cv2.copyMakeBorder(img, top, bottom, left, right, cv2.BORDER_CONSTANT, value=color)
    return img, r, (dw, dh)

def preprocess_model2_crop(crop_bgr):
    crop_rgb = cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGB)
    crop_resized = cv2.resize(crop_rgb, (224, 224), interpolation=cv2.INTER_LINEAR)
    img_float = crop_resized.astype(np.float32) / 255.0

    mean = np.array([0.485, 0.456, 0.406], dtype=np.float32)
    std = np.array([0.229, 0.224, 0.225], dtype=np.float32)
    img_norm = (img_float - mean) / std

    tensor = np.transpose(img_norm, (2, 0, 1)) # HWC -> CHW
    tensor = np.expand_dims(tensor, axis=0) # CHW -> NCHW
    return tensor

def run_reference_pipeline(image_path, variant="V6_PRIMARY"):
    print("=" * 65)
    print(f"  UNITY SENTIS REFERENCE INFERENCE PIPELINE ({variant})")
    print("=" * 65)

    with open(CONTRACT_PATH, "r") as f:
        contract = json.load(f)

    if variant == "V7_EXPERIMENTAL" and os.path.exists(MODEL2_V7_ONNX):
        m2_onnx_path = MODEL2_V7_ONNX
    elif os.path.exists(MODEL2_V6_ONNX):
        m2_onnx_path = MODEL2_V6_ONNX
    else:
        m2_onnx_path = MODEL2_DEFAULT_ONNX

    print(f"Using Model 2 ONNX Path: {m2_onnx_path}")

    img_bgr = cv2.imread(image_path)
    if img_bgr is None:
        print(f"Error: Unable to load input image from {image_path}")
        return

    h_orig, w_orig = img_bgr.shape[:2]
    print(f"Input image: {image_path} ({w_orig}x{h_orig})")

    # 1. MODEL 1 INFERENCE
    m1_session = ort.InferenceSession(MODEL1_ONNX, providers=['CPUExecutionProvider'])
    img_rgb = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2RGB)
    img_padded, ratio, (pad_w, pad_h) = letterbox(img_rgb, (640, 640))
    m1_tensor = img_padded.astype(np.float32) / 255.0
    m1_tensor = np.transpose(m1_tensor, (2, 0, 1))
    m1_tensor = np.expand_dims(m1_tensor, axis=0)

    m1_outs = m1_session.run(None, {"images": m1_tensor})[0] # [1, 5, 8400]
    pred_data = m1_outs[0]
    cx_arr = pred_data[0, :]
    cy_arr = pred_data[1, :]
    w_arr = pred_data[2, :]
    h_arr = pred_data[3, :]
    conf_arr = pred_data[4, :]

    best_idx = np.argmax(conf_arr)
    best_conf = float(conf_arr[best_idx])

    m1_conf_thresh = contract["model1"]["postprocessing"]["confidence_threshold"]

    print("\nModel 1:")
    if best_conf < m1_conf_thresh:
        print("  detected = false")
        print(f"  max_confidence = {best_conf:.4f} (< {m1_conf_thresh} threshold)")
        print("  FINAL STATE: NO_PLANT")
        return

    best_cx = float(cx_arr[best_idx])
    best_cy = float(cy_arr[best_idx])
    best_w = float(w_arr[best_idx])
    best_h = float(h_arr[best_idx])

    # Convert 640x640 letterboxed coords to original image pixels
    x1_raw = (best_cx - best_w / 2.0 - pad_w) / ratio
    y1_raw = (best_cy - best_h / 2.0 - pad_h) / ratio
    x2_raw = (best_cx + best_w / 2.0 - pad_w) / ratio
    y2_raw = (best_cy + best_h / 2.0 - pad_h) / ratio

    print("  detected = true")
    print(f"  box = [{x1_raw:.1f}, {y1_raw:.1f}, {x2_raw:.1f}, {y2_raw:.1f}]")
    print(f"  confidence = {best_conf:.4f}")

    # 2. CROP CONTRACT
    x1 = max(0, int(np.floor(x1_raw)))
    y1 = max(0, int(np.floor(y1_raw)))
    x2 = min(w_orig, int(np.ceil(x2_raw)))
    y2 = min(h_orig, int(np.ceil(y2_raw)))
    crop_w = max(0, x2 - x1)
    crop_h = max(0, y2 - y1)

    print("\nCrop:")
    print(f"  x1 = {x1}, y1 = {y1}, x2 = {x2}, y2 = {y2}")
    print(f"  width = {crop_w}, height = {crop_h}")

    if crop_w < 10 or crop_h < 10:
        print("  ERROR: Crop size too small or invalid.")
        print("  FINAL STATE: INFERENCE_ERROR")
        return

    crop_bgr = img_bgr[y1:y2, x1:x2]

    # 3. MODEL 2 INFERENCE
    m2_session = ort.InferenceSession(m2_onnx_path, providers=['CPUExecutionProvider'])
    m2_tensor = preprocess_model2_crop(crop_bgr)

    m2_outs = m2_session.run(None, {"image": m2_tensor})[0] # [1, 10]
    probs = m2_outs[0]

    sorted_indices = np.argsort(probs)[::-1]
    top1_idx = int(sorted_indices[0])
    top2_idx = int(sorted_indices[1])
    top1_prob = float(probs[top1_idx])
    top2_prob = float(probs[top2_idx])

    class_mapping = contract["model2"]["class_mapping"]
    species_name = class_mapping.get(str(top1_idx), f"class_{top1_idx}")
    top2_species_name = class_mapping.get(str(top2_idx), f"class_{top2_idx}")
    m2_conf_thresh = contract["model2"]["acceptance_rule"]["confidence_threshold"]
    unknown_idx = contract["model2"]["acceptance_rule"]["unknown_class_index"]

    accepted = (top1_idx != unknown_idx) and (top1_prob >= m2_conf_thresh)

    print("\nModel 2:")
    print(f"  active_variant = {variant}")
    print(f"  top1_class_index = {top1_idx}")
    print(f"  species = {species_name}")
    print(f"  top1_confidence = {top1_prob:.4f}")
    print(f"  top2_species = {top2_species_name} ({top2_prob:.4f})")
    print(f"  accepted = {accepted}")
    if accepted:
        print(f"  Reason: Top-1 species meets confidence threshold ({top1_prob:.4f} >= {m2_conf_thresh})")
        print(f"\nFINAL RESULT: TARGET_SPECIES -> {species_name}")
    elif top1_idx == unknown_idx:
        print(f"  Reason: Top-1 predicted class is explicitly unknown_plant")
        print("\nFINAL RESULT: UNKNOWN_PLANT")
    else:
        print(f"  Reason: Top-1 confidence ({top1_prob:.4f}) is below {m2_conf_thresh} threshold")
        print("\nFINAL RESULT: UNKNOWN_PLANT")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Unity Sentis Reference ONNX Pipeline")
    parser.add_argument("image_path", nargs="?", help="Path to input test image")
    parser.add_argument("--variant", choices=["V6_PRIMARY", "V7_EXPERIMENTAL"], default="V6_PRIMARY", help="Model 2 Variant")
    args = parser.parse_args()

    if args.image_path:
        test_img = args.image_path
    else:
        import glob
        root_dir = os.path.abspath(os.path.join(DELIVERY_DIR, ".."))
        test_img = glob.glob(os.path.join(root_dir, "scratch/poc_images/*.jpg"))[0]

    run_reference_pipeline(test_img, variant=args.variant)
