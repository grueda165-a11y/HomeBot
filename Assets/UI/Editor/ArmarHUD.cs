using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menú: HomeBot > Armar HUD
// Arma la interfaz dentro del Canvas de la escena abierta:
//  - 3 caritas de vidas (arriba a la derecha) con efecto eléctrico
//  - Contador de intentos debajo de las caritas
//  - Pila de juguete abajo al centro (90 s), con alerta roja y rebote al recargar
//  - Pantalla de muerte: el robot se apaga (estática, apagado de tele, carita con ojos en X)
//  - Quita los textos viejos y la barra plana anterior
// Se puede volver a ejecutar sin duplicar nada, y todo se puede deshacer con Ctrl+Z.
public static class ArmarHUD
{
    const string CarpetaSprites = "Assets/UI/Sprites/";
    const string NombreVidas = "HUD_Vidas";
    const string NombreIntentos = "HUD_Intentos";
    const string NombrePila = "HUD_Bateria";
    const string NombreMuerte = "PantallaMuerte";
    const string NombreRayos = "Rayos";

    const float Margen = 40f;
    const float TamCarita = 110f;
    const float EspacioCaritas = 16f;

    // Pila: 616 x 140 (sprite de 704x160), con su centro a 105 px del borde inferior (~35 px de aire abajo)
    static readonly Vector2 TamPila = new Vector2(616f, 140f);
    const float AlturaPila = 105f;
    const float TiempoBateria = 90f;

    static readonly Color ColorRayo = new Color(0.45f, 0.9f, 1f, 1f);
    static readonly Color ColorTextoRobot = new Color(0.78f, 0.88f, 0.9f);

    [MenuItem("HomeBot/Armar HUD")]
    public static void Armar()
    {
        Sprite marco = CargarSprite("Carita_Marco.png");
        Sprite pantalla = CargarSprite("Carita_Pantalla.png");
        Sprite cara = CargarSprite("Carita_Cara.png");
        Sprite ojosX = CargarSprite("Carita_OjosX.png");
        Sprite carcasa = CargarSprite("Pila_Carcasa.png");
        Sprite rellenoPila = CargarSprite("Pila_Relleno.png");
        Sprite brillo = CargarSprite("Pila_Brillo.png");
        Sprite boton = CargarSprite("Boton.png", new Vector4(24, 24, 24, 24));
        if (marco == null || pantalla == null || cara == null || ojosX == null ||
            carcasa == null || rellenoPila == null || brillo == null || boton == null)
        {
            EditorUtility.DisplayDialog("Armar HUD", "Faltan sprites en " + CarpetaSprites, "OK");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Armar HUD", "No hay un Canvas en la escena.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Armar HUD");
        int grupo = Undo.GetCurrentGroup();

        VidaJugador jugador = Object.FindFirstObjectByType<VidaJugador>();
        if (jugador != null)
        {
            Undo.RecordObject(jugador, "Tiempo de batería");
            jugador.vidaMaxima = TiempoBateria;
            jugador.vidaActual = TiempoBateria;
        }

        Transform raiz = canvas.transform;

        QuitarTextosViejos(raiz);
        QuitarBarraVieja(raiz);
        CrearPila(raiz, jugador, carcasa, rellenoPila, brillo);
        CrearVidas(raiz, jugador, marco, pantalla, cara);
        CrearIntentos(raiz);
        CrearPantallaMuerte(raiz, jugador, marco, pantalla, ojosX, boton);
        AsegurarEventSystem();

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("HUD armado. Guarda la escena (Ctrl+S).");
    }

    // ---------- Sprites ----------

    static Sprite CargarSprite(string archivo, Vector4 borde = default)
    {
        string ruta = CarpetaSprites + archivo;
        TextureImporter importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(ruta);
            importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importer == null) return null;
        }

