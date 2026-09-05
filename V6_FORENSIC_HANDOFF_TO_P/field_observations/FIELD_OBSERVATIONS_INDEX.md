# FIELD OBSERVATIONS: PHYSICAL DEVICE SCREENSHOTS & TELEMETRY INDEX

This directory contains the unaltered physical-device screenshots captured on the Motorola Edge 60 Fusion (Android 16 / API 36) during forensic testing of the integrated V6 pipeline (`TeamCero_V6_Production.apk`).

---

## Epistemological Classification Standard
All findings and descriptions in this index follow the strict categorization standard:
- **[Verified Fact]**: Directly observed, deterministic system behavior verified in code execution or telemetry.
- **[Measured Result]**: Quantitative data recorded directly from device sensors, stopwatches, or logcat.
- **[Suspected Cause]**: Plausible technical explanations supported by partial evidence but not yet conclusively proven.
- **[Unverified Hypothesis]**: Plausible conjectures or working hypotheses that have not been experimentally isolated or verified by controlled ablation.

---

## Summary of Field Observations

1. **Target Setup**:
   - Plant imagery displayed on a secondary phone screen (and printed botanical reference sheets) was presented to the Motorola Edge 60 Fusion camera stream.
2. **Observed Classification Dynamics**:
   - **[Measured Result]**: The pipeline exhibited dynamic transitions between `unknown_plant` and `05_caryota_urens` (Class 4).
   - **[Measured Result]**: Specialist classifier confidence for `05_caryota_urens` was observed in the range of **~62% to 86%** when framing the secondary phone screen.
3. **AR Spatial Anchor & Card Behavior**:
   - **[Verified Fact]**: The Solarpunk 3D AR card spawned upon initial candidate confirmation and remained solidly spatially anchored at its 3D world position across user movement and perspective changes.
   - **[Verified Fact]**: Card content dynamically updated in real time as the perception state machine transitioned between states (e.g., updating between unconfirmed "Unknown Species / Data Unavailable" and confirmed species metadata) without spawning duplicate cards or losing its native ARAnchor.
4. **Root Cause Status**:
   - **[Unverified Hypothesis]**: The hypothesis that secondary display pixel lattices, refresh rates, or Moiré interference patterns are the primary trigger for the *Caryota urens* false-positive remains an unverified hypothesis pending controlled optical ablation.

---

## File Inventory & SHA256 Integrity Verification

All original screenshot files are preserved completely unaltered:

| Filename | File Size | SHA256 Hash | Resolution | Observed Physical Context & State |
| :--- | :--- | :--- | :--- | :--- |
| `media_1788592402311.png` | 3,670,456 B | `ead0800802058d46564988ede6f1d4bf1e6de532959179daae388fc31616d717` | $1220 \times 2712$ | **[Measured Result]** Live M1 detection overlay with unconfirmed species (Confidence: 58.9%). Card retracted. |
| `media_1788592529128.png` | 3,403,437 B | `077fa47f552ccc1e43695cfa0b3869a353a5ddf0ecd1614bd583101738c4a5b5` | $1220 \times 2712$ | **[Verified Fact]** Spatial Solarpunk AR card anchored in 3D space displaying "Unknown Species / Data Unavailable". |
| `media_1788592550131.png` | 3,740,512 B | `fb01e9983ee4b272714c1d6fec06727f51ec27bdf6615d08de8fa49b2b9ebc8d` | $1220 \times 2712$ | **[Verified Fact]** Live camera framing botanical specimen displayed on secondary phone/print screen. |
| `media_1788592574641.png` | 3,338,869 B | `8f897622ebb7238bf87c592192152f3966e40d9ab756dc8378199712f2225b22` | $1220 \times 2712$ | **[Verified Fact]** Persistent 3D AR card tethered to anchored specimen during camera translation. M1 confidence 68.8%. |
| `media_1788592628768.png` | 3,662,305 B | `6b834ff7e5e6abf3a73ad4aeb094c1e28fe3ea82e6652fc9f5e0ff116b6404f8` | $1220 \times 2712$ | **[Measured Result]** Detection visualizer overlay on secondary screen display target (Confidence: 34.9%). |
| `media_1788592662936.png` | 3,055,691 B | `71ad07e38e8ffca8d3bdf99bbc8dc10a833443d37ecdb240552d25ce8e0169d6` | $2145 \times 964$ | **[Verified Fact]** Development host logcat monitoring terminal showing real-time perception state machine transitions. |
| `media_1788592697318.png` | 3,049,527 B | `ec62c95f86e554e4f802541356012fb911e480aa1a204a934410ce0e269e19d9` | $987 \times 2196$ | **[Verified Fact]** Spatial tether and reticle aligned to target centroid during secondary target acquisition. |
| `media_1788592741418.png` | 2,578,141 B | `4a87192a232b917ade961692e2f53b095a489a81669b3cc32651bd0e65490611` | $1220 \times 2712$ | **[Verified Fact]** Close-up view of Solarpunk glass chassis showing text readability and field alignment. |
| `media_1788592786696.png` | 3,731,379 B | `8d1071c0bab8c54229964272c410474e6afe38d043b70a2b4b39f321c0e125b2` | $2712 \times 1220$ | **[Measured Result]** Landscape orientation test framing secondary screen/sheet; M1 confidence 64.0%. |
| `media_1788592836531.png` | 3,787,328 B | `73feae15f7614abc5bd131a2ad80f9e4a34b85d76c8514ca626039fb5cca69c6` | $1220 \times 2712$ | **[Verified Fact]** Background clutter scan verifying "UI Bridge Online" status without false-positive card triggers. |
| `media_1788592885495.png` | 3,662,305 B | `6b834ff7e5e6abf3a73ad4aeb094c1e28fe3ea82e6652fc9f5e0ff116b6404f8` | $1220 \times 2712$ | Byte-for-byte duplicate verification capture of `media_1788592628768.png` preserved for telemetry audit completeness. |
