using UnityEngine;

// Cámara PTZ (Pan - Tilt - Zoom), como una cámara de seguridad:
//  - Se queda montada en un punto fijo (no persigue al jugador por detrás).
//  - Gira de izquierda a derecha (pan) y de arriba a abajo (tilt) para seguir al jugador.
//  - Hace zoom (campo de visión) para que el jugador se vea de un tamaño parecido aunque se aleje.
//  - Puede tener varios puntos de montaje repartidos por la casa: usa el más cercano
//    que vea al jugador, y se desliza suavemente al cambiar de punto.
[RequireComponent(typeof(Camera))]
public class CamaraPTZ : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform jugador;
    [Tooltip("A qué altura del jugador apunta la cámara (pecho/cabeza).")]
    public Vector3 alturaMirada = new Vector3(0f, 1.2f, 0f);
    [Tooltip("Cuánto se adelanta la mirada hacia donde corre el jugador (segundos).")]
    public float anticipacion = 0.25f;

    [Header("Puntos de montaje")]
    [Tooltip("Objeto cuyos hijos son los puntos. Duplica un hijo (Ctrl+D) y muévelo para agregar otro.")]
    public Transform contenedorPuntos;
    [Tooltip("Lugares donde puede estar la cámara (si no usas el contenedor). Si está vacío, se queda donde empieza.")]
    public Transform[] puntos;
    [Tooltip("Metros que tiene que estar más cerca otro punto para cambiarse a él (evita saltos).")]
    public float margenCambio = 1.5f;
    [Tooltip("Preferir puntos que vean al jugador sin paredes en medio.")]
    public bool comprobarVision = true;
    public LayerMask capasQueTapan = ~0;
    [Tooltip("Segundos que tarda en deslizarse de un punto a otro (0 = corte directo).")]
    public float tiempoCambio = 0.6f;

    [Header("Puntos automáticos (si no pusiste puntos)")]
    [Tooltip("Sin puntos puestos a mano, la cámara se monta sola a un lado del jugador (nunca detrás) " +
             "y solo se cambia de sitio cuando el jugador se aleja mucho o una pared lo tapa.")]
    public bool puntosAutomaticos = true;
    public float distanciaAuto = 7f;
    public float alturaAuto = 4.5f;
    [Tooltip("Si el jugador se aleja más que esto del punto, la cámara se vuelve a montar más cerca.")]
    public float distanciaMaximaAuto = 14f;
    [Tooltip("Segundos que el jugador puede estar tapado por una pared antes de cambiar de punto.")]
    public float tiempoTapadoAuto = 0.4f;

    [Header("Pan / Tilt")]
    [Tooltip("Qué tan rápido gira para seguir al jugador.")]
    public float suavidadGiro = 6f;
    [Tooltip("Ángulo en el que no gira, para que no tiemble con movimientos pequeños.")]
    public float zonaMuerta = 2f;
    [Range(-89f, 89f)] public float tiltMinimo = -80f;
    [Range(-89f, 89f)] public float tiltMaximo = 80f;

    [Header("Zoom")]
    public bool usarZoom = true;
    [Tooltip("Alto (en metros) de lo que se ve alrededor del jugador.")]
    public float encuadre = 7f;
    public float fovMinimo = 30f;
    public float fovMaximo = 65f;
    public float suavidadZoom = 3f;

    [Header("Controles")]
    [Tooltip("Referencia para el movimiento del jugador. Solo gira cuando el jugador suelta las teclas, " +
             "así los controles no cambian de golpe mientras corre.")]
    public Transform referenciaControles;

    public Transform PuntoActual { get; private set; }

    // ---------- Activación automática al dar Play ----------
    // Si la cámara principal todavía usa CameraFollow (la que seguía por detrás), se cambia sola a PTZ.
    // Así no hace falta configurar nada en la escena para probarla.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ActivarAlIniciar()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AlCargarEscena;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += AlCargarEscena;
        Activar();
    }

    static void AlCargarEscena(UnityEngine.SceneManagement.Scene escena, UnityEngine.SceneManagement.LoadSceneMode modo)
    {
        Activar();
    }

    static void Activar()
    {
        Camera cam = Camera.main;
        if (cam == null || cam.GetComponent<CamaraPTZ>() != null) return;

        CameraFollow viejo = cam.GetComponent<CameraFollow>();
        if (viejo == null) return;   // la escena usa otra cámara a propósito: no se toca
        viejo.enabled = false;

        CamaraPTZ ptz = cam.gameObject.AddComponent<CamaraPTZ>();
        if (viejo.target != null) ptz.jugador = viejo.target;

        foreach (Movimiento_Player mov in FindObjectsByType<Movimiento_Player>(FindObjectsSortMode.None))
            mov.cameraTransform = ptz.referenciaControles;
    }

    private Camera cam;
    private Vector3 velocidadPosicion;
    private Vector3 posicionAnteriorJugador;
    private Vector3 velocidadJugador;
    private float proximaRevision;
    private Transform puntoInicial;
    private Transform montajeAuto;
    private float tiempoTapado;

    bool UsaAuto => montajeAuto != null;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (contenedorPuntos != null && contenedorPuntos.childCount > 0)
        {
            puntos = new Transform[contenedorPuntos.childCount];
            for (int i = 0; i < puntos.Length; i++)
                puntos[i] = contenedorPuntos.GetChild(i);
        }

        // Sin puntos: se monta sola (automático) o se queda donde empieza.
        bool sinPuntos = puntos == null || puntos.Length == 0;
        if (sinPuntos && puntosAutomaticos)
        {
            montajeAuto = new GameObject("PuntoCamara_Auto").transform;
            montajeAuto.position = transform.position;
        }
        else if (sinPuntos)
        {
            puntoInicial = new GameObject("PuntoCamara_Inicial").transform;
            puntoInicial.position = transform.position;
            puntos = new[] { puntoInicial };
        }

        if (referenciaControles == null)
            referenciaControles = new GameObject("ReferenciaControles_PTZ").transform;
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (jugador != null)
        {
            posicionAnteriorJugador = jugador.position;
            PuntoActual = ElegirPunto(true);
            if (PuntoActual != null) transform.position = PuntoActual.position;
            transform.rotation = Quaternion.LookRotation(PuntoMirada() - transform.position);
        }

        AlinearControles();
    }

    void LateUpdate()
    {
        if (jugador == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Velocidad del jugador (para anticipar la mirada). Si se teletransporta, se ignora el salto.
        Vector3 delta = jugador.position - posicionAnteriorJugador;
        bool teletransporte = delta.sqrMagnitude > 25f;
        velocidadJugador = teletransporte ? Vector3.zero : Vector3.Lerp(velocidadJugador, delta / dt, 1f - Mathf.Exp(-8f * dt));
        posicionAnteriorJugador = jugador.position;

        // ---- Elegir punto de montaje ----
        if (Time.time >= proximaRevision || teletransporte)
        {
            proximaRevision = Time.time + 0.2f;
            Transform nuevo = ElegirPunto(teletransporte);
            if (nuevo != null)
            {
                bool cambio = nuevo != PuntoActual;
                PuntoActual = nuevo;
                // Al reaparecer (teletransporte) se corta directo, no se desliza por todo el mapa.
                if (teletransporte || (cambio && tiempoCambio <= 0f))
                {
                    transform.position = PuntoActual.position;
                    velocidadPosicion = Vector3.zero;
                }
            }
        }

        // ---- Moverse al punto (solo cambia al pasar de un punto a otro) ----
        if (PuntoActual != null)
        {
            transform.position = tiempoCambio > 0f
                ? Vector3.SmoothDamp(transform.position, PuntoActual.position, ref velocidadPosicion, tiempoCambio)
                : PuntoActual.position;
        }

        // ---- Pan / Tilt ----
        Vector3 direccion = PuntoMirada() - transform.position;
        if (direccion.sqrMagnitude > 0.0001f)
        {
            Quaternion objetivo = LimitarTilt(Quaternion.LookRotation(direccion, Vector3.up));
            float angulo = Quaternion.Angle(transform.rotation, objetivo);
            if (angulo > zonaMuerta || teletransporte)
            {
                float t = teletransporte ? 1f : 1f - Mathf.Exp(-suavidadGiro * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, t);
            }
        }

        // ---- Zoom ----
        if (usarZoom)
        {
            float distancia = Mathf.Max(0.5f, direccion.magnitude);
            float fov = 2f * Mathf.Atan(encuadre * 0.5f / distancia) * Mathf.Rad2Deg;
            fov = Mathf.Clamp(fov, fovMinimo, fovMaximo);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-suavidadZoom * dt));
        }

        // ---- Referencia de controles ----
        bool soltoTeclas = Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f && Mathf.Abs(Input.GetAxisRaw("Vertical")) < 0.1f;
        if (soltoTeclas || teletransporte)
            AlinearControles();
    }

    Vector3 PuntoMirada()
    {
        Vector3 adelanto = velocidadJugador * anticipacion;
        adelanto.y = 0f;
        return jugador.position + alturaMirada + adelanto;
    }

    Quaternion LimitarTilt(Quaternion rot)
    {
        Vector3 e = rot.eulerAngles;
        float tilt = e.x > 180f ? e.x - 360f : e.x;   // positivo = mirando hacia abajo
        tilt = Mathf.Clamp(tilt, -tiltMaximo, -tiltMinimo);
        return Quaternion.Euler(tilt, e.y, 0f);
    }

    void AlinearControles()
    {
        if (referenciaControles == null) return;
        referenciaControles.position = transform.position;
        referenciaControles.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    Transform ElegirPunto(bool forzar)
    {
        if (UsaAuto) return ActualizarMontajeAuto(forzar);
        if (puntos == null || puntos.Length == 0) return PuntoActual;

        Vector3 mirada = jugador.position + alturaMirada;
        Transform mejor = null;
        float mejorPuntaje = float.MaxValue;
        float puntajeActual = float.MaxValue;

        foreach (Transform p in puntos)
        {
            if (p == null || !p.gameObject.activeInHierarchy) continue;

            float puntaje = Vector3.Distance(p.position, mirada);
            if (comprobarVision && Tapado(p.position, mirada))
                puntaje += 1000f;   // un punto que no ve al jugador solo se usa si no hay otro

            if (p == PuntoActual) puntajeActual = puntaje;
            if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejor = p; }
        }

        if (!forzar && PuntoActual != null && mejor != PuntoActual && puntajeActual - mejorPuntaje < margenCambio)
            return PuntoActual;

        return mejor;
    }

    // ---------- Montaje automático ----------

    Transform ActualizarMontajeAuto(bool forzar)
    {
        Vector3 mirada = jugador.position + alturaMirada;
        bool primeraVez = PuntoActual == null;

        if (!forzar && !primeraVez)
        {
            bool lejos = Vector3.Distance(montajeAuto.position, mirada) > distanciaMaximaAuto;
            bool tapado = comprobarVision && Tapado(montajeAuto.position, mirada);
            tiempoTapado = tapado ? tiempoTapado + 0.2f : 0f;   // se revisa cada 0.2 s

            if (!lejos && tiempoTapado < tiempoTapadoAuto)
                return montajeAuto;
        }

        tiempoTapado = 0f;
        montajeAuto.position = BuscarSitioAuto(mirada);
        return montajeAuto;
    }

    Vector3 BuscarSitioAuto(Vector3 mirada)
    {
        // Hacia dónde va el jugador; si está quieto, se usa hacia dónde mira la cámara ahora.
        Vector3 rumbo = velocidadJugador;
        rumbo.y = 0f;
        if (rumbo.sqrMagnitude < 0.25f)
        {
            rumbo = mirada - transform.position;
            rumbo.y = 0f;
        }
        if (rumbo.sqrMagnitude < 0.0001f) rumbo = Vector3.forward;
        rumbo.Normalize();

        // Se prueban sitios por delante y a los lados del rumbo. Nunca justo detrás (180°).
        float[] angulos = { 70f, -70f, 45f, -45f, 100f, -100f, 25f, -25f, 130f, -130f };
        foreach (float a in angulos)
        {
            for (float escala = 1f; escala >= 0.5f; escala -= 0.25f)
            {
                Vector3 pos = jugador.position
                            + Quaternion.Euler(0f, a, 0f) * rumbo * (distanciaAuto * escala)
                            + Vector3.up * (alturaAuto * Mathf.Lerp(0.7f, 1f, escala));

                if (Physics.CheckSphere(pos, 0.4f, capasQueTapan, QueryTriggerInteraction.Ignore)) continue;
                if (comprobarVision && Tapado(pos, mirada)) continue;
                return pos;
            }
        }

        // Sin sitio libre: arriba del jugador, un poco hacia un lado.
        return jugador.position + Quaternion.Euler(0f, 70f, 0f) * rumbo * 2f + Vector3.up * (alturaAuto * 0.8f);
    }

    bool Tapado(Vector3 desde, Vector3 hasta)
    {
        Vector3 dir = hasta - desde;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;

        RaycastHit[] golpes = Physics.RaycastAll(desde, dir / dist, dist - 0.3f, capasQueTapan, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in golpes)
        {
            if (h.transform == jugador || h.transform.IsChildOf(jugador)) continue;
            return true;
        }
        return false;
    }

    void OnDestroy()
    {
        if (puntoInicial != null) Destroy(puntoInicial.gameObject);
        if (montajeAuto != null) Destroy(montajeAuto.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 1f);
        if (contenedorPuntos != null)
            foreach (Transform p in contenedorPuntos) DibujarPunto(p);
        if (puntos != null)
            foreach (Transform p in puntos) DibujarPunto(p);
    }

    void DibujarPunto(Transform p)
    {
        if (p == null) return;
        Gizmos.DrawWireSphere(p.position, 0.4f);
        if (jugador != null) Gizmos.DrawLine(p.position, jugador.position + alturaMirada);
    }
}
