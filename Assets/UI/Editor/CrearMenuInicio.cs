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
    internal const string Sprites = "Assets/UI/Sprites/Menu/";

    // Medidas en píxeles de las imágenes recortadas (origen arriba a la izquierda)
    static readonly Vector2 TamFondo = new Vector2(1456f, 736f);

    // Columna centrada del menú principal: tamaños en píxeles de la imagen original.
    // Se muestran a la misma escala que el fondo (1080 / 736) para que se vean igual que en la imagen.
    static readonly Vector2 PxLogo = new Vector2(528f, 208f);
    static readonly Vector2 PxPila = new Vector2(215f, 80f);
    static readonly Vector2 PxJugar = new Vector2(250f, 68f);
    static readonly Vector2 PxAjustes = new Vector2(242f, 63f);
    static readonly Vector2 PxSalir = new Vector2(241f, 63f);
    const float EspacioColumna = 10f;

    internal static readonly Vector2 TamPanel = new Vector2(491f, 410f);
    internal static readonly Rect PxVolumen = new Rect(168f, 192f, 244f, 38f);
    internal static readonly Rect PxEfectos = new Rect(168f, 262f, 244f, 38f);
    internal static readonly Rect PxVolver = new Rect(158f, 313f, 202f, 62f);

    internal const float AltoPanel = 700f;   // en la resolución de referencia 1920x1080

    [MenuItem("HomeBot/Crear escena del menú de inicio")]
    public static void Crear()
    {
        if (!HerramientasHomeBot.FueraDePlay("Menú de inicio")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog("Menú de inicio", "Ya existe " + RutaEscena + ". ¿Reemplazarla?", "Reemplazar", "Cancelar"))
            return;

        Sprite fondo = Cargar("Menu_Fondo.png");
        Sprite logo = Cargar("Menu_Logo.png");
        Sprite pila = Cargar("Menu_Pila.png");
        Sprite ojo = Cargar("Menu_OjoRobot.png");
        Sprite bJugar = Cargar("Menu_BotonJugar.png");
        Sprite bAjustes = Cargar("Menu_BotonAjustes.png");
        Sprite bSalir = Cargar("Menu_BotonSalir.png");
        Sprite panel = Cargar("Ajustes_Panel.png");
        Sprite bVolver = Cargar("Ajustes_BotonVolver.png");
        Sprite riel = Cargar("Slider_Riel.png", new Vector4(22, 22, 22, 22));
        Sprite carga = Cargar("Slider_Carga.png");
        Sprite perilla = Cargar("Slider_Perilla.png");
        if (fondo == null || logo == null || pila == null || bJugar == null || bAjustes == null || bSalir == null || panel == null ||
            bVolver == null || riel == null || carga == null || perilla == null || ojo == null)
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

        CrearOjosRobot(imgFondo.rectTransform, ojo);

        // Título, pila y botones: columna centrada en la pantalla, independiente del fondo.
        // Para moverla o cambiar el espacio, ajusta "Principal" (Pos Y) o su Vertical Layout Group (Spacing).
        float k = 1080f / TamFondo.y;
        GameObject principalGO = NuevoUI("Principal", canvas);
        RectTransform rtPrincipal = (RectTransform)principalGO.transform;
        rtPrincipal.anchorMin = rtPrincipal.anchorMax = rtPrincipal.pivot = new Vector2(0.5f, 0.5f);
        rtPrincipal.anchoredPosition = Vector2.zero;
        rtPrincipal.sizeDelta = new Vector2(PxLogo.x * k, 1000f);
        CanvasGroup principal = principalGO.AddComponent<CanvasGroup>();
        VerticalLayoutGroup columna = principalGO.AddComponent<VerticalLayoutGroup>();
        columna.childAlignment = TextAnchor.MiddleCenter;
        columna.spacing = EspacioColumna;
        columna.childControlWidth = columna.childControlHeight = true;
        columna.childForceExpandWidth = columna.childForceExpandHeight = false;

        Pieza("Titulo", principalGO.transform, logo, PxLogo * k);
        Pieza("Pila", principalGO.transform, pila, PxPila * k);
        Button jugar = CrearBotonColumna("BotonJugar", principalGO.transform, bJugar, PxJugar * k);
        Button ajustesBtn = CrearBotonColumna("BotonAjustes", principalGO.transform, bAjustes, PxAjustes * k);
        Button salir = CrearBotonColumna("BotonSalir", principalGO.transform, bSalir, PxSalir * k);

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

    // ---------- Ojos del robot (siguen al mouse) ----------

    // Medidas en la imagen original: punto entre los dos ojos, caja de la cara y giro de la cabeza
    static readonly Vector2 PxCentroOjos = new Vector2(1245.25f, 216.25f);
    static readonly Vector2 PxCajaOjos = new Vector2(70f, 40f);
    const float SeparacionOjos = 19.5f;   // de cada ojo al centro
    const float TamOjo = 42f;             // ojo con su brillo
    const float GiroCabeza = 15.6f;       // grados

    static void CrearOjosRobot(RectTransform fondo, Sprite ojo)
    {
        GameObject go = NuevoUI("OjosRobot", fondo);
        RectTransform rt = (RectTransform)go.transform;
        Vector2 c = PxCentroOjos, m = PxCajaOjos * 0.5f;
        rt.anchorMin = new Vector2((c.x - m.x) / TamFondo.x, 1f - (c.y + m.y) / TamFondo.y);
        rt.anchorMax = new Vector2((c.x + m.x) / TamFondo.x, 1f - (c.y - m.y) / TamFondo.y);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localRotation = Quaternion.Euler(0f, 0f, GiroCabeza);

        RectTransform[] ojos = new RectTransform[2];
        float[] lados = { -SeparacionOjos, SeparacionOjos };
        for (int i = 0; i < 2; i++)
        {
            Image img = NuevaImagen(i == 0 ? "OjoIzquierdo" : "OjoDerecho", rt, ojo);
            img.raycastTarget = false;
            RectTransform o = img.rectTransform;
            float cx = 0.5f + lados[i] / PxCajaOjos.x;
            float mx = TamOjo * 0.5f / PxCajaOjos.x, my = TamOjo * 0.5f / PxCajaOjos.y;
            o.anchorMin = new Vector2(cx - mx, 0.5f - my);
            o.anchorMax = new Vector2(cx + mx, 0.5f + my);
            o.offsetMin = o.offsetMax = Vector2.zero;
            ojos[i] = o;
        }

        go.AddComponent<OjosRobot>().ojos = ojos;
    }

    // ---------- Piezas ----------

    static Image Pieza(string nombre, Transform padre, Sprite sprite, Vector2 tam)
    {
        Image img = NuevaImagen(nombre, padre, sprite);
        img.preserveAspect = true;
        img.raycastTarget = false;
        LayoutElement le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = tam.x;
        le.preferredHeight = tam.y;
        return img;
    }

    static Button CrearBotonColumna(string nombre, Transform padre, Sprite sprite, Vector2 tam)
    {
        Image img = Pieza(nombre, padre, sprite, tam);
        img.raycastTarget = true;

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;   // la animación la hace BotonMenu
        img.gameObject.AddComponent<BotonMenu>();
        return b;
    }

    internal static Button CrearBoton(string nombre, Transform padre, Sprite sprite, Rect px, Vector2 tam)
    {
        Image img = NuevaImagen(nombre, padre, sprite);
        AnclarPx(img.rectTransform, px, tam);

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;   // la animación la hace BotonMenu
        img.gameObject.AddComponent<BotonMenu>();
        return b;
    }

    internal static Slider CrearSlider(string nombre, RectTransform panel, Rect px, Sprite riel, Sprite carga, Sprite perilla)
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
        return CargarRuta(Sprites + archivo, borde);
    }

    internal static Sprite CargarRuta(string ruta, Vector4 borde = default)
    {
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
    internal static void AnclarPx(RectTransform rt, Rect px, Vector2 tam)
    {
        rt.anchorMin = new Vector2(px.xMin / tam.x, 1f - px.yMax / tam.y);
        rt.anchorMax = new Vector2(px.xMax / tam.x, 1f - px.yMin / tam.y);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    internal static void Estirar(RectTransform rt, float margenX = 0f, float margenY = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margenX, margenY);
        rt.offsetMax = new Vector2(-margenX, -margenY);
    }

    internal static Image NuevaImagen(string nombre, Transform padre, Sprite sprite)
    {
        GameObject go = NuevoUI(nombre, padre);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        return img;
    }

    internal static GameObject NuevoUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(padre, false);
        return go;
    }
}
