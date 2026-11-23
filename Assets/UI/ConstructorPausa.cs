using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Arma el botón de pausa del HUD y el menú de pausa.
//  - Automático: al dar Play en una escena de juego (una que tenga VidaJugador),
//    si todavía no hay menú de pausa, se crea solo. No hay que configurar nada.
//  - También lo usa la herramienta del editor (HomeBot > Armar menú de pausa) para dejarlo guardado en la escena.
// Las imágenes están en Assets/UI/Resources/HomeBotUI.
public static class ConstructorPausa
{
    const string Carpeta = "HomeBotUI/";

    // Medidas en píxeles de la imagen del panel (pantalla del robot, 491x410)
    static readonly Vector2 TamPanel = new Vector2(491f, 410f);
    static readonly Rect PxVolumen = new Rect(168f, 192f, 244f, 38f);
    static readonly Rect PxEfectos = new Rect(168f, 262f, 244f, 38f);
    static readonly Rect PxVolver = new Rect(158f, 313f, 202f, 62f);
    const float AltoPanel = 700f;   // en la resolución de referencia 1920x1080

    static readonly string[] Botones = { "Continuar", "Reiniciar", "Ajustes", "Menu" };
    const float BotonX = 148.5f, BotonY = 140f, BotonAncho = 193f, BotonAlto = 50f, BotonEspacio = 7f;

    const float TamBotonHUD = 110f, MargenHUD = 40f;

    // ---------- Automático al dar Play ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Revisar(SceneManager.GetActiveScene());
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Revisar(escena);

    // Solo se mira DENTRO de la escena que se cargó: al pasar del juego al menú,
    // los objetos del juego todavía existen un instante y no deben contar.
    static void Revisar(Scene escena)
    {
        if (!escena.IsValid() || !escena.isLoaded) return;

        VidaJugador jugador = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (raiz.GetComponentInChildren<MenuInicio>(true) != null) return;   // es el menú de inicio
            if (raiz.GetComponentInChildren<MenuPausa>(true) != null) return;    // ya tiene menú de pausa
            if (jugador == null) jugador = raiz.GetComponentInChildren<VidaJugador>(true);
        }
        if (jugador == null) return;   // no es una escena de juego

