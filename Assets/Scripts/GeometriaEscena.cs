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
