# AR Object Contract — Team Cero

> Defines what a prefab must satisfy to function correctly as a Team Cero spawnable AR object.  
> This is the authoritative engineering contract for all AR objects in this project.  
> Derived from: `UNITY_XR_FUNDAMENTALS.md`, `MOBILE_AR_TEMPLATE_REVERSE_ENGINEERING.md`, `XR_ARCHITECTURE.md`, `WORKING_STATE.md`.  
> Do not modify the Unity project based solely on this document. Validate against the live project before implementation.

---

## Classification Legend

| Symbol | Meaning |
|--------|---------|
| **REQ** | REQUIRED — object will not work without this |
| **COND** | CONDITIONALLY REQUIRED — required only when a named capability is needed |
| **OPT** | OPTIONAL — improves UX but object functions without it |
| **TMPL** | TEMPLATE-SPECIFIC — required for full compatibility with the Mobile AR Template |
| **UNV** | UNKNOWN / UNVERIFIED — insufficient evidence to classify |

| Tag | Meaning |
|-----|---------|
| `[FACT]` | Documented in official Unity/XRI/AR Foundation sources |
| `[OBS]` | Observed directly in the live Team Cero project via MCP inspection |
| `[INF]` | Logically inferred; has not been contradicted but is not confirmed by source |
| `[UNV]` | Not verified; stated as hypothesis only |

---

## 1. Root Structure

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R1.1 | Prefab must have exactly one root GameObject. | **REQ** | `[FACT]` Unity prefab format |
| R1.2 | Root must have a `Transform` component. | **REQ** | `[FACT]` All GameObjects have a Transform |
| R1.3 | Root layer must be 0 (Default). | **REQ** | `[OBS]` CubeVariant root: layer 0. XRRayInteractor `interactionLayers=-1` (all layers). Any layer change requires matching both sides. |
| R1.4 | Root tag must not conflict with reserved Unity tags unless intentional. | **OPT** | `[INF]` CubeVariant tag is "Untagged". |
| R1.5 | Root must NOT be a raw imported FBX model asset. | **REQ** | `[FACT]` FBX model prefabs are read-only; components cannot be added. Must use a Prefab Variant. |

---

## 2. Transform Configuration

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R2.1 | Root localScale must be `(1, 1, 1)` at prefab authoring time. | **REQ** | `[OBS]` CubeVariant root scale is (1,1,1). `[INF]` ARTransformer applies scale relative to the root; non-unit root scale will distort min/max scale limits. |
| R2.2 | Root localPosition must be `(0, 0, 0)` at prefab authoring time. | **REQ** | `[INF]` ObjectSpawner sets world position after instantiation. Non-zero local position would offset the spawned object from the raycast hit point. |
| R2.3 | Root localRotation must be `(0, 0, 0)` at prefab authoring time. | **REQ** | `[OBS]` ObjectSpawner sets rotation to face camera at spawn time (overrides root rotation). Model geometry rotation must be corrected inside the FBX or in a child transform, not on the root. |
| R2.4 | `m_ConstrainProportionsScale` should be True on root Transform. | **OPT** | `[OBS]` CubeVariant root has this enabled. Ensures uniform scaling via ARTransformer. |
| R2.5 | Child transforms may carry geometry-correcting rotation (e.g., -90° X). | **OPT** | `[INF]` If a model is authored Y-up in a DCC tool but FBX exports Z-up, correction is typically applied on a child "Visuals" transform rather than the root. |

---

