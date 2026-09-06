using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BuildingHologramPreviewUI : MonoBehaviour
{
    private const string PreviewImageName = "HologramModelPreview";
    private const int PreviewLayer = 31;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");
    private static readonly int FresnelIntensityId = Shader.PropertyToID("_FresnelIntensity");
    private static readonly int MeshGlowIntensityId = Shader.PropertyToID("_MeshGlowIntensity");
    private static readonly int SurfaceGridScaleId = Shader.PropertyToID("_SurfaceGridScale");
    private static readonly int SurfaceGridWidthId = Shader.PropertyToID("_SurfaceGridWidth");
    private static readonly int SurfaceGridStrengthId = Shader.PropertyToID("_SurfaceGridStrength");
    private static readonly int ScanlineDensityId = Shader.PropertyToID("_ScanlineDensity");
    private static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
    private static readonly int ScanlineStrengthId = Shader.PropertyToID("_ScanlineStrength");

    [SerializeField] private RawImage previewImage;
    [SerializeField] private float rotationSpeed = 35f;

    [Header("Render Texture")]
    [Tooltip("Optional external RenderTexture asset. If empty, this component creates one at runtime.")]
    [SerializeField] private RenderTexture targetRenderTexture;
    [SerializeField] private bool createRenderTextureIfMissing = true;
    [Min(64)] [SerializeField] private int renderTextureWidth = 512;
    [Min(64)] [SerializeField] private int renderTextureHeight = 512;
    [SerializeField] private int renderTextureDepthBits = 24;
    [SerializeField] private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;
    [SerializeField] private RenderTextureReadWrite renderTextureReadWrite = RenderTextureReadWrite.Default;
    [SerializeField] private FilterMode renderTextureFilterMode = FilterMode.Bilinear;
    [SerializeField] private int renderTextureAntiAliasing = 4;
    [SerializeField] private bool renderTextureUseMipMap;

    [Header("Camera Settings")]
    [Tooltip("Use orthographic camera when checked. Perspective camera is used when unchecked.")]
    [SerializeField] private bool orthographic = true;
    [SerializeField] private float orthographicSize = 1.1f;
    [Range(1f, 179f)] [SerializeField] private float fieldOfView = 60f;
    [SerializeField] private Vector3 cameraEuler = new Vector3(18f, 0f, 0f);
    [SerializeField] private float cameraDistance = 6f;
    [Min(0.001f)] [SerializeField] private float nearClipPlane = 0.01f;
    [Min(0.01f)] [SerializeField] private float farClipPlane = 50f;
    [SerializeField] private bool allowHDR = true;
    [SerializeField] private bool allowMSAA = true;
    [Tooltip("Set alpha to 0 for transparent preview background.")]
    [SerializeField] private Color clearColor = new Color(0f, 0f, 0f, 0f);

    [Header("Model Framing")]
    [SerializeField] private Vector3 modelEuler = new Vector3(0f, -35f, 0f);
    [SerializeField] private Vector3 modelOffset;
    [SerializeField] private bool fitModelToFrame = true;
    [Min(0.01f)] [SerializeField] private float fitTargetSize = 1.45f;
    [Min(0.01f)] [SerializeField] private float modelScaleMultiplier = 1f;

    [Header("Hologram Effect")]
    [SerializeField] private Color hologramColor = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    [Range(0f, 1f)] [SerializeField] private float hologramAlpha = 0.55f;
    [Range(0f, 8f)] [SerializeField] private float emissionIntensity = 4f;
    [Range(0.5f, 8f)] [SerializeField] private float fresnelPower = 2f;
    [Range(0f, 5f)] [SerializeField] private float fresnelIntensity = 2.4f;
    [Range(0f, 6f)] [SerializeField] private float meshGlowIntensity = 2.1f;

    [Header("Hologram Surface Lines")]
    [Range(1f, 80f)] [SerializeField] private float surfaceGridScale = 14f;
    [Range(0.001f, 0.08f)] [SerializeField] private float surfaceGridWidth = 0.018f;
    [Range(0f, 3f)] [SerializeField] private float surfaceGridStrength = 1.25f;
    [Range(1f, 80f)] [SerializeField] private float scanlineDensity = 28f;
    [Range(-10f, 10f)] [SerializeField] private float scanlineSpeed = 1.8f;
    [Range(0f, 1f)] [SerializeField] private float scanlineStrength = 0.32f;

    private static int nextRigIndex;
    private static Material sharedHologramMaterial;

    private BuildingData currentBuilding;
    private RenderTexture ownedRenderTexture;
    private GameObject rigRoot;
    private Transform pivot;
    private Camera previewCamera;
    private GameObject spawnedModel;
    private Renderer[] modelRenderers;
    private MaterialPropertyBlock materialPropertyBlock;
    private bool hasPreview;
    private bool warnedMissingRenderTexture;

    private void Update()
    {
        if (!hasPreview || pivot == null || previewCamera == null)
        {
            return;
        }

        pivot.Rotate(Vector3.up, rotationSpeed * Time.unscaledDeltaTime, Space.World);
        ApplyCameraSettings();
        PositionCamera();
        EnsureRenderTexture();
    }

    private void OnValidate()
    {
        ClampRenderTextureSettings();

        if (previewCamera != null)
        {
            ApplyCameraSettings();
            PositionCamera();
        }

        if (spawnedModel != null)
        {
            FitModelToPreview();
        }

        ApplyEffectSettings();
    }

    private void OnDisable()
    {
        ClearPreview();
    }

    private void OnDestroy()
    {
        ClearPreview();
    }

    public void Show(BuildingData building, float alpha = 1f)
    {
        currentBuilding = building;

        if (building == null || building.IsEmptySlot())
        {
            ClearPreview();
            return;
        }

        GameObject prefab = building.LoadHologramPrefab();
        if (prefab == null)
        {
            ClearPreview();
            return;
        }

        ShowPrefabInternal(prefab, alpha);
    }

    public void ShowPrefab(GameObject prefab, float alpha = 1f)
    {
        currentBuilding = null;
        ShowPrefabInternal(prefab, alpha);
    }

    private void ShowPrefabInternal(GameObject prefab, float alpha)
    {
        if (prefab == null)
        {
            ClearPreview();
            return;
        }

        EnsureSharedMaterial();
        EnsurePreviewImage();
        DisablePreviewRaycasts();
        EnsureRig();
        EnsureRenderTexture();

        if (spawnedModel == null || spawnedModel.name != prefab.name)
        {
            RebuildModel(prefab);
        }

        SetVisible(true);
        SetAlpha(alpha);
        ApplyEffectSettings();
        hasPreview = true;
    }

    public void ConfigurePreviewProfile(int textureWidth, int textureHeight, float orthoSize, float targetSize, float scaleMultiplier, float rotation)
    {
        renderTextureWidth = Mathf.Max(64, textureWidth);
        renderTextureHeight = Mathf.Max(64, textureHeight);
        orthographic = true;
        orthographicSize = Mathf.Max(0.01f, orthoSize);
        fitTargetSize = Mathf.Max(0.01f, targetSize);
        modelScaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
        rotationSpeed = rotation;

        ClampRenderTextureSettings();

        if (previewCamera != null)
        {
            ApplyCameraSettings();
            PositionCamera();
        }

        if (spawnedModel != null)
            FitModelToPreview();

        EnsureRenderTexture();
        ApplyEffectSettings();
    }

    public void ClearPreview()
    {
        currentBuilding = null;
        hasPreview = false;
        SetVisible(false);
        ClearModel();
        DestroyRig();
        ReleaseRenderTexture();
    }

    private void ClampRenderTextureSettings()
    {
        renderTextureWidth = Mathf.Max(64, renderTextureWidth);
        renderTextureHeight = Mathf.Max(64, renderTextureHeight);
        renderTextureDepthBits = Mathf.Max(0, renderTextureDepthBits);
        renderTextureAntiAliasing = NormalizeAntiAliasing(renderTextureAntiAliasing);
        farClipPlane = Mathf.Max(nearClipPlane + 0.01f, farClipPlane);
    }

    private void EnsureSharedMaterial()
    {
        if (sharedHologramMaterial == null)
        {
            sharedHologramMaterial = Resources.Load<Material>("Materials/HologramBuilding");
        }
    }

    private void EnsurePreviewImage()
    {
        if (previewImage != null)
        {
            return;
        }

        previewImage = GetComponent<RawImage>();
        if (previewImage != null)
        {
            previewImage.raycastTarget = false;
            return;
        }

        Transform existing = transform.Find(PreviewImageName);
        if (existing != null)
        {
            previewImage = existing.GetComponent<RawImage>();
        }

        if (previewImage == null)
        {
            GameObject imageObject = new GameObject(PreviewImageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(transform, false);

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            previewImage = imageObject.GetComponent<RawImage>();
            previewImage.raycastTarget = false;
        }
    }

    private void DisablePreviewRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private void EnsureRig()
    {
        if (rigRoot != null && previewCamera != null && pivot != null)
        {
            return;
        }

        int rigIndex = nextRigIndex++;
        Vector3 rigPosition = new Vector3(10000f + rigIndex * 12f, -10000f, 10000f);

        rigRoot = new GameObject($"BuildingHologramPreviewRig_{rigIndex}");
        rigRoot.hideFlags = HideFlags.HideAndDontSave;
        rigRoot.transform.position = rigPosition;

        GameObject pivotObject = new GameObject("Pivot");
        pivotObject.hideFlags = HideFlags.HideAndDontSave;
        pivotObject.transform.SetParent(rigRoot.transform, false);
        pivot = pivotObject.transform;

        GameObject cameraObject = new GameObject("Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(rigRoot.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.useOcclusionCulling = false;

#if UNITY_2019_1_OR_NEWER
        var cameraData = previewCamera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;
            cameraData.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            cameraData.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            cameraData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
        }
#endif

        ApplyCameraSettings();
        PositionCamera();
    }

    private void ApplyCameraSettings()
    {
        if (previewCamera == null)
        {
            return;
        }

        previewCamera.orthographic = orthographic;
        previewCamera.orthographicSize = orthographicSize;
        previewCamera.fieldOfView = fieldOfView;
        previewCamera.backgroundColor = clearColor;
        previewCamera.nearClipPlane = nearClipPlane;
        previewCamera.farClipPlane = farClipPlane;
        previewCamera.allowHDR = allowHDR;
        previewCamera.allowMSAA = allowMSAA;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.cullingMask = 1 << PreviewLayer;
    }

    private void PositionCamera()
    {
        if (previewCamera == null || rigRoot == null)
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(cameraEuler);
        previewCamera.transform.SetPositionAndRotation(
            rigRoot.transform.position + rotation * new Vector3(0f, 0f, -cameraDistance),
            rotation);
    }

    private void EnsureRenderTexture()
    {
        if (previewImage == null || previewCamera == null)
        {
            return;
        }

        RenderTexture renderTexture;
        if (targetRenderTexture != null)
        {
            ReleaseOwnedRenderTexture();
            renderTexture = targetRenderTexture;
        }
        else
        {
            renderTexture = GetOrCreateOwnedRenderTexture();
        }

        if (renderTexture == null)
        {
            if (!warnedMissingRenderTexture)
            {
                Debug.LogWarning("[BuildingHologramPreviewUI] No RenderTexture is available. Assign Target Render Texture or enable Create Render Texture If Missing.");
                warnedMissingRenderTexture = true;
            }

            return;
        }

        warnedMissingRenderTexture = false;
        previewCamera.targetTexture = renderTexture;
        previewImage.texture = renderTexture;
    }

    private RenderTexture GetOrCreateOwnedRenderTexture()
    {
        if (!createRenderTextureIfMissing)
        {
            ReleaseOwnedRenderTexture();
            return null;
        }

        ClampRenderTextureSettings();
        int antiAliasing = NormalizeAntiAliasing(renderTextureAntiAliasing);
        bool needsNewTexture = ownedRenderTexture == null
            || ownedRenderTexture.width != renderTextureWidth
            || ownedRenderTexture.height != renderTextureHeight
            || ownedRenderTexture.depth != renderTextureDepthBits
            || ownedRenderTexture.format != renderTextureFormat
            || ownedRenderTexture.antiAliasing != antiAliasing
            || ownedRenderTexture.useMipMap != renderTextureUseMipMap
            || ownedRenderTexture.filterMode != renderTextureFilterMode;

        if (!needsNewTexture)
        {
            return ownedRenderTexture;
        }

        ReleaseOwnedRenderTexture();

        ownedRenderTexture = new RenderTexture(
            renderTextureWidth,
            renderTextureHeight,
            renderTextureDepthBits,
            renderTextureFormat,
            renderTextureReadWrite)
        {
            name = $"{name}_BuildingHologramPreviewRT",
            hideFlags = HideFlags.HideAndDontSave,
            antiAliasing = antiAliasing,
            useMipMap = renderTextureUseMipMap,
            autoGenerateMips = renderTextureUseMipMap,
            filterMode = renderTextureFilterMode,
            wrapMode = TextureWrapMode.Clamp
        };

        ownedRenderTexture.Create();
        return ownedRenderTexture;
    }

    private void RebuildModel(GameObject prefab)
    {
        ClearModel();

        spawnedModel = Instantiate(prefab, pivot, false);
        spawnedModel.name = prefab.name;
        spawnedModel.hideFlags = HideFlags.HideAndDontSave;
        SetHideFlagsAndLayerRecursively(spawnedModel.transform, PreviewLayer);

        modelRenderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
        ApplyHologramMaterial();
        FitModelToPreview();
        ApplyEffectSettings();
    }

    private void ApplyHologramMaterial()
    {
        if (sharedHologramMaterial == null || modelRenderers == null)
        {
            return;
        }

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer modelRenderer = modelRenderers[i];
            if (modelRenderer == null)
            {
                continue;
            }

            Material[] materials = modelRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                modelRenderer.sharedMaterial = sharedHologramMaterial;
                continue;
            }

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = sharedHologramMaterial;
            }

            modelRenderer.sharedMaterials = materials;
        }
    }

    public void ApplyEffectSettings()
    {
        if (spawnedModel == null)
        {
            return;
        }

        if (modelRenderers == null || modelRenderers.Length == 0)
        {
            modelRenderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
        }

        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer modelRenderer = modelRenderers[i];
            if (modelRenderer == null)
            {
                continue;
            }

            modelRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetColor(BaseColorId, hologramColor);
            materialPropertyBlock.SetFloat(AlphaId, hologramAlpha);
            materialPropertyBlock.SetFloat(EmissionIntensityId, emissionIntensity);
            materialPropertyBlock.SetFloat(FresnelPowerId, fresnelPower);
            materialPropertyBlock.SetFloat(FresnelIntensityId, fresnelIntensity);
            materialPropertyBlock.SetFloat(MeshGlowIntensityId, meshGlowIntensity);
            materialPropertyBlock.SetFloat(SurfaceGridScaleId, surfaceGridScale);
            materialPropertyBlock.SetFloat(SurfaceGridWidthId, surfaceGridWidth);
            materialPropertyBlock.SetFloat(SurfaceGridStrengthId, surfaceGridStrength);
            materialPropertyBlock.SetFloat(ScanlineDensityId, scanlineDensity);
            materialPropertyBlock.SetFloat(ScanlineSpeedId, scanlineSpeed);
            materialPropertyBlock.SetFloat(ScanlineStrengthId, scanlineStrength);
            modelRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

    private void FitModelToPreview()
    {
        if (spawnedModel == null || pivot == null || rigRoot == null)
        {
            return;
        }

        pivot.position = rigRoot.transform.position;
        pivot.rotation = Quaternion.Euler(modelEuler);
        pivot.localScale = Vector3.one;

        if (!TryGetRendererBounds(spawnedModel, out Bounds bounds))
        {
            return;
        }

        Vector3 offset = rigRoot.transform.position + modelOffset - bounds.center;
        spawnedModel.transform.position += offset;

        if (!TryGetRendererBounds(spawnedModel, out bounds))
        {
            return;
        }

        float largestSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        float fitScale = fitModelToFrame && largestSize > 0.0001f
            ? fitTargetSize / largestSize
            : 1f;

        pivot.localScale = Vector3.one * fitScale * modelScaleMultiplier;
    }

    private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void SetVisible(bool visible)
    {
        if (previewImage != null)
        {
            previewImage.enabled = visible;
        }

        if (previewCamera != null)
        {
            previewCamera.enabled = visible;
        }

        if (spawnedModel != null)
        {
            spawnedModel.SetActive(visible);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (previewImage == null)
        {
            return;
        }

        Color color = previewImage.color;
        color.a = Mathf.Clamp01(alpha);
        previewImage.color = color;
    }

    private void ClearModel()
    {
        if (spawnedModel == null)
        {
            return;
        }

        DestroyPreviewObject(spawnedModel);
        spawnedModel = null;
        modelRenderers = null;
    }

    private void DestroyRig()
    {
        if (rigRoot == null)
        {
            return;
        }

        DestroyPreviewObject(rigRoot);
        rigRoot = null;
        pivot = null;
        previewCamera = null;
    }

    private void ReleaseRenderTexture()
    {
        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }

        if (previewImage != null)
        {
            previewImage.texture = null;
        }

        ReleaseOwnedRenderTexture();
    }

    private void ReleaseOwnedRenderTexture()
    {
        if (ownedRenderTexture == null)
        {
            return;
        }

        if (previewCamera != null && previewCamera.targetTexture == ownedRenderTexture)
        {
            previewCamera.targetTexture = null;
        }

        if (previewImage != null && previewImage.texture == ownedRenderTexture)
        {
            previewImage.texture = null;
        }

        ownedRenderTexture.Release();
        DestroyPreviewObject(ownedRenderTexture);
        ownedRenderTexture = null;
    }

    private static int NormalizeAntiAliasing(int value)
    {
        if (value <= 1)
            return 1;

        if (value <= 2)
            return 2;

        if (value <= 4)
            return 4;

        return 8;
    }

    private static void DestroyPreviewObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private static void SetHideFlagsAndLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.hideFlags = HideFlags.HideAndDontSave;
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetHideFlagsAndLayerRecursively(root.GetChild(i), layer);
        }
    }
}
