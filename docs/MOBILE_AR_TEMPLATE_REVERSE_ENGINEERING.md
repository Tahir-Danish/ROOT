# Mobile AR Template Reverse Engineering — Team Cero

> This document reverse-engineers the actual implementation of Team Cero's mobile AR application. All data was extracted via live Unity MCP inspection on 1 September 2026. No project files were modified during this investigation.

---

## 1. System Trace — First Principles

### Stage 1: Phone Camera → AR Session → Tracking

| Attribute | Value |
|-----------|-------|
| **GameObject** | `AR Session` (scene root) |
| **Components** | `ARSession` [AR Foundation], `ARInputManager` [AR Foundation] |
| **Package** | `com.unity.xr.arfoundation@6.5.0` |
| **Provider** | `com.unity.xr.arcore@6.5.0` (Android) |
| **Runtime behavior** | `ARSession` initializes the AR subsystem on device. ARCore provides 6DOF tracking, environmental understanding. `matchFrameRateRequested = True` syncs Unity's frame rate to ARCore's. |
| **Dependencies** | Android device with ARCore support. `ARCameraManager` on Main Camera to receive the camera feed. |
| **Failure modes** | No ARCore-supported device → session fails to initialize. Occluded camera → tracking lost. |
| **Origin** | AR Foundation built-in |

---

### Stage 2: XR Origin → AR Camera

| Attribute | Value |
|-----------|-------|
| **GameObject** | `XR Origin (AR Rig)` (scene root) |
| **Components** | `XROrigin` [XR Core Utilities], `InputActionManager` [Input System], `ARPlaneManager` [AR Foundation], `ARRaycastManager` [AR Foundation] |
| **Camera** | `XR Origin (AR Rig) > Camera Offset > Main Camera` |
| **Camera Components** | `Camera` (tag: `MainCamera`, depth: 0, clearFlags: SolidColor), `AudioListener`, `ARCameraManager`, `ARCameraBackground`, `TrackedPoseDriver` |
| **Runtime behavior** | `XROrigin` maps physical device position to Unity world space. `TrackedPoseDriver` updates the camera's Transform from ARCore tracking data. `ARCameraBackground` replaces the camera's background with the live camera feed. |
| **Origin** | AR Foundation / XR Core Utilities |

---

### Stage 3: AR Plane Detection

| Attribute | Value |
|-----------|-------|
| **Component** | `ARPlaneManager` on `XR Origin (AR Rig)` |
| **Plane Prefab** | `ARFeatheredOcclusionPlane` |
| **Runtime behavior** | Detects flat surfaces and instantiates plane GameObjects. Planes are used for AR raycasting (placement) and for constraining object movement (via ARTransformer). |
| **Origin** | AR Foundation |

---

### Stage 4: Screen Touch → Screen-Space Ray Interaction

| Attribute | Value |
|-----------|-------|
| **EventSystem** | `EventSystem` root GameObject with `XRUIInputModule` [XRI] |
| **Interactor GameObject** | `XR Origin (AR Rig) > Camera Offset > Screen Space Ray Interactor` |
| **Components on Interactor** | |

| Component | Package | Role |
|-----------|---------|------|
| `TouchscreenGestureInputLoader` | XRI | Creates `TouchscreenGestureInputController` device at runtime |
| `XRRayInteractor` | XRI | Main interactor: physics raycast + AR raycast |
| `XRInteractionGroup` | XRI | Groups interactors; tracks `focusInteractable` |
| `TouchscreenHoverFilter` | XRI | Only allows hover while screen is touched |
| `ScreenSpaceRayPoseDriver` | XRI | Converts touch position to ray pose |

**Children providing input:**

| Child GameObject | Component | Role |
|-----------------|-----------|------|
| `Select Input` | `ScreenSpaceSelectInput` | Tap → select; drag → translate |
| `Rotate Input` | `ScreenSpaceRotateInput` | 2-finger twist → rotation delta |
| `Scale Distance Delta Input` | `ScreenSpacePinchScaleInput` | 2-finger pinch → scale delta |

**Key XRRayInteractor properties:**
- `interactionLayers` = `-1` (all layers)
- `enableARRaycasting` = `True`
- `hitDetectionType` = `Raycast`
- `maxRaycastDistance` = `30`
- `lineType` = `Straight Line`
- `manipulateAttachTransform` = `True`
- `blockSpawnWhenInteractorHasSelection` = `True` (on `ARInteractorSpawnTrigger`)

