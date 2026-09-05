#!/usr/bin/env python3
"""
Prototype V3 Reference ONNX & PyTorch Pipeline Execution Script
Executes explicit V6 Data-First pipeline contract with mandatory aspect-preserving crop preprocessing.
"""

import os
import sys
import json
import argparse
import cv2
import numpy as np
import onnxruntime as ort
from PIL import Image

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROTOTYPEV3_DIR = os.path.dirname(SCRIPT_DIR)
ROOT_DIR = os.path.abspath(os.path.join(PROTOTYPEV3_DIR, ".."))

MODEL1_ONNX = os.path.join(ROOT_DIR, "prototypev2/models/flora_localizer_v1_tuned.onnx")
MODEL2_V6_ONNX = os.path.join(PROTOTYPEV3_DIR, "models/flora_species_classifier_v6_datafirst.onnx")
CONTRACT_PATH = os.path.join(PROTOTYPEV3_DIR, "config/model_contract.json")

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

def preprocess_model2_crop_v3(crop_bgr):
    """
    Explicit V6 Contract:
    1. BGR -> RGB
    2. Resize short side to 256 preserving aspect ratio
    3. CenterCrop 224x224
    4. ImageNet Normalize
    """
    crop_rgb = cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGB)
    h, w = crop_rgb.shape[:2]

    # Aspect preserving resize to short side 256
    target_short = 256
    if w < h:
        new_w = target_short
        new_h = int(round(target_short * (float(h) / float(w))))
    else:
        new_h = target_short
        new_w = int(round(target_short * (float(w) / float(h))))

    img_scaled = cv2.resize(crop_rgb, (new_w, new_h), interpolation=cv2.INTER_LINEAR)

    # Center crop 224x224
    start_x = (new_w - 224) // 2
    start_y = (new_h - 224) // 2
    img_cropped = img_scaled[start_y:start_y+224, start_x:start_x+224]

    img_float = img_cropped.astype(np.float32) / 255.0

    mean = np.array([0.485, 0.456, 0.406], dtype=np.float32)
    std = np.array([0.229, 0.224, 0.225], dtype=np.float32)
    img_norm = (img_float - mean) / std

    tensor = np.transpose(img_norm, (2, 0, 1)) # HWC -> CHW
    tensor = np.expand_dims(tensor, axis=0) # CHW -> NCHW
    return tensor

def run_v3_reference_pipeline(image_path):
    print("=" * 75)
    print("  PROTOTYPE V3 REFERENCE INTEGRATION PIPELINE")
    print("=" * 75)

    with open(CONTRACT_PATH, "r") as f:
        contract = json.load(f)

    img_bgr = cv2.imread(image_path)
    if img_bgr is None:
        print(f"Error: Unable to load input image: {image_path}")
        return

    h_orig, w_orig = img_bgr.shape[:2]
    print(f"Input Image: {image_path} ({w_orig}x{h_orig})")

    # 1. Model 1 Detection
    m1_session = ort.InferenceSession(MODEL1_ONNX, providers=['CPUExecutionProvider'])
    img_rgb = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2RGB)
    img_padded, ratio, (pad_w, pad_h) = letterbox(img_rgb, (640, 640))
    m1_tensor = (img_padded.astype(np.float32) / 255.0).transpose(2, 0, 1)
    m1_tensor = np.expand_dims(m1_tensor, axis=0)

    m1_outs = m1_session.run(None, {"images": m1_tensor})[0][0]
    cx_arr, cy_arr, w_arr, h_arr, conf_arr = m1_outs[0], m1_outs[1], m1_outs[2], m1_outs[3], m1_outs[4]

    best_idx = np.argmax(conf_arr)
    best_conf = float(conf_arr[best_idx])
    m1_thresh = contract["model1"]["postprocessing"]["confidence_threshold"]

    if best_conf < m1_thresh:
        print("\nModel 1 Result: NO_PLANT_DETECTED (max conf < 0.40)")
        return

    cx, cy, bw, bh = cx_arr[best_idx], cy_arr[best_idx], w_arr[best_idx], h_arr[best_idx]
    x1 = max(0, int(np.floor((cx - bw / 2.0 - pad_w) / ratio)))
    y1 = max(0, int(np.floor((cy - bh / 2.0 - pad_h) / ratio)))
    x2 = min(w_orig, int(np.ceil((cx + bw / 2.0 - pad_w) / ratio)))
    y2 = min(h_orig, int(np.ceil((cy + bh / 2.0 - pad_h) / ratio)))

    print(f"Model 1 Detection: conf = {best_conf:.4f}, box = [{x1}, {y1}, {x2}, {y2}]")
    crop_bgr = img_bgr[y1:y2, x1:x2]

    # 2. Model 2 V6 Preprocessing & Inference
    m2_session = ort.InferenceSession(MODEL2_V6_ONNX, providers=['CPUExecutionProvider'])
    m2_tensor = preprocess_model2_crop_v3(crop_bgr)
    probs = m2_session.run(None, {"image": m2_tensor})[0][0]

    top1_idx = int(np.argmax(probs))
    top1_conf = float(probs[top1_idx])
    species_map = contract["model2_v6_datafirst"]["class_mapping"]
    species_name = species_map[str(top1_idx)]

    m2_thresh = contract["model2_v6_datafirst"]["acceptance_rule"]["confidence_threshold"]
    accepted = (top1_idx != 9) and (top1_conf >= m2_thresh)

    print("\nModel 2 V6 Result:")
    print(f"  top1_species = {species_name}")
    print(f"  top1_confidence = {top1_conf:.4f}")
    print(f"  accepted = {accepted}")
    print(f"  final_state = {'TARGET_SPECIES -> ' + species_name if accepted else 'UNKNOWN_PLANT'}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Prototype V3 Reference ONNX Pipeline")
    parser.add_argument("image_path", nargs="?", help="Path to input test image")
    args = parser.parse_args()

    if args.image_path:
        test_p = args.image_path
    else:
        live_crops = sorted(glob.glob("/home/param-hypr/Desktop/ML_Crops/ML_Crops/*.jpg"))
        test_p = live_crops[0]

    run_v3_reference_pipeline(test_p)
