using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Menú: HomeBot > Arreglar materiales rosados
// Los objetos se ven ROSA FUERTE (magenta) cuando su material usa un shader que no funciona en URP.
// Pasa mucho con modelos de Maya que traen materiales "standardSurface" (Arnold).
// Esta herramienta:
//  - En los modelos (.fbx): crea un material URP Lit por cada material roto, copiando su color y
//    su textura, lo guarda en Assets/Materiales/Arreglados y le dice al modelo que use ese
//    (igual que el botón "Extract Materials" + "Remap" de Unity).
//  - En los archivos .mat sueltos: les cambia el shader a URP Lit conservando color y textura.
public static class ArreglarMaterialesRosados
{
    const string Carpeta = "Assets/Materiales/Arreglados";

    [MenuItem("HomeBot/Arreglar materiales rosados")]
    public static void Arreglar()
    {
        if (!HerramientasHomeBot.FueraDePlay("Arreglar materiales")) return;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            EditorUtility.DisplayDialog("Arreglar materiales", "No encontré el shader 'Universal Render Pipeline/Lit'.", "OK");
            return;
        }

        var arreglados = new List<string>();

        try
        {
            // ---- Materiales dentro de los modelos ----
            string[] modelos = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
            for (int i = 0; i < modelos.Length; i++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(modelos[i]);
                EditorUtility.DisplayProgressBar("Arreglar materiales rosados", ruta, (float)i / modelos.Length);

                ModelImporter importer = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importer == null) continue;

                var mapeos = importer.GetExternalObjectMap();
                bool cambio = false;

                foreach (Material m in AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Material>())
                {
                    if (!EstaRoto(m)) continue;

                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name);
                    if (mapeos.ContainsKey(id)) continue;   // ya tiene un reemplazo

                    Material nuevo = new Material(lit) { name = m.name };
                    CopiarColorYTextura(m, nuevo);

                    Directory.CreateDirectory(Carpeta);
                    string archivo = AssetDatabase.GenerateUniqueAssetPath(
                        Carpeta + "/" + Path.GetFileNameWithoutExtension(ruta) + "_" + Limpio(m.name) + ".mat");
                    AssetDatabase.CreateAsset(nuevo, archivo);

                    importer.AddRemap(id, nuevo);
                    cambio = true;
                    arreglados.Add(Path.GetFileName(ruta) + " → " + m.name);
                }

                if (cambio) importer.SaveAndReimport();
            }

            // ---- Archivos .mat sueltos ----
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ruta.EndsWith(".mat")) continue;
                Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null || !EstaRoto(m)) continue;

                Material copia = new Material(m);   // guarda color/textura antes de cambiar el shader
                m.shader = lit;
                CopiarColorYTextura(copia, m);
                Object.DestroyImmediate(copia);
                EditorUtility.SetDirty(m);
                arreglados.Add(ruta);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (arreglados.Count == 0)
        {
            EditorUtility.DisplayDialog("Arreglar materiales", "No encontré materiales rotos. Si algo sigue rosado, mándame una captura.", "OK");
            return;
        }

        Debug.Log("Materiales arreglados (" + arreglados.Count + "):\n" + string.Join("\n", arreglados));
        EditorUtility.DisplayDialog("Arreglar materiales",
            "Arreglé " + arreglados.Count + " materiales.\n\nLos nuevos están en " + Carpeta +
            ".\nLa lista completa está en la Console.", "OK");
    }

    // Un material está "roto" si su shader no existe, tiene errores o no funciona en este proyecto
    static bool EstaRoto(Material m)
    {
        if (m == null) return false;
        Shader s = m.shader;
        if (s == null) return true;
        if (s.name == "Hidden/InternalErrorShader") return true;
        if (!s.isSupported) return true;
        if (ShaderUtil.ShaderHasError(s)) return true;
        // Shaders del pipeline viejo (Built-in) no funcionan en URP
        string n = s.name;
        return n == "Standard" || n == "Standard (Specular setup)" || n.StartsWith("Legacy Shaders/") ||
               n == "Autodesk Interactive";
    }

    // Copia el color base y la textura principal leyendo las propiedades GUARDADAS del material
    // (sirve aunque su shader esté roto y ya no las muestre).
    static void CopiarColorYTextura(Material origen, Material destino)
    {
        Color color = Color.white;
        bool hayColor = false;
        Texture textura = null, normal = null;

        var so = new SerializedObject(origen);
        SerializedProperty colores = so.FindProperty("m_SavedProperties.m_Colors");
        if (colores != null)
        {
            for (int i = 0; i < colores.arraySize; i++)
            {
                var e = colores.GetArrayElementAtIndex(i);
                string nombre = e.FindPropertyRelative("first").stringValue.ToLowerInvariant();
                if (!hayColor && (nombre.Contains("base") || nombre == "_color" || nombre.Contains("diffuse") || nombre.Contains("albedo")))
                {
                    color = e.FindPropertyRelative("second").colorValue;
                    hayColor = true;
                }
            }
        }

        SerializedProperty texs = so.FindProperty("m_SavedProperties.m_TexEnvs");
        if (texs != null)
        {
            for (int i = 0; i < texs.arraySize; i++)
            {
                var e = texs.GetArrayElementAtIndex(i);
                string nombre = e.FindPropertyRelative("first").stringValue.ToLowerInvariant();
                Texture t = e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                if (t == null) continue;
                if (normal == null && (nombre.Contains("normal") || nombre.Contains("bump"))) { normal = t; continue; }
                if (textura == null && (nombre.Contains("base") || nombre.Contains("main") || nombre.Contains("albedo") ||
                                        nombre.Contains("diffuse") || nombre.Contains("color"))) textura = t;
            }
        }

        if (!hayColor && origen.HasProperty("_Color")) color = origen.GetColor("_Color");
        color.a = 1f;

        destino.SetColor("_BaseColor", color);
        if (textura != null) destino.SetTexture("_BaseMap", textura);
        if (normal != null)
        {
            destino.SetTexture("_BumpMap", normal);
            destino.EnableKeyword("_NORMALMAP");
        }
    }

    static string Limpio(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(':', '_');
    }
}
