# Camera Setup & Frame Acquisition Instructions

## 1. WebCamTexture Setup
- The Unity inference pipeline accepts live camera frames via Unity's `WebCamTexture` API or ARFoundation camera texture (`XRCameraSubsystem`).
- Ensure camera permission is granted in Android Manifest / iOS Info.plist.
- Standard input frame resolution: $1280 \times 720$ or $1920 \times 1080$ RGB.

---

## 2. Color Format & Layout Requirements
- Input textures must be converted to float32 NCHW format.
- **Model 1**: Divide raw uint8 RGB by $255.0f$. Apply $640 \times 640$ letterboxing with background color `(114, 114, 114)`.
- **Model 2**: Extract ROI crop from camera frame, resize to $224 \times 224$, divide by $255.0f$, apply ImageNet normalization.

---

## 3. DroidCam & Virtual Camera Testing
- For desktop editor testing, DroidCam or virtual webcam devices can feed real mobile camera video streams.
- If DroidCam displays a green-screen or empty frame, verify that DroidCam Client is active and set resolution to $1280 \times 720$ before starting Unity Play mode.
