using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class BuildChineseFont
{
    private const string SourceFontPath = "Assets/Fonts/NotoSansTC.ttf";
    private const string TargetFontAssetPath = "Assets/NotoSansTC.asset";
    private const string FontAssetName = "NotoSansTC";
    private const string BundleName = "font.bundle";

    public static void Build()
    {
        try
        {
            ExecuteBuild();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BuildChineseFont] Build FAILED: {ex.Message}\n{ex.StackTrace}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            throw;
        }
    }

    private static void ExecuteBuild()
    {
        // 1. Resolve repo relative to Application.dataPath: Assets -> FontBundle -> tools -> repo
        string assetsPath = Application.dataPath;
        DirectoryInfo assetsDir = new DirectoryInfo(assetsPath);
        DirectoryInfo fontBundleDir = assetsDir.Parent;
        if (fontBundleDir == null)
        {
            throw new DirectoryNotFoundException($"Cannot resolve FontBundle directory from {assetsPath}");
        }

        DirectoryInfo toolsDir = fontBundleDir.Parent;
        if (toolsDir == null)
        {
            throw new DirectoryNotFoundException($"Cannot resolve tools directory from {fontBundleDir.FullName}");
        }

        DirectoryInfo repoDir = toolsDir.Parent;
        if (repoDir == null)
        {
            throw new DirectoryNotFoundException($"Cannot resolve repo root directory from {toolsDir.FullName}");
        }

        string repoPath = repoDir.FullName;
        Debug.Log($"[BuildChineseFont] Resolved repo root: {repoPath}");

        // 2. Ensure TMP Essential Resources and SDF shader are available
        Shader sdfShader = EnsureTmpResources();

        // 3. Load source font
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            throw new FileNotFoundException(
                $"Source font not found at '{SourceFontPath}'. Primary must copy NotoSansTC.ttf into Assets/Fonts before execution.");
        }

        // 4. Create TMP_FontAsset with dynamic atlas mode initially
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            64,
            9,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic,
            true
        );

        if (fontAsset == null)
        {
            throw new InvalidOperationException("TMP_FontAsset.CreateFontAsset returned null.");
        }

        fontAsset.name = FontAssetName;

        // Ensure material and shader are assigned
        if (fontAsset.material == null)
        {
            fontAsset.material = new Material(sdfShader);
            fontAsset.material.name = $"{FontAssetName} Material";
        }
        else if (fontAsset.material.shader == null || fontAsset.material.shader.name == "Hidden/InternalErrorShader")
        {
            fontAsset.material.shader = sdfShader;
        }

        if (fontAsset.material == null || fontAsset.material.shader == null)
        {
            throw new InvalidOperationException("Failed to ensure a valid material and shader for TMP_FontAsset.");
        }

        // 5. Collect all unique printable chars from translated category contents plus ASCII U+20..7E
        string langDir = Path.Combine(repoPath, "Mods", "GloomSlation", "TraditionalChinese");
        if (!Directory.Exists(langDir))
        {
            throw new DirectoryNotFoundException(
                $"Language directory not found at '{langDir}'. Primary must generate validated language source before building.");
        }

        string charString = CollectUniqueCharacters(langDir);
        Debug.Log($"[BuildChineseFont] Prepopulating {charString.Length} unique characters...");

        // Prepopulate characters into atlas
        string missingCharacters;
        bool populated = fontAsset.TryAddCharacters(charString, out missingCharacters);
        if (!populated || !string.IsNullOrEmpty(missingCharacters))
        {
            throw new InvalidOperationException(
                $"TryAddCharacters failed. Missing {missingCharacters?.Length ?? 0} characters in font: '{missingCharacters}'");
        }

        // 6. Lock atlas population mode to Static
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        // 7. Save font asset at EXACT "Assets/NotoSansTC.asset", attach subassets, and save/reimport
        SaveFontAssetWithSubassets(fontAsset, TargetFontAssetPath);

        // 8. Build AssetBundle
        string outputBundleDir = Path.Combine(repoPath, "bin", "font-bundle");
        if (!Directory.Exists(outputBundleDir))
        {
            Directory.CreateDirectory(outputBundleDir);
        }

        var build = new AssetBundleBuild
        {
            assetBundleName = BundleName,
            assetNames = new[] { TargetFontAssetPath }
        };

        Debug.Log($"[BuildChineseFont] Building AssetBundle to '{outputBundleDir}'...");
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputBundleDir,
            new[] { build },
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        if (manifest == null)
        {
            throw new InvalidOperationException("BuildPipeline.BuildAssetBundles returned null manifest.");
        }

        string emittedBundlePath = Path.Combine(outputBundleDir, BundleName);
        if (!File.Exists(emittedBundlePath))
        {
            throw new FileNotFoundException($"Emitted bundle file not found at '{emittedBundlePath}'.");
        }

        FileInfo bundleInfo = new FileInfo(emittedBundlePath);
        if (bundleInfo.Length == 0)
        {
            throw new InvalidOperationException($"Emitted bundle file at '{emittedBundlePath}' is empty (0 bytes).");
        }

        Debug.Log($"[BuildChineseFont] Successfully built {BundleName} ({bundleInfo.Length} bytes).");

        // Copy font.bundle to repo/Mods/GloomSlation/TraditionalChinese/font.bundle
        string targetModBundlePath = Path.Combine(langDir, BundleName);
        File.Copy(emittedBundlePath, targetModBundlePath, true);
        Debug.Log($"[BuildChineseFont] Copied bundle to '{targetModBundlePath}'.");

        // Package first; explicit verification can be run after the user tries the build.
        Debug.Log("FONT_BUNDLE_CREATED");
    }

    private static Shader EnsureTmpResources()
    {
        Shader sdfShader = Shader.Find("TextMeshPro/Distance Field");
        if (sdfShader == null)
        {
            Debug.Log("[BuildChineseFont] TMP Distance Field shader missing. Importing TMP Essential Resources...");
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly);
            if (packageInfo == null)
            {
                throw new InvalidOperationException(
                    "PackageInfo.FindForAssembly failed to locate TextMeshPro package assembly.");
            }

            string packagePath = Path.Combine(packageInfo.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException($"TMP Essential Resources package not found at '{packagePath}'.");
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            sdfShader = Shader.Find("TextMeshPro/Distance Field");
            if (sdfShader == null)
            {
                sdfShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            }

            if (sdfShader == null)
            {
                throw new InvalidOperationException(
                    "TextMeshPro Distance Field shader still missing after importing TMP Essential Resources.");
            }
        }

        Debug.Log($"[BuildChineseFont] Using TMP shader: {sdfShader.name}");
        return sdfShader;
    }

    private static string CollectUniqueCharacters(string langDir)
    {
        var charSet = new HashSet<char>();

        // ASCII U+0020 .. U+007E
        for (int i = 0x20; i <= 0x7E; i++)
        {
            charSet.Add((char)i);
        }

        var ignoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".bundle", ".ttf", ".otf", ".png", ".jpg", ".jpeg", ".mp3", ".wav", ".ogg", ".meta"
        };

        string[] files = Directory.GetFiles(langDir, "*", SearchOption.AllDirectories);
        int scannedCount = 0;

        foreach (string file in files)
        {
            string ext = Path.GetExtension(file);
            if (ignoredExtensions.Contains(ext))
            {
                continue;
            }

            string relative = file.Substring(langDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (relative.StartsWith("Textures" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                relative.StartsWith("Audio" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                relative.Equals(BundleName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            scannedCount++;
            string[] lines = File.ReadAllLines(file, Encoding.UTF8);
            foreach (string rawLine in lines)
            {
                string trimmed = rawLine.TrimStart();
                if (trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                {
                    continue;
                }

                foreach (char ch in rawLine)
                {
                    if (char.IsControl(ch) || char.IsSurrogate(ch))
                    {
                        continue;
                    }
                    charSet.Add(ch);
                }
            }
        }

        Debug.Log($"[BuildChineseFont] Scanned {scannedCount} language files. Found {charSet.Count} unique printable characters.");
        return new string(charSet.OrderBy(c => c).ToArray());
    }

    private static void SaveFontAssetWithSubassets(TMP_FontAsset fontAsset, string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        AssetDatabase.CreateAsset(fontAsset, assetPath);

        // Add atlas textures as subassets
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D tex = fontAsset.atlasTextures[i];
                if (tex != null)
                {
                    tex.name = (fontAsset.atlasTextures.Length > 1)
                        ? $"{fontAsset.name} Atlas {i}"
                        : $"{fontAsset.name} Atlas";
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }
        }

        // Add material as subasset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = $"{fontAsset.name} Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.material != null)
        {
            EditorUtility.SetDirty(fontAsset.material);
        }

        if (fontAsset.atlasTextures != null)
        {
            foreach (Texture2D tex in fontAsset.atlasTextures)
            {
                if (tex != null)
                {
                    EditorUtility.SetDirty(tex);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    public static void Verify()
    {
        string repo = Directory.GetParent(Application.dataPath).Parent.Parent.FullName;
        string languageDirectory = Path.Combine(repo, "Mods", "GloomSlation", "TraditionalChinese");
        VerifyBundle(Path.Combine(languageDirectory, BundleName), CollectUniqueCharacters(languageDirectory));
    }

    private static void VerifyBundle(string bundlePath, string requiredChars)
    {
        Debug.Log($"[BuildChineseFont] Verifying bundle at '{bundlePath}'...");
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            throw new InvalidOperationException($"Verification failed: AssetBundle.LoadFromFile returned null for '{bundlePath}'.");
        }

        try
        {
            TMP_FontAsset loadedFont = bundle.LoadAsset<TMP_FontAsset>(TargetFontAssetPath);
            if (loadedFont == null)
            {
                throw new InvalidOperationException($"Verification failed: Could not load '{TargetFontAssetPath}' from bundle.");
            }

            loadedFont.ReadFontAssetDefinition();

            var missingChars = new List<char>();
            foreach (char ch in requiredChars)
            {
                bool found = false;
                if (loadedFont.characterLookupTable != null && loadedFont.characterLookupTable.ContainsKey((uint)ch))
                {
                    found = true;
                }
                else if (loadedFont.HasCharacter(ch))
                {
                    found = true;
                }

                if (!found)
                {
                    missingChars.Add(ch);
                }
            }

            if (missingChars.Count > 0)
            {
                string preview = new string(missingChars.Take(20).ToArray());
                throw new InvalidOperationException(
                    $"Verification failed: {missingChars.Count} required characters missing from loaded bundle asset. Preview: '{preview}'");
            }

            int glyphCount = (loadedFont.glyphTable != null) ? loadedFont.glyphTable.Count : 0;
            Debug.Log($"[BuildChineseFont] Verified {requiredChars.Length} characters in bundle. Glyph count: {glyphCount}");
            Debug.Log("FONT_BUNDLE_OK");
        }
        finally
        {
            bundle.Unload(true);
        }
    }
}
