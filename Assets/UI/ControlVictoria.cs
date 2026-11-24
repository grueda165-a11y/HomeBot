using System.Collections.Generic;
using UnityEngine;

// Decide cuándo se gana:
//  - Si hay baterías marcadas con BateriaFinal: al recogerlas (todas las marcadas).
//  - Si no: al recoger la última batería que queda dentro del cuarto del hermano (room_brothers).
public class ControlVictoria : MonoBehaviour
{
    public PantallaVictoria pantalla;
    public string cuartoFinal = "room_brothers";

    void OnEnable()  { ObjetoSumarVida.Recogida += AlRecoger; }
    void OnDisable() { ObjetoSumarVida.Recogida -= AlRecoger; }

    void Start()
    {
        List<ObjetoSumarVida> finales = BateriasFinales();
        if (finales.Count == 0)
            Debug.LogWarning("Victoria: no hay baterías marcadas con BateriaFinal ni baterías dentro de '" + cuartoFinal +
                             "'. Pon una batería en el cuarto del hermano (o márcala con BateriaFinal) para poder ganar.");
        else
            Debug.Log("Victoria: se gana al recoger " + (finales.Count == 1 ? "la batería '" + finales[0].name + "'" : finales.Count + " baterías finales") + ".");
    }

    void AlRecoger(ObjetoSumarVida bateria)
    {
        if (pantalla == null || pantalla.Mostrando) return;

        List<ObjetoSumarVida> finales = BateriasFinales();
        if (finales.Count == 0 || !finales.Contains(bateria)) return;

        foreach (ObjetoSumarVida b in finales)
            if (b != null && !b.Usado) return;   // todavía queda alguna

        pantalla.Mostrar();
    }

    List<ObjetoSumarVida> BateriasFinales()
    {
        var lista = new List<ObjetoSumarVida>();
        ObjetoSumarVida[] todas = FindObjectsByType<ObjetoSumarVida>(FindObjectsSortMode.None);

        // 1) Las marcadas a mano
        foreach (ObjetoSumarVida b in todas)
            if (b.GetComponent<BateriaFinal>() != null) lista.Add(b);
        if (lista.Count > 0) return lista;

        // 2) Las que están dentro del cuarto del hermano
        if (!LimitesDelCuarto(out Bounds cuarto)) return lista;
        foreach (ObjetoSumarVida b in todas)
        {
            Vector3 p = b.transform.position;
            if (p.x >= cuarto.min.x && p.x <= cuarto.max.x && p.z >= cuarto.min.z && p.z <= cuarto.max.z &&
                p.y >= cuarto.min.y - 2f && p.y <= cuarto.max.y + 2f)
                lista.Add(b);
        }
        return lista;
    }

    private bool limitesListos;
    private Bounds limites;

    bool LimitesDelCuarto(out Bounds b)
    {
        if (!limitesListos)
        {
            limitesListos = true;
            foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != cuartoFinal) continue;
                Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                limites = rs[0].bounds;
                foreach (Renderer r in rs) limites.Encapsulate(r.bounds);
                break;
            }
        }
        b = limites;
        return limites.size != Vector3.zero;
    }
}