## 3. Collider

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R3.1 | At least one Collider must exist on the root or any descendant. | **REQ** | `[FACT]` XRGrabInteractable docs: "if empty [colliders list], will use any child Colliders." `[OBS]` CubeVariant uses MeshCollider on child `Cube_COL`. Rob_Walk and MH_Sam_full use BoxCollider on root. |
| R3.2 | The Collider must be reachable by a physics raycast (not on a layer masked out by the interactor). | **REQ** | `[OBS]` All objects on Layer 0. XRRayInteractor `raycastMask` inspected and does not exclude Layer 0. |
| R3.3 | BoxCollider is the lowest-effort correct option for imported models. | **OPT** | `[INF]` Auto-calculated bounds from renderers. Sufficient for screen-space tap selection. |
| R3.4 | MeshCollider gives more accurate tap targets for irregular shapes. | **OPT** | `[OBS]` CubeVariant uses MeshCollider on a dedicated `Cube_COL` child. More expensive at runtime. |
| R3.5 | BoxCollider center and size must encapsulate visible geometry. | **COND** | `[INF]` Required when using BoxCollider. Auto-calculated from renderer bounds is sufficient. |
| R3.6 | Collider must not be a Trigger. | **REQ** | `[FACT]` Physics raycasts do not hit triggers by default (`RaycastTriggerInteraction.Ignore` is the XRRayInteractor default). `[OBS]` XRRayInteractor `m_RaycastTriggerInteraction = Ignore`. |

---

## 4. Rigidbody

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R4.1 | A `Rigidbody` component must exist on the root (same GameObject as XRGrabInteractable). | **REQ** | `[FACT]` XRGrabInteractable requires a Rigidbody. `[OBS]` CubeVariant: Rigidbody on root. |
| R4.2 | `isKinematic` must be True. | **REQ** | `[OBS]` CubeVariant: `m_IsKinematic = True`. `[INF]` Without kinematic, physics gravity will immediately move the object when spawned onto an AR plane. |
| R4.3 | `useGravity` must be False. | **REQ** | `[OBS]` CubeVariant: `m_UseGravity = False`. Required together with R4.2. |
| R4.4 | `m_ImplicitCom = True`, `m_ImplicitTensor = True`. | **OPT** | `[OBS]` CubeVariant values. Default behavior; no action required if not explicitly changed. |

---

## 5. XRGrabInteractable

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R5.1 | `XRGrabInteractable` must be on the root GameObject. | **REQ** | `[OBS]` CubeVariant root. `[INF]` ObjectSpawner grants initial focus by passing the instantiated GameObject to the XRI system; the interactable must be findable at the root. |
| R5.2 | `movementType` must be `Instantaneous`. | **REQ** | `[OBS]` CubeVariant: Instantaneous. `[INF]` Other modes introduce lag or require non-kinematic Rigidbody. |
| R5.3 | `trackPosition = True`. | **REQ** | `[FACT]` Required for ARTransformer to move the object. |
| R5.4 | `trackRotation = True`. | **REQ** | `[FACT]` Required for ARTransformer to rotate the object. |
| R5.5 | `trackScale = True`. | **REQ** | `[OBS]` CubeVariant. Required for ARTransformer to scale the object. |
| R5.6 | `addDefaultGrabTransformers = True`. | **REQ** | `[FACT]` When True and an ARTransformer is present, XRI automatically uses it. Without this, the object has no movement logic. |
| R5.7 | `useDynamicAttach = True`. | **REQ** | `[OBS]` CubeVariant: True. `[INF]` Without dynamic attach, the object jumps to a fixed attach point on grab rather than being grabbed where tapped. |
| R5.8 | `matchAttachPosition = True`. | **REQ** | `[OBS]` CubeVariant: True. Required for dynamic attach to match tap position. |
| R5.9 | `matchAttachRotation = False`. | **OPT** | `[OBS]` CubeVariant: False. `[INF]` Setting True would snap object rotation to interactor at grab, overriding existing world rotation. False is appropriate for AR objects that maintain their own orientation. |
| R5.10 | `interactionLayers = 1` (Default). | **REQ** | `[OBS]` CubeVariant: value 1. Must match or overlap with `XRRayInteractor.interactionLayers`. |
| R5.11 | `selectMode = Single`. | **REQ** | `[OBS]` CubeVariant. Prevents conflicting multi-selector grabs on mobile. |
| R5.12 | `throwOnDetach = False`. | **REQ** | `[OBS]` CubeVariant: False. `[INF]` ThrowOnDetach with kinematic Rigidbody has no documented effect but should be False to avoid unexpected behavior on release. |
| R5.13 | `m_InteractionManager` may be left null. | **OPT** | `[FACT]` XRI auto-finds the XRInteractionManager in the scene at runtime. |
| R5.14 | `attachTransform` should reference a dedicated `AttachTransform` child. | **TMPL** | `[OBS]` CubeVariant has an `AttachTransform` child; `XRGrabInteractable.m_AttachTransform` references it. `[UNV]` Whether this is required for correct grab behavior vs. dynamic attach is unverified. |
| R5.15 | Persistent `selectEntered` / `selectExited` UnityEvents must have 0 entries. | **REQ** | `[OBS]` CubeVariant and all experimental prefabs: 0 persistent event listeners. Adding listeners risks breaking template event flow. |

