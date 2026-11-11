using UnityEngine;
using UnityEngine.UI;

// Dibuja rayos eléctricos en zigzag dentro de su rectángulo (UI).
// Cada vez que se llama a Regenerar() los rayos cambian de forma.
[RequireComponent(typeof(CanvasRenderer))]
public class RayoUI : MaskableGraphic
{
    [Header("Rayos")]
    [Range(1, 8)] public int cantidad = 3;
    [Range(3, 16)] public int segmentos = 8;
    public float grosor = 3f;
    [Tooltip("Qué tanto se desvía el zigzag (proporción del largo del rayo).")]
    [Range(0f, 0.5f)] public float desvio = 0.18f;
    [Range(0f, 1f)] public float probabilidadRama = 0.6f;

    [Header("Brillo")]
    public Color colorNucleo = Color.white;
    [Range(1f, 6f)] public float anchoBrillo = 3.5f;
    [Range(0f, 1f)] public float alfaBrillo = 0.35f;

    private int semilla = 1;

    public void Regenerar()
    {
        semilla = Random.Range(1, int.MaxValue);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        System.Random rnd = new System.Random(semilla);

        for (int i = 0; i < cantidad; i++)
        {
            Vector2 a = PuntoEnBorde(r, rnd);
            Vector2 b = PuntoEnBorde(r, rnd);
            if ((b - a).sqrMagnitude < r.width * r.width * 0.05f)
                b = r.center + (r.center - a);

            Vector2[] puntos = Zigzag(a, b, segmentos, desvio, rnd);
            DibujarLinea(vh, puntos);

            if (rnd.NextDouble() < probabilidadRama)
            {
                Vector2 inicio = puntos[rnd.Next(1, puntos.Length - 1)];
                Vector2 dir = (b - a);
                Vector2 perp = new Vector2(-dir.y, dir.x).normalized * (rnd.NextDouble() < 0.5 ? -1f : 1f);
                Vector2 fin = inicio + dir * 0.25f + perp * dir.magnitude * 0.25f;
                DibujarLinea(vh, Zigzag(inicio, fin, Mathf.Max(3, segmentos / 2), desvio, rnd));
            }
        }
    }

    static Vector2 PuntoEnBorde(Rect r, System.Random rnd)
    {
        float t = (float)rnd.NextDouble();
        switch (rnd.Next(4))
        {
            case 0: return new Vector2(Mathf.Lerp(r.xMin, r.xMax, t), r.yMin);
            case 1: return new Vector2(Mathf.Lerp(r.xMin, r.xMax, t), r.yMax);
            case 2: return new Vector2(r.xMin, Mathf.Lerp(r.yMin, r.yMax, t));
            default: return new Vector2(r.xMax, Mathf.Lerp(r.yMin, r.yMax, t));
        }
    }

    static Vector2[] Zigzag(Vector2 a, Vector2 b, int n, float desvio, System.Random rnd)
    {
        Vector2[] p = new Vector2[n + 1];
        Vector2 dir = b - a;
        Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
        float largo = dir.magnitude;

        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            float desplazamiento = (i == 0 || i == n) ? 0f : ((float)rnd.NextDouble() * 2f - 1f) * desvio * largo;
            p[i] = a + dir * t + perp * desplazamiento;
        }
        return p;
    }

    void DibujarLinea(VertexHelper vh, Vector2[] puntos)
    {
        Color brillo = color;
        brillo.a *= alfaBrillo;
        Color nucleo = colorNucleo;
        nucleo.a *= color.a;

        for (int i = 0; i < puntos.Length - 1; i++)
            Segmento(vh, puntos[i], puntos[i + 1], grosor * anchoBrillo, brillo);
        for (int i = 0; i < puntos.Length - 1; i++)
            Segmento(vh, puntos[i], puntos[i + 1], grosor * 1.6f, color);
        for (int i = 0; i < puntos.Length - 1; i++)
            Segmento(vh, puntos[i], puntos[i + 1], grosor * 0.6f, nucleo);
    }

    static void Segmento(VertexHelper vh, Vector2 a, Vector2 b, float ancho, Color32 c)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 n = new Vector2(-dir.y, dir.x) * (ancho * 0.5f);
        // Se alarga un poco cada segmento para que las uniones no queden con huecos.
        Vector2 extra = dir * (ancho * 0.25f);
        a -= extra;
        b += extra;

        int i = vh.currentVertCount;
        vh.AddVert(a + n, c, Vector2.zero);
        vh.AddVert(a - n, c, Vector2.zero);
        vh.AddVert(b - n, c, Vector2.zero);
        vh.AddVert(b + n, c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i + 2, i + 3, i);
    }
}