        Construir(BuscarCanvasHUD(escena), jugador);
    }

    static Transform BuscarCanvasHUD(Scene escena)
    {
        Canvas elegido = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        foreach (Canvas c in raiz.GetComponentsInChildren<Canvas>(true))
        {
            if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (c.transform.Find("HUD_Vidas") != null) return c.transform;   // el del HUD
            if (elegido == null) elegido = c;
        }
        if (elegido != null) return elegido.transform;

        // No hay Canvas: se crea uno
        GameObject go = new GameObject("CanvasPausa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        if (go.scene != escena) SceneManager.MoveGameObjectToScene(go, escena);
        return go.transform;
    }

    // ---------- Construcción ----------

    // Devuelve la raíz del menú (y el botón del HUD por "botonHUD"), o null si faltan imágenes.
    public static MenuPausa Construir(Transform canvas, VidaJugador jugador)
    {
        Sprite panelPausa = Cargar("Pausa_Panel");
        Sprite panelAjustes = Cargar("Ajustes_Panel");
        Sprite volver = Cargar("Ajustes_BotonVolver");
        Sprite riel = Cargar("Slider_Riel");
        Sprite carga = Cargar("Slider_Carga");
        Sprite perilla = Cargar("Slider_Perilla");
        Sprite botonHUD = Cargar("Pausa_BotonHUD");
        var sprites = new Sprite[Botones.Length];
        for (int i = 0; i < Botones.Length; i++) sprites[i] = Cargar("Pausa_Boton" + Botones[i]);

        if (panelPausa == null || panelAjustes == null || volver == null || riel == null || carga == null ||
            perilla == null || botonHUD == null || System.Array.IndexOf(sprites, null) >= 0)
        {
            Debug.LogWarning("Menú de pausa: faltan imágenes en Assets/UI/Resources/" + Carpeta);
            return null;
        }

        AsegurarEventSystem(canvas.gameObject.scene);
        Borrar(canvas, "MenuPausa");
        Borrar(canvas, "BotonPausa");

        // ---- Botón de pausa del HUD (arriba a la izquierda, como las vidas y la pila) ----
        Image imgHUD = Imagen("BotonPausa", canvas, botonHUD);
        imgHUD.preserveAspect = true;
        RectTransform rtHUD = imgHUD.rectTransform;
        rtHUD.anchorMin = rtHUD.anchorMax = new Vector2(0f, 1f);
        rtHUD.pivot = new Vector2(0.5f, 0.5f);
        rtHUD.sizeDelta = new Vector2(TamBotonHUD, TamBotonHUD);
        rtHUD.anchoredPosition = new Vector2(MargenHUD + TamBotonHUD / 2f, -MargenHUD - TamBotonHUD / 2f);
        Button bHUD = imgHUD.gameObject.AddComponent<Button>();
        bHUD.targetGraphic = imgHUD;
        bHUD.transition = Selectable.Transition.None;
        Navigation nav = bHUD.navigation; nav.mode = Navigation.Mode.None; bHUD.navigation = nav;   // que Espacio (saltar) no lo active
        imgHUD.gameObject.AddComponent<BotonMenu>().latirSeleccionado = false;
        Transform muerte = canvas.Find("PantallaMuerte");
        if (muerte != null) imgHUD.transform.SetSiblingIndex(muerte.GetSiblingIndex());   // debajo de la pantalla de muerte

        // ---- Raíz del menú: capa propia, siempre encima de todo el HUD ----
        GameObject raiz = NuevoUI("MenuPausa", canvas);
        Estirar((RectTransform)raiz.transform);
        raiz.transform.SetAsLastSibling();
        Canvas capa = raiz.AddComponent<Canvas>();
        capa.overrideSorting = true;
        capa.sortingOrder = 100;
        raiz.AddComponent<GraphicRaycaster>();
        CanvasGroup grupo = raiz.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = grupo.blocksRaycasts = false;

        Image oscuro = Imagen("FondoOscuro", raiz.transform, null);
        Estirar(oscuro.rectTransform);
        oscuro.color = new Color(0.02f, 0.03f, 0.07f, 0.65f);

        Vector2 tamPanel = new Vector2(AltoPanel * TamPanel.x / TamPanel.y, AltoPanel);

        RectTransform rtPausa = Panel("PanelPausa", raiz.transform, panelPausa, tamPanel, out CanvasGroup cgPausa);
        var b = new Button[Botones.Length];
        for (int i = 0; i < Botones.Length; i++)
        {
            Rect px = new Rect(BotonX, BotonY + i * (BotonAlto + BotonEspacio), BotonAncho, BotonAlto);
            b[i] = Boton("Boton" + Botones[i], rtPausa, sprites[i], px);
        }

        RectTransform rtAjustes = Panel("PanelAjustes", raiz.transform, panelAjustes, tamPanel, out CanvasGroup cgAjustes);
        Slider volumen = CrearSlider("SliderVolumen", rtAjustes, PxVolumen, riel, carga, perilla);
        Slider efectos = CrearSlider("SliderEfectos", rtAjustes, PxEfectos, riel, carga, perilla);
        Button bVolver = Boton("BotonVolver", rtAjustes, volver, PxVolver);
        cgAjustes.alpha = 0f;
        cgAjustes.interactable = cgAjustes.blocksRaycasts = false;

        MenuPausa menu = raiz.AddComponent<MenuPausa>();
        menu.jugador = jugador;
        menu.botonPausaHUD = bHUD;
        menu.panelPausa = cgPausa;
        menu.botonContinuar = b[0];
        menu.botonReiniciar = b[1];
        menu.botonAjustes = b[2];
        menu.botonMenu = b[3];
        menu.panelAjustes = cgAjustes;
        menu.sliderVolumen = volumen;
        menu.sliderEfectos = efectos;
        menu.botonVolver = bVolver;
        return menu;
    }

    // ---------- Piezas ----------

    static RectTransform Panel(string nombre, Transform padre, Sprite sprite, Vector2 tam, out CanvasGroup cg)
    {
        Image img = Imagen(nombre, padre, sprite);
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tam;
        rt.anchoredPosition = Vector2.zero;
        cg = img.gameObject.AddComponent<CanvasGroup>();
        return rt;
    }

    static Button Boton(string nombre, Transform padre, Sprite sprite, Rect px)
    {
        Image img = Imagen(nombre, padre, sprite);
        AnclarPx(img.rectTransform, px);
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
        AnclarPx(rt, px);

        float alto = AltoPanel * px.height / TamPanel.y;
        float margen = Mathf.Round(alto * 0.18f);

        Image imgRiel = Imagen("Riel", go.transform, riel);
        imgRiel.type = Image.Type.Sliced;
        Estirar(imgRiel.rectTransform);

        Image imgCarga = Imagen("Carga", go.transform, carga);
        imgCarga.type = Image.Type.Filled;
        imgCarga.fillMethod = Image.FillMethod.Horizontal;
        imgCarga.fillOrigin = (int)Image.OriginHorizontal.Left;
        imgCarga.raycastTarget = false;
        Estirar(imgCarga.rectTransform, margen, margen);

        GameObject area = NuevoUI("AreaPerilla", go.transform);
        Estirar((RectTransform)area.transform, margen, 0f);

        Image imgPerilla = Imagen("Perilla", area.transform, perilla);
        imgPerilla.rectTransform.sizeDelta = new Vector2(alto * 0.55f, alto * 0.35f);

        // SliderCarga va antes que Slider para que esté listo cuando el valor cambie
        SliderCarga sc = go.AddComponent<SliderCarga>();
        Slider s = go.GetComponent<Slider>();
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.handleRect = imgPerilla.rectTransform;
        s.targetGraphic = imgPerilla;
        ColorBlock cbl = s.colors;
        cbl.highlightedColor = new Color(1f, 1f, 0.85f);
        cbl.selectedColor = new Color(1f, 1f, 0.8f);
        s.colors = cbl;
        sc.carga = imgCarga;
        s.value = AjustesAudio.Musica;
        return s;
    }

    // ---------- Utilidades ----------

    static Sprite Cargar(string nombre) => Resources.Load<Sprite>(Carpeta + nombre);

    static void AsegurarEventSystem(Scene escena)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
            if (raiz.GetComponentInChildren<EventSystem>(true) != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        if (go.scene != escena) SceneManager.MoveGameObjectToScene(go, escena);
    }

    static void Borrar(Transform padre, string nombre)
    {
        Transform t = padre.Find(nombre);
        if (t == null) return;
        if (Application.isPlaying) Object.Destroy(t.gameObject);
        else Object.DestroyImmediate(t.gameObject);
    }

    static void AnclarPx(RectTransform rt, Rect px)
    {
        rt.anchorMin = new Vector2(px.xMin / TamPanel.x, 1f - px.yMax / TamPanel.y);
        rt.anchorMax = new Vector2(px.xMax / TamPanel.x, 1f - px.yMin / TamPanel.y);
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

    static Image Imagen(string nombre, Transform padre, Sprite sprite)
    {
        Image img = NuevoUI(nombre, padre).AddComponent<Image>();
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
