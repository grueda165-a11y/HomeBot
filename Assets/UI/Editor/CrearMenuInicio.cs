using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú: HomeBot > Crear escena del menú de inicio
// Crea Assets/Scenes/MenuInicio.unity con la pantalla principal (imagen de Gemini con botones reales)
// y el panel de Ajustes (Volumen y Efectos), y la pone como primera escena del juego.
public static class CrearMenuInicio
{
    const string RutaEscena = "Assets/Scenes/MenuInicio.unity";
    const string RutaJuego = "Assets/HomeBotEntrega.unity";
    const string Sprites = "Assets/UI/Sprites/Menu/";

    // Medidas en píxeles de las imágenes recortadas (origen arriba a la izquierda)
    static readonly Vector2 TamFondo = new Vector2(1456f, 736f);
    static readonly Rect PxJugar = new Rect(620f, 310f, 250f, 68f);
    static readonly Rect PxAjustes = new Rect(625f, 386f, 242f, 63f);
    static readonly Rect PxSalir = new Rect(625f, 457f, 241f, 63f);

    static readonly Vector2 TamPanel = new Vector2(491f, 410f);
    static readonly Rect PxVolumen = new Rect(168f, 192f, 244f, 38f);
    static readonly Rect PxEfectos = new Rect(168f, 262f, 244f, 38f);
    static readonly Rect PxVolver = new Rect(158f, 313f, 202f, 62f);

    const float AltoPanel = 700f;   // en la resolución de referencia 1920x1080

