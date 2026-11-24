using UnityEngine;

// Puerta de un solo sentido: cuando el robot entra en esta zona, se activan unos bloqueos
// (colliders invisibles) detrás de él y ya no puede volver al cuarto anterior.
[RequireComponent(typeof(Collider))]
public class PuertaUnSentido : MonoBehaviour
{
    [Header("Qué se cierra")]
    [Tooltip("Objetos con collider SÓLIDO (sin Is Trigger). Empiezan apagados y se prenden al cruzar.")]
    public GameObject[] bloqueos;

    [Header("Opciones")]
    [Tooltip("Segundos de espera antes de cerrar (por si el robot aún está pasando por la puerta).")]
    public float retraso = 0.3f;
    [Tooltip("Al morir: si el robot reaparece DELANTE de la puerta se queda cerrada; si reaparece detrás, se abre otra vez.")]
    public bool reabrirAlMorir = true;

    private Transform jugador;
    private bool cerrada = false;

    void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void Awake()
    {
        Poner(false);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
    }

    void OnEnable()
    {
        VidaJugador.JugadorMurio += AlMorir;
    }

    void OnDisable()
    {
        VidaJugador.JugadorMurio -= AlMorir;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || cerrada) return;
        CancelInvoke(nameof(Cerrar));
        Invoke(nameof(Cerrar), retraso);
    }

    void Cerrar()
    {
        Poner(true);
    }

    void AlMorir()
    {
        CancelInvoke(nameof(Cerrar));
        if (!reabrirAlMorir)
        {
            return;     // se queda como estaba
        }

        // Al reaparecer, ¿el robot está delante o detrás de la puerta?
        bool delante = false;
        if (jugador != null)
            delante = Vector3.Dot(jugador.position - transform.position, transform.forward) > 0f;

        Poner(delante);
    }

    void Poner(bool estado)
    {
        cerrada = estado;
        if (bloqueos == null) return;
        foreach (GameObject b in bloqueos)
            if (b != null) b.SetActive(estado);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
        Gizmos.DrawSphere(transform.position + transform.forward * 3f, 0.25f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, "PUERTA 1 SENTIDO  →");
#endif
    }
}