---

## 6. ARTransformer

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R6.1 | `ARTransformer` must be on the root (same GameObject as XRGrabInteractable). | **REQ** | `[FACT]` ARTransformer is a grab transformer and must be on the same GO as XRGrabInteractable. `[OBS]` CubeVariant root. |
| R6.2 | `objectPlaneTranslationMode = 2` (Any Plane). | **REQ** | `[OBS]` CubeVariant: enum index 2. Required for 1-finger drag translation along AR planes. |
| R6.3 | `minScale > 0`. | **REQ** | `[OBS]` CubeVariant: 0.3. Prevents degenerate zero/negative scale. |
| R6.4 | `maxScale` must be set to a practical upper limit. | **REQ** | `[OBS]` CubeVariant: 2.0. Prevents unbounded growth. |
| R6.5 | `scaleSensitivity = 0.75`. | **OPT** | `[OBS]` CubeVariant value. Controls how aggressively pinch scales the object. |
| R6.6 | `elasticity = 0.15`, `enableElasticBreakLimit = True`, `elasticBreakLimit = 0.5`. | **OPT** | `[OBS]` CubeVariant values. Provides spring-back behaviour at scale limits. |
| R6.7 | `useInteractorOrientation = False`. | **OPT** | `[OBS]` CubeVariant: False. |
| R6.8 | `enablePlaneClassificationFilter = False`. | **OPT** | `[OBS]` CubeVariant: False. Allows placement on any plane type. Change only if the object should only land on horizontal or vertical surfaces. |

---

## 7. Attach Transform

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R7.1 | An empty child GameObject named `AttachTransform` with a `Transform` is used by CubeVariant. | **TMPL** | `[OBS]` CubeVariant has `AttachTransform` child; referenced by `XRGrabInteractable.m_AttachTransform`. |
| R7.2 | Without an explicit `AttachTransform`, XRI uses the root Transform as the attach point. | **FACT** | `[FACT]` XRGrabInteractable docs: "will use this object's position if none set." |
| R7.3 | For objects using `useDynamicAttach = True`, the static `attachTransform` may be less critical. | **UNV** | `[UNV]` Dynamic attach overrides the static attach point. Whether omitting `AttachTransform` causes runtime issues when dynamic attach is active has not been tested. |

---

## 8. Visuals / Predicted Visuals

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R8.1 | A `MeshRenderer` (or `SkinnedMeshRenderer`) on the root or a child provides the visual representation. | **REQ** | `[INF]` An invisible object provides no AR value, though the system won't fail without one. |
| R8.2 | CubeVariant uses a dedicated `Visuals` child referenced by `XRGrabInteractable.m_PredictedVisualsTransform`. | **TMPL** | `[OBS]` CubeVariant: `m_PredictedVisualsTransform = Visuals`. |
| R8.3 | Without a `predictedVisualsTransform`, visual lag during grab is possible. | **UNV** | `[UNV]` Effect of omitting this reference has not been observed on a device. |
| R8.4 | Materials on child renderers must be URP-compatible. | **REQ** | `[FACT]` The project uses URP. Legacy or HDRP shaders will render pink/incorrect. |

---

