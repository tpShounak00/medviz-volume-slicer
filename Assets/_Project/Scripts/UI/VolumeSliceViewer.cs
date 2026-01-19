using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSliceViewer : MonoBehaviour
{
    public enum ViewPlane { Axial, Coronal, Sagittal }
    public enum TransferPreset { Grayscale, Bone }

    [Header("UI")]
    [SerializeField] private RawImage sliceImage;
    [SerializeField] private Slider sliceSlider;
    [SerializeField] private TMP_Text sliceLabel;

    [Header("Settings")]
    [SerializeField] private ViewPlane plane = ViewPlane.Axial;
    [SerializeField] private TransferPreset preset = TransferPreset.Grayscale;

    [Header("Volume Size")]
    [SerializeField] private int width = 192;
    [SerializeField] private int height = 192;
    [SerializeField] private int depth = 128;

    private float[,,] _vol;
    private Texture2D _tex;
    private int _maxIndex;

    private void Start()
    {
        _vol = SyntheticVolumeGenerator.Generate(width, height, depth, seed: 1024);

        _maxIndex = plane == ViewPlane.Axial ? depth - 1 :
                    plane == ViewPlane.Coronal ? height - 1 :
                    width - 1;

        SetupSlider();
        RenderSlice(0);
    }

    public void SetPreset(int presetIndex)
    {
        preset = (TransferPreset)presetIndex;
        RenderSlice((int)sliceSlider.value);
    }

    private void SetupSlider()
    {
        if (sliceSlider == null) return;

        sliceSlider.onValueChanged.RemoveAllListeners();
        sliceSlider.wholeNumbers = true;
        sliceSlider.minValue = 0;
        sliceSlider.maxValue = _maxIndex;
        sliceSlider.value = _maxIndex / 2;
        sliceSlider.onValueChanged.AddListener(v => RenderSlice((int)v));
    }

    private void EnsureTex(int w, int h)
    {
        if (_tex != null && _tex.width == w && _tex.height == h) return;
        if (_tex != null) Destroy(_tex);
        _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _tex.wrapMode = TextureWrapMode.Clamp;
        _tex.filterMode = FilterMode.Bilinear;
    }

    private void RenderSlice(int index)
    {
        index = Mathf.Clamp(index, 0, _maxIndex);

        int w = plane == ViewPlane.Sagittal ? depth : width;
        int h = plane == ViewPlane.Coronal ? depth : height;

        // For Axial: w=width, h=height
        // Coronal: w=width, h=depth
        // Sagittal: w=depth, h=height

        EnsureTex(w, h);

        var px = new Color32[w * h];

        int i = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float density = SampleDensity(x, y, index, w, h);
            byte g = ApplyTransfer(density);
            px[i++] = new Color32(g, g, g, 255);
        }

        _tex.SetPixels32(px);
        _tex.Apply(false, false);

        if (sliceImage != null) sliceImage.texture = _tex;
        if (sliceLabel != null)
            sliceLabel.text = $"{plane} Slice: {index} / {_maxIndex}  |  Preset: {preset}";
    }

    private float SampleDensity(int x, int y, int sliceIndex, int outW, int outH)
    {
        // Map output pixel coords to volume coords depending on plane
        // Axial: (x,y,z=slice)
        // Coronal: (x, y=slice, z=y)
        // Sagittal: (x=slice, y=y, z=x)

        if (plane == ViewPlane.Axial)
            return _vol[x, y, sliceIndex];

        if (plane == ViewPlane.Coronal)
        {
            int z = y; // output y maps to z
            return _vol[x, sliceIndex, z];
        }

        // Sagittal
        {
            int z = x; // output x maps to z
            return _vol[sliceIndex, y, z];
        }
    }

    private byte ApplyTransfer(float d)
    {
        d = Mathf.Clamp01(d);

        if (preset == TransferPreset.Grayscale)
            return (byte)(d * 255f);

        // Bone-ish: push highs brighter, suppress mids
        float bone = Mathf.InverseLerp(0.55f, 0.95f, d);
        bone = Mathf.Clamp01(bone);
        return (byte)(bone * 255f);
    }

    private void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }
}
