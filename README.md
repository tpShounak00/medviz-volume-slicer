# 3D Volume Slice Simulator (Tri-Planar Viewer) — Unity

A lightweight medical-visualization style demo that generates a synthetic 3D volume and renders synchronized **Axial / Coronal / Sagittal** slices in real time.

This project is intentionally plugin-free (no DICOM dependency) to keep the focus on **visualization fundamentals**, UI clarity, and performance-safe rendering in Unity.

## Demo
![Tri-planar overview](Docs/tri_planar_overview.png)

**Presets**
- Grayscale  
  ![Grayscale](Docs/preset_grayscale.png)
- Bone-like (high-density emphasis)  
  ![Bone](Docs/preset_bone.png)

## Features
- **Tri-planar slice rendering** (Axial XY@Z, Coronal XZ@Y, Sagittal YZ@X)
- **Single slider scrub** with plane mode switching
- **Transfer function presets** (Grayscale, Bone-like)
- **Synthetic volume generation** (noise + blobs + shell for “bone-ish” structures)
- Texture reuse + simple CPU-side rendering (clear and explainable)

## Controls
- **Axial / Coronal / Sagittal buttons:** choose which plane the slider controls
- **Slider:** scrubs slice index for the selected plane
- **Preset buttons:** switch transfer function (applies to all views)

## Implementation Notes (Visualization + Performance)
- The volume is stored as a `float[,,]` density field in **[0..1]**
- Each view renders into a reusable `Texture2D` (no per-frame allocations)
- Transfer functions are intentionally simple to keep behavior deterministic and explainable

## How to Run
1. Open the scene: `Assets/_Project/Scenes/VolumeSliceDemo.unity`
2. Press Play

**Recommended Unity version:** 2021 LTS / 2022 LTS+

## Why this is relevant to health-tech visualization
This mirrors common imaging UI patterns used in medical software:
- multi-planar views
- synchronized slice navigation
- density-to-intensity mapping via transfer functions
- clarity-focused UI for scan interpretation workflows

## Next Improvements (if extended)
- Window/Level sliders (continuous transfer control)
- Histogram view + interactive thresholds
- GPU shader-based slice sampling for larger volumes
- Import pipeline for real volumes (DICOM/NIfTI) in native builds

---
Author: MD. Asafuddaula Sobahani Shounak
