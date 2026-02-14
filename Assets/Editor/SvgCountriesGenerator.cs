using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.VectorGraphics;

public static class SvgCountriesGenerator
{
    private const string OutputSpritesFolder = "Assets/Sprites/Countries";
    private const string OutputPrefabsFolder = "Assets/Prefabs/Countries";

    [MenuItem("Tools/SVG/Generate Country Sprites (Separate)")]
    public static void GenerateSeparateSpritesAndPrefabs()
    {
        var svgAsset = Selection.activeObject as TextAsset;
        if (svgAsset == null)
        {
            EditorUtility.DisplayDialog(
                "SVG seçilmedi",
                "Project penceresinde bir .svg (TextAsset) seç ve tekrar dene.",
                "OK");
            return;
        }

        EnsureFolder(OutputSpritesFolder);
        EnsureFolder(OutputPrefabsFolder);

        // 1) SVG'yi import et (Scene ya da SceneInfo dönebilir)
        object importResult;
        using (var reader = new StringReader(svgAsset.text))
        {
            importResult = ImportSvgAnyVersion(reader);
        }

        if (importResult == null)
        {
            Debug.LogError("ImportSVG çağrısı başarısız. Paket sürümü/kurulumunu kontrol et.");
            return;
        }

        // 2) SceneInfo geldiyse .Scene, direkt Scene geldiyse kendisi
        Scene scene = ExtractScene(importResult);
        if (scene == null)
        {
            Debug.LogError("SVG import edildi ama Scene elde edilemedi.");
            return;
        }

        // 3) Tüm node'ları topla (Root tek node da olabilir, liste de)
        var allNodes = new List<SceneNode>();
        CollectNodesFromScene(scene, allNodes);

        if (allNodes.Count == 0)
        {
            Debug.LogError("SceneNode bulunamadı. SVG içeriğini kontrol et.");
            return;
        }

        // 4) Tessellation ayarları
        var tessOptions = new VectorUtils.TessellationOptions
        {
            StepDistance = 0.25f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f
        };

        // 5) Sprite ayarları
        const float pixelsPerUnit = 100f;
        const ushort gradientResolution = 128;
        const bool generatePhysicsShape = false;

        int created = 0;
        var usedNames = new HashSet<string>();

        // 6) Her node için ayrı sprite/prefab
        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];

            // Boş node'ları ele (shape yoksa sprite üretme)
            if (!NodeHasDrawableContent(node))
                continue;

            string id = GetNodeIdOrName(node);
            if (string.IsNullOrWhiteSpace(id))
                id = $"node_{i}";

            id = SanitizeName(id);

            // Aynı isim gelirse benzersiz yap
            string unique = id;
            int suffix = 1;
            while (usedNames.Contains(unique))
            {
                unique = $"{id}_{suffix}";
                suffix++;
            }
            usedNames.Add(unique);

            // Tek node’dan geçici Scene yarat (Scene.Root sürüme göre farklı)
            var singleScene = CreateSingleNodeScene(node);

            // Tessellate
            var geoms = VectorUtils.TessellateScene(singleScene, tessOptions);
            if (geoms == null || geoms.Count == 0)
                continue;

            // Sprite üret (named arg yok: sürümler arası stabil)
            var sprite = VectorUtils.BuildSprite(
                geoms,
                pixelsPerUnit,
                VectorUtils.Alignment.Center,
                Vector2.zero,
                gradientResolution,
                generatePhysicsShape
            );

            sprite.name = unique;

            // Sprite asset
            var spritePath = $"{OutputSpritesFolder}/{unique}.asset";
            AssetDatabase.DeleteAsset(spritePath);
            AssetDatabase.CreateAsset(sprite, spritePath);