**Origin:** XRI / AR Starter Assets

---

### Stage 5: Raycast Decision — Select vs Spawn

**[From source code: `ARInteractorSpawnTrigger.cs`]**

The XRRayInteractor performs TWO types of raycasts simultaneously:

1. **Physics raycast** against Colliders → If it hits an `XRGrabInteractable`, the object is SELECTED.
2. **AR raycast** against `ARPlane` trackables → If no selection occurs, `ARInteractorSpawnTrigger` attempts to SPAWN.

Decision flow (per frame):
```
Touch performed
  → XRRayInteractor checks select state
  → If hasSelection: block spawn (m_BlockSpawnWhenInteractorHasSelection)
  → If !hasSelection AND selectState.wasCompletedThisFrame:
    → Check: is pointer over UI? If yes, abort.
    → ARInteractor.TryGetCurrentARRaycastHit()
    → If hit is ARPlane: invoke m_ObjectSpawnTriggered (→ ObjectSpawner.TrySpawnObject)
```

**Critical insight:** Spawning only happens when the interactor does NOT have a selection. If an object lacks a Collider, tapping "through" it to a plane behind it will spawn a new object instead of selecting the existing one.

---

### Stage 6: Object Menu → Spawn Trigger → Object Spawner

| Attribute | Value |
|-----------|-------|
| **Menu UI** | `UI > Object Menu Animator > Object Menu` |
| **Menu Manager** | `ARTemplateMenuManager` on `UI` GameObject |
| **Spawner GameObject** | `XR Origin (AR Rig) > Camera Offset > Object Spawner` |
| **Spawner Components** | `ObjectSpawner` [XRI Starter Assets], `ARInteractorSpawnTrigger` [XRI AR Starter Assets] |

**ObjectSpawner serialized configuration:**
- `m_CameraToFace` = `Main Camera`
- `m_SpawnAsChildren` = `True` (spawned objects become children of `Object Spawner`)
- `m_OnlySpawnInView` = `True`
- `m_ApplyRandomAngleAtSpawn` = `True`
- `m_SpawnAngleRange` = `45°`
- `m_SpawnVisualizationPrefab` = `null` (no spawn ring/effect)

**ObjectSpawner prefab list (m_ObjectPrefabs):**
| Index | Prefab | Origin |
|-------|--------|--------|
| 0 | `CubeVariant` | Template |
| 1 | `Rob_Walk` | Team Cero experiment |
| 2 | `MH_Sam_full` | Team Cero experiment |
| 3 | `Pringles_Cans` | Team Cero experiment |
| 4 | `ArchVariant` | Template |
| 5 | `CylinderVariant` | Template |
| 6 | `DebugCubeVariant` | Template |

**Menu → Spawner connection:**
- `ARTemplateMenuManager.SetObjectToSpawn(int index)` sets `ObjectSpawner.spawnOptionIndex`.
- The Object Menu contains icon buttons. Each button's `onClick` calls `SetObjectToSpawn(i)`.
- The icons are static UI Images — they do NOT automatically derive from the 3D prefab.

**[From source code: `ObjectSpawner.TrySpawnObject()`]:**
```csharp
var newObject = Instantiate(m_ObjectPrefabs[objectIndex]);
if (m_SpawnAsChildren)
    newObject.transform.parent = transform;
newObject.transform.position = spawnPoint;
// Face camera + optional random rotation
objectSpawned?.Invoke(newObject);  // GoalManager listens to this
```

**Origin:** XRI Starter Assets (ObjectSpawner), XRI AR Starter Assets (ARInteractorSpawnTrigger), Mobile AR Template (ARTemplateMenuManager)

---

### Stage 7: Spawned Prefab → Collider → Selection

After spawning, the object exists in the scene. For it to be selectable:

1. It must have a `Collider` (Box, Mesh, Capsule, etc.) — so the `XRRayInteractor` physics raycast can hit it.
2. It must have an `XRGrabInteractable` — so the hit registers as a valid interaction target.
3. It must have a `Rigidbody` — required by `XRGrabInteractable`.
4. Its `interactionLayers` must overlap with the interactor's layers.

