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
//  - Barra de tiempo un poco más arriba, con efecto eléctrico al recargar
//  - Pantalla de muerte (bordes rojos + Reintentar / Salir)
//  - Quita los textos viejos de tiempo y vidas
// Se puede volver a ejecutar sin duplicar nada, y todo se puede deshacer con Ctrl+Z.
public static class ArmarHUD
{
    const string CarpetaSprites = "Assets/UI/Sprites/";
    const string NombreVidas = "HUD_Vidas";
    const string NombreIntentos = "HUD_Intentos";
    const string NombreMuerte = "PantallaMuerte";
    const string NombreRayos = "Rayos";

    const float Margen = 40f;
    const float TamCarita = 110f;
    const float EspacioCaritas = 16f;
    const float AlturaBarra = 160f;   // distancia del centro de la barra al borde inferior

    static readonly Color ColorRayo = new Color(0.45f, 0.9f, 1f, 1f);
    static readonly Color ColorVerde = new Color(0.24f, 0.86f, 0.35f);
    static readonly Color ColorRojo = new Color(0.90f, 0.22f, 0.23f);

    [MenuItem("HomeBot/Armar HUD")]
    public static void Armar()
    {
        Sprite marco = CargarSprite("Carita_Marco.png");
        Sprite pantalla = CargarSprite("Carita_Pantalla.png");
        Sprite cara = CargarSprite("Carita_Cara.png");
        Sprite vineta = CargarSprite("Vineta.png");
        Sprite boton = CargarSprite("Boton.png", new Vector4(24, 24, 24, 24));
        if (marco == null || pantalla == null || cara == null || vineta == null || boton == null)
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
        Transform raiz = canvas.transform;

        QuitarTextosViejos(raiz);
        ConectarBarra(raiz, jugador);
        CrearVidas(raiz, jugador, marco, pantalla, cara);
        CrearIntentos(raiz);
        CrearPantallaMuerte(raiz, jugador, vineta, boton);
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
        if (importer == null) return null;

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

    // ---------- Textos viejos ----------

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

    // ---------- Barra de tiempo ----------

    static void ConectarBarra(Transform canvas, VidaJugador jugador)
    {
        Image relleno = null;
        foreach (Image img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img.name == "fill") { relleno = img; break; }
        }

        if (relleno == null)
        {
            Debug.LogWarning("Armar HUD: no encontré la imagen 'fill' de la barra.");
            return;
        }

        Undo.RecordObject(relleno, "Barra de tiempo");
        relleno.type = Image.Type.Filled;
        relleno.fillMethod = Image.FillMethod.Horizontal;
        relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
        relleno.fillAmount = 1f;

        RectTransform barraRT = relleno.transform.parent as RectTransform;
        if (barraRT == null || barraRT == canvas) barraRT = (RectTransform)relleno.transform;
        GameObject barraGO = barraRT.gameObject;

        // La barra estaba estirada con Scale; se pasa a tamaño real para que los rayos no se deformen.
        QuitarEscala(barraRT);

        // Un poco más arriba del borde inferior de la pantalla.
        Undo.RecordObject(barraRT, "Barra de tiempo");
        Vector2 tam = barraRT.sizeDelta;
        barraRT.anchorMin = barraRT.anchorMax = new Vector2(0.5f, 0f);
        barraRT.pivot = new Vector2(0.5f, 0.5f);
        barraRT.sizeDelta = tam;
        barraRT.anchoredPosition = new Vector2(0f, AlturaBarra);

        EfectoElectrico efecto = AsegurarEfecto(barraGO, 22f, 3f);
        Undo.RecordObject(efecto, "Barra de tiempo");
        efecto.duracion = 0.7f;
        efecto.temblor = 4f;

        BarraTiempo barra = barraGO.GetComponent<BarraTiempo>();
        if (barra == null) barra = Undo.AddComponent<BarraTiempo>(barraGO);

        Undo.RecordObject(barra, "Barra de tiempo");
        barra.relleno = relleno;
        barra.jugador = jugador;
        barra.efecto = efecto;
    }

