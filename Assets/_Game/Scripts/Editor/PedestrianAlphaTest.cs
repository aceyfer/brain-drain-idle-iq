using UnityEngine;
using UnityEditor;
using System.IO;

public class PedestrianAlphaTest : EditorWindow
{
    [MenuItem("Tools/Brain Drain/TEST Single Sprite Fix")]
    public static void TestSingleSprite()
    {
        string path = "Assets/_Game/Sprites/Pedestrians/Ped1_Stage1.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[AlphaTest] Could not find Ped1_Stage1.png at expected path.");
            return;
        }

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Texture2D sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int w = sourceTex.width;
        int h = sourceTex.height;
        Color[] srcPixels = sourceTex.GetPixels();

        Texture2D transparentTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        transparentTex.SetPixels(srcPixels);
        Color[] pixels = transparentTex.GetPixels();

        Color c0 = pixels[0];
        Color c1 = pixels[w - 1];
        Color c2 = pixels[(h - 1) * w];
        Color c3 = pixels[h * w - 1];

        bool[] visited = new bool[pixels.Length];
        System.Collections.Generic.Queue<int> queue = new System.Collections.Generic.Queue<int>();

        int[] startCoords = { 0, 0, w - 1, 0, 0, h - 1, w - 1, h - 1 };
        for (int idx = 0; idx < startCoords.Length; idx += 2)
        {
            int sx = startCoords[idx];
            int sy = startCoords[idx + 1];
            int si = sy * w + sx;
            queue.Enqueue(si);
            visited[si] = true;
        }

        float tolerance = 0.15f;
        int clearedCount = 0;

        while (queue.Count > 0)
        {
            int curr = queue.Dequeue();
            pixels[curr] = Color.clear;
            clearedCount++;

            int cx = curr % w;
            int cy = curr / w;

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int n = 0; n < 4; n++)
            {
                int nx = cx + dx[n];
                int ny = cy + dy[n];
                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                {
                    int nidx = ny * w + nx;
                    if (!visited[nidx])
                    {
                        Color cNeigh = pixels[nidx];
                        float dist = Mathf.Min(
                            Mathf.Min(ColorDist(cNeigh, c0), ColorDist(cNeigh, c1)),
                            Mathf.Min(ColorDist(cNeigh, c2), ColorDist(cNeigh, c3))
                        );
                        if (dist < tolerance)
                        {
                            visited[nidx] = true;
                            queue.Enqueue(nidx);
                        }
                    }
                }
            }
        }

        transparentTex.SetPixels(pixels);
        transparentTex.Apply();

        byte[] pngBytes = transparentTex.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);
        Object.DestroyImmediate(transparentTex);

        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        AssetDatabase.Refresh();
        Debug.Log($"[AlphaTest] Done. Cleared {clearedCount} pixels on Ped1_Stage1.png");
    }

    private static float ColorDist(Color a, Color b)
    {
        return Mathf.Sqrt(
            (a.r - b.r) * (a.r - b.r) +
            (a.g - b.g) * (a.g - b.g) +
            (a.b - b.b) * (a.b - b.b)
        );
    }
}