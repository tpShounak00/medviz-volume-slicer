using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriPlanarVolumeController : MonoBehaviour
{
    public enum TransferPreset { Grayscale, Bone }

    [Header("View RawImages")]
    [SerializeField] private RawImage axialImage;
    [SerializeField] private RawImage coronalImage;
    [SerializeField] private RawImage sagittalImage;

    [Header("UI")]
    [SerializeField] private Slider sliceSlider;
    [SerializeField] private TMP_Text sliceLabel;

    [Header("Optional Crosshair (UI Image overlays)")]
    [SerializeField] private RectTransform axialCrosshair;
    [SerializeField] private RectTransform coronalCrosshair;
    [SerializeField] private RectTransform sagittalCrosshair;

    [Header("Volume Size")]
    [SerializeField] private int width = 192;
    [SerializeField] private int height = 192;
    [SerializeField] private int depth = 128;

    [Header("Preset")]
    [SerializeField] private TransferPreset preset = TransferPreset.Grayscale;

    private float[,,] _vol;

    private Texture2D _axTex;
    private Texture2D _coTex;
    private Texture2D _saTex;

    // Slice indices for each plane
    private int _z; // axial
    private int _y; // coronal
    private int _x; // sagittal
    
    [Header("Plane Buttons (optional highlight)")]
    [SerializeField] private Button axialModeButton;
    [SerializeField] private Button coronalModeButton;
    [SerializeField] private Button sagittalModeButton;


    private void SetButtonInteractable(Button b, bool interactable)
    {
        if (b != null) b.interactable = interactable;
    }

    private void Start()
    {
        _vol = SyntheticVolumeGenerator.Generate(width, height, depth, seed: 1024);

        // start at center
        _x = width / 2;
        _y = height / 2;
        _z = depth / 2;

        SetupSlider();
        RenderAll();
        UpdateLabel();
        UpdateCrosshairs();
    }

    public void SetPreset(int presetIndex)
    {
        preset = (TransferPreset)presetIndex;
        RenderAll();
        UpdateLabel();
    }

    private void SetupSlider()
    {
        if (sliceSlider == null) return;

        // One slider controls the "active" plane index — simplest: control axial (Z) by default.
        sliceSlider.onValueChanged.RemoveAllListeners();
        sliceSlider.wholeNumbers = true;
        sliceSlider.minValue = 0;
        sliceSlider.maxValue = depth - 1;
        sliceSlider.value = _z;

        sliceSlider.onValueChanged.AddListener(v =>
        {
            _z = (int)v;
            RenderAxial();
            RenderCoronal();  // axial change affects crosshair line, keep views consistent
            RenderSagittal();
            UpdateLabel();
            UpdateCrosshairs();
        });
    }

    // Optional: call these from buttons later if you want a plane selector.
    public void SetAxialMode()
    {
        if (sliceSlider == null) return;
        sliceSlider.maxValue = depth - 1;
        sliceSlider.value = _z;
        sliceSlider.onValueChanged.RemoveAllListeners();
        sliceSlider.onValueChanged.AddListener(v =>
        {
            _z = (int)v;
            RenderAll();
            UpdateLabel();
            UpdateCrosshairs();
        });
        
        SetButtonInteractable(axialModeButton, false);
        SetButtonInteractable(coronalModeButton, true);
        SetButtonInteractable(sagittalModeButton, true);

    }

    public void SetCoronalMode()
    {
        if (sliceSlider == null) return;
        sliceSlider.maxValue = height - 1;
        sliceSlider.value = _y;
        sliceSlider.onValueChanged.RemoveAllListeners();
        sliceSlider.onValueChanged.AddListener(v =>
        {
            _y = (int)v;
            RenderAll();
            UpdateLabel();
            UpdateCrosshairs();
        });
        
        SetButtonInteractable(axialModeButton, true);
        SetButtonInteractable(coronalModeButton, false);
        SetButtonInteractable(sagittalModeButton, true);

    }

    public void SetSagittalMode()
    {
        if (sliceSlider == null) return;
        sliceSlider.maxValue = width - 1;
        sliceSlider.value = _x;
        sliceSlider.onValueChanged.RemoveAllListeners();
        sliceSlider.onValueChanged.AddListener(v =>
        {
            _x = (int)v;
            RenderAll();
            UpdateLabel();
            UpdateCrosshairs();
        });
        
        SetButtonInteractable(axialModeButton, true);
        SetButtonInteractable(coronalModeButton, true);
        SetButtonInteractable(sagittalModeButton, false);

    }

    private void RenderAll()
    {
        RenderAxial();
        RenderCoronal();
        RenderSagittal();
    }

    private void RenderAxial()
    {
        // Axial: XY at Z
        EnsureTex(ref _axTex, width, height);
        var px = new Color32[width * height];

        int i = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            byte g = ApplyTransfer(_vol[x, y, _z]);
            px[i++] = new Color32(g, g, g, 255);
        }

        _axTex.SetPixels32(px);
        _axTex.Apply(false, false);
        if (axialImage != null) axialImage.texture = _axTex;
    }

    private void RenderCoronal()
    {
        // Coronal: XZ at Y
        EnsureTex(ref _coTex, width, depth);
        var px = new Color32[width * depth];

        int i = 0;
        for (int z = 0; z < depth; z++)
        for (int x = 0; x < width; x++)
        {
            byte g = ApplyTransfer(_vol[x, _y, z]);
            px[i++] = new Color32(g, g, g, 255);
        }

        _coTex.SetPixels32(px);
        _coTex.Apply(false, false);
        if (coronalImage != null) coronalImage.texture = _coTex;
    }

    private void RenderSagittal()
    {
        // Sagittal: YZ at X
        EnsureTex(ref _saTex, height, depth);
        var px = new Color32[height * depth];

        int i = 0;
        for (int z = 0; z < depth; z++)
        for (int y = 0; y < height; y++)
        {
            byte g = ApplyTransfer(_vol[_x, y, z]);
            px[i++] = new Color32(g, g, g, 255);
        }

        _saTex.SetPixels32(px);
        _saTex.Apply(false, false);
        if (sagittalImage != null) sagittalImage.texture = _saTex;
    }

    private byte ApplyTransfer(float d)
    {
        d = Mathf.Clamp01(d);

        if (preset == TransferPreset.Grayscale)
            return (byte)(d * 255f);

        float bone = Mathf.InverseLerp(0.55f, 0.95f, d);
        bone = Mathf.Clamp01(bone);
        return (byte)(bone * 255f);
    }

    private void UpdateLabel()
    {
        if (sliceLabel == null) return;
        sliceLabel.text = $"X(sag): {_x}/{width-1}   Y(cor): {_y}/{height-1}   Z(ax): {_z}/{depth-1}   | Preset: {preset}";
    }

    private void UpdateCrosshairs()
    {
        // Very simple: position crosshair to represent the other two indices.
        // You can keep it minimal: only show on axial for demo.
        if (axialCrosshair != null)
        {
            // normalized position inside axial view
            float nx = (float)_x / (width - 1);
            float ny = (float)_y / (height - 1);
            SetCrosshairNormalized(axialCrosshair, nx, ny);
        }

        if (coronalCrosshair != null)
        {
            float nx = (float)_x / (width - 1);
            float nz = (float)_z / (depth - 1);
            SetCrosshairNormalized(coronalCrosshair, nx, nz);
        }

        if (sagittalCrosshair != null)
        {
            float ny = (float)_y / (height - 1);
            float nz = (float)_z / (depth - 1);
            SetCrosshairNormalized(sagittalCrosshair, ny, nz);
        }
    }

    private void SetCrosshairNormalized(RectTransform crosshair, float nx, float ny)
    {
        // Assumes crosshair is anchored center in its parent
        var parent = crosshair.parent as RectTransform;
        if (parent == null) return;

        float x = (nx - 0.5f) * parent.rect.width;
        float y = (ny - 0.5f) * parent.rect.height;
        crosshair.anchoredPosition = new Vector2(x, y);
    }

    private void EnsureTex(ref Texture2D tex, int w, int h)
    {
        if (tex != null && tex.width == w && tex.height == h) return;
        if (tex != null) Destroy(tex);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
    }

    private void OnDestroy()
    {
        if (_axTex != null) Destroy(_axTex);
        if (_coTex != null) Destroy(_coTex);
        if (_saTex != null) Destroy(_saTex);
    }
}
