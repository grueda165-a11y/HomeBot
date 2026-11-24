using UnityEngine;

public class ObjetoSumarVida : MonoBehaviour
{
    [Tooltip("Si está activo, la batería llena la barra de tiempo por completo.")]
    public bool recargaCompleta = true;

    [Tooltip("Segundos a sumar cuando 'recargaCompleta' está desactivado.")]
    public float segundosASumar = 30f;

    // Aviso para otros scripts (por ejemplo la pantalla de victoria) cuando se recoge una batería
    public static event System.Action<ObjetoSumarVida> Recogida;

    // true mientras está recogida (vuelve a false si el robot muere y las baterías reaparecen)
    public bool Usado => usado;

    private Collider _collider;
    private Renderer[] _renderers;
    private bool usado = false;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>();
    }

    void OnEnable()
    {
        VidaJugador.JugadorMurio += Reaparecer;
    }

    void OnDisable()
    {
        VidaJugador.JugadorMurio -= Reaparecer;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (usado) return;

        if (other.CompareTag("Player"))
        {
            VidaJugador vida = other.GetComponent<VidaJugador>();
            if (vida != null)
            {
                if (recargaCompleta)
                    vida.RecargarTiempo();
                else
                    vida.SumarVida(segundosASumar);
                Desaparecer();
            }
        }
    }

    void Desaparecer()
    {
        usado = true;
        _collider.enabled = false;
        foreach (Renderer r in _renderers) r.enabled = false;
        Recogida?.Invoke(this);
    }

    void Reaparecer()
    {
        usado = false;
        _collider.enabled = true;
        foreach (Renderer r in _renderers) r.enabled = true;
    }
}