## 9. Grab Transformers

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R9.1 | `startingSingleGrabTransformers` and `startingMultipleGrabTransformers` lists must be empty. | **REQ** | `[OBS]` CubeVariant: both lists empty. `[FACT]` When `addDefaultGrabTransformers = True` and lists are empty, XRI auto-selects the transformer (ARTransformer if present). Manual entries would override this. |
| R9.2 | Do not manually add `XRGeneralTransformer` alongside `ARTransformer`. | **REQ** | `[INF]` Doing so with empty starting lists causes undefined transformer order. ARTransformer alone is the correct choice for AR objects. |

---

## 10. Interaction Layers

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R10.1 | `XRGrabInteractable.interactionLayers` must overlap with `XRRayInteractor.interactionLayers`. | **REQ** | `[FACT]` XRI documentation. `[OBS]` CubeVariant interactable = 1; interactor = -1 (all). Default layer (1) satisfies this. |
| R10.2 | Do not remove the Default layer (bit 0) from interactionLayers without also updating the interactor. | **REQ** | `[OBS]` Both parties currently use Default. |

---

## 11. Selection

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R11.1 | Physics raycast must be able to hit the Collider (R3.1–R3.6). | **REQ** | See section 3. |
| R11.2 | XRGrabInteractable must be present (R5.1–R5.15). | **REQ** | See section 5. |
| R11.3 | After first selection, `XRInteractionGroup.focusInteractable` is set to this object. | **TMPL** | `[OBS]` Source: `ARTemplateMenuManager.Update()` checks `m_InteractionGroup.focusInteractable`. |
| R11.4 | Re-selection (after deselection) requires a valid Collider hit on re-tap. | **REQ** | `[OBS]` Rob_Walk experiment: objects without correct camera state could not be re-selected. `[INF]` If tap passes through (no Collider hit), `ARInteractorSpawnTrigger` spawns a new object instead. |

---

## 12. Spawning

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R12.1 | Prefab must be registered in `ObjectSpawner.m_ObjectPrefabs` list. | **REQ** | `[FACT]` ObjectSpawner source: `Instantiate(m_ObjectPrefabs[index])`. Not in the list = cannot be spawned. |
| R12.2 | The prefab must not require scene references that won't exist at runtime. | **REQ** | `[INF]` ObjectSpawner calls `Instantiate()` with no additional initialization. Any required references must be auto-found at runtime or set via Inspector on the prefab itself. |
| R12.3 | Spawned object is parented to `Object Spawner` transform (`spawnAsChildren = True`). | **TMPL** | `[OBS]` ObjectSpawner: `m_SpawnAsChildren = True`. |
| R12.4 | Spawned object receives position and rotation from ObjectSpawner (faces camera ± random 45°). | **TMPL** | `[OBS]` ObjectSpawner source: sets `newObject.transform.position` and `rotation`. |
| R12.5 | `spawnOptionIndex` is set by `ARTemplateMenuManager.SetObjectToSpawn(int index)`. | **TMPL** | `[OBS]` ARTemplateMenuManager source. |

---

## 13. Move (Translation)

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R13.1 | `ARTransformer` present on root with `objectPlaneTranslationMode = 2`. | **REQ** | See R6.1–R6.2. |
| R13.2 | `XRGrabInteractable.trackPosition = True`. | **REQ** | See R5.3. |
| R13.3 | `ARRaycastManager` must exist in the scene (on XR Origin). | **REQ** | `[OBS]` Present on `XR Origin (AR Rig)`. `[FACT]` ARTransformer uses it to constrain movement to AR planes. |
| R13.4 | The object must be selectable (R11.1–R11.4) before translation can begin. | **REQ** | `[FACT]` Translation only occurs on a selected object. |

---

## 14. Rotate

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R14.1 | `ARTransformer` present on root. | **REQ** | See R6.1. |
| R14.2 | `XRGrabInteractable.trackRotation = True`. | **REQ** | See R5.4. |
| R14.3 | `ScreenSpaceRotateInput` child on `Screen Space Ray Interactor` must be active. | **REQ** | `[OBS]` Child `Rotate Input` with `ScreenSpaceRotateInput`. This is a scene object; prefab does not control it. |
| R14.4 | No enabled Camera inside the prefab hierarchy. | **REQ** | `[OBS]` Rob_Walk: enabled Camera on `robot_rig:persp1` corrupted ARTransformer rotation math. `[INF]` ARTransformer uses the active camera's vectors for twist-to-rotation projection. A second active Camera breaks this. |

