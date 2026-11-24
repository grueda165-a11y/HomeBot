using System.Collections.Generic;
using UnityEngine;

// Ayuda a la cámara a "ver" las paredes aunque no tengan collider:
// usa las cajas (bounds) de los objetos visibles de la escena, además de los colliders que haya.
public static class GeometriaEscena
{
    static readonly List<Renderer> renderers = new List<Renderer>();
    static int escenaCacheada = -1;
    static Transform jugadorCacheado;

    public static List<Renderer> Renderers(Transform jugador)
    {
        int escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (escena != escenaCacheada || jugador != jugadorCacheado || renderers.Count == 0)
        {
            renderers.Clear();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (jugador != null && r.transform.IsChildOf(jugador)) continue;   // el robot no cuenta
                if (r.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
                Vector3 t = r.bounds.size;
                if (t.x > 80f || t.z > 80f) continue;                              // terrenos o cosas gigantes
                renderers.Add(r);
            }
            escenaCacheada = escena;
            jugadorCacheado = jugador;
        }
        return renderers;
    }

    // ---------- Techo ----------

    static readonly List<Renderer> techos = new List<Renderer>();
    static int escenaTechos = -1;

    // Piezas que son techo de verdad: se llaman roof / ceiling / techo y son grandes
    // (así no cuentan los techitos de juguete, como el de la casa Lego).
    static List<Renderer> Techos()
    {
        int escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (escena == escenaTechos) return techos;
        techos.Clear();
        foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            string n = r.name.ToLowerInvariant();
            if (!(n.Contains("roof") || n.Contains("ceiling") || n.Contains("techo"))) continue;
            if (n.Contains("lego") || n.Contains("hitbox")) continue;
            Vector3 t = r.bounds.size;
            if (t.x < 8f && t.z < 8f) continue;
            techos.Add(r);
        }
        escenaTechos = escena;
        return techos;
    }

    // Altura del techo justo encima de un punto (la cara de abajo del techo), o "porDefecto" si no hay.
    public static float TechoSobre(Vector3 p, float porDefecto)
    {
        float y = porDefecto;
        foreach (Renderer r in Techos())
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            Bounds b = r.bounds;
            if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z) continue;
            if (b.min.y > p.y && b.min.y < y) y = b.min.y;
        }

        // También lo que tenga collider justo arriba
        if (Physics.Raycast(p, Vector3.up, out RaycastHit h, Mathf.Max(0.01f, y - p.y), ~0, QueryTriggerInteraction.Ignore))
            y = Mathf.Min(y, h.point.y);
        return y;
    }

    // Distancia hasta lo primero que hay en esa dirección (paredes visibles o colliders).
    // Ignora objetos cuya caja contiene el punto de origen (por ejemplo un piso enorme).
    public static float Distancia(Transform jugador, Vector3 origen, Vector3 direccion, float maximo)
    {
        float mejor = maximo;
        Ray rayo = new Ray(origen, direccion);

        foreach (Renderer r in Renderers(jugador))
        {
            if (r == null || !r.enabled) continue;
            Bounds b = r.bounds;
            if (b.Contains(origen)) continue;
            if (b.IntersectRay(rayo, out float d) && d < mejor) mejor = d;
        }

        foreach (RaycastHit h in Physics.RaycastAll(origen, direccion, maximo, ~0, QueryTriggerInteraction.Ignore))
        {
            if (jugador != null && h.transform.IsChildOf(jugador)) continue;
            if (h.distance < mejor) mejor = h.distance;
        }
        return mejor;
    }
}
