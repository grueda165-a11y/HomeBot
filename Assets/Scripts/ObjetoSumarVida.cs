using UnityEngine;

public class ObjetoSumarVida : MonoBehaviour
{
    [Tooltip("Si está activo, la batería llena la barra de tiempo por completo.")]
    public bool recargaCompleta = true;

    [Tooltip("Segundos a sumar cuando 'recargaCompleta' está desactivado.")]
    public float segundosASumar = 30f;

    private Collider[] _colliders;
    private Renderer _propioRenderer;
    private bool usado = false;

    void Awake()
    {
        _colliders = GetComponents<Collider>();
        _propioRenderer = GetComponent<Renderer>();
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

        // Busca VidaJugador en el objeto que tocó o en sus padres
        VidaJugador vida = other.GetComponentInParent<VidaJugador>();
        if (vida == null) return;

        if (recargaCompleta)
            vida.RecargarTiempo();
        else
            vida.SumarVida(segundosASumar);

        Desaparecer();
    }

    void Desaparecer()
    {
        usado = true;
        foreach (Collider c in _colliders) c.enabled = false;
        if (_propioRenderer != null) _propioRenderer.enabled = false;

        // Apaga todos los hijos (modelo, luces, partículas, etc.)
        foreach (Transform hijo in transform)
            hijo.gameObject.SetActive(false);
    }

    void Reaparecer()
    {
        usado = false;
        foreach (Collider c in _colliders) c.enabled = true;
        if (_propioRenderer != null) _propioRenderer.enabled = true;

        foreach (Transform hijo in transform)
            hijo.gameObject.SetActive(true);
    }
}