    static void QuitarEscala(RectTransform padre)
    {
        Vector3 s = padre.localScale;
        if (Mathf.Approximately(s.x, 1f) && Mathf.Approximately(s.y, 1f)) return;
        if (Mathf.Approximately(s.x, 0f) || Mathf.Approximately(s.y, 0f)) return;

        Undo.RecordObject(padre, "Barra de tiempo");
        padre.sizeDelta = new Vector2(padre.sizeDelta.x * s.x, padre.sizeDelta.y * s.y);
        padre.localScale = Vector3.one;

        foreach (Transform t in padre)
        {
            RectTransform hijo = t as RectTransform;
            if (hijo == null || hijo.name == NombreRayos) continue;

            Undo.RecordObject(hijo, "Barra de tiempo");
            Vector3 hs = hijo.localScale;
            hijo.sizeDelta = new Vector2(hijo.sizeDelta.x * hs.x * s.x, hijo.sizeDelta.y * hs.y * s.y);
            hijo.anchoredPosition = new Vector2(hijo.anchoredPosition.x * s.x, hijo.anchoredPosition.y * s.y);
            hijo.localScale = Vector3.one;
        }
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

    // ---------- Intentos ----------

    static void CrearIntentos(Transform canvas)
    {
        Borrar(canvas, NombreIntentos);

        GameObject go = NuevoUI(NombreIntentos, canvas);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);  // debajo de las caritas
        rt.anchoredPosition = new Vector2(-Margen, -Margen - TamCarita - 12f);
        rt.sizeDelta = new Vector2(3 * TamCarita + 2 * EspacioCaritas, 60f);

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        texto.text = "Intento: 1";
        texto.fontSize = 44;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.TopRight;
        texto.color = Color.white;
        texto.raycastTarget = false;

        ContadorIntentos contador = go.AddComponent<ContadorIntentos>();
        contador.texto = texto;
    }

    // ---------- Pantalla de muerte ----------

    static void CrearPantallaMuerte(Transform canvas, VidaJugador jugador, Sprite vineta, Sprite boton)
    {
        Borrar(canvas, NombreMuerte);

        GameObject panel = NuevoUI(NombreMuerte, canvas);
        Estirar((RectTransform)panel.transform, 0f);
        panel.transform.SetAsLastSibling();   // por encima de todo
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Fondo oscuro suave
        GameObject oscuro = NuevoUI("Oscuro", panel.transform);
        Estirar((RectTransform)oscuro.transform, 0f);
        Image imgOscuro = oscuro.AddComponent<Image>();
        imgOscuro.color = new Color(0f, 0f, 0f, 0.45f);

        // Bordes rojos
        GameObject vin = NuevoUI("BordesRojos", panel.transform);
        Estirar((RectTransform)vin.transform, 0f);
        Image imgVineta = vin.AddComponent<Image>();
        imgVineta.sprite = vineta;
        imgVineta.color = new Color(0.85f, 0.03f, 0.03f, 0.95f);
        imgVineta.raycastTarget = false;

        // Título
        GameObject titulo = NuevoUI("Titulo", panel.transform);
        RectTransform rtTitulo = (RectTransform)titulo.transform;
        rtTitulo.anchorMin = rtTitulo.anchorMax = rtTitulo.pivot = new Vector2(0.5f, 0.5f);
        rtTitulo.anchoredPosition = new Vector2(0f, 150f);
        rtTitulo.sizeDelta = new Vector2(1100f, 170f);
        TextMeshProUGUI txtTitulo = titulo.AddComponent<TextMeshProUGUI>();
        txtTitulo.text = "SIN ENERGÍA";
        txtTitulo.fontSize = 120;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.alignment = TextAlignmentOptions.Center;
        txtTitulo.color = new Color(1f, 0.88f, 0.88f);
        txtTitulo.raycastTarget = false;
        EfectoElectrico efTitulo = AsegurarEfecto(titulo, 10f, 4f);
        efTitulo.continuo = true;
        efTitulo.temblor = 5f;

        // Botones uno al lado del otro
        GameObject fila = NuevoUI("Botones", panel.transform);
        RectTransform rtFila = (RectTransform)fila.transform;
        rtFila.anchorMin = rtFila.anchorMax = rtFila.pivot = new Vector2(0.5f, 0.5f);
        rtFila.anchoredPosition = new Vector2(0f, -90f);
        rtFila.sizeDelta = new Vector2(760f, 110f);
        HorizontalLayoutGroup hl = fila.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 40f;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;

        Button reintentar = CrearBoton("Reintentar", fila.transform, boton, ColorVerde, new Color(0.06f, 0.18f, 0.2f));
        Button salir = CrearBoton("Salir", fila.transform, boton, ColorRojo, Color.white);

        PantallaMuerte pm = panel.AddComponent<PantallaMuerte>();
        pm.jugador = jugador;
        pm.botonReintentar = reintentar;
        pm.botonSalir = salir;
        pm.vineta = imgVineta;
        pm.efectosAlAparecer = new[] { efTitulo };
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
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.text = texto.ToUpper();
        tmp.fontSize = 46;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = colorTexto;
        tmp.raycastTarget = false;

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

        // Que el layout de las caritas no lo acomode como si fuera otra carita.
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
