# Unity XR Fundamentals — Team Cero Technical Reference

> Internal engineering reference for an engineer building a mobile AR application using Unity 6.x, AR Foundation, and XR Interaction Toolkit. Not a generic tutorial. Facts are labelled by source: **[DOCUMENTED FACT]**, **[OBSERVED IN TEAM CERO]**, **[INFERENCE]**, or **[UNVERIFIED HYPOTHESIS]**.

---

## 1. Unity Core Concepts

### 1.1 Project Structure

**What it is.** A Unity project is a directory containing `Assets/` (all source content), `ProjectSettings/` (global configuration), `Packages/` (package manifest), and `Library/` (cached compiled data, not version-controlled).

**Why it exists.** Separates source-of-truth files from derived/cached data. Every file in `Assets/` has a companion `.meta` file containing a persistent GUID that Unity uses for all internal references.

**What happens at runtime.** Only content referenced (directly or transitively) by a scene included in Build Settings is compiled into the build. The `Library/` is irrelevant at runtime.

**How to inspect with MCP.** `AssetDatabase.FindAssets()` to search. `AssetDatabase.GUIDToAssetPath()` to resolve references.

**Common failure modes.** Deleting a `.meta` file breaks all references to that asset. Moving an asset without its `.meta` creates a "missing" reference everywhere it was used.

**[OBSERVED IN TEAM CERO]** The project uses `Assets/MobileARTemplateAssets/` (template-provided), `Assets/Prefab/` (Team Cero custom prefabs), `Assets/3dModels/` (imported FBX/OBJ files), and `Assets/Scenes/SampleScene.unity` (the single active scene).

---

### 1.2 Assets

**What it is.** Any file in the `Assets/` folder. Includes scripts (`.cs`), models (`.fbx`, `.obj`), textures (`.png`, `.jpg`), materials (`.mat`), prefabs (`.prefab`), scenes (`.unity`), and scriptable objects (`.asset`).

**What problem it solves.** Provides a unified content pipeline. Unity's Model Importer, Texture Importer, etc. convert raw files into engine-internal representations.

**[DOCUMENTED FACT]** Imported model assets (FBX) are read-only in Unity. You cannot add components to the FBX root directly. You must create a Prefab or Prefab Variant to extend them.

**[OBSERVED IN TEAM CERO]** `Rob_Walk.fbx` and `MH_Sam_full.fbx` are imported models in `Assets/3dModels/`. Their corresponding prefabs in `Assets/Prefab/` are Prefab Variants that allow adding interaction components.

---

### 1.3 Scenes

**What it is.** A serialized container (`.unity` file) holding a hierarchy of GameObjects with their component data.

**What it depends on.** References to prefabs, materials, scripts, and other assets via GUIDs.

**[OBSERVED IN TEAM CERO]** The project uses a single scene: `Assets/Scenes/SampleScene.unity`. It contains 5 root GameObjects: `AR Session`, `Directional Light`, `EventSystem`, `UI`, and `XR Origin (AR Rig)`.

---

### 1.4 GameObjects & Components

**What they are.** A `GameObject` is an empty container that only gains behavior through `Components`. Every GameObject has exactly one `Transform` component (position, rotation, scale in 3D space). All other components (rendering, physics, interaction, scripts) are optional.

**What problem it solves.** The Entity-Component pattern allows composing complex behavior from reusable building blocks without deep inheritance hierarchies.