**[OBSERVED]** CubeVariant has `MeshCollider` on child `Cube_COL` (Layer 0). Rob_Walk has `BoxCollider` on root (Layer 0). Both use `interactionLayers = 1`.

---

### Stage 8: XRGrabInteractable → Grab Transformers → ARTransformer

When selected, `XRGrabInteractable` delegates actual movement to its linked grab transformers.

If `addDefaultGrabTransformers = True` and an `ARTransformer` component exists on the same GameObject, the ARTransformer is used.

**ARTransformer behavior:**
- **Translation:** 1-finger drag. ARTransformer raycasts against AR planes and moves the object to the new plane hit point.
- **Rotation:** 2-finger twist. `ScreenSpaceRotateInput` feeds twist delta to the interactor. ARTransformer applies Y-axis rotation.
- **Scaling:** 2-finger pinch. `ScreenSpacePinchScaleInput` feeds pinch delta. ARTransformer applies uniform scale within `[minScale, maxScale]`.

---

### Stage 9: Delete / UI State

**[From source code: `ARTemplateMenuManager.cs`, line 298-307]:**

```csharp
// In Update():
m_DeleteButton.gameObject.SetActive(m_InteractionGroup?.focusInteractable != null);
```

The Delete button is shown when `XRInteractionGroup.focusInteractable` is not null. Focus is set when an object is selected and persists after deselection.

**[From source code: `ARTemplateMenuManager.cs`, line 452-458]:**

```csharp
void DeleteFocusedObject()
{
    var currentFocusedObject = m_InteractionGroup.focusInteractable;
    if (currentFocusedObject != null)
    {
        Destroy(currentFocusedObject.transform.gameObject);
    }
}
```

Delete simply destroys the focused interactable's GameObject. The `m_DeleteButton.onClick` is wired to `DeleteFocusedObject` in `OnEnable`.

---

## 2. CubeVariant — Deep Prefab Analysis

### Full Hierarchy

```
CubeVariant [root, Layer 0]
├── [Transform] localScale=(1,1,1) rotation=(0,0,0)
├── [Rigidbody] mass=1, isKinematic=True, useGravity=False
├── [XRGrabInteractable] → see config table below
├── [ARTransformer] → see config table below
│
├── Interaction Affordance [child]
│   ├── [XRInteractableAffordanceStateProvider]
│   │     interactableSource = CubeVariant's XRGrabInteractable
│   │     transitionDuration = 0.125s
│   └── Color Affordance [grandchild]
│       ├── [MaterialPropertyBlockHelper]
│       └── [ColorMaterialPropertyAffordanceReceiver]
│
├── AttachTransform [child]
│   └── [Transform] (referenced by XRGrabInteractable.m_AttachTransform)
│
├── Visuals [child]
│   ├── [MeshFilter]
│   └── [MeshRenderer] (the visible cube mesh)
│
└── Cube_COL [child]
    └── [MeshCollider] (the physics shape for selection)
```

### XRGrabInteractable Configuration

| Property | Value | Notes |
|----------|-------|-------|
| interactionManager | null | Auto-found at runtime |
| interactionLayers | 1 (Default) | Must match interactor |
| selectMode | Single | |
| movementType | Instantaneous | |
| trackPosition | True | |
| trackRotation | True | |
| trackScale | True | |
| useDynamicAttach | True | |
| matchAttachPosition | True | |
| matchAttachRotation | False | |
| snapToColliderVolume | False | |
| attachEaseInTime | 0.15 | |
| addDefaultGrabTransformers | True | Auto-links ARTransformer |
| throwOnDetach | False | |
| attachTransform | `AttachTransform` child | |
| predictedVisualsTransform | `Visuals` child | |

### ARTransformer Configuration

| Property | Value |
|----------|-------|
| objectPlaneTranslationMode | 2 (Any Plane) |
| enablePlaneClassificationFilter | False |
| useInteractorOrientation | False |
| minScale | 0.3 |
| maxScale | 2.0 |
| scaleSensitivity | 0.75 |
| elasticity | 0.15 |
| enableElasticBreakLimit | True |
| elasticBreakLimit | 0.5 |

### What makes CubeVariant work

| Capability | Required Components | How it works |
|------------|-------------------|--------------|
| **Spawnable** | Listed in `ObjectSpawner.m_ObjectPrefabs[0]` | `Instantiate()` called by ObjectSpawner |
| **Selectable** | `MeshCollider` (Cube_COL), `XRGrabInteractable`, `Rigidbody` | Physics raycast hits collider → interactor selects interactable |
| **Movable** | `ARTransformer`, `trackPosition=True` | ARTransformer raycasts to AR plane for constrained movement |
| **Rotatable** | `ScreenSpaceRotateInput`, `ARTransformer`, `trackRotation=True` | 2-finger twist → ARTransformer Y-rotation |
| **Scalable** | `ScreenSpacePinchScaleInput`, `ARTransformer`, `trackScale=True` | 2-finger pinch → ARTransformer scale within [0.3, 2.0] |
| **Deletable** | `XRGrabInteractable` (provides focusInteractable), `XRInteractionGroup` | Focus tracked after selection → Delete button shown |
| **Visually highlighted** | `XRInteractableAffordanceStateProvider` → `ColorMaterialPropertyAffordanceReceiver` | Color changes on hover/select states |

---

## 3. GoalManager — Onboarding Flow

**[From source code: `GoalManager.cs`]**

The GoalManager runs a 5-step onboarding flow:

| Step | Goal | stepObject | Trigger to advance |
|------|------|------------|-------------------|
| 0 | `FindSurfaces` | `Prompt Scan Surfaces` | 5s timer or any tap |
| 1 | `TapSurface` | `Prompt Tap to Place` | `ObjectSpawner.objectSpawned` event fires |
| 2 | `Hints` (translate) | `Prompt Move Object` | 6s timer or any tap |
| 3 | `Scale` | `Prompt Scale Object` | 8s timer or any tap |
| 4 | `Hints` (rotate) | `Prompt Rotate Object` | Any tap (since it's the Hints type) |

The greeting prompt (`GreetingPrompt`) is shown first. User presses Continue → `StartCoaching()` begins the goal sequence.

---

## 4. Failed Experiment Analysis

### 4.1 Why custom prefabs spawned successfully
`ObjectSpawner.TrySpawnObject()` simply calls `Instantiate(m_ObjectPrefabs[index])`. It has NO requirements on the prefab's component stack. Any valid GameObject prefab will spawn.

### 4.2 Why menu icons did not change
The Object Menu icons are static `UI.Image` components on hardcoded UI buttons. Each button calls `SetObjectToSpawn(i)` where `i` is the button's position. The icons are NOT derived from the 3D prefab's appearance — they must be manually replaced.

### 4.3 Why imported models lacked manipulation
Raw FBX-derived prefabs contain only geometry components (`MeshFilter`, `MeshRenderer`, `Transform`). They lack `Collider`, `Rigidbody`, `XRGrabInteractable`, and `ARTransformer`. Without these, the XRI system cannot detect or interact with them.

### 4.4 Why adding interaction components gave only partial behavior (Rob_Walk)
The correct components were added, but `Rob_Walk.fbx` was imported with `importCameras: 1`, creating an enabled `Camera` on `robot_rig:persp1`. When spawned, this Camera:
1. Became an active renderer, drawing from inside the robot mesh.
2. Hijacked `Camera.main` (or was rendered alongside it, corrupting the display).
3. Broke physics raycasts (originating from wrong camera position, hitting collider backfaces).
4. Broke twist rotation math (ARTransformer uses camera vectors for rotation projection).

### 4.5 Why Rob_Walk animation did not play
- `Rob_Walk.fbx` contains `AnimationClip` "Take 001" (importAnimation: 1, animationType: 2 / Generic).
- `Rob_Walk.prefab` has NO `Animator` component.
- Even if an `Animator` existed, there is NO `AnimatorController` asset to drive the state machine.
- Both are required for runtime playback.

### 4.6 Valid assumptions
- The component stack (`Collider` + `Rigidbody` + `XRGrabInteractable` + `ARTransformer`) is correct and necessary.
- `MovementType.Instantaneous` is correct for AR objects.
- `IsKinematic = True` and `UseGravity = False` are correct.
- Spawning is prefab-agnostic — any prefab can be spawned.

### 4.7 Invalid/incomplete assumptions
- **Assumed FBX models are just geometry.** Wrong. FBX files can contain Cameras, Lights, and other components that affect global state.
- **Assumed animation would "just work" from clip data.** Wrong. A runtime Animator + AnimatorController are required.
- **Assumed menu icons would auto-derive from prefabs.** Wrong. Icons are static UI Images.
- **Did not verify import settings before adding interaction components.** The `importCameras: 1` setting should have been caught during pre-modification inspection.

---

## 5. Verified Architecture Model

### A. System Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│  PHYSICAL WORLD                                                          │
│  Phone Camera → ARCore SDK → AR Foundation Subsystem                     │
└──────────┬───────────────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  AR SESSION (ARSession + ARInputManager)                                 │
│  Manages lifecycle, frame rate sync                                      │
└──────────┬───────────────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  XR ORIGIN (XROrigin + ARPlaneManager + ARRaycastManager)                │
│  Maps physical space → Unity world                                       │
│  Detects planes, provides AR raycasts                                    │
│                                                                          │
│  ┌─── Camera Offset ─────────────────────────────────────────────┐       │
│  │                                                               │       │
│  │  ┌─ Main Camera ──────────────┐                               │       │
│  │  │  Camera + ARCameraManager  │                               │       │
│  │  │  + ARCameraBackground      │                               │       │
│  │  │  + TrackedPoseDriver       │                               │       │
│  │  └────────────────────────────┘                               │       │
│  │                                                               │       │
│  │  ┌─ Screen Space Ray Interactor ─────────────────────────┐    │       │
│  │  │  XRRayInteractor + XRInteractionGroup                  │    │       │
│  │  │  + TouchscreenGestureInputLoader                       │    │       │
│  │  │  + ScreenSpaceRayPoseDriver + TouchscreenHoverFilter   │    │       │
│  │  │                                                        │    │       │
│  │  │  Children: SelectInput, RotateInput, ScaleDeltaInput   │    │       │
│  │  └──────────────┬─────────────────────────────────────────┘    │       │
│  │                 │                                              │       │
│  │  ┌─ Object Spawner ────────────────────────────────┐           │       │
│  │  │  ObjectSpawner + ARInteractorSpawnTrigger        │           │       │
│  │  │  Prefab list: [CubeVariant, Rob_Walk, ...]       │           │       │
│  │  └──────────────┬──────────────────────────────────┘           │       │
│  └─────────────────┼──────────────────────────────────────────────┘       │
└────────────────────┼─────────────────────────────────────────────────────┘
                     │
    ┌────────────────┴────────────────────────────┐
    │          SPAWNED OBJECTS                     │
    │  (children of Object Spawner)               │
    │                                             │
    │  ┌─ e.g. CubeVariant(Clone) ──────────────┐│
    │  │  Rigidbody (kinematic)                  ││
    │  │  XRGrabInteractable ←──────────┐        ││
    │  │  ARTransformer ←───────────────┤        ││
    │  │  Collider (Cube_COL child)     │        ││
    │  │  Interaction Affordance        │        ││
    │  └────────────────────────────────┘        │
    └────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  UI CANVAS (ARTemplateMenuManager + GoalManager)                         │
│  Create Button, Delete Button, Object Menu, Coaching UI                  │
│  Delete logic: Destroy(m_InteractionGroup.focusInteractable.gameObject)  │
└──────────────────────────────────────────────────────────────────────────┘
```

### B. Prefab Dependency Diagram

```
AR-Ready Prefab
  ├── requires: Collider (root or child)
  ├── requires: Rigidbody (root, IsKinematic=true)
  ├── requires: XRGrabInteractable (root)
  │     ├── depends on: Rigidbody (same GO)
  │     ├── depends on: Collider (same GO or children)
  │     └── auto-links: ARTransformer (if present and addDefaultGrabTransformers=true)
  ├── requires: ARTransformer (root)
  │     └── depends on: ARRaycastManager (in scene, on XR Origin)
  ├── optional: Interaction Affordance (child)
  │     └── depends on: XRGrabInteractable (for state events)
  ├── optional: Animator + AnimatorController (for animation)
  └── MUST NOT have: enabled Camera component anywhere in hierarchy
```

### C. Interaction Dependency Graph

```
Touch → Input System → TouchscreenGestureInputController
  → ScreenSpaceRayPoseDriver (sets ray pose)
  → XRRayInteractor (performs raycast)
    → Physics hit? → XRGrabInteractable.Select()
      → ARTransformer.Process()
        → 1-finger: AR plane translation
        → 2-finger pinch: scale [min,max]
        → 2-finger twist: Y-rotation
    → No physics hit? → AR hit? → ARInteractorSpawnTrigger
      → ObjectSpawner.TrySpawnObject()
```

### D. Spawn Lifecycle

```
1. User opens Object Menu (Create Button → ARTemplateMenuManager.ShowMenu())
2. User taps prefab icon → SetObjectToSpawn(index) → ObjectSpawner.spawnOptionIndex = index
3. Menu hides
4. User taps AR surface
5. XRRayInteractor: no physics hit (no interactable under tap)
6. ARInteractorSpawnTrigger: AR raycast hits ARPlane
7. m_ObjectSpawnTriggered.Invoke(hitPosition, planeNormal)
8. ObjectSpawner.TrySpawnObject():
   a. Instantiate(prefab)
   b. Parent to ObjectSpawner transform (spawnAsChildren=true)
   c. Position at hit point
   d. Rotate to face camera + random angle
   e. Fire objectSpawned event → GoalManager advances onboarding
```

### E. Selection/Manipulation Lifecycle

```
1. User taps spawned object
2. XRRayInteractor physics raycast → hits Collider
3. Collider belongs to GO with XRGrabInteractable → SELECT
4. XRGrabInteractable enters Selected state
5. ARTransformer begins processing each frame
6. XRInteractionGroup.focusInteractable = this object
7. ARTemplateMenuManager shows Delete button
8. User drags (1 finger) → ARTransformer translates on AR plane
9. User pinches (2 fingers) → ARTransformer scales
10. User twists (2 fingers) → ARTransformer rotates
11. User lifts finger → XRGrabInteractable deselected
12. Focus remains on this object → Delete still visible
```

### F. Delete Lifecycle

```
1. Object has been selected at least once → XRInteractionGroup.focusInteractable != null
2. ARTemplateMenuManager.Update() → m_DeleteButton.SetActive(true)
3. User taps Delete button
4. DeleteFocusedObject() → Destroy(focusInteractable.transform.gameObject)
5. focusInteractable becomes null → Delete button hides
```

### G. Model-Import-to-AR-Prefab Lifecycle

```
1. Import FBX into Assets/3dModels/
2. INSPECT import settings:
   - importCameras: MUST be 0 (off) unless camera is intentional
   - importAnimation: 1 if animation needed
   - animationType: Generic or Humanoid as appropriate
3. Create Prefab Variant of the FBX model (Assets/Prefab/)
4. On the root of the Variant:
   a. Add BoxCollider (sized to encapsulate renderers)
   b. Add Rigidbody (IsKinematic=true, UseGravity=false)
   c. Add XRGrabInteractable (match CubeVariant config)
   d. Add ARTransformer (match CubeVariant config)
5. If animation needed:
   a. Create AnimatorController asset
   b. Add state with the FBX's AnimationClip
   c. Add Animator component to root
   d. Assign controller to Animator
6. Add Prefab to ObjectSpawner.m_ObjectPrefabs list
7. Update Object Menu UI icons (manual)
8. Test on Android device
```

### H. Invariants (Must remain true for template to work)

1. **Exactly one active Camera tagged `MainCamera` in the scene.** Spawned objects MUST NOT contain enabled Camera components.
2. **`XRRayInteractor` must exist on `Screen Space Ray Interactor`** with `enableARRaycasting = True`.
3. **`ARPlaneManager` and `ARRaycastManager` must exist** on the same GameObject as `XROrigin`.
4. **All interactable prefabs must have a Collider** reachable by the physics raycast for selection to work.
5. **All interactable prefabs must have `Rigidbody` (IsKinematic=true)** for `XRGrabInteractable` to function.
6. **`XRGrabInteractable.interactionLayers` must overlap with the interactor's layers.** Currently both use default (1/-1).
7. **`ObjectSpawner.spawnAsChildren = True`.** Spawned objects parent to the spawner. `ClearAllObjects()` depends on this.
8. **`ARInteractorSpawnTrigger.blockSpawnWhenInteractorHasSelection = True`.** Selection prevents accidental spawn.

### I. Extension Points (Safe to modify)

1. **ObjectSpawner.m_ObjectPrefabs list.** Add/remove/reorder prefabs freely.
2. **Prefab Variant creation.** Create new Variants of any model with the standard component stack.
3. **AnimatorControllers.** Add animation to any prefab without affecting others.
4. **Object Menu UI.** Add/remove/reorder icon buttons. Each button calls `SetObjectToSpawn(i)`.
5. **GoalManager.m_StepList.** Add/remove/reorder onboarding steps.
6. **ARTransformer min/max scale.** Per-prefab configuration.
7. **Custom MonoBehaviours on spawned prefabs** (as long as they don't conflict with XRI).

### J. Dangerous Modification Points (High break risk)

1. **Screen Space Ray Interactor hierarchy.** Removing or reconfiguring any child (Select Input, Rotate Input, Scale Delta Input) breaks gestures.
2. **XR Origin structure.** Moving the Camera or changing tracking configuration breaks AR.
3. **ARTemplateMenuManager / GoalManager references.** These have hardcoded serialized references to specific UI elements. Renaming or removing referenced objects causes null errors.
4. **Physics Layer Matrix.** Changing which layers raycast against each other can silently break selection.
5. **Input Action Map bindings.** The gesture input chain is fragile — changing action names or bindings can disconnect the pipeline.
6. **EventSystem / XRUIInputModule.** Removing or misconfiguring breaks all UI interaction AND the IsPointerOverGameObject guard (which prevents spawning when tapping UI).
7. **Global Camera settings.** Adding cameras, changing render pipeline settings, or modifying camera depth values.

---

## 6. Source Register

| Title | URL | Authority | Version Relevance | Concepts |
|-------|-----|-----------|-------------------|----------|
| Unity 6 User Manual | https://docs.unity3d.com/6000.0/Documentation/Manual/ | Unity Technologies | Unity 6.0+ (covers 6.5) | GameObjects, Components, Transforms, Prefabs, Physics, Camera, Serialization, Model Import |
| XR Interaction Toolkit 3.1 Manual | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/ | Unity Technologies | XRI 3.x (API stable through 3.5.1) | XRGrabInteractable, Interactors, Grab Transformers, AR Interaction Overview |
| AR Foundation 6.5 Manual | https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.5/manual/ | Unity Technologies | AR Foundation 6.5.0 | ARSession, XROrigin, ARPlaneManager, ARRaycastManager |
| ARCore for Unity | https://developers.google.com/ar/develop/unity-arf | Google | ARCore latest | Android AR provider, plane detection, tracking |
| XRI AR Interaction Overview | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/manual/ar-interaction-overview.html | Unity Technologies | XRI 3.x | ARTransformer setup, screen-space interaction, gesture input |

**Package source code inspected (in Library/PackageCache):**
- `ObjectSpawner.cs` — XRI Starter Assets
- `ARInteractorSpawnTrigger.cs` — XRI AR Starter Assets

**Template source code inspected (in Assets/MobileARTemplateAssets/Scripts):**
- `ARTemplateMenuManager.cs` — Mobile AR Template
- `GoalManager.cs` — Mobile AR Template

---

## 7. Verification Metadata

### Last verified date
1 September 2026

### Project/package versions inspected
- Unity: `6000.5.10f1`
- AR Foundation: `6.5.0`
- XR Interaction Toolkit: `3.5.1`
- ARCore XR Plugin: `6.5.0`
- Input System: `1.20.0`

### Known uncertainties
1. The exact mechanism by which `ARTransformer` auto-registers as the default grab transformer when `addDefaultGrabTransformers = True` has not been traced through XRI source code. It is inferred from observed behavior and official documentation.
2. The `ScreenSpaceRotateInput` → ARTransformer rotation pipeline has not been traced line-by-line through source. The 2-finger twist failure on Rob_Walk was diagnosed by elimination (camera hijack), not by tracing the exact rotation code path.
3. Whether the `ObjectSpawner.spawnAsChildren` reparenting affects `ARTransformer`'s plane raycasting behavior is unverified.
4. The `XRInteractionGroup` focus retention behavior after the focused object is destroyed has not been explicitly tested.

### What would require re-verification
- Upgrading XRI to 4.x (grab transformer API may change)
- Upgrading AR Foundation (subsystem lifecycle changes noted in 6.5 release notes)
- Replacing the Mobile AR Template with a custom scene setup
- Adding a second XRRayInteractor or XRInteractionGroup
- Any modification to the Object Menu UI structure