    [MenuItem("HomeBot/Crear escena del menú de inicio")]
    public static void Crear()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog("Menú de inicio", "Ya existe " + RutaEscena + ". ¿Reemplazarla?", "Reemplazar", "Cancelar"))
            return;

        Sprite fondo = Cargar("Menu_Fondo.png");
        Sprite bJugar = Cargar("Menu_BotonJugar.png");
        Sprite bAjustes = Cargar("Menu_BotonAjustes.png");
        Sprite bSalir = Cargar("Menu_BotonSalir.png");
        Sprite panel = Cargar("Ajustes_Panel.png");
        Sprite bVolver = Cargar("Ajustes_BotonVolver.png");
        Sprite riel = Cargar("Slider_Riel.png", new Vector4(22, 22, 22, 22));
        Sprite carga = Cargar("Slider_Carga.png");
        Sprite perilla = Cargar("Slider_Perilla.png");
        if (fondo == null || bJugar == null || bAjustes == null || bSalir == null || panel == null ||
            bVolver == null || riel == null || carga == null || perilla == null)
        {
            EditorUtility.DisplayDialog("Menú de inicio", "Faltan imágenes en " + Sprites, "OK");
            return;
        }

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Cámara (fondo negro) y EventSystem
        GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Canvas
        GameObject canvasGO = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGO.transform;

        // ---------- Pantalla principal ----------
        Image imgFondo = NuevaImagen("Fondo", canvas, fondo);
        Estirar(imgFondo.rectTransform);
        imgFondo.raycastTarget = false;
        AspectRatioFitter arf = imgFondo.gameObject.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;   // cubre toda la pantalla
        arf.aspectRatio = TamFondo.x / TamFondo.y;

        GameObject principalGO = NuevoUI("Principal", imgFondo.transform);
        Estirar((RectTransform)principalGO.transform);
        CanvasGroup principal = principalGO.AddComponent<CanvasGroup>();

        Button jugar = CrearBoton("BotonJugar", principalGO.transform, bJugar, PxJugar, TamFondo);
        Button ajustesBtn = CrearBoton("BotonAjustes", principalGO.transform, bAjustes, PxAjustes, TamFondo);
        Button salir = CrearBoton("BotonSalir", principalGO.transform, bSalir, PxSalir, TamFondo);

        // ---------- Ajustes ----------
        GameObject ajustesGO = NuevoUI("Ajustes", canvas);
        Estirar((RectTransform)ajustesGO.transform);
        CanvasGroup ajustes = ajustesGO.AddComponent<CanvasGroup>();

        Image oscuro = NuevaImagen("FondoNoche", ajustesGO.transform, null);   // hijo 0
        Estirar(oscuro.rectTransform);
        oscuro.color = new Color(0.02f, 0.03f, 0.07f, 0.6f);   // de noche, pero se ve HomeBot detrás

        Image imgPanel = NuevaImagen("Panel", ajustesGO.transform, panel);      // hijo 1 (se anima)
        imgPanel.raycastTarget = false;
        RectTransform rtPanel = imgPanel.rectTransform;
        rtPanel.anchorMin = rtPanel.anchorMax = rtPanel.pivot = new Vector2(0.5f, 0.5f);
        rtPanel.sizeDelta = new Vector2(AltoPanel * TamPanel.x / TamPanel.y, AltoPanel);
        rtPanel.anchoredPosition = Vector2.zero;

        Slider volumen = CrearSlider("SliderVolumen", rtPanel, PxVolumen, riel, carga, perilla);
        Slider efectos = CrearSlider("SliderEfectos", rtPanel, PxEfectos, riel, carga, perilla);
        Button volver = CrearBoton("BotonVolver", rtPanel, bVolver, PxVolver, TamPanel);

        // ---------- Fundido a negro ----------
        Image negro = NuevaImagen("Fundido", canvas, null);
        Estirar(negro.rectTransform);
        negro.color = Color.black;
        negro.raycastTarget = false;
        CanvasGroup fundido = negro.gameObject.AddComponent<CanvasGroup>();
        fundido.blocksRaycasts = false;

        // ---------- Lógica ----------
        MenuInicio menu = new GameObject("Menu").AddComponent<MenuInicio>();
        menu.escenaJuego = Path.GetFileNameWithoutExtension(RutaJuego);
        menu.principal = principal;
        menu.botonJugar = jugar;
        menu.botonAjustes = ajustesBtn;
        menu.botonSalir = salir;
        menu.ajustes = ajustes;
        menu.sliderVolumen = volumen;
        menu.sliderEfectos = efectos;
        menu.botonVolver = volver;
        menu.fundido = fundido;

        ajustes.alpha = 0f;
        ajustes.interactable = ajustes.blocksRaycasts = false;
        fundido.alpha = 0f;

        Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerEnBuild();

        Debug.Log("Menú de inicio creado en " + RutaEscena + " y puesto como primera escena del juego.");
    }

    // ---------- Piezas ----------

    static Button CrearBoton(string nombre, Transform padre, Sprite sprite, Rect px, Vector2 tam)
    {
        Image img = NuevaImagen(nombre, padre, sprite);
        AnclarPx(img.rectTransform, px, tam);

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;   // la animación la hace BotonMenu
        img.gameObject.AddComponent<BotonMenu>();
        return b;
    }

    static Slider CrearSlider(string nombre, RectTransform panel, Rect px, Sprite riel, Sprite carga, Sprite perilla)
    {
        GameObject go = NuevoUI(nombre, panel);
        RectTransform rt = (RectTransform)go.transform;
        AnclarPx(rt, px, TamPanel);

        float alto = AltoPanel * px.height / TamPanel.y;
        float margen = Mathf.Round(alto * 0.18f);

        Image imgRiel = NuevaImagen("Riel", go.transform, riel);
        imgRiel.type = Image.Type.Sliced;
        Estirar(imgRiel.rectTransform);

        Image imgCarga = NuevaImagen("Carga", go.transform, carga);
        imgCarga.type = Image.Type.Filled;
        imgCarga.fillMethod = Image.FillMethod.Horizontal;
        imgCarga.fillOrigin = (int)Image.OriginHorizontal.Left;
        imgCarga.fillAmount = 0.8f;
        imgCarga.raycastTarget = false;
        Estirar(imgCarga.rectTransform, margen, margen);

        GameObject area = NuevoUI("AreaPerilla", go.transform);
        Estirar((RectTransform)area.transform, margen, 0f);

        Image imgPerilla = NuevaImagen("Perilla", area.transform, perilla);
        RectTransform rtPerilla = imgPerilla.rectTransform;
        rtPerilla.sizeDelta = new Vector2(alto * 0.55f, alto * 0.35f);   // ancho fijo, sobresale arriba y abajo

        Slider s = go.AddComponent<Slider>();
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.handleRect = rtPerilla;
        s.targetGraphic = imgPerilla;
        s.value = 0.8f;
        ColorBlock cb = s.colors;
        cb.highlightedColor = new Color(1f, 1f, 0.85f);
        cb.selectedColor = new Color(1f, 1f, 0.8f);
        s.colors = cb;

        go.AddComponent<SliderCarga>().carga = imgCarga;
        return s;
    }

    // ---------- Utilidades ----------

    static Sprite Cargar(string archivo, Vector4 borde = default)
    {
        string ruta = Sprites + archivo;
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null)
        {
            AssetDatabase.ImportAsset(ruta);
            imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) return null;
        }

        if (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled || imp.spriteBorder != borde ||
            imp.textureCompression != TextureImporterCompression.Uncompressed)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.spriteBorder = borde;
            imp.textureCompression = TextureImporterCompression.Uncompressed;   // nítido, sin artefactos
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    static void PonerEnBuild()
    {
        var lista = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(RutaEscena, true),
        };
        if (File.Exists(RutaJuego)) lista.Add(new EditorBuildSettingsScene(RutaJuego, true));

        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.path == RutaEscena || s.path == RutaJuego || !File.Exists(s.path)) continue;
            lista.Add(s);
        }
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // Coloca un RectTransform usando píxeles de la imagen (origen arriba-izquierda) como anclas.
    static void AnclarPx(RectTransform rt, Rect px, Vector2 tam)
    {
        rt.anchorMin = new Vector2(px.xMin / tam.x, 1f - px.yMax / tam.y);
        rt.anchorMax = new Vector2(px.xMax / tam.x, 1f - px.yMin / tam.y);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void Estirar(RectTransform rt, float margenX = 0f, float margenY = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margenX, margenY);
        rt.offsetMax = new Vector2(-margenX, -margenY);
    }

    static Image NuevaImagen(string nombre, Transform padre, Sprite sprite)
    {
        GameObject go = NuevoUI(nombre, padre);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        return img;
    }

    static GameObject NuevoUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(padre, false);
        return go;
    }
}
