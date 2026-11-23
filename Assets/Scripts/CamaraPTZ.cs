using UnityEngine;
using UnityEngine.SceneManagement;

// Cámara PTZ (Pan - Tilt - Zoom), como una cámara de VIGILANCIA:
//  - Se monta ARRIBA, en la zona central del cuarto donde está el robot, siempre por debajo
//    del techo y nunca detrás de una pared (no se sale de la casa).
//  - Desde ahí gira suavemente de izquierda a derecha (pan) y de arriba a abajo (tilt)
//    para seguir al robot, y hace zoom poco a poco cuando se aleja.
//  - Al pasar a otro cuarto o al pasillo, se desliza despacio al nuevo sitio.
//  - Las paredes que queden entre la cámara y el robot se vuelven transparentes (DesvanecerParedes).
//  - Opcional: puntos puestos a mano (PuntosCamaraPTZ) en lugar del sitio automático.
[RequireComponent(typeof(Camera))]
public class CamaraPTZ : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform jugador;
    [Tooltip("A qué altura del robot apunta la cámara.")]
    public Vector3 alturaMirada = new Vector3(0f, 1.0f, 0f);
    [Tooltip("Cuánto se adelanta la mirada hacia donde corre el robot (segundos). Poco = más tranquila.")]
    public float anticipacionMirada = 0.1f;

    [Header("Montaje tipo vigilancia (automático)")]
    [Tooltip("Altura de la cámara sobre los pies del robot (como una cámara colgada arriba).")]
    public float alturaVigilancia = 7f;
    [Tooltip("Distancia mínima al techo.")]
    public float margenTecho = 0.6f;
    [Tooltip("Distancia horizontal mínima al robot, para que lo mire en diagonal desde arriba y no desde encima.")]
    public float distanciaMinimaVigilancia = 4f;
    [Tooltip("En cuartos muy grandes, la cámara no se aleja más que esto del robot (horizontal).")]
    public float distanciaMaximaVigilancia = 10f;
    [Tooltip("Cuánto se tiene que mover el sitio calculado para que la cámara se cambie de lugar.")]
    public float margenRecolocar = 3f;
    [Tooltip("Hasta dónde se buscan paredes alrededor del robot (la casa es enorme para el robot).")]
    public float alcanceMedicion = 120f;
    [Tooltip("Distancia que la cámara deja entre ella y cualquier pared o mueble con collider.")]
    public float separacionParedes = 0.5f;

    [Header("Puntos puestos a mano (opcional)")]
    [Tooltip("Actívalo para usar los puntos de PuntosCamaraPTZ en vez del sitio automático.")]
    public bool usarPuntosManuales = false;
    public Transform contenedorPuntos;

    [Header("Suavidad (más alto = más lento y suave)")]
    [Tooltip("Segundos que tarda en deslizarse a un sitio nuevo.")]
    public float tiempoDeslizamiento = 1.6f;
    [Tooltip("Segundos que tarda en girar hacia el robot.")]
    public float tiempoGiro = 0.45f;
    [Tooltip("Velocidad máxima de giro (grados por segundo).")]
    public float giroMaximo = 120f;
    [Tooltip("Segundos que tarda el zoom en ajustarse.")]
    public float tiempoZoom = 0.8f;

    [Header("Zoom")]
    public bool usarZoom = true;
    [Tooltip("Alto (en metros) de lo que se ve alrededor del robot.")]
    public float encuadreVigilancia = 9f;
    public float fovMinimoVigilancia = 35f;
    public float fovMaximoVigilancia = 70f;

    [Header("Controles")]
    [Tooltip("Referencia para el movimiento del robot. Solo gira cuando se sueltan las teclas, " +
             "así los controles no cambian de golpe mientras corre.")]
    public Transform referenciaControles;

    private Camera cam;
    private Vector3 destino;
    private Vector3 velocidadPosicion;
    private Vector3 posicionAnteriorJugador;
    private Vector3 velocidadJugador;
    private Vector3 miradaSuave;
    private float proximaMedicion;
    private bool tieneDestino;
    private float yaw, pitch, velYaw, velPitch, velFov;

    // ---------- Activación automática al dar Play (en escenas de juego) ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ActivarAlIniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Activar(SceneManager.GetActiveScene());
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Activar(escena);

    static void Activar(Scene escena)
    {
        if (!escena.IsValid() || !escena.isLoaded) return;

        VidaJugador jugador = null;
        Camera cam = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            if (jugador == null) jugador = raiz.GetComponentInChildren<VidaJugador>(true);
            foreach (Camera c in raiz.GetComponentsInChildren<Camera>(true))
                if (cam == null || c.CompareTag("MainCamera")) cam = c;
        }
        if (jugador == null || cam == null) return;                 // no es una escena de juego
        if (cam.GetComponent<CamaraPTZ>() != null) return;          // ya la tiene

        CameraFollow viejo = cam.GetComponent<CameraFollow>();       // la cámara vieja: fuera
        if (viejo != null) Object.Destroy(viejo);

        CamaraPTZ ptz = cam.gameObject.AddComponent<CamaraPTZ>();
        ptz.jugador = jugador.transform;

        foreach (Movimiento_Player mov in Object.FindObjectsByType<Movimiento_Player>(FindObjectsSortMode.None))
            mov.cameraTransform = ptz.referenciaControles;
    }

    // ---------- Ciclo ----------

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.1f);

        if (referenciaControles == null)
            referenciaControles = new GameObject("ReferenciaControles_PTZ").transform;

        if (GetComponent<DesvanecerParedes>() == null)
            gameObject.AddComponent<DesvanecerParedes>();
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }
        if (jugador == null) return;

        // La cámara ya no cuelga de otro objeto que la mueva
        transform.SetParent(null, true);

        posicionAnteriorJugador = jugador.position;
        miradaSuave = jugador.position + alturaMirada;
        CalcularDestino(true);
        Cortar();
        AlinearControles();

        DesvanecerParedes d = GetComponent<DesvanecerParedes>();
        if (d != null) d.jugador = jugador;
    }

    void LateUpdate()
    {
        if (jugador == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Velocidad del robot, muy suavizada (para anticipar sin tirones)
        Vector3 delta = jugador.position - posicionAnteriorJugador;
        bool teletransporte = delta.sqrMagnitude > 25f;
        velocidadJugador = teletransporte ? Vector3.zero : Vector3.Lerp(velocidadJugador, delta / dt, 1f - Mathf.Exp(-3f * dt));
        posicionAnteriorJugador = jugador.position;

        // ---- ¿Dónde va la cámara? ----
        if (teletransporte || Time.time >= proximaMedicion)
        {
            proximaMedicion = Time.time + 0.5f;
            CalcularDestino(teletransporte);
        }
        if (teletransporte) { Cortar(); AlinearControles(); return; }   // al reaparecer: corte directo

        transform.position = Vector3.SmoothDamp(transform.position, destino, ref velocidadPosicion, tiempoDeslizamiento);

        // ---- Pan / Tilt suave (amortiguado, sin tirones) ----
        miradaSuave = Vector3.Lerp(miradaSuave, PuntoMirada(), 1f - Mathf.Exp(-6f * dt));
        Vector3 direccion = miradaSuave - transform.position;
        if (direccion.sqrMagnitude > 0.0001f)
        {
            Vector3 e = Quaternion.LookRotation(direccion, Vector3.up).eulerAngles;
            yaw = Mathf.SmoothDampAngle(yaw, e.y, ref velYaw, tiempoGiro, giroMaximo, dt);
            pitch = Mathf.SmoothDampAngle(pitch, e.x, ref velPitch, tiempoGiro, giroMaximo, dt);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // ---- Zoom suave ----
        if (usarZoom)
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, FovPara(direccion.magnitude), ref velFov, tiempoZoom, Mathf.Infinity, dt);

        // ---- Referencia de controles ----
        bool soltoTeclas = Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f && Mathf.Abs(Input.GetAxisRaw("Vertical")) < 0.1f;
        if (soltoTeclas) AlinearControles();
    }

    // Pone la cámara directamente en su sitio y mirando al robot (al empezar o al reaparecer)
    void Cortar()
    {
        transform.position = destino;
        velocidadPosicion = Vector3.zero;
        miradaSuave = jugador.position + alturaMirada;
        Vector3 dir = miradaSuave - transform.position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector3 e = Quaternion.LookRotation(dir, Vector3.up).eulerAngles;
            yaw = e.y; pitch = e.x; velYaw = velPitch = 0f;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            if (usarZoom) { cam.fieldOfView = FovPara(dir.magnitude); velFov = 0f; }
        }
    }

    float FovPara(float distancia)
    {
        distancia = Mathf.Max(0.5f, distancia);
        return Mathf.Clamp(2f * Mathf.Atan(encuadreVigilancia * 0.5f / distancia) * Mathf.Rad2Deg,
                           fovMinimoVigilancia, fovMaximoVigilancia);
    }

    // ---------- Posición ----------

    void CalcularDestino(bool forzar)
    {
        Vector3 nuevo = usarPuntosManuales && contenedorPuntos != null && contenedorPuntos.childCount > 0
            ? PuntoManualMasCercano()
            : SitioVigilancia();

        if (forzar || !tieneDestino || Vector3.Distance(nuevo, destino) > margenRecolocar)
        {
            destino = nuevo;
            tieneDestino = true;
        }
    }

    Vector3 SitioVigilancia()
    {
        Vector3 pies = jugador.position;
        Vector3 pecho = pies + Vector3.up * 1.0f;

        // Distancia a las paredes en 8 direcciones (a la altura del pecho)
        float[] d = new float[8];
        Vector3[] dir = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            dir[i] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            d[i] = GeometriaEscena.Distancia(jugador, pecho, dir[i], alcanceMedicion);
        }

        // Hacia el centro del cuarto (punto medio entre paredes opuestas)
        float dx = (d[2] - d[6]) * 0.5f;   // este (+x) menos oeste (-x)
        float dz = (d[0] - d[4]) * 0.5f;   // norte (+z) menos sur (-z)
        Vector3 plano = new Vector3(dx, 0f, dz);

        // Que lo mire en diagonal desde arriba: ni encima del robot ni demasiado lejos
        if (plano.magnitude < distanciaMinimaVigilancia)
        {
            int masLejos = 0;
            for (int i = 1; i < 8; i++) if (d[i] > d[masLejos]) masLejos = i;
            plano = dir[masLejos] * Mathf.Min(distanciaMinimaVigilancia, Mathf.Max(0.5f, d[masLejos] - 0.6f));
        }
        else if (plano.magnitude > distanciaMaximaVigilancia)
        {
            plano = plano.normalized * distanciaMaximaVigilancia;
        }

        // Arriba, como cámara de vigilancia, pero siempre debajo del techo
        Vector3 arriba = pecho + plano;
        float techo = GeometriaEscena.Distancia(jugador, arriba, Vector3.up, alturaVigilancia + 5f);
        float subir = Mathf.Min(alturaVigilancia - 1f, techo - margenTecho);
        Vector3 candidato = arriba + Vector3.up * Mathf.Max(0.5f, subir);

        return SinAtravesarParedes(pecho, candidato);
    }

    // Seguridad: si entre el robot y el sitio elegido hay una pared o techo (con collider),
    // la cámara se queda de este lado, nunca detrás ni fuera de la casa.
    Vector3 SinAtravesarParedes(Vector3 desde, Vector3 hasta)
    {
        Vector3 dir = hasta - desde;
        float largo = dir.magnitude;
        if (largo < 0.01f) return hasta;
        dir /= largo;

        float libre = largo;
        foreach (RaycastHit h in Physics.SphereCastAll(desde, 0.3f, dir, largo, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform.IsChildOf(jugador) || h.distance <= 0f) continue;
            if (h.distance < libre) libre = h.distance;
        }
        if (libre >= largo) return hasta;
        return desde + dir * Mathf.Max(0.8f, libre - separacionParedes);
    }

    Vector3 PuntoManualMasCercano()
    {
        Vector3 mirada = jugador.position + alturaMirada;
        Transform mejor = null;
        float mejorD = float.MaxValue;
        foreach (Transform p in contenedorPuntos)
        {
            if (!p.gameObject.activeInHierarchy) continue;
            float dist = Vector3.Distance(p.position, mirada);
            if (dist < mejorD) { mejorD = dist; mejor = p; }
        }
        return mejor != null ? mejor.position : transform.position;
    }

    Vector3 PuntoMirada()
    {
        Vector3 adelanto = velocidadJugador * anticipacionMirada;
        adelanto.y = 0f;
        return jugador.position + alturaMirada + adelanto;
    }

    void AlinearControles()
    {
        if (referenciaControles == null) return;
        referenciaControles.position = transform.position;
        referenciaControles.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || jugador == null) return;
        Gizmos.color = new Color(0.3f, 0.9f, 1f);
        Gizmos.DrawWireSphere(destino, 0.3f);
        Gizmos.DrawLine(destino, jugador.position + alturaMirada);
    }
}