---

## 15. Scale

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R15.1 | `ARTransformer` present on root with valid `minScale` and `maxScale`. | **REQ** | See R6.1, R6.3–R6.4. |
| R15.2 | `XRGrabInteractable.trackScale = True`. | **REQ** | See R5.5. |
| R15.3 | `ScreenSpacePinchScaleInput` child on `Screen Space Ray Interactor` must be active. | **REQ** | `[OBS]` Child `Scale Distance Delta Input`. Scene object; prefab does not control it. |
| R15.4 | Root scale `(1,1,1)` at authoring time ensures `minScale`/`maxScale` behave as expected. | **REQ** | See R2.1. |

---

## 16. Delete

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R16.1 | `XRGrabInteractable` must be present so `XRInteractionGroup.focusInteractable` can be set. | **REQ** | `[OBS]` Source: `ARTemplateMenuManager.DeleteFocusedObject()` uses `m_InteractionGroup.focusInteractable`. |
| R16.2 | Object must have been selected at least once after spawning for focus to be set. | **TMPL** | `[OBS]` Source: focus is set on Select, not on spawn. |
| R16.3 | Object must be a direct or indirect child of `Object Spawner` (or at least not parented elsewhere). | **UNV** | `[INF]` `ClearAllObjects()` in ARTemplateMenuManager iterates `m_ObjectSpawner.transform` children. Delete button uses `Destroy(gameObject)` so parenting doesn't affect the delete action itself; only `ClearAllObjects` depends on parentage. |

---

## 17. Menu Icon Association

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R17.1 | The 3D prefab and its menu icon are entirely separate assets. | **TMPL** | `[OBS]` Object Menu icons are static `UI.Image` components on hardcoded UI buttons. `[OBS]` WORKING_STATE.md: "Template object menu icons are separate UI assets and do not automatically change." |
| R17.2 | To associate a new prefab with a menu icon, the corresponding UI button's `Image.sprite` must be set manually. | **TMPL** | `[INF]` Menu buttons call `SetObjectToSpawn(i)` where `i` is button position. The icon is the button's static Image sprite. |
| R17.3 | Adding a prefab to `ObjectSpawner.m_ObjectPrefabs[i]` does NOT automatically update the icon at index `i`. | **TMPL** | `[OBS]` Confirmed via experiment: Rob_Walk and MH_Sam_full spawned but their icons showed template defaults. |

---

## 18. Camera Safety

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R18.1 | No `Camera` component, enabled or disabled, should exist anywhere in the prefab hierarchy. | **REQ** | `[OBS]` Rob_Walk embedded Camera caused: rendering corruption, selection failure, rotation failure, delete UI failure. `[OBS]` FBX import setting `importCameras: 1` is the cause. |
| R18.2 | FBX import setting `importCameras` must be set to 0 (Off) for all AR object source assets. | **REQ** | `[OBS]` Rob_Walk.fbx: `importCameras: 1`. `[FACT]` This is set in the `.meta` file (Model Importer). |
| R18.3 | Similarly, `importLights` should be 0 unless Light components are intentional. | **OPT** | `[INF]` Imported Lights do not break AR interaction the way a Camera does, but they may affect scene illumination unexpectedly. |
| R18.4 | Verification: after import, inspect the prefab hierarchy for any `Camera` type sub-assets. | **REQ** | `[OBS]` Verified via `GetComponentsInChildren<Camera>(true)` — must return 0 results. |

---

