using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Arma solo, al dar Play en una escena de juego, la pantalla de victoria y el control que decide cuándo se gana.
// Imágenes en Assets/UI/Resources/HomeBotUI (Victoria_Fondo y Victoria_BotonMenu).
public static class ConstructorVictoria
{
    const string Carpeta = "HomeBotUI/";
    static readonly Vector2 TamImagen = new Vector2(1024f, 572f);
    static readonly Rect PxBoton = new Rect(378f, 445f, 267f, 76f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        Revisar(SceneManager.GetActiveScene());
    }

    static void AlCargar(Scene escena, LoadSceneMode modo) => Revisar(escena);

    static void Revisar(Scene escena)
    {
        if (!escena.IsValid() || !escena.isLoaded) return;
        VidaJugador jugador = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (raiz.GetComponentInChildren<MenuInicio>(true) != null) return;          // menú de inicio
            if (raiz.GetComponentInChildren<PantallaVictoria>(true) != null) return;    // ya está
            if (jugador == null) jugador = raiz.GetComponentInChildren<VidaJugador>(true);
        }
        if (jugador == null) return;   // no es una escena de juego

        PantallaVictoria pantalla = Construir(escena);
        if (pantalla == null) return;

        GameObject go = new GameObject("ControlVictoria");
        if (go.scene != escena) SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<ControlVictoria>().pantalla = pantalla;
    }

    static PantallaVictoria Construir(Scene escena)
    {
        Sprite fondo = Resources.Load<Sprite>(Carpeta + "Victoria_Fondo");
        Sprite boton = Resources.Load<Sprite>(Carpeta + "Victoria_BotonMenu");
        if (fondo == null || boton == null)
        {
            Debug.LogWarning("Pantalla de victoria: faltan imágenes en Assets/UI/Resources/" + Carpeta);
            return null;
        }

        // Capa propia, encima de todo (HUD, cámaras, pausa)
        GameObject raiz = new GameObject("PantallaVictoria", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        if (raiz.scene != escena) SceneManager.MoveGameObjectToScene(raiz, escena);
        Canvas c = raiz.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 150;
        CanvasScaler cs = raiz.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;

        // Fondo que cubre toda la pantalla (el botón va encima, en el mismo lugar que en la imagen)
        Image imgFondo = Imagen("Fondo", raiz.transform, fondo);
        RectTransform rf = imgFondo.rectTransform;
        rf.anchorMin = Vector2.zero; rf.anchorMax = Vector2.one;
        rf.offsetMin = rf.offsetMax = Vector2.zero;
        AspectRatioFitter arf = imgFondo.gameObject.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        arf.aspectRatio = TamImagen.x / TamImagen.y;

        Image imgBoton = Imagen("BotonMenu", rf, boton);
        RectTransform rb = imgBoton.rectTransform;
        rb.anchorMin = new Vector2(PxBoton.xMin / TamImagen.x, 1f - PxBoton.yMax / TamImagen.y);
        rb.anchorMax = new Vector2(PxBoton.xMax / TamImagen.x, 1f - PxBoton.yMin / TamImagen.y);
        rb.offsetMin = rb.offsetMax = Vector2.zero;
        Button b = imgBoton.gameObject.AddComponent<Button>();
        b.targetGraphic = imgBoton;
        b.transition = Selectable.Transition.None;
        imgBoton.gameObject.AddComponent<BotonMenu>();

        // EventSystem para poder hacer clic
        bool hayEventos = false;
        foreach (GameObject g in escena.GetRootGameObjects())
            if (g.GetComponentInChildren<EventSystem>(true) != null) { hayEventos = true; break; }
        if (!hayEventos)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (es.scene != escena) SceneManager.MoveGameObjectToScene(es, escena);
        }

        PantallaVictoria p = raiz.AddComponent<PantallaVictoria>();
        p.botonMenu = b;
        return p;
    }

    static Image Imagen(string nombre, Transform padre, Sprite sprite)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        return img;
    }
}