**What happens at runtime.** Components receive lifecycle callbacks (`Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `OnDisable`, `OnDestroy`). Disabling a GameObject disables all its components and all descendant GameObjects.

**How to inspect with MCP.** `GameObject.Find("name")` to locate, `.GetComponent<T>()` to query components, `SerializedObject`/`SerializedProperty` to read serialized fields.

**Common failure modes.** Adding a component that requires another component (e.g., `XRGrabInteractable` requires `Rigidbody`) without adding the dependency. A component referencing a destroyed object gets a `MissingReferenceException` at runtime.

---

### 1.5 Transform & Hierarchy

**What it is.** `Transform` defines local position, rotation (as Quaternion), and scale relative to the parent. The root Transform's local space equals world space.

**Why it exists.** Allows hierarchical scene organization. Moving a parent moves all children.

**[OBSERVED IN TEAM CERO]** CubeVariant has root scale `(1,1,1)` and rotation `(0,0,0)`. MH_Sam_full's FBX root has rotation `(-90,0,0)` applied during import to compensate for DCC tool coordinate system differences.

**Common failure modes.** Non-uniform scale on parents causes skewing. Rotation differences between DCC tools (Maya uses Y-up, Blender uses Z-up) cause models to appear sideways if not corrected during import.

---

### 1.6 Serialization & Inspector

**What it is.** Unity serializes public fields and fields marked `[SerializeField]` into YAML (for scenes/prefabs) or binary (for builds). The Inspector is the visual editor for these serialized fields.

**What problem it solves.** Allows configuration without code changes. Designers can tweak values; the engine persists them.

**[DOCUMENTED FACT]** Only serializable types are persisted: primitives, strings, enums, Unity objects (by reference GUID), `List<T>`, arrays, and types marked `[Serializable]`.

**How to inspect with MCP.** `new SerializedObject(component).FindProperty("m_FieldName")` reads any serialized field, including private ones.

---

### 1.7 Prefabs, Instances, & Variants

**What they are.**

| Concept | Definition |
|---------|-----------|
| **Prefab** | A reusable asset (`.prefab`) defining a GameObject hierarchy with all its components and serialized data. |
| **Prefab Instance** | A clone of a Prefab placed in a scene. Changes to the Prefab propagate to all instances (unless overridden). |
| **Prefab Variant** | A Prefab that inherits from another Prefab (the "base"). It stores only the *differences* (overrides). |

**What problem it solves.** Prefabs allow "configure once, use many times." Variants allow specialization without duplicating the base.

**[DOCUMENTED FACT]** When Unity imports an FBX, it generates a read-only model prefab. To add components (like `XRGrabInteractable`), you must create a Prefab Variant of the model.

**[OBSERVED IN TEAM CERO]** `CubeVariant` is a Variant of `Cube.prefab` (from AR Starter Assets). `Rob_Walk.prefab` and `MH_Sam_full.prefab` are Variants of their respective FBX model assets. The Variants add Colliders, Rigidbodies, and XRI components that cannot be added to the read-only FBX.

**Common failure modes.** Breaking the base Prefab (e.g., renaming children) cascades breakage to all Variants. Overriding a property in a Variant disconnects it from base changes for that property.

---

### 1.8 Imported Model Assets (FBX)

**What it is.** Unity's Model Importer processes FBX/OBJ/glTF files and generates: the model prefab (read-only GameObject hierarchy), Meshes, Materials, AnimationClips, and optionally an Avatar.

**What problem it solves.** Bridges DCC tools (Maya, Blender, MakeHuman) and the Unity engine.

**Import Settings (key ones):**

| Setting | What it controls |
|---------|-----------------|
| `importCameras` | Whether Camera components embedded in the DCC file are imported. |
| `importLights` | Whether Light components are imported. |
| `importAnimation` | Whether animation data is extracted as AnimationClips. |
| `animationType` | `None` / `Legacy` / `Generic` / `Humanoid` — determines the animation system used. |
| `clipAnimations` | Explicit clip definitions. If empty, Unity auto-generates clips from the animation takes. |
| `avatarSetup` | Whether a Humanoid Avatar is created for retargeting. |

**[OBSERVED IN TEAM CERO]** `Rob_Walk.fbx` is imported with `importCameras: 1` and `importAnimation: 1`, `animationType: 2` (Generic). This means it contains a Camera component on the `robot_rig:persp1` child AND an AnimationClip named `Take 001`. The `avatarSetup: 0` means no Avatar was generated.

**[OBSERVED IN TEAM CERO]** `MH_Sam_full.fbx` is a simpler model: single mesh, no deep rig hierarchy, no embedded camera.

**Common failure modes.**
- **Embedded Cameras:** If `importCameras` is enabled, cameras baked into the DCC scene are imported as active Camera components. When spawned at runtime, they hijack rendering. **This was the root cause of the Rob_Walk rendering failure.**
- **Missing Animation:** The FBX may contain clips, but the *prefab* lacks an Animator component or an assigned AnimatorController. The clip data exists but nothing plays it.
- **Scale/Orientation:** Different DCC tools use different up-axes and unit scales. A model may appear 100x too large or rotated 90°.

---

### 1.9 Materials, Meshes, & Textures

**What they are.** A `Mesh` is geometry data (vertices, triangles, UVs). A `Material` defines how a surface is rendered (shader + properties). A `Texture` is a 2D image used by materials.

**[DOCUMENTED FACT]** Unity's URP (Universal Render Pipeline) uses the `Lit` shader by default. Materials reference textures and shader parameters. Meshes reference nothing — they're pure data.

---

### 1.10 Scripts / MonoBehaviour

**What it is.** Any C# class inheriting from `MonoBehaviour` can be attached to a GameObject as a component. Scripts live in `Assets/` and are compiled by Unity.

**[OBSERVED IN TEAM CERO]** The project has only two custom scripts in `Assets/MobileARTemplateAssets/Scripts/`: `ARTemplateMenuManager.cs` and `GoalManager.cs`. Both are part of the Mobile AR Template, not Team Cero custom code. Team Cero has written zero custom MonoBehaviours so far.

---

### 1.11 Runtime vs Editor

**What it is.** Code under `UnityEditor` namespace runs only in the Editor, not in builds. `MonoBehaviour` lifecycle runs at runtime. `AssetDatabase`, `SerializedObject`, `PrefabUtility` are Editor-only APIs.

**Why it matters.** MCP inspections use Editor APIs (`AssetDatabase`, `SerializedObject`). These do not exist at runtime on the Android device. Runtime behavior must be verified on-device.

---

## 2. Runtime Fundamentals

### 2.1 Camera & Rendering

**What it is.** The `Camera` component defines a viewport from which the scene is rendered. In AR, the camera's pose is driven by device tracking (via `TrackedPoseDriver`), and its background is replaced by the physical camera feed (via `ARCameraBackground`).

**What depends on it.** Raycasting, UI interaction, and ARTransformer rotation math all use the active Camera's transform.

**[DOCUMENTED FACT]** If multiple enabled Cameras exist, Unity renders from all of them (based on depth order). The camera tagged `MainCamera` is used by `Camera.main`.

**[OBSERVED IN TEAM CERO]** The Main Camera is at `XR Origin (AR Rig) > Camera Offset > Main Camera`. It has tag `MainCamera`, depth `0`, and components: `Camera`, `AudioListener`, `ARCameraManager`, `ARCameraBackground`, `TrackedPoseDriver`.

**Failure mode observed.** Rob_Walk's FBX contained an enabled Camera on `robot_rig:persp1`. When instantiated, this second Camera rendered from inside the robot, hijacking the display and breaking all screen-space interaction.

---

### 2.2 Physics: Colliders & Rigidbody

**What they are.** `Collider` components define a physics shape (Box, Sphere, Capsule, Mesh). `Rigidbody` makes a GameObject participate in physics simulation. `IsKinematic = true` means physics doesn't move it (but it can still be detected by raycasts).

**What depends on it.** `XRGrabInteractable` requires a `Rigidbody` on the same GameObject. The `XRRayInteractor` performs physics raycasts that can only hit objects with Colliders.

**[DOCUMENTED FACT]** `XRGrabInteractable` documentation states: "Colliders to use for interaction with this Interactable (if empty, will use any child Colliders)." So colliders can be on the root or on children.

**[OBSERVED IN TEAM CERO]** CubeVariant uses a `MeshCollider` on a dedicated child `Cube_COL`. Rob_Walk and MH_Sam_full use `BoxCollider` on the root.

---

### 2.3 Raycast

**What it is.** A line test through 3D space. Unity has two separate raycast systems:

1. **Physics Raycast** (`Physics.Raycast`): Tests against Colliders. Used by `XRRayInteractor` for object selection.
2. **AR Raycast** (`ARRaycastManager.Raycast`): Tests against AR trackables (planes). Used by `ARInteractorSpawnTrigger` for object placement.

**[DOCUMENTED FACT]** The `XRRayInteractor` has `m_EnableARRaycasting = True` which means it does BOTH: physics raycasts against Colliders (for selection) AND AR raycasts against trackables (for placement).

---

### 2.4 Input System

**What it is.** Unity's Input System package (`com.unity.inputsystem`) replaces the legacy `Input` class. It maps physical inputs (touches, buttons) to abstract Actions.

**[OBSERVED IN TEAM CERO]** `com.unity.inputsystem@1.20.0` is installed. The `TouchscreenGestureInputLoader` on the Screen Space Ray Interactor creates a virtual `TouchscreenGestureInputController` device that converts raw touch data into structured gestures (tap, drag, pinch, twist).

---

## 3. XR Interaction Toolkit (XRI)

### 3.1 Architecture Overview

**[DOCUMENTED FACT]** XRI uses three core concepts:

| Concept | Role | Example in Team Cero |
|---------|------|---------------------|
| **Interactor** | Initiates interaction | `XRRayInteractor` on `Screen Space Ray Interactor` |
| **Interactable** | Receives interaction | `XRGrabInteractable` on `CubeVariant` |
| **Interaction Manager** | Brokers events between them | Auto-found `XRInteractionManager` (implicit, not explicitly placed in scene) |

The `XRInteractionManager` is found automatically at runtime if not explicitly assigned.

---

### 3.2 Interaction States: Hover → Select → Focus

**[DOCUMENTED FACT]** States progress as follows:

1. **Hover:** The Interactor's ray is pointing at the Interactable's collider.
2. **Select:** The user performs the select input (tap/press). The Interactable becomes "grabbed."
3. **Focus:** After selection ends, the last-selected object retains "focus" status via the `XRInteractionGroup`. This is what drives the Delete button visibility.

**[OBSERVED IN TEAM CERO]** `ARTemplateMenuManager.Update()` checks `m_InteractionGroup.focusInteractable != null` to decide whether to show the Delete button. This means an object must have been *selected and then deselected* (or still selected) for the Delete button to appear.

---

### 3.3 XRGrabInteractable

**[DOCUMENTED FACT]** An Interactable that allows "grab" behavior. Key properties (from official docs and CubeVariant inspection):

| Property | CubeVariant Value | Effect |
|----------|------------------|--------|
| `movementType` | Instantaneous | Moves Transform directly each frame (no physics lag) |
| `trackPosition` | True | Object follows interactor position |
| `trackRotation` | True | Object follows interactor rotation |
| `trackScale` | True | Object follows interactor scale |
| `useDynamicAttach` | True | Attach point is where the user tapped, not a fixed point |
| `matchAttachPosition` | True | Dynamic attach matches tap position |
| `matchAttachRotation` | False | Does NOT match tap rotation |
| `addDefaultGrabTransformers` | True | Auto-adds default transformer (ARTransformer or XRGeneralTransformer) |
| `throwOnDetach` | False | Object doesn't fly away when released |
| `interactionLayers` | 1 (Default) | Must match the Interactor's layers |
| `selectMode` | Single | Only one Interactor can select at a time |
| `attachTransform` | `AttachTransform` child | The reference point for grab offset |

**Dependencies:** Requires a `Rigidbody` on the same GameObject. Requires at least one `Collider` on the object or its children.

---

### 3.4 Grab Transformers & ARTransformer

**[DOCUMENTED FACT]** Grab Transformers implement `IXRGrabTransformer` and compute the target position/rotation/scale for a grabbed object each frame.

**ARTransformer** is the AR-specific transformer that:
- Constrains translation to detected AR planes (via `ARRaycastManager`)
- Converts 2-finger pinch delta into scale changes
- Converts 2-finger twist delta into Y-axis rotation
- Has `minScale` / `maxScale` limits

**[OBSERVED IN TEAM CERO]** CubeVariant's ARTransformer: `minScale=0.3`, `maxScale=2.0`, `scaleSensitivity=0.75`, `objectPlaneTranslationMode=2` (Any Plane).

**[DOCUMENTED FACT]** When `addDefaultGrabTransformers` is True and an `ARTransformer` is present on the same GameObject, the `XRGrabInteractable` automatically uses it instead of the default `XRGeneralTransformer`.

---

### 3.5 XRInteractionGroup

**What it is.** Groups interactors so that only one can be active at a time. Also tracks `focusInteractable` — the last object that was selected.

**[OBSERVED IN TEAM CERO]** The `XRInteractionGroup` is on the `Screen Space Ray Interactor` GameObject. `ARTemplateMenuManager` references it as `m_InteractionGroup` to check `focusInteractable` for Delete button visibility.

---

## 4. AR Foundation

### 4.1 ARSession

**What it is.** Manages the AR session lifecycle (initialization, tracking, pause, resume).

**[OBSERVED IN TEAM CERO]** `AR Session` root GameObject with `ARSession` + `ARInputManager` components. `matchFrameRateRequested = True`.

---

### 4.2 XROrigin

**What it is.** Maps the device's physical position to Unity world space. Its child `Camera Offset` contains the tracked camera.

**[OBSERVED IN TEAM CERO]** `XR Origin (AR Rig)` is the root. It also hosts `ARPlaneManager` and `ARRaycastManager` as components. Children: `Camera Offset` → `Main Camera`, `Object Spawner`, `Screen Space Ray Interactor`.

---

### 4.3 ARPlaneManager & ARRaycastManager

**ARPlaneManager:** Detects flat surfaces and creates `ARPlane` GameObjects. Uses `ARFeatheredOcclusionPlane` as the plane prefab.

**ARRaycastManager:** Raycasts against detected AR trackables (planes). Used by `ARInteractorSpawnTrigger` for placement and by `ARTransformer` for constrained movement.

---

### 4.4 AR Foundation vs Platform Provider (ARCore)

**[DOCUMENTED FACT]** AR Foundation is the Unity abstraction layer. `com.unity.xr.arcore` is the Android-specific provider plugin that implements the actual tracking, plane detection, and light estimation using Google ARCore.

**[OBSERVED IN TEAM CERO]** `com.unity.xr.arcore@6.5.0` is installed alongside `com.unity.xr.arfoundation@6.5.0`.

---

## 5. Mobile Touchscreen AR Interaction

### 5.1 Touch Gesture Pipeline

**[DOCUMENTED FACT]** The full pipeline from screen touch to object manipulation:

```
Physical Touch
  → Input System (TouchscreenGestureInputController)
    → TouchscreenGestureInputLoader (creates the device)
    → ScreenSpaceRayPoseDriver (converts touch to ray pose)
    → XRRayInteractor (performs raycast)
      → Physics hit → XRGrabInteractable selected
        → ARTransformer processes manipulation
      → AR hit → ARInteractorSpawnTrigger spawns object
```

**[OBSERVED IN TEAM CERO]** The `Screen Space Ray Interactor` has children:
- `Select Input` → `ScreenSpaceSelectInput` (tap/drag gestures → select)
- `Rotate Input` → `ScreenSpaceRotateInput` (twist gesture → rotation delta)
- `Scale Distance Delta Input` → `ScreenSpacePinchScaleInput` (pinch gesture → scale delta)

### 5.2 Selection vs Placement Decision

**[DOCUMENTED FACT from source code]** `ARInteractorSpawnTrigger` has `m_BlockSpawnWhenInteractorHasSelection = True`. Logic flow:

1. User taps screen.
2. `XRRayInteractor` physics-raycasts first.
3. If it HITS a Collider with `XRGrabInteractable` → **SELECT** that object. Spawn is blocked.
4. If it does NOT hit any selectable object → `ARInteractorSpawnTrigger` checks the AR raycast.
5. If the AR raycast hits an `ARPlane` → **SPAWN** the selected prefab at that point.

This is why objects need Colliders to be re-selectable after placement: without a Collider, tapping the object doesn't register as a physics hit, so the system falls through to spawning a new object instead.

---

## 6. 3D Content Pipeline

### 6.1 FBX → AR-Ready Prefab Pipeline

**[DOCUMENTED FACT + OBSERVED IN TEAM CERO]** The correct pipeline:

```
FBX File (DCC export)
  → Unity Model Importer (generates read-only model prefab + sub-assets)
    → Inspect: embedded cameras? lights? animation clips?
    → Configure import settings (disable importCameras if not needed)
  → Create Prefab Variant of the model
    → Add BoxCollider (sized to renderer bounds)
    → Add Rigidbody (IsKinematic=true, UseGravity=false)
    → Add XRGrabInteractable (match CubeVariant config)
    → Add ARTransformer (match CubeVariant config)
    → [Optional] Add Animator + AnimatorController if animation needed
    → [Optional] Add Interaction Affordance for visual highlight
  → Assign Prefab Variant to ObjectSpawner.objectPrefabs list
  → Assign icon to Object Menu UI (manual step)
```

### 6.2 Animation Requirements

**[DOCUMENTED FACT]** For animation to play at runtime, ALL of the following must be true:
1. The FBX `importAnimation` must be enabled (imports `AnimationClip` sub-assets).
2. An `Animator` component must exist on the prefab (usually on the root).
3. An `AnimatorController` asset must be created (a state machine).
4. The `AnimationClip` must be assigned to a state in the controller.
5. The `Animator` must reference the controller.

**[OBSERVED IN TEAM CERO]** `Rob_Walk.fbx` has `importAnimation: 1` and contains `AnimationClip` "Take 001". However, the `Rob_Walk.prefab` has NO `Animator` component and NO `AnimatorController`. Animation cannot play.

---

## 7. Verification & Metadata

### Last verified date
1 September 2026

### Project/package versions inspected
- Unity: `6000.5.10f1` (Unity 6.5)
- AR Foundation: `6.5.0`
- XR Interaction Toolkit: `3.5.1`
- ARCore XR Plugin: `6.5.0`
- Input System: `1.20.0`
- URP/uGUI: `2.5.0` (built-in)

### Known uncertainties
1. The exact default `XRGeneralTransformer` vs `ARTransformer` auto-selection priority when both could apply has not been verified from source — only the observed behavior that ARTransformer is used when present.
2. The `XRInteractionGroup.focusInteractable` behavior after scene reload has not been tested.
3. Whether `matchAttachRotation = False` on CubeVariant is template default or a deliberate override has not been confirmed.

### What would require re-verification after changes
- Any upgrade to XRI (especially 4.x) could change the grab transformer auto-selection logic.
- Any change to the `Screen Space Ray Interactor` prefab could break the gesture-to-interaction pipeline.
- Changes to `ARTemplateMenuManager.cs` could break Delete button logic.
