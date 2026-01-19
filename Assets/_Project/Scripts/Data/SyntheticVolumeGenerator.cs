using UnityEngine;

public static class SyntheticVolumeGenerator
{
    // Volume is stored as normalized density [0..1]
    public static float[,,] Generate(int w, int h, int d, int seed = 1234)
    {
        var vol = new float[w, h, d];
        var rng = new System.Random(seed);

        // Base noise + a few gaussian blobs (organs-ish)
        for (int z = 0; z < d; z++)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = (x - w * 0.5f) / (w * 0.5f);
            float ny = (y - h * 0.5f) / (h * 0.5f);
            float nz = (z - d * 0.5f) / (d * 0.5f);

            // Body-ish falloff
            float r = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
            float body = Mathf.Clamp01(1f - r);

            // Fine noise
            float noise = (float)rng.NextDouble() * 0.12f;

            vol[x, y, z] = Mathf.Clamp01(body * 0.55f + noise);
        }

        // Add blobs (bright structures)
        AddBlob(vol, w, h, d, cx: 0.10f, cy: 0.05f, cz: 0.00f, radius: 0.25f, intensity: 0.7f);
        AddBlob(vol, w, h, d, cx: -0.15f, cy: -0.10f, cz: 0.10f, radius: 0.20f, intensity: 0.9f);

        // Add "bone ring" near edges
        AddShell(vol, w, h, d, inner: 0.65f, outer: 0.78f, intensity: 0.9f);

        return vol;
    }

    private static void AddBlob(float[,,] vol, int w, int h, int d, float cx, float cy, float cz, float radius, float intensity)
    {
        for (int z = 0; z < d; z++)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = (x - w * 0.5f) / (w * 0.5f);
            float ny = (y - h * 0.5f) / (h * 0.5f);
            float nz = (z - d * 0.5f) / (d * 0.5f);

            float dx = nx - cx;
            float dy = ny - cy;
            float dz = nz - cz;

            float dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
            float t = Mathf.Clamp01(1f - dist / radius);
            float add = t * t * intensity;

            vol[x, y, z] = Mathf.Clamp01(vol[x, y, z] + add);
        }
    }

    private static void AddShell(float[,,] vol, int w, int h, int d, float inner, float outer, float intensity)
    {
        for (int z = 0; z < d; z++)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = (x - w * 0.5f) / (w * 0.5f);
            float ny = (y - h * 0.5f) / (h * 0.5f);
            float nz = (z - d * 0.5f) / (d * 0.5f);

            float r = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
            if (r > inner && r < outer)
                vol[x, y, z] = Mathf.Clamp01(vol[x, y, z] + intensity * 0.35f);
        }
    }
}