## 19. Animation Compatibility

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R19.1 | Static AR objects (no animation) require no Animator or AnimationClip. | **REQ** | `[INF]` MH_Sam_full is currently static and functions correctly for manipulation after correct component setup. |
| R19.2 | For animated AR objects, an `Animator` component must be on the prefab (typically the root). | **COND** | `[FACT]` Animator is the runtime component that plays animation. `[OBS]` Rob_Walk: has AnimationClip "Take 001" but NO Animator → animation does not play. |
| R19.3 | An `AnimatorController` asset must be created, contain the AnimationClip in a state, and be assigned to the Animator. | **COND** | `[FACT]` Without a controller, the Animator has no state machine and plays nothing. |
| R19.4 | `animationType` in FBX import settings must match the controller type (Generic or Humanoid). | **COND** | `[FACT]` Mismatch between Generic FBX and a Humanoid controller causes import errors or silent failure. |
| R19.5 | Animator-driven child transforms (bones) are compatible with root-level ARTransformer manipulation. | **UNV** | `[UNV]` The ARTransformer applies transforms to the root; bone animations apply to children. This should be compatible, but has not been tested on a device with a rigged, animated object. |
| R19.6 | `clipAnimations` array in FBX import settings must define explicit clips OR `importAnimation: 1` must be set (auto-generates from takes). | **COND** | `[OBS]` Rob_Walk: `clipAnimations: []` (empty) but `importAnimation: 1` → Unity auto-generates "Take 001". |

---

## 20. FBX / GLB Import Requirements

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R20.1 | `importCameras: 0` — mandatory. | **REQ** | See R18.2. |
| R20.2 | `importLights: 0` recommended. | **OPT** | See R18.3. |
| R20.3 | `globalScale` should match the target world-space size in meters. | **REQ** | `[INF]` ARTransformer places objects at AR-detected surface level. A model 100 units tall at scale 1 will appear 100 meters tall in AR. |
| R20.4 | `bakeAxisConversion: 0` is preferred; axis correction should be applied in the DCC tool before export. | **OPT** | `[INF]` Baking axis conversion can cause unexpected rotation on child bones in animated rigs. |
| R20.5 | `optimizeBones: 1` is safe for non-humanoid rigs. | **OPT** | `[OBS]` Rob_Walk: `optimizeBones: 1`. |
| R20.6 | `materialImportMode: 2` (Import via Material Description). | **OPT** | `[OBS]` Rob_Walk.fbx meta. Materials are created from FBX material data. |
| R20.7 | Generated materials must use URP shaders. Inspect after import; the Model Importer may create Standard (legacy) materials. | **REQ** | `[FACT]` URP project. `[INF]` Legacy Standard materials render pink in URP. May require manual shader assignment post-import. |

---

## 21. Prefab Authoring Rules

| # | Requirement | Class | Evidence |
|---|-------------|-------|----------|
| R21.1 | AR objects must be Prefab Variants of the imported model asset, not scene instances saved as prefabs. | **REQ** | `[FACT]` Only Prefab Variants maintain a live link to the source model; updates to the model propagate. |
| R21.2 | Interaction components (Collider, Rigidbody, XRGrabInteractable, ARTransformer) must be added as overrides on the Variant root. | **REQ** | `[OBS]` This is the approach used for Rob_Walk and MH_Sam_full. `[FACT]` The model root is read-only; Variant overrides are the only mechanism. |
| R21.3 | Do not nest the FBX model as a child of a new empty root. | **OPT** | `[INF]` This would add an extra Transform hierarchy level and require the BoxCollider to be sized differently. Variant-of-FBX is simpler. Acceptable if the project needs it, but requires re-evaluating R2.3. |
| R21.4 | Prefab must be saved to `Assets/Prefab/` following existing project convention. | **OPT** | `[OBS]` Existing custom prefabs in `Assets/Prefab/`. |

---

## 22. Prefab Validation Checklist

Use before adding any new prefab to ObjectSpawner:

```
PRE-IMPORT
□ FBX import: importCameras = 0
□ FBX import: importLights = 0 (unless intentional)
□ FBX import: globalScale appropriate for AR world scale
□ FBX import: animationType matches intended animation approach
□ FBX import: importAnimation = 1 if animation clips needed

POST-IMPORT
□ No Camera component in prefab hierarchy: GetComponentsInChildren<Camera>(true).Length == 0
□ No Light component (unless intentional)
□ Materials use URP shaders (not Standard / pink)

PREFAB VARIANT STRUCTURE
□ Root is Prefab Variant (not a raw model asset or scene GameObject)
□ Root localPosition = (0, 0, 0)
□ Root localRotation = (0, 0, 0)
□ Root localScale = (1, 1, 1)
□ Root layer = 0 (Default)

PHYSICS
□ BoxCollider or MeshCollider on root or child
□ Collider.isTrigger = False
□ Collider bounds encapsulate visible geometry
□ Rigidbody on root
□ Rigidbody.isKinematic = True
□ Rigidbody.useGravity = False

INTERACTION
□ XRGrabInteractable on root
□ movementType = Instantaneous
□ trackPosition = True
□ trackRotation = True
□ trackScale = True
□ addDefaultGrabTransformers = True
□ useDynamicAttach = True
□ matchAttachPosition = True
□ matchAttachRotation = False
□ throwOnDetach = False
□ interactionLayers = 1 (Default)
□ selectMode = Single
□ selectEntered persistent event count = 0
□ startingSingleGrabTransformers list = empty
□ startingMultipleGrabTransformers list = empty

AR TRANSFORMER
□ ARTransformer on root (same GO as XRGrabInteractable)
□ objectPlaneTranslationMode = 2 (Any Plane)
□ minScale > 0 (recommended: 0.2–0.3)
□ maxScale > minScale (recommended: 2.0–5.0)

ANIMATION (if applicable)
□ Animator component on root (or named bone root)
□ AnimatorController asset assigned
□ AnimationClip assigned to a state in the controller
□ animationType in import settings matches controller type

SPAWNER REGISTRATION
□ Prefab added to ObjectSpawner.m_ObjectPrefabs at the correct index
□ Corresponding Object Menu button icon updated to match prefab
```

---

## A. Minimum Static AR Object

An object that can be spawned and placed but NOT selected or manipulated after placement.

```
Requirements: R1.1, R1.2, R1.5, R12.1, R18.1, R18.2, R20.3, R21.1

Components:
  Root: Transform, MeshFilter, MeshRenderer (or child renderers)

Notes:
  ObjectSpawner.Instantiate() works on any valid prefab.
  Object cannot be re-selected, moved, rotated, scaled, or deleted.
  No Collider = tapping it spawns a new object on the plane behind it.
```

---

## B. Minimum Manipulable AR Object

An object that can be spawned, selected, moved, rotated, scaled, and deleted.

```
Requirements: All REQ requirements from sections 1–16, 18, 20–21.

Minimum component stack on root:
  Transform          (localPos=0, localRot=0, localScale=1)
  BoxCollider        (sized to geometry, isTrigger=False)
  Rigidbody          (isKinematic=True, useGravity=False)
  XRGrabInteractable (config per section 5)
  ARTransformer      (config per section 6)
  MeshFilter         (or child renderers for geometry)
  MeshRenderer

Notes:
  No Interaction Affordance (no visual highlight on hover/select).
  No AttachTransform child (root used as attach point).
  No Visuals child (predictedVisualsTransform = null).
  Functionally complete for AR manipulation.
```

---

## C. Fully Template-Compatible Object

An object that matches the CubeVariant component stack exactly.

```
Requirements: All REQ + all TMPL requirements.

Component stack on root:
  Transform
  Rigidbody          (isKinematic=True, useGravity=False)
  XRGrabInteractable (full CubeVariant config, AttachTransform referenced)
  ARTransformer      (full CubeVariant config)

Children:
  AttachTransform/   [empty GO, Transform only]
  Visuals/           [MeshFilter + MeshRenderer; referenced by predictedVisualsTransform]
  Cube_COL/          [MeshCollider; physics shape]
  Interaction Affordance/
    [XRInteractableAffordanceStateProvider → interactableSource = root XRGrabInteractable]
    Color Affordance/
      [MaterialPropertyBlockHelper + ColorMaterialPropertyAffordanceReceiver]

UI:
  Object Menu button icon manually updated to show the object's 2D icon.

Notes:
  Interaction Affordance subsystem requires assets from the template.
  The affordance color receiver requires the mesh to use a compatible shader/material setup.
```