            // Prefab
            var go = new GameObject(unique);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            var prefabPath = $"{OutputPrefabsFolder}/{unique}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Bitti. Oluşturulan Sprite/Prefab sayısı: {created}");
    }

    // -------------------------
    // SVG Import (sürüm bağımsız)
    // -------------------------
    private static object ImportSvgAnyVersion(TextReader reader)
    {
        // SVGParser.ImportSVG(TextReader) arıyoruz
        var methods = typeof(SVGParser).GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (var m in methods)
        {
            if (m.Name != "ImportSVG") continue;

            var ps = m.GetParameters();
            if (ps.Length == 1 && typeof(TextReader).IsAssignableFrom(ps[0].ParameterType))
            {
                try { return m.Invoke(null, new object[] { reader }); }
                catch (Exception e) { Debug.LogError(e); return null; }
            }
        }

        // Bazı sürümlerde signature farklı olabiliyor; en azından bunu dene:
        // ImportSVG(string) gibi overload varsa
        foreach (var m in methods)
        {
            if (m.Name != "ImportSVG") continue;

            var ps = m.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                try
                {
                    // reader artık kullanılamaz, o yüzden metin üzerinden çağır
                    // (buraya gelirse zaten TextReader overload bulunamadı demek)
                    return m.Invoke(null, new object[] { ReadAll(reader) });
                }
                catch (Exception e) { Debug.LogError(e); return null; }
            }
        }

        return null;
    }

    private static string ReadAll(TextReader r)
    {
        // StringReader ise ToString() değil, ReadToEnd
        return r.ReadToEnd();
    }

    private static Scene ExtractScene(object importResult)
    {
        // Direkt Scene ise
        if (importResult is Scene s)
            return s;

        // SceneInfo ise property/field "Scene" içerir
        var t = importResult.GetType();
        var prop = t.GetProperty("Scene", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(Scene))
            return (Scene)prop.GetValue(importResult);

        var field = t.GetField("Scene", BindingFlags.Public | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(Scene))
            return (Scene)field.GetValue(importResult);

        return null;
    }

    // -------------------------
    // Scene graph traversal
    // -------------------------
    private static void CollectNodesFromScene(Scene scene, List<SceneNode> outNodes)
    {
        // Scene.Root bazı sürümlerde SceneNode, bazılarında List<SceneNode>
        var rootProp = typeof(Scene).GetProperty("Root", BindingFlags.Public | BindingFlags.Instance);
        if (rootProp == null) return;

        var rootVal = rootProp.GetValue(scene);
        if (rootVal == null) return;

        if (rootVal is SceneNode singleRoot)
        {
            Traverse(singleRoot, outNodes);
            return;
        }

        if (rootVal is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is SceneNode n)
                    Traverse(n, outNodes);
            }
        }
    }

    private static void Traverse(SceneNode node, List<SceneNode> outNodes)
    {
        outNodes.Add(node);

        // SceneNode.Children (List<SceneNode>) genelde vardır
        var childrenProp = typeof(SceneNode).GetProperty("Children", BindingFlags.Public | BindingFlags.Instance);
        if (childrenProp == null) return;

        var childrenVal = childrenProp.GetValue(node);
        if (childrenVal is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is SceneNode child)
                    Traverse(child, outNodes);
            }
        }
    }

    private static bool NodeHasDrawableContent(SceneNode node)
    {
        // Çoğu sürümde node.Shapes vardır. Yoksa yine de deneyeceğiz.
        var shapesProp = typeof(SceneNode).GetProperty("Shapes", BindingFlags.Public | BindingFlags.Instance);
        if (shapesProp == null) return true; // emin değilsek dene
        var shapesVal = shapesProp.GetValue(node);
        if (shapesVal == null) return false;

        if (shapesVal is ICollection col)
            return col.Count > 0;

        // ICollection değilse enumerable olup en az 1 eleman var mı bak
        if (shapesVal is IEnumerable en)
        {
            foreach (var _ in en) return true;
            return false;
        }

        return true;
    }

    // -------------------------
    // ID / Name çekme (sürüm bağımsız)
    // -------------------------
    private static string GetNodeIdOrName(SceneNode node)
    {
        // Bazı sürümlerde "Name", bazılarında "name", bazılarında "Id"/"id" olabilir
        string v;

        v = GetStringMember(node, "Name");
        if (!string.IsNullOrWhiteSpace(v)) return v;

        v = GetStringMember(node, "name");
        if (!string.IsNullOrWhiteSpace(v)) return v;

        v = GetStringMember(node, "Id");
        if (!string.IsNullOrWhiteSpace(v)) return v;

        v = GetStringMember(node, "id");
        if (!string.IsNullOrWhiteSpace(v)) return v;

        return null;
    }

    private static string GetStringMember(object obj, string memberName)
    {
        var t = obj.GetType();

        var p = t.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(string))
            return (string)p.GetValue(obj);

        var f = t.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(string))
            return (string)f.GetValue(obj);

        return null;
    }

    // -------------------------
    // Tek node Scene yarat (Root tipi farklı olabilir)
    // -------------------------
    private static Scene CreateSingleNodeScene(SceneNode node)
    {
        var sc = new Scene();

        var rootProp = typeof(Scene).GetProperty("Root", BindingFlags.Public | BindingFlags.Instance);
        if (rootProp == null)
            return sc;

        // Root tipi SceneNode ise direkt ata
        if (rootProp.PropertyType == typeof(SceneNode))
        {
            rootProp.SetValue(sc, node);
            return sc;
        }

        // Root tipi liste/collection ise new list yap
        if (typeof(IList).IsAssignableFrom(rootProp.PropertyType))
        {
            // Root: IList (genelde List<SceneNode>)
            var list = (IList)Activator.CreateInstance(rootProp.PropertyType);
            list.Add(node);
            rootProp.SetValue(sc, list);
            return sc;
        }

        // Root: IEnumerable vs ise en basit fallback: List<SceneNode>
        if (typeof(IEnumerable).IsAssignableFrom(rootProp.PropertyType))
        {
            var list = new List<SceneNode> { node };
            rootProp.SetValue(sc, list);
            return sc;
        }

        return sc;
    }

    // -------------------------
    // Klasör/isim yardımcıları
    // -------------------------
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }
}
