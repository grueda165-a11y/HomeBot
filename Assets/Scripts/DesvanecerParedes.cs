using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Cuando una pared (o un mueble) queda entre la cámara y el robot, se vuelve transparente
// poco a poco para que el robot se siga viendo. Al dejar de tapar, vuelve a la normalidad.
// Va en la cámara (CamaraPTZ lo agrega sola).
[DefaultExecutionOrder(100)]   // después de que la cámara se movió
public class DesvanecerParedes : MonoBehaviour
{
    public Transform jugador;

    [Header("Desvanecimiento")]
    [Tooltip("Qué tan visible queda la pared mientras tapa al robot (0 = invisible, 1 = normal).")]
    [Range(0f, 1f)] public float transparencia = 0.2f;
    [Tooltip("Qué tan rápido se desvanece y vuelve (por segundo).")]
    public float velocidad = 5f;

    [Header("Qué se revisa")]
    [Tooltip("Alturas del robot que se revisan (pies, pecho, cabeza).")]
    public float[] alturas = { 0.3f, 1.0f, 1.6f };
    [Tooltip("Margen para no desvanecer lo que está pegado al robot o a la cámara.")]
    public float margen = 0.25f;

    class Estado
    {
        public Material[] originales;
        public Material[] copias;
        public float alfa = 1f;
        public bool tapa;
    }

    private readonly Dictionary<Renderer, Estado> estados = new Dictionary<Renderer, Estado>();
    private readonly List<Renderer> paraQuitar = new List<Renderer>();
    private readonly HashSet<Renderer> tapandoAhora = new HashSet<Renderer>();

    void LateUpdate()
    {
        if (jugador == null) return;

        // ---- ¿Qué tapa al robot ahora? ----
        tapandoAhora.Clear();
        Vector3 cam = transform.position;
        List<Renderer> todos = GeometriaEscena.Renderers(jugador);

        foreach (float h in alturas)
        {
            Vector3 punto = jugador.position + Vector3.up * h;
            Vector3 hacia = punto - cam;
            float largo = hacia.magnitude;
            if (largo < 0.01f) continue;
            Ray rayo = new Ray(cam, hacia / largo);

            // Paredes sin collider: por su caja
            foreach (Renderer r in todos)
            {
                if (r == null || !r.enabled) continue;
                Bounds b = r.bounds;
                if (b.Contains(cam) || b.Contains(punto)) continue;   // piso bajo el robot, cuarto entero, etc.
                if (b.IntersectRay(rayo, out float d) && d > margen && d < largo - margen)
                    tapandoAhora.Add(r);
            }

            // Lo que sí tiene collider
            foreach (RaycastHit hit in Physics.RaycastAll(rayo, largo - margen, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(jugador) || hit.distance < margen) continue;
                foreach (Renderer r in hit.collider.GetComponentsInChildren<Renderer>())
                    if (!r.transform.IsChildOf(jugador)) tapandoAhora.Add(r);
            }
        }

        // ---- Empezar a desvanecer lo nuevo ----
        foreach (Renderer r in tapandoAhora)
        {
            if (!estados.TryGetValue(r, out Estado e))
            {
                e = Preparar(r);
                if (e == null) continue;
                estados[r] = e;
            }
            e.tapa = true;
        }

        // ---- Animar y devolver lo que ya no tapa ----
        float dt = Time.unscaledDeltaTime;
        paraQuitar.Clear();
        foreach (var par in estados)
        {
            Renderer r = par.Key; Estado e = par.Value;
            if (r == null) { paraQuitar.Add(r); continue; }

            bool tapa = tapandoAhora.Contains(r);
            float objetivo = tapa ? transparencia : 1f;
            e.alfa = Mathf.MoveTowards(e.alfa, objetivo, velocidad * dt);
            foreach (Material m in e.copias) PonerAlfa(m, e.alfa);

            if (!tapa && e.alfa >= 1f)
            {
                r.sharedMaterials = e.originales;
                foreach (Material m in e.copias) Destroy(m);
                paraQuitar.Add(r);
            }
        }
        foreach (Renderer r in paraQuitar) estados.Remove(r);
    }

    Estado Preparar(Renderer r)
    {
        Material[] originales = r.sharedMaterials;
        if (originales == null || originales.Length == 0) return null;

        Material[] copias = new Material[originales.Length];
        for (int i = 0; i < originales.Length; i++)
        {
            if (originales[i] == null) return null;
            copias[i] = new Material(originales[i]) { name = originales[i].name + " (transparente)" };
            HacerTransparente(copias[i]);
        }
        r.sharedMaterials = copias;
        return new Estado { originales = originales, copias = copias };
    }

    // Pasa un material a modo transparente (URP Lit/Unlit y Standard)
    static void HacerTransparente(Material m)
    {
        if (m.HasProperty("_Surface"))   // URP
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        else if (m.HasProperty("_Mode"))   // Standard (por si acaso)
        {
            m.SetFloat("_Mode", 2f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }

    static void PonerAlfa(Material m, float a)
    {
        string prop = m.HasProperty("_BaseColor") ? "_BaseColor" : (m.HasProperty("_Color") ? "_Color" : null);
        if (prop == null) return;
        Color c = m.GetColor(prop);
        c.a = a;
        m.SetColor(prop, c);
    }

    void OnDisable()
    {
        // Devolver todo a la normalidad
        foreach (var par in estados)
        {
            if (par.Key != null) par.Key.sharedMaterials = par.Value.originales;
            foreach (Material m in par.Value.copias) if (m != null) Destroy(m);
        }
        estados.Clear();
    }
}
