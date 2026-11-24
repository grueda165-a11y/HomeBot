using System.Collections.Generic;
using UnityEngine;

// Central de las cámaras de vigilancia: hay una cámara por zona de la casa.
// Solo está prendida la de la zona donde está el robot; al pasar a otra zona,
// se apaga la anterior y se prende la nueva (con estática y el cartel "CAM").
// La crea CamaraPTZ sola al dar Play; no hay que ponerla en la escena.
public class CentralCamaras : MonoBehaviour
{
    private Transform jugador;
    private List<ZonaCamara> zonas;
    private List<CamaraPTZ> camaras;
    private EfectoCambioCamara efecto;
    private ZonaCamara zonaActual;
    private float ultimoCambio = -10f;

    [Tooltip("Tiempo mínimo entre cambios de cámara (evita idas y vueltas en el borde entre zonas).")]
    public float tiempoMinimoEntreCambios = 0.5f;

    public void Configurar(Transform jugador, List<ZonaCamara> zonas, List<CamaraPTZ> camaras, EfectoCambioCamara efecto)
    {
        this.jugador = jugador;
        this.zonas = new List<ZonaCamara>(zonas);
        this.camaras = camaras;
        this.efecto = efecto;

        zonaActual = CamaraPTZ.ZonaQueContiene(this.zonas, null, jugador.position) ?? this.zonas[0];
        Activar(zonaActual);
    }

    void LateUpdate()
    {
        if (jugador == null || zonas == null) return;

        // ¿Se teletransportó (reaparecer)? Entonces cambia de cámara sin esperar
        bool salto = Vector3.Distance(jugador.position, ultimaPosicion) > 5f;
        ultimaPosicion = jugador.position;

        ZonaCamara nueva = CamaraPTZ.ZonaQueContiene(zonas, zonaActual, jugador.position);
        if (nueva == null || nueva == zonaActual) return;
        if (!salto && Time.time - ultimoCambio < tiempoMinimoEntreCambios) return;

        zonaActual = nueva;
        ultimoCambio = Time.time;
        Activar(nueva);
    }

    private Vector3 ultimaPosicion;

    void Activar(ZonaCamara zona)
    {
        CamaraPTZ elegida = null;
        foreach (CamaraPTZ c in camaras)
        {
            if (c == null) continue;
            if (c.zonaPropia == zona) elegida = c;
            else c.Apagar();
        }
        if (elegida != null) elegida.Prender();
        if (efecto != null) efecto.Mostrar(zona.numero, zona.etiqueta);
    }
}