        if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || importer.spriteBorder != borde)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteBorder = borde;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ---------- Limpieza ----------

    static void QuitarTextosViejos(Transform canvas)
    {
        // Los textos "Text (TMP)" que estaban directo en el Canvas mostraban "Vida: 120" y "Vidas: 3".
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            Transform hijo = canvas.GetChild(i);
            if (hijo.name.StartsWith("Text (TMP)") && hijo.GetComponent<TMP_Text>() != null)
                Undo.DestroyObjectImmediate(hijo.gameObject);
        }
    }

    static void QuitarBarraVieja(Transform canvas)
    {
        // La barra plana anterior: el objeto que contiene la imagen "fill".
        foreach (Image img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img == null || img.name != "fill") continue;
            if (img.GetComponentInParent<BarraTiempo>(true) is BarraTiempo b && b.name == NombrePila) continue;

            Transform viejo = img.transform.parent != null && img.transform.parent != canvas ? img.transform.parent : img.transform;
            Undo.DestroyObjectImmediate(viejo.gameObject);
            return;
        }
    }

    // ---------- Pila de juguete ----------

    static void CrearPila(Transform canvas, VidaJugador jugador, Sprite carcasa, Sprite rellenoSprite, Sprite brillo)
    {
        Borrar(canvas, NombrePila);

        GameObject pila = NuevoUI(NombrePila, canvas);
        RectTransform rt = (RectTransform)pila.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // abajo al centro
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = TamPila;
        rt.anchoredPosition = new Vector2(0f, AlturaPila);

        Image imgCarcasa = Capa("Carcasa", pila.transform, carcasa);
        imgCarcasa.preserveAspect = false;

        // La carga va justo dentro de la ventana de la carcasa (medidas en píxeles del sprite de 704x160)
        GameObject goRelleno = NuevoUI("Carga", pila.transform);
        RectTransform rtRelleno = (RectTransform)goRelleno.transform;
        rtRelleno.anchorMin = new Vector2(58f / 704f, 36f / 160f);
        rtRelleno.anchorMax = new Vector2(598f / 704f, 124f / 160f);
        rtRelleno.offsetMin = rtRelleno.offsetMax = Vector2.zero;
        Image relleno = goRelleno.AddComponent<Image>();
        relleno.sprite = rellenoSprite;
        relleno.type = Image.Type.Filled;
        relleno.fillMethod = Image.FillMethod.Horizontal;
        relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
        relleno.fillAmount = 1f;
        relleno.raycastTarget = false;

        Image imgBrillo = Capa("Brillo", pila.transform, brillo);
        imgBrillo.preserveAspect = false;

        // Chispita discreta al recargar: un solo rayo fino y muy corto, sin temblor ni parpadeo
        EfectoElectrico efecto = AsegurarEfecto(pila, 2f, 1.2f);
        efecto.duracion = 0.22f;
        efecto.temblor = 0f;
        efecto.parpadeo = 0f;
        efecto.intervaloRayos = 0.08f;
        efecto.rayos.cantidad = 1;
        efecto.rayos.segmentos = 6;
        efecto.rayos.desvio = 0.06f;
        efecto.rayos.probabilidadRama = 0f;
        efecto.rayos.alfaBrillo = 0.15f;
        efecto.rayos.anchoBrillo = 2.5f;

        BarraTiempo barra = pila.AddComponent<BarraTiempo>();
        barra.jugador = jugador;
        barra.relleno = relleno;
        barra.objetoRebote = rt;
        barra.efecto = efecto;
        relleno.color = barra.colorLlena;
    }

    // ---------- Vidas ----------

    static void CrearVidas(Transform canvas, VidaJugador jugador, Sprite marco, Sprite pantalla, Sprite cara)
    {
        Borrar(canvas, NombreVidas);

        GameObject contenedor = NuevoUI(NombreVidas, canvas);
        RectTransform rt = (RectTransform)contenedor.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);  // arriba a la derecha
        rt.anchoredPosition = new Vector2(-Margen, -Margen);
        rt.sizeDelta = new Vector2(3 * TamCarita + 2 * EspacioCaritas, TamCarita);

        HorizontalLayoutGroup layout = contenedor.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = EspacioCaritas;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        GameObject[] caritas = new GameObject[3];
        Image[] pantallas = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject carita = NuevoUI("Carita_" + (i + 1), contenedor.transform);
            ((RectTransform)carita.transform).sizeDelta = new Vector2(TamCarita, TamCarita);

            Capa("Marco", carita.transform, marco);
            pantallas[i] = Capa("Pantalla", carita.transform, pantalla);
            Capa("OjosYBoca", carita.transform, cara);

            caritas[i] = carita;
        }

        EfectoElectrico efecto = AsegurarEfecto(contenedor, 28f, 3.5f);
        efecto.duracion = 0.8f;
        efecto.temblor = 8f;

        HUDVidas hud = contenedor.AddComponent<HUDVidas>();
        hud.jugador = jugador;
        hud.caritas = caritas;
        hud.pantallas = pantallas;
        hud.efecto = efecto;

        foreach (Image p in pantallas) p.color = hud.colorTresVidas;
    }

    // ---------- Intentos ----------

    static void CrearIntentos(Transform canvas)
    {
        Borrar(canvas, NombreIntentos);

        GameObject go = NuevoUI(NombreIntentos, canvas);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);  // debajo de las caritas
        rt.anchoredPosition = new Vector2(-Margen, -Margen - TamCarita - 12f);
        rt.sizeDelta = new Vector2(3 * TamCarita + 2 * EspacioCaritas, 60f);

        TextMeshProUGUI texto = Texto(go, "Intento: 1", 44, TextAlignmentOptions.TopRight, Color.white);

        ContadorIntentos contador = go.AddComponent<ContadorIntentos>();
        contador.texto = texto;
    }

    // ---------- Pantalla de muerte: robot apagado ----------

    static void CrearPantallaMuerte(Transform canvas, VidaJugador jugador, Sprite marco, Sprite pantalla, Sprite ojosX, Sprite boton)
    {
        Borrar(canvas, NombreMuerte);

        GameObject panel = NuevoUI(NombreMuerte, canvas);
        Estirar((RectTransform)panel.transform, 0f);
        panel.transform.SetAsLastSibling();   // por encima de todo
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        AudioSource audio = panel.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        // 1. Estática
        GameObject goEstatica = NuevoUI("Estatica", panel.transform);
        Estirar((RectTransform)goEstatica.transform, 0f);
        RawImage estatica = goEstatica.AddComponent<RawImage>();
        estatica.color = new Color(1f, 1f, 1f, 0.5f);
        estatica.raycastTarget = false;
        goEstatica.SetActive(false);

        // Franjas de interferencia
        GameObject goFranjas = NuevoUI("Interferencia", panel.transform);
        Estirar((RectTransform)goFranjas.transform, 0f);
        Image[] franjas = new Image[6];
        for (int i = 0; i < franjas.Length; i++)
        {
            GameObject f = NuevoUI("Franja_" + (i + 1), goFranjas.transform);
            franjas[i] = f.AddComponent<Image>();
            franjas[i].raycastTarget = false;
            f.SetActive(false);
        }

        // 2. Cortinas negras + línea del apagado
        RectTransform cortinaAbajo = Negro("CortinaAbajo", panel.transform);
        RectTransform cortinaArriba = Negro("CortinaArriba", panel.transform);

        GameObject goLinea = NuevoUI("LineaApagado", panel.transform);
        Image linea = goLinea.AddComponent<Image>();
        linea.color = new Color(0.85f, 0.97f, 1f, 1f);
        linea.raycastTarget = false;
        goLinea.SetActive(false);

        // 4. Contenido final
        GameObject goContenido = NuevoUI("RobotApagado", panel.transform);
        Estirar((RectTransform)goContenido.transform, 0f);
        CanvasGroup contenido = goContenido.AddComponent<CanvasGroup>();
        contenido.alpha = 0f;

        GameObject carita = NuevoUI("CaritaApagada", goContenido.transform);
        RectTransform rtCarita = (RectTransform)carita.transform;
        rtCarita.anchorMin = rtCarita.anchorMax = rtCarita.pivot = new Vector2(0.5f, 0.5f);
        rtCarita.anchoredPosition = new Vector2(0f, 200f);
        rtCarita.sizeDelta = new Vector2(220f, 220f);
        Capa("Marco", carita.transform, marco);
        Image pantallaApagada = Capa("Pantalla", carita.transform, pantalla);
        Capa("OjosX", carita.transform, ojosX);

        GameObject goTitulo = NuevoUI("Titulo", goContenido.transform);
        Centro((RectTransform)goTitulo.transform, new Vector2(0f, 20f), new Vector2(1300f, 120f));
        TextMeshProUGUI titulo = Texto(goTitulo, "BATERÍA AGOTADA", 92, TextAlignmentOptions.Center, ColorTextoRobot);
        titulo.characterSpacing = 6f;

        GameObject goSub = NuevoUI("Subtitulo", goContenido.transform);
        Centro((RectTransform)goSub.transform, new Vector2(0f, -55f), new Vector2(1300f, 60f));
        TextMeshProUGUI sub = Texto(goSub, "El robot se apagó...", 38, TextAlignmentOptions.Center, new Color(0.55f, 0.62f, 0.64f));
        sub.fontStyle = FontStyles.Italic;

        GameObject fila = NuevoUI("Botones", goContenido.transform);
        Centro((RectTransform)fila.transform, new Vector2(0f, -180f), new Vector2(760f, 110f));
        HorizontalLayoutGroup hl = fila.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 40f;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;

        Button reintentar = CrearBoton("Reintentar", fila.transform, boton, new Color(0.20f, 0.56f, 0.62f), Color.white);
        Button salir = CrearBoton("Menú", fila.transform, boton, new Color(0.26f, 0.29f, 0.31f), Color.white);   // vuelve al menú de inicio

        goContenido.SetActive(false);

        PantallaMuerte pm = panel.AddComponent<PantallaMuerte>();
        pm.jugador = jugador;
        pm.botonReintentar = reintentar;
        pm.botonSalir = salir;
        pm.estatica = estatica;
        pm.franjasGlitch = franjas;
        pm.cortinaArriba = cortinaArriba;
        pm.cortinaAbajo = cortinaAbajo;
        pm.lineaApagado = (RectTransform)goLinea.transform;
        pm.contenido = contenido;
        pm.pantallaCarita = pantallaApagada;
        pm.titulo = titulo;
        pm.audioSource = audio;
        pantallaApagada.color = pm.colorPantallaApagada;
    }

    static RectTransform Negro(string nombre, Transform padre)
    {
        GameObject go = NuevoUI(nombre, padre);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        go.SetActive(false);
        return rt;
    }

    static Button CrearBoton(string texto, Transform padre, Sprite sprite, Color color, Color colorTexto)
    {
        GameObject go = NuevoUI("Boton" + texto, padre);
        ((RectTransform)go.transform).sizeDelta = new Vector2(340f, 100f);

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.selectedColor = new Color(1.1f, 1.1f, 1.1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        cb.colorMultiplier = 1.2f;
        btn.colors = cb;

        GameObject t = NuevoUI("Texto", go.transform);
        Estirar((RectTransform)t.transform, 0f);
        Texto(t, texto.ToUpper(), 46, TextAlignmentOptions.Center, colorTexto);

        return btn;
    }

    static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "EventSystem");
    }

    // ---------- Efecto eléctrico ----------

    static EfectoElectrico AsegurarEfecto(GameObject go, float margenRayos, float grosor)
    {
        if (go.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(go);

        Transform t = go.transform.Find(NombreRayos);
        RayoUI rayo = t != null ? t.GetComponent<RayoUI>() : null;
        if (rayo == null)
        {
            GameObject r = NuevoUI(NombreRayos, go.transform);
            rayo = r.AddComponent<RayoUI>();
        }

        // Que los layouts no lo acomoden como si fuera un elemento más.
        LayoutElement le = rayo.GetComponent<LayoutElement>();
        if (le == null) le = Undo.AddComponent<LayoutElement>(rayo.gameObject);
        le.ignoreLayout = true;

        Undo.RecordObject(rayo, "Rayos");
        Estirar((RectTransform)rayo.transform, margenRayos);
        rayo.color = ColorRayo;
        rayo.grosor = grosor;
        rayo.raycastTarget = false;
        rayo.enabled = false;
        rayo.transform.SetAsLastSibling();

        EfectoElectrico efecto = go.GetComponent<EfectoElectrico>();
        if (efecto == null) efecto = Undo.AddComponent<EfectoElectrico>(go);
        Undo.RecordObject(efecto, "Efecto eléctrico");
        efecto.rayos = rayo;
        efecto.grupo = go.GetComponent<CanvasGroup>();
        return efecto;
    }

    // ---------- Utilidades ----------

    static Image Capa(string nombre, Transform padre, Sprite sprite)
    {
        GameObject go = NuevoUI(nombre, padre);
        Estirar((RectTransform)go.transform, 0f);

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Texto(GameObject go, string texto, float tam, TextAlignmentOptions alineacion, Color color)
    {
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tam;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alineacion;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Centro(RectTransform rt, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Borrar(Transform padre, string nombre)
    {
        Transform viejo = padre.Find(nombre);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);
    }

    static void Estirar(RectTransform rt, float margenAfuera)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-margenAfuera, -margenAfuera);
        rt.offsetMax = new Vector2(margenAfuera, margenAfuera);
    }

    static GameObject NuevoUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(padre, false);
        return go;
    }
}
