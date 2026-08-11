using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EighthKind.EditorTools
{
    public class ArtImportTool : EditorWindow
    {
        private const string PREF_SOURCE_FOLDER = "EighthKind_ArtImport_SourceFolder";
        private const string PREF_OUTPUT_FOLDER = "EighthKind_ArtImport_OutputFolder";

        private string _sourceFolder = "";
        private string _outputFolder = "Assets/_Game/Sprites/UI";

        private List<string> _imageFiles = new List<string>();
        private Dictionary<string, Texture2D> _thumbnailCache = new Dictionary<string, Texture2D>();
        private int _selectedFileIndex = -1;

        private Vector2 _fileListScroll = Vector2.zero;
        private Vector2 _previewScroll = Vector2.zero;

        private Texture2D _sourceTexture;
        private Texture2D _previewTexture;
        private Texture2D _checkerboardTexture;

        // Toggleable Options
        private bool _enableSplit = false;
        public enum SplitMode { Vertical, Horizontal }
        private SplitMode _splitMode = SplitMode.Vertical;
        private int _splitCount = 2;
        private int _selectedRegionIndex = 0;

        private bool _enableChromaKey = true;
        private Color _chromaKeyColor = Color.black;
        private float _chromaTolerance = 0.15f;
        private float _chromaFeather = 0.08f;
        private bool _preserveTranslucency = true;

        private bool _enableAutoCrop = true;
        private int _cropMargin = 4;

        private bool _enableDownscale = false;
        private int _maxDimension = 1024;

        // Unlike the four modes above, this operates in-place on an existing tracked Unity
        // asset (preserving its GUID/.meta and import settings) rather than processing an
        // external source into a newly-imported asset -- deliberately kept out of ProcessImage
        // and the Import Selected Image button's pipeline. See FlattenSelectedAssetInPlace().
        private bool _enableFlattenRGB = false;

        private string _statusMessage = "";

        [MenuItem("Tools/Eighth Kind/Art Import")]
        public static void ShowWindow()
        {
            var window = GetWindow<ArtImportTool>("Art Import Tool");
            window.minSize = new Vector2(880, 620);
        }

        private void OnEnable()
        {
            string defaultDownloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "Downloads"
            );
            _sourceFolder = EditorPrefs.GetString(PREF_SOURCE_FOLDER, defaultDownloads);
            _outputFolder = EditorPrefs.GetString(PREF_OUTPUT_FOLDER, "Assets/_Game/Sprites/UI");

            RefreshFileList();
            CreateCheckerboardTexture();
        }

        private void OnDisable()
        {
            CleanupTextures();
            ClearThumbnailCache();
        }

        private void CleanupTextures()
        {
            if (_sourceTexture != null) { DestroyImmediate(_sourceTexture); _sourceTexture = null; }
            if (_previewTexture != null) { DestroyImmediate(_previewTexture); _previewTexture = null; }
            if (_checkerboardTexture != null) { DestroyImmediate(_checkerboardTexture); _checkerboardTexture = null; }
        }

        private void ClearThumbnailCache()
        {
            foreach (var kvp in _thumbnailCache)
            {
                if (kvp.Value != null) DestroyImmediate(kvp.Value);
            }
            _thumbnailCache.Clear();
        }

        private void CreateCheckerboardTexture()
        {
            if (_checkerboardTexture != null) return;
            _checkerboardTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color c1 = new Color(0.8f, 0.8f, 0.8f, 1f);
            Color c2 = new Color(0.6f, 0.6f, 0.6f, 1f);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isC1 = ((x / 8) + (y / 8)) % 2 == 0;
                    _checkerboardTexture.SetPixel(x, y, isC1 ? c1 : c2);
                }
            }
            _checkerboardTexture.Apply();
            _checkerboardTexture.wrapMode = TextureWrapMode.Repeat;
            _checkerboardTexture.filterMode = FilterMode.Point;
        }

        public void RefreshFileList()
        {
            _imageFiles.Clear();
            ClearThumbnailCache();

            if (Directory.Exists(_sourceFolder))
            {
                var files = Directory.GetFiles(_sourceFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => File.GetLastWriteTime(f));
                _imageFiles.AddRange(files);
            }

            if (_selectedFileIndex >= _imageFiles.Count)
            {
                _selectedFileIndex = _imageFiles.Count - 1;
            }

            if (_selectedFileIndex >= 0 && _selectedFileIndex < _imageFiles.Count)
            {
                LoadSourceTexture(_imageFiles[_selectedFileIndex]);
            }
            else
            {
                _selectedFileIndex = -1;
                CleanupTextures();
            }
        }

        private void LoadSourceTexture(string filePath)
        {
            if (!File.Exists(filePath)) return;

            byte[] bytes = File.ReadAllBytes(filePath);
            if (_sourceTexture != null) DestroyImmediate(_sourceTexture);

            _sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!_sourceTexture.LoadImage(bytes))
            {
                Debug.LogError("[ArtImportTool] Failed to load image at: " + filePath);
                DestroyImmediate(_sourceTexture);
                _sourceTexture = null;
                return;
            }

            UpdatePreview();
        }

        private Texture2D GetThumbnail(string filePath)
        {
            if (_thumbnailCache.TryGetValue(filePath, out Texture2D tex) && tex != null)
            {
                return tex;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (rawTex.LoadImage(bytes))
                {
                    Texture2D thumb = ResampleBilinearToTexture(rawTex, 36, 36);
                    DestroyImmediate(rawTex);
                    _thumbnailCache[filePath] = thumb;
                    return thumb;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ArtImportTool] Could not generate thumbnail: " + ex.Message);
            }

            return null;
        }

        private void OnGUI()
        {
            CreateCheckerboardTexture();

            EditorGUILayout.BeginVertical();
            DrawHeaderAndFolders();
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            DrawFileListPane(280);
            DrawControlsPane(300);
            DrawPreviewPane();
            EditorGUILayout.EndHorizontal();

            DrawFooter();
            EditorGUILayout.EndVertical();
        }

        private void DrawHeaderAndFolders()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Eighth Kind Studios — Reusable Art Import Tool", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            _sourceFolder = EditorGUILayout.TextField("Source Folder", _sourceFolder);
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Source Folder", _sourceFolder, "");
                if (!string.IsNullOrEmpty(path)) _sourceFolder = path;
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(65)))
            {
                RefreshFileList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Asset Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        _outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Folder", "Output folder must be inside Assets.", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PREF_SOURCE_FOLDER, _sourceFolder);
                EditorPrefs.SetString(PREF_OUTPUT_FOLDER, _outputFolder);
                RefreshFileList();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFileListPane(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
            EditorGUILayout.LabelField("Source Images (" + _imageFiles.Count + ")", EditorStyles.boldLabel);

            _fileListScroll = EditorGUILayout.BeginScrollView(_fileListScroll);

            for (int i = 0; i < _imageFiles.Count; i++)
            {
                string filePath = _imageFiles[i];
                string fileName = Path.GetFileName(filePath);

                bool isSelected = (i == _selectedFileIndex);
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
                };

                EditorGUILayout.BeginHorizontal();
                Texture2D thumb = GetThumbnail(filePath);
                if (thumb != null)
                {
                    GUILayout.Label(thumb, GUILayout.Width(36), GUILayout.Height(36));
                }

                if (GUILayout.Button(fileName, btnStyle, GUILayout.Height(36)))
                {
                    _selectedFileIndex = i;
                    LoadSourceTexture(filePath);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawControlsPane(float width)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
            EditorGUILayout.LabelField("Processing Options", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            // 1. SPLIT
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _enableSplit = EditorGUILayout.ToggleLeft("1. Split / Region Pick", _enableSplit, EditorStyles.boldLabel);
            if (_enableSplit)
            {
                EditorGUI.indentLevel++;
                _splitMode = (SplitMode)EditorGUILayout.EnumPopup("Mode", _splitMode);
                _splitCount = Mathf.Max(2, EditorGUILayout.IntField("Count", _splitCount));

                _selectedRegionIndex = Mathf.Clamp(_selectedRegionIndex, 0, _splitCount - 1);
                _selectedRegionIndex = EditorGUILayout.IntSlider("Keep Region", _selectedRegionIndex, 0, _splitCount - 1);

                string label = "Region " + (_selectedRegionIndex + 1) + " of " + _splitCount;
                if (_splitMode == SplitMode.Vertical)
                {
                    label += (_selectedRegionIndex == 0 ? " (Top)" : (_selectedRegionIndex == _splitCount - 1 ? " (Bottom)" : ""));
                }
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // 2. CHROMA KEY
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _enableChromaKey = EditorGUILayout.ToggleLeft("2. Chroma Key (BG Removal)", _enableChromaKey, EditorStyles.boldLabel);
            if (_enableChromaKey)
            {
                EditorGUI.indentLevel++;
                _chromaKeyColor = EditorGUILayout.ColorField("Key Color", _chromaKeyColor);
                _chromaTolerance = EditorGUILayout.Slider("Tolerance", _chromaTolerance, 0.01f, 0.8f);
                _chromaFeather = EditorGUILayout.Slider("Feather", _chromaFeather, 0.001f, 0.3f);
                _preserveTranslucency = EditorGUILayout.Toggle("Preserve Glass Glow", _preserveTranslucency);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // 3. AUTO CROP
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _enableAutoCrop = EditorGUILayout.ToggleLeft("3. Auto-Crop Padding", _enableAutoCrop, EditorStyles.boldLabel);
            if (_enableAutoCrop)
            {
                EditorGUI.indentLevel++;
                _cropMargin = EditorGUILayout.IntSlider("Margin (px)", _cropMargin, 0, 32);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // 4. DOWNSCALE
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _enableDownscale = EditorGUILayout.ToggleLeft("4. Downscale Max Dimension", _enableDownscale, EditorStyles.boldLabel);
            if (_enableDownscale)
            {
                EditorGUI.indentLevel++;
                int[] sizes = new int[] { 256, 512, 1024, 2048 };
                string[] options = new string[] { "256", "512", "1024", "2048" };
                _maxDimension = EditorGUILayout.IntPopup("Max Size", _maxDimension, options, sizes);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                UpdatePreview();
            }

            // 5. FLATTEN RGB (IN-PLACE) -- deliberately outside the change-check block above:
            // it doesn't participate in ProcessImage/the Before-After preview, since it's an
            // in-place edit of an existing asset, not a "process source into a new asset" step.
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _enableFlattenRGB = EditorGUILayout.ToggleLeft("5. Flatten RGB (keep alpha)", _enableFlattenRGB, EditorStyles.boldLabel);
            if (_enableFlattenRGB)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Sets every pixel's RGB to white; alpha is left byte-identical. Operates " +
                    "in-place on the selected file if it's already an imported Unity asset under " +
                    "Assets/ -- overwrites the same PNG, keeps the same GUID/.meta, and restores " +
                    "the asset's exact prior import settings afterward (only isReadable is " +
                    "temporarily toggled to allow the pixel read on non-readable textures). Does " +
                    "not use the Output Folder or the Import Selected Image button below.",
                    MessageType.Info);
                GUI.enabled = (_selectedFileIndex >= 0 && _selectedFileIndex < _imageFiles.Count);
                if (GUILayout.Button("Flatten RGB In-Place", GUILayout.Height(30)))
                {
                    FlattenSelectedAssetInPlace();
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            GUI.enabled = (_selectedFileIndex >= 0 && _selectedFileIndex < _imageFiles.Count && _previewTexture != null);
            if (GUILayout.Button("Import Selected Image", GUILayout.Height(36)))
            {
                ImportSelectedImage();
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewPane()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Before / After Preview", EditorStyles.boldLabel);

            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll);
            EditorGUILayout.BeginHorizontal();

            // Before
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.LabelField("Source (Before)", EditorStyles.boldLabel);
            if (_sourceTexture != null)
            {
                EditorGUILayout.LabelField(_sourceTexture.width + " x " + _sourceTexture.height + " px");
                Rect rect = GUILayoutUtility.GetAspectRect((float)_sourceTexture.width / _sourceTexture.height, GUILayout.MaxHeight(250));
                GUI.DrawTexture(rect, _sourceTexture, ScaleMode.ScaleToFit, false);
            }
            else
            {
                EditorGUILayout.HelpBox("Select an image to view", MessageType.None);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // After
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.LabelField("Processed (After)", EditorStyles.boldLabel);
            if (_previewTexture != null)
            {
                EditorGUILayout.LabelField(_previewTexture.width + " x " + _previewTexture.height + " px");
                Rect rect = GUILayoutUtility.GetAspectRect((float)_previewTexture.width / _previewTexture.height, GUILayout.MaxHeight(250));
                
                Rect texCoords = new Rect(0, 0, rect.width / 16f, rect.height / 16f);
                GUI.DrawTextureWithTexCoords(rect, _checkerboardTexture, texCoords);
                GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                EditorGUILayout.HelpBox("No processed output", MessageType.None);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Import Settings: Sprite (2D/UI) | Single | PPU 100 | Bilinear | Max 2048 | Mipmaps Off | Clamp | AlphaIsTransparency On | Read/Write Off", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void UpdatePreview()
        {
            if (_sourceTexture == null)
            {
                if (_previewTexture != null) DestroyImmediate(_previewTexture);
                _previewTexture = null;
                return;
            }

            _previewTexture = ProcessImage(_sourceTexture, 
                _enableSplit, _splitMode, _splitCount, _selectedRegionIndex,
                _enableChromaKey, _chromaKeyColor, _chromaTolerance, _chromaFeather, _preserveTranslucency,
                _enableAutoCrop, _cropMargin,
                _enableDownscale, _maxDimension);
        }

        public static Texture2D ProcessImage(
            Texture2D source,
            bool enableSplit, SplitMode splitMode, int splitCount, int selectedRegionIndex,
            bool enableChromaKey, Color keyColor, float tolerance, float feather, bool preserveTranslucency,
            bool enableAutoCrop, int cropMargin,
            bool enableDownscale, int maxDimension)
        {
            if (source == null) return null;

            int w = source.width;
            int h = source.height;
            Color[] pixels = source.GetPixels();

            RectInt cropRegion = new RectInt(0, 0, w, h);
            if (enableSplit)
            {
                if (splitMode == SplitMode.Vertical)
                {
                    int sliceH = h / Mathf.Max(1, splitCount);
                    int idx = Mathf.Clamp(selectedRegionIndex, 0, splitCount - 1);
                    int startY = h - (idx + 1) * sliceH;
                    cropRegion = new RectInt(0, Mathf.Max(0, startY), w, sliceH);
                }
                else if (splitMode == SplitMode.Horizontal)
                {
                    int sliceW = w / Mathf.Max(1, splitCount);
                    int idx = Mathf.Clamp(selectedRegionIndex, 0, splitCount - 1);
                    int startX = idx * sliceW;
                    cropRegion = new RectInt(startX, 0, sliceW, h);
                }
            }

            int rw = cropRegion.width;
            int rh = cropRegion.height;
            Color[] regionPixels = new Color[rw * rh];

            for (int y = 0; y < rh; y++)
            {
                for (int x = 0; x < rw; x++)
                {
                    int srcX = cropRegion.x + x;
                    int srcY = cropRegion.y + y;
                    regionPixels[y * rw + x] = pixels[srcY * w + srcX];
                }
            }

            if (enableChromaKey)
            {
                float keyLum = keyColor.r * 0.299f + keyColor.g * 0.587f + keyColor.b * 0.114f;
                bool isDarkKey = keyLum < 0.25f;

                for (int i = 0; i < regionPixels.Length; i++)
                {
                    Color c = regionPixels[i];
                    float dist = ColorDistance(c, keyColor);

                    float alphaFactor = 1f;
                    float minDist = Mathf.Max(0f, tolerance - feather);
                    float maxDist = tolerance + feather;

                    if (dist <= minDist)
                    {
                        alphaFactor = 0f;
                    }
                    else if (dist >= maxDist)
                    {
                        alphaFactor = 1f;
                    }
                    else
                    {
                        alphaFactor = (dist - minDist) / Mathf.Max(0.0001f, maxDist - minDist);
                        alphaFactor = alphaFactor * alphaFactor * (3f - 2f * alphaFactor);
                    }

                    c.a *= alphaFactor;

                    if (preserveTranslucency && isDarkKey && c.a > 0f)
                    {
                        float unblackFactor = Mathf.Clamp01(c.a);
                        if (unblackFactor > 0.05f)
                        {
                            c.r = Mathf.Clamp01(c.r / unblackFactor);
                            c.g = Mathf.Clamp01(c.g / unblackFactor);
                            c.b = Mathf.Clamp01(c.b / unblackFactor);
                        }
                    }

                    regionPixels[i] = c;
                }
            }

            int minX = rw, maxX = 0, minY = rh, maxY = 0;
            bool hasVisiblePixels = false;

            if (enableAutoCrop)
            {
                for (int y = 0; y < rh; y++)
                {
                    for (int x = 0; x < rw; x++)
                    {
                        if (regionPixels[y * rw + x].a > 0.01f)
                        {
                            hasVisiblePixels = true;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }

            RectInt finalCrop;
            if (enableAutoCrop && hasVisiblePixels)
            {
                minX = Mathf.Max(0, minX - cropMargin);
                maxX = Mathf.Min(rw - 1, maxX + cropMargin);
                minY = Mathf.Max(0, minY - cropMargin);
                maxY = Mathf.Min(rh - 1, maxY + cropMargin);
                finalCrop = new RectInt(minX, minY, Mathf.Max(1, maxX - minX + 1), Mathf.Max(1, maxY - minY + 1));
            }
            else
            {
                finalCrop = new RectInt(0, 0, rw, rh);
            }

            int fw = finalCrop.width;
            int fh = finalCrop.height;
            Color[] finalPixels = new Color[fw * fh];

            for (int y = 0; y < fh; y++)
            {
                for (int x = 0; x < fw; x++)
                {
                    int srcX = finalCrop.x + x;
                    int srcY = finalCrop.y + y;
                    finalPixels[y * fw + x] = regionPixels[srcY * rw + srcX];
                }
            }

            int targetW = fw;
            int targetH = fh;
            if (enableDownscale && maxDimension > 0 && (fw > maxDimension || fh > maxDimension))
            {
                if (fw >= fh)
                {
                    targetW = maxDimension;
                    targetH = Mathf.RoundToInt((float)fh * maxDimension / fw);
                }
                else
                {
                    targetH = maxDimension;
                    targetW = Mathf.RoundToInt((float)fw * maxDimension / fh);
                }
                finalPixels = ResampleBilinear(finalPixels, fw, fh, targetW, targetH);
            }

            Texture2D result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            result.SetPixels(finalPixels);
            result.Apply();
            return result;
        }

        private static float ColorDistance(Color c1, Color c2)
        {
            float dr = c1.r - c2.r;
            float dg = c1.g - c2.g;
            float db = c1.b - c2.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db) / 1.7320508f;
        }

        private static Color[] ResampleBilinear(Color[] sourcePixels, int w1, int h1, int w2, int h2)
        {
            Color[] newPixels = new Color[w2 * h2];
            float ratioX = (float)w1 / w2;
            float ratioY = (float)h1 / h2;

            for (int y = 0; y < h2; y++)
            {
                float floorY = Mathf.Floor(y * ratioY);
                int y1 = (int)floorY;
                int y2 = Mathf.Min(y1 + 1, h1 - 1);
                float lerpY = (y * ratioY) - floorY;

                for (int x = 0; x < w2; x++)
                {
                    float floorX = Mathf.Floor(x * ratioX);
                    int x1 = (int)floorX;
                    int x2 = Mathf.Min(x1 + 1, w1 - 1);
                    float lerpX = (x * ratioX) - floorX;

                    Color cTop = Color.Lerp(sourcePixels[y1 * w1 + x1], sourcePixels[y1 * w1 + x2], lerpX);
                    Color cBottom = Color.Lerp(sourcePixels[y2 * w1 + x1], sourcePixels[y2 * w1 + x2], lerpX);

                    newPixels[y * w2 + x] = Color.Lerp(cTop, cBottom, lerpY);
                }
            }

            return newPixels;
        }

        private static Texture2D ResampleBilinearToTexture(Texture2D src, int targetW, int targetH)
        {
            Color[] srcPixels = src.GetPixels();
            Color[] resampled = ResampleBilinear(srcPixels, src.width, src.height, targetW, targetH);
            Texture2D tex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            tex.SetPixels(resampled);
            tex.Apply();
            return tex;
        }

        private void ImportSelectedImage()
        {
            if (_selectedFileIndex < 0 || _selectedFileIndex >= _imageFiles.Count || _previewTexture == null)
            {
                return;
            }

            string sourcePath = _imageFiles[_selectedFileIndex];
            string fileName = Path.GetFileNameWithoutExtension(sourcePath) + ".png";

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
                AssetDatabase.Refresh();
            }

            string destinationPath = Path.Combine(_outputFolder, fileName).Replace('\\', '/');

            if (File.Exists(destinationPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Overwrite Existing File?",
                    "Destination file already exists:\n" + destinationPath + "\n\nDo you want to overwrite it?",
                    "Overwrite", "Cancel"
                );

                if (!overwrite)
                {
                    _statusMessage = "Import cancelled by user.";
                    return;
                }
            }

            byte[] pngBytes = _previewTexture.EncodeToPNG();
            
            using (FileStream fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(pngBytes, 0, pngBytes.Length);
            }

            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);

            ApplyRequiredImportSettings(destinationPath);

            _statusMessage = "Successfully imported: " + destinationPath;
            Debug.Log("[ArtImportTool] Imported sprite to: " + destinationPath);
        }

        /// <summary>
        /// Sets every pixel's RGB to white and leaves alpha byte-identical, then writes the PNG
        /// back to the same path the selected file already lives at -- an in-place edit of an
        /// existing tracked Unity asset, not the "process external source into a new/overwritten
        /// asset with fixed import settings" flow ImportSelectedImage() uses. Deliberately does
        /// not call ApplyRequiredImportSettings: that would stomp sprite mode/PPU/wrap/etc. on an
        /// asset that's supposed to keep its exact current settings. The only setting ever
        /// touched is isReadable, and only long enough to read pixels off a non-readable texture
        /// -- restored to its original value in a finally block so it's undone even on failure.
        /// </summary>
        private void FlattenSelectedAssetInPlace()
        {
            if (_selectedFileIndex < 0 || _selectedFileIndex >= _imageFiles.Count)
            {
                return;
            }

            string sourcePath = _imageFiles[_selectedFileIndex];
            string assetPath = ToAssetDatabasePath(sourcePath);
            if (string.IsNullOrEmpty(assetPath))
            {
                _statusMessage = "Flatten RGB In-Place requires the selected file to already be an imported Unity asset under Assets/. Selected file is outside the project: " + sourcePath;
                Debug.LogWarning("[ArtImportTool] " + _statusMessage);
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                _statusMessage = "Flatten RGB In-Place: no TextureImporter found at " + assetPath;
                Debug.LogWarning("[ArtImportTool] " + _statusMessage);
                return;
            }

            bool originalIsReadable = importer.isReadable;

            try
            {
                if (!originalIsReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex == null)
                {
                    _statusMessage = "Flatten RGB In-Place: failed to load texture at " + assetPath;
                    Debug.LogError("[ArtImportTool] " + _statusMessage);
                    return;
                }

                Color[] pixels = tex.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
                }

                Texture2D flattened = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                flattened.SetPixels(pixels);
                flattened.Apply();
                byte[] pngBytes = flattened.EncodeToPNG();
                DestroyImmediate(flattened);

                File.WriteAllBytes(Path.GetFullPath(assetPath), pngBytes);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                _statusMessage = "Flattened RGB in place (alpha preserved, dimensions unchanged): " + assetPath;
                Debug.Log("[ArtImportTool] " + _statusMessage);
            }
            finally
            {
                // Re-fetch: the importer reference above may be stale after the reimport(s).
                TextureImporter importerAfter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importerAfter != null && importerAfter.isReadable != originalIsReadable)
                {
                    importerAfter.isReadable = originalIsReadable;
                    importerAfter.SaveAndReimport();
                }
            }
        }

        /// <summary>Converts an absolute file path to a project-relative "Assets/..." AssetDatabase path, or null if the path isn't inside this project's Assets folder. Same conversion DrawHeaderAndFolders() already does for the Output Folder field.</summary>
        private static string ToAssetDatabasePath(string absolutePath)
        {
            string fullDataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            string fullAbsolute = Path.GetFullPath(absolutePath).Replace('\\', '/');
            if (!fullAbsolute.StartsWith(fullDataPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return "Assets" + fullAbsolute.Substring(fullDataPath.Length);
        }

        public static void ApplyRequiredImportSettings(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;

                importer.SaveAndReimport();
            }
        }
    }
}