---

## D. Animated AR Object

A manipulable object that also plays a looping animation.

```
Requirements: B (Minimum manipulable) + R19.2, R19.3, R19.4, R19.6.

Additional components:
  Animator on root (or skeleton root child)
    RuntimeAnimatorController → [AnimatorController asset]
      State: "Walk" / "Idle"
        Motion: [AnimationClip from FBX sub-asset]

FBX import:
  importAnimation = 1
  animationType = 2 (Generic) or 3 (Humanoid)
  clipAnimations = [explicit definitions] OR auto-generated from takes

Notes:
  ARTransformer manipulates the root Transform.
  Animator drives child bone Transforms.
  Both operate on separate transforms and should not conflict [UNV — unverified on device].
  importCameras = 0 is MANDATORY (see R18.2).
```

---

## E. Recommended Team Cero Asset-Authoring Pipeline

```
1. SOURCE ASSET
   Export FBX from DCC tool.
   Ensure: correct world scale, correct up-axis, no embedded cameras, no embedded lights.
   Target: 1 FBX unit ≈ 1 meter in AR world.

2. IMPORT
   Place FBX in Assets/3dModels/.
   Set import settings in Inspector:
     importCameras = 0
     importLights = 0
     globalScale = 1 (adjust if model is in cm/mm)
     animationType = Generic (if animated), None (if static)
     importAnimation = 1 (if animated)
   Apply.

3. VERIFY IMPORT
   Inspect: GetComponentsInChildren<Camera>(true).Length == 0
   Inspect: Materials are URP-compatible (not pink).
   If pink: manually reassign shader to URP/Lit on generated material assets.

4. CREATE PREFAB VARIANT
   In Project window, right-click FBX → Create > Prefab Variant.
   Save to Assets/Prefab/.

5. ADD INTERACTION COMPONENTS (via MCP or Editor)
   On root:
     BoxCollider      — size to geometry bounds
     Rigidbody        — isKinematic=True, useGravity=False
     XRGrabInteractable — per section 5 config
     ARTransformer    — per section 6 config

6. ADD ANIMATION (if needed)
   Create AnimatorController asset in Assets/Animation/ (or similar).
   Add state referencing the FBX AnimationClip sub-asset.
   Add Animator component to root; assign controller.

7. VERIFY PREFAB
   Run validation checklist (section 22).
   Open prefab in isolation; confirm no Camera in hierarchy.

8. REGISTER WITH SPAWNER
   Add prefab to ObjectSpawner.m_ObjectPrefabs list (via Editor).
   Note the index.
   Update the corresponding Object Menu button icon.

9. TEST ON DEVICE
   Build to Android.
   Confirm: spawn, select, move, rotate, scale, delete.
   Confirm: no rendering corruption (no active Camera in hierarchy).
   Confirm: animation plays (if applicable).
```

---

## F. Final Notes

### What is NOT covered by this contract
- Interaction affordance visual subsystem (affordance color, hover highlight) — requires separate investigation.
- `predictedVisualsTransform` effect on grab smoothness — unverified.
- `AttachTransform` child necessity with dynamic attach — unverified.
- Rigged/animated object root-manipulation compatibility — unverified on device.
- Multi-object interaction or UI state reset behavior.

### Versions this contract was validated against
- Unity 6000.5.10f1
- AR Foundation 6.5.0
- XR Interaction Toolkit 3.5.1
- ARCore XR Plugin 6.5.0

### Re-validation triggers
This contract should be re-validated if any of the following change:
- XRI package version (especially major version bump)
- AR Foundation package version
- Mobile AR Template scene structure
- ObjectSpawner or ARInteractorSpawnTrigger source code
- Screen Space Ray Interactor configuration in the scene

---

*Last verified: 1 September 2026*
