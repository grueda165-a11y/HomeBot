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
    public float alturaCamara = 4.5f;
    [Tooltip("Distancia que la cámara deja siempre por debajo del techo.")]
    public float distanciaAlTecho = 1.5f;
    [Tooltip("Distancia horizontal mínima al robot, para que lo mire en diagonal desde arriba y no desde encima.")]
    public float distanciaMinimaVigilancia = 4f;
    [Tooltip("En cuartos muy grandes, la cámara no se aleja más que esto del robot (horizontal).")]
    public float distanciaMaximaVigilancia = 10f;
    [Tooltip("Cuánto se tiene que mover el sitio calculado para que la cámara se cambie de lugar.")]
    public float margenRecolocar = 3f;
    [Tooltip("Hasta dónde se buscan paredes alrededor del robot (la casa es enorme para el robot).")]
    public float alcanceMedicion = 120f;
    [Tooltip("En las zonas: distancia mínima entre la cámara y las paredes del cuarto (para no salirse).")]
    public float margenParedes = 2f;
    [Tooltip("Distancia que la cámara deja entre ella y cualquier pared o mueble con collider.")]
    public float separacionParedes = 0.5f;

    [Header("Zonas: una cámara de vigilancia por zona")]
    [Tooltip("Cada zona tiene su propia cámara fija. Al pasar a otra zona se apaga la anterior y se prende la nueva.")]
    public bool usarZonas = true;
    [Tooltip("Una cámara de verdad por zona (se ven en la Hierarchy al dar Play). Apagado: una sola cámara que salta de zona.")]
    public bool unaCamaraPorZona = true;
    [Tooltip("Nombres de los objetos del modelo de la casa que son cada zona.")]
    public string[] zonas = { "room_sister", "hallway", "room_brothers" };
    [Tooltip("Nombre que se muestra al cambiar de cámara (mismo orden que las zonas).")]
    public string[] nombresZonas = { "CUARTO DE LA HERMANA", "PASILLO", "CUARTO DEL DUEÑO" };
    [Tooltip("La cámara se queda en el CENTRO del cuarto (a la altura del robot + 'Altura En Centro') y gira para seguirlo.")]
    public bool enCentroDelCuarto = true;
    [Tooltip("Altura sobre los pies del robot cuando la cámara está en el centro del cuarto.")]
    public float alturaEnCentro = 2.5f;
    [Tooltip("Fondo negro en vez de cielo: si por algún hueco se ve hacia afuera de la casa, se ve oscuro.")]
    public bool fondoNegro = true;
    [Tooltip("Apagado: en cada zona la cámara se acomoda siguiendo al robot (la de siempre), sin salir de la zona.\n" +
             "Encendido: cámara quieta en el centro de cada zona.")]
    public bool camaraFijaEnZona = false;
    [Tooltip("Solo con cámara fija: a qué altura de la zona va (0 = piso, 1 = techo).")]
    [Range(0.2f, 0.95f)] public float alturaEnZona = 0.7f;
    [Tooltip("Zoom mínimo en las zonas (más bajo = puede acercarse más al robot desde lejos).")]
    public float fovMinimoZona = 15f;
    [Tooltip("Estática y cartel 'CAM' al cambiar de cámara.")]
    public bool efectoCambio = true;

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

    private readonly System.Collections.Generic.List<ZonaCamara> listaZonas = new System.Collections.Generic.List<ZonaCamara>();
    private ZonaCamara zonaActual;

    // Si esta cámara pertenece a una zona (modo "una cámara por zona"), aquí está su zona
    [HideInInspector] public ZonaCamara zonaPropia;
    private bool iniciado;
    private float proximaZona;
    private EfectoCambioCamara efecto;

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
        if (fondoNegro)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        if (referenciaControles == null)
            referenciaControles = new GameObject("ReferenciaControles_PTZ").transform;

        if (GetComponent<DesvanecerParedes>() == null)
            gameObject.AddComponent<DesvanecerParedes>();
    }

    void Start()
    {
        Iniciar();
    }

    void Iniciar()
    {
        if (iniciado) return;
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }
        if (jugador == null) return;
        iniciado = true;

        // La cámara ya no cuelga de otro objeto que la mueva
        transform.SetParent(null, true);

        posicionAnteriorJugador = jugador.position;
        miradaSuave = jugador.position + alturaMirada;

        DesvanecerParedes d = GetComponent<DesvanecerParedes>();
        if (d != null) d.jugador = jugador;

        // Cámara de una zona (creada por la central): solo trabaja en su zona
        if (zonaPropia != null)
        {
            zonaActual = zonaPropia;
            CalcularDestino(true);
            Cortar();
            return;
        }

        if (usarZonas) BuscarZonas();
        if (efectoCambio) efecto = EfectoCambioCamara.Crear();

        // Una cámara de verdad por zona: esta queda como la de la primera zona y se crean las demás
        if (usarZonas && unaCamaraPorZona && listaZonas.Count > 1)
        {
            CrearCamarasPorZona();
            return;
        }

        zonaActual = ZonaDelJugador();
        if (zonaActual != null && efecto != null) efecto.Mostrar(zonaActual.numero, zonaActual.etiqueta);

        CalcularDestino(true);
        Cortar();
        AlinearControles();
    }

    // ---------- Una cámara por zona ----------

    void CrearCamarasPorZona()
    {
        var camaras = new System.Collections.Generic.List<CamaraPTZ>();

        zonaPropia = listaZonas[0];
        zonaActual = zonaPropia;
        name = "Camara 1 · " + listaZonas[0].etiqueta;
        camaras.Add(this);

        for (int i = 1; i < listaZonas.Count; i++)
        {
            GameObject copia = Instantiate(gameObject);
            copia.name = "Camara " + listaZonas[i].numero + " · " + listaZonas[i].etiqueta;
            CamaraPTZ c = copia.GetComponent<CamaraPTZ>();
            c.jugador = jugador;
            c.zonaPropia = listaZonas[i];
            c.referenciaControles = referenciaControles;
            camaras.Add(c);
        }

        foreach (CamaraPTZ c in camaras) c.Apagar();

        CentralCamaras central = new GameObject("CentralCamaras").AddComponent<CentralCamaras>();
        central.Configurar(jugador, listaZonas, camaras, efecto);
    }

    // La central prende la cámara de la zona donde está el robot
    public void Prender()
    {
        enabled = true;
        if (cam == null) cam = GetComponent<Camera>();
        cam.enabled = true;
        gameObject.tag = "MainCamera";
        AudioListener oido = GetComponent<AudioListener>();
        if (oido != null) oido.enabled = true;
        DesvanecerParedes d = GetComponent<DesvanecerParedes>();
        if (d != null) d.enabled = true;

        Iniciar();
        if (jugador == null) return;
        posicionAnteriorJugador = jugador.position;
        velocidadJugador = Vector3.zero;
        CalcularDestino(true);
        Cortar();
        AlinearControles();
    }

    // ...y apaga las demás
    public void Apagar()
    {
        if (cam == null) cam = GetComponent<Camera>();
        cam.enabled = false;
        if (gameObject.CompareTag("MainCamera")) gameObject.tag = "Untagged";
        AudioListener oido = GetComponent<AudioListener>();
        if (oido != null) oido.enabled = false;
        DesvanecerParedes d = GetComponent<DesvanecerParedes>();
        if (d != null) d.enabled = false;   // devuelve las paredes a la normalidad
        enabled = false;
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

        // ---- ¿Cambió de zona? Se apaga la cámara anterior y se prende la de la nueva zona ----
        // (solo en modo de una sola cámara; con una cámara por zona lo hace CentralCamaras)
        if (zonaPropia == null && listaZonas.Count > 0 && (teletransporte || Time.time >= proximaZona))
        {
            proximaZona = Time.time + 0.2f;
            ZonaCamara nueva = ZonaDelJugador();
            if (nueva != null && nueva != zonaActual)
            {
                zonaActual = nueva;
                CalcularDestino(true);
                Cortar();
                AlinearControles();
                if (efecto != null) efecto.Mostrar(zonaActual.numero, zonaActual.etiqueta);
                return;
            }
        }

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
        float minimo = zonaActual != null && camaraFijaEnZona ? Mathf.Min(fovMinimoZona, fovMinimoVigilancia) : fovMinimoVigilancia;
        return Mathf.Clamp(2f * Mathf.Atan(encuadreVigilancia * 0.5f / distancia) * Mathf.Rad2Deg,
                           minimo, fovMaximoVigilancia);
    }

    // ---------- Zonas ----------

    void BuscarZonas()
    {
        listaZonas.Clear();
        Scene escena = gameObject.scene;
        for (int i = 0; i < zonas.Length; i++)
        {
            Transform t = BuscarEnEscena(escena, zonas[i]);
            if (t == null) continue;

            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);

            Bounds interior = InteriorDeLaZona(rs, b);

            // Cámara fija arriba, en el centro de la zona y debajo del techo
            float y = Mathf.Min(interior.min.y + interior.size.y * alturaEnZona, interior.max.y);
            listaZonas.Add(new ZonaCamara
            {
                etiqueta = i < nombresZonas.Length ? nombresZonas[i] : zonas[i],
                numero = listaZonas.Count + 1,
                limites = b,
                interior = interior,
                montaje = BajoElTecho(new Vector3(interior.center.x, y, interior.center.z)),
            });
        }
        if (listaZonas.Count == 0 && usarZonas)
            Debug.Log("Cámara PTZ: no encontré las zonas (" + string.Join(", ", zonas) + "); uso la cámara automática.");
    }

    // El espacio de adentro del cuarto, donde la cámara puede estar sin salirse:
    //  - a los lados: el piso (floor) o, si no hay, la pieza de paredes más grande; con margen a las paredes
    //  - arriba/abajo: desde el piso hasta debajo del techo (roof)
    Bounds InteriorDeLaZona(Renderer[] rs, Bounds zona)
    {
        Renderer piso = null, techo = null, paredes = null;
        foreach (Renderer r in rs)
        {
            string n = r.name.ToLowerInvariant();
            Vector3 s = r.bounds.size;
            if (n.Contains("floor") && !n.Contains("window") && (piso == null || s.x * s.z > piso.bounds.size.x * piso.bounds.size.z)) piso = r;
            else if ((n.Contains("roof") || n.Contains("ceiling") || n.Contains("techo")) && !n.Contains("lego") && !n.Contains("hitbox") &&
                     (techo == null || s.x * s.z > techo.bounds.size.x * techo.bounds.size.z)) techo = r;
            else if (n.Contains("wall") && (paredes == null || s.x * s.y * s.z > paredes.bounds.size.x * paredes.bounds.size.y * paredes.bounds.size.z)) paredes = r;
        }

        Bounds lados = piso != null ? piso.bounds : (paredes != null ? paredes.bounds : zona);
        float abajo = piso != null ? piso.bounds.max.y : (paredes != null ? paredes.bounds.min.y : zona.min.y);
        float arriba = techo != null ? techo.bounds.min.y : (paredes != null ? paredes.bounds.max.y : zona.max.y);

        float m = margenParedes;
        Vector3 min = new Vector3(lados.min.x + m, abajo + 0.5f, lados.min.z + m);
        Vector3 max = new Vector3(lados.max.x - m, arriba - distanciaAlTecho, lados.max.z - m);
        // Por si el cuarto es más chico que los márgenes
        if (max.x < min.x) { float c = (min.x + max.x) * 0.5f; min.x = max.x = c; }
        if (max.z < min.z) { float c = (min.z + max.z) * 0.5f; min.z = max.z = c; }
        if (max.y < min.y) { float c = (min.y + max.y) * 0.5f; min.y = max.y = c; }

        Bounds interior = new Bounds();
        interior.SetMinMax(min, max);
        return interior;
    }

    static Transform BuscarEnEscena(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t;
        return null;
    }

    // Mantiene la cámara dentro de los límites de su zona (con un pequeño margen),
    // y vuelve a comprobar que no quede detrás de una pared.
    // En el centro de la habitación, un poco por encima del robot, siempre dentro del cuarto y bajo el techo
    Vector3 CentroDelCuarto(ZonaCamara zona)
    {
        Bounds b = zona.interior;
        float y = Mathf.Clamp(jugador.position.y + alturaEnCentro, b.min.y, b.max.y);
        Vector3 p = BajoElTecho(new Vector3(b.center.x, y, b.center.z));
        return new Vector3(p.x, Mathf.Clamp(p.y, b.min.y, b.max.y), p.z);
    }

    Vector3 DentroDeLaZona(Vector3 sitio, ZonaCamara zona)
    {
        // Siempre dentro del cuarto: entre sus paredes, sobre su piso y bajo su techo
        Bounds b = zona.interior;
        Vector3 dentro = new Vector3(
            Mathf.Clamp(sitio.x, b.min.x, b.max.x),
            Mathf.Clamp(sitio.y, b.min.y, b.max.y),
            Mathf.Clamp(sitio.z, b.min.z, b.max.z));
        if (dentro != sitio) dentro = SinAtravesarParedes(jugador.position + Vector3.up * 1f, dentro);
        dentro = BajoElTecho(dentro);
        // SinAtravesarParedes puede acercarla al robot: se vuelve a asegurar que siga adentro
        return new Vector3(
            Mathf.Clamp(dentro.x, b.min.x, b.max.x),
            Mathf.Clamp(dentro.y, b.min.y, b.max.y),
            Mathf.Clamp(dentro.z, b.min.z, b.max.z));
    }

    // La zona donde está el robot. Si está en el borde entre dos, se queda con la actual.
    ZonaCamara ZonaDelJugador() => ZonaQueContiene(listaZonas, zonaActual, jugador.position);

    public static ZonaCamara ZonaQueContiene(System.Collections.Generic.List<ZonaCamara> zonas, ZonaCamara actual, Vector3 posicion)
    {
        Vector3 p = posicion + Vector3.up * 0.5f;
        if (actual != null && actual.limites.Contains(p)) return actual;

        ZonaCamara mejor = null;
        float menorVolumen = float.MaxValue;
        foreach (ZonaCamara z in zonas)
        {
            if (!z.limites.Contains(p)) continue;
            float v = z.limites.size.x * z.limites.size.y * z.limites.size.z;
            if (v < menorVolumen) { menorVolumen = v; mejor = z; }
        }
        return mejor ?? actual;
    }

    // ---------- Posición ----------

    void CalcularDestino(bool forzar)
    {
        Vector3 nuevo;
        if (usarPuntosManuales && contenedorPuntos != null && contenedorPuntos.childCount > 0)
            nuevo = PuntoManualMasCercano();
        else if (zonaActual != null && enCentroDelCuarto && !camaraFijaEnZona)
            nuevo = CentroDelCuarto(zonaActual);                         // centro de la habitación, a la altura del robot
        else if (zonaActual != null && camaraFijaEnZona)
            nuevo = zonaActual.montaje;                                  // cámara quieta de la zona
        else if (zonaActual != null)
            nuevo = DentroDeLaZona(SitioVigilancia(), zonaActual);      // la de siempre, sin salir de su zona
        else
            nuevo = SitioVigilancia();

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
        float porRayo = GeometriaEscena.Distancia(jugador, arriba, Vector3.up, alturaCamara + 5f);
        float techoY = Mathf.Min(arriba.y + porRayo, GeometriaEscena.TechoSobre(arriba, arriba.y + alturaCamara + 50f));
        float y = Mathf.Min(pies.y + alturaCamara, techoY - distanciaAlTecho);
        Vector3 candidato = new Vector3(arriba.x, y, arriba.z);

        candidato = SinAtravesarParedes(pecho, candidato);
        return BajoElTecho(candidato);
    }

    // Última comprobación: la cámara nunca queda por encima del techo (ni pegada a él)
    Vector3 BajoElTecho(Vector3 p)
    {
        float techoY = GeometriaEscena.TechoSobre(p - Vector3.up * 0.5f, float.MaxValue);
        if (techoY < float.MaxValue && p.y > techoY - distanciaAlTecho)
            p.y = techoY - distanciaAlTecho;
        return p;
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

#if UNITY_EDITOR
    // ---------- Ver las zonas y sus cámaras en la vista Scene (sin darle Play) ----------
    static readonly Color[] coloresZona =
    {
        new Color(1f, 0.45f, 0.75f), new Color(0.35f, 0.85f, 1f), new Color(0.5f, 1f, 0.45f), new Color(1f, 0.85f, 0.3f)
    };
    static System.Collections.Generic.List<Bounds> cacheLimites;
    static System.Collections.Generic.List<int> cacheIndices;
    static double cacheHasta;

    void OnDrawGizmos()
    {
        if (Application.isPlaying || !usarZonas || zonas == null) return;

        // Se recalculan cada pocos segundos (el modelo de la casa tiene muchas piezas)
        if (cacheLimites == null || UnityEditor.EditorApplication.timeSinceStartup > cacheHasta)
        {
            cacheLimites = new System.Collections.Generic.List<Bounds>();
            cacheIndices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < zonas.Length; i++)
            {
                Transform t = BuscarEnEscena(gameObject.scene, zonas[i]);
                if (t == null) continue;
                Renderer[] rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                cacheLimites.Add(b);
                cacheIndices.Add(i);
            }
            cacheHasta = UnityEditor.EditorApplication.timeSinceStartup + 3.0;
        }

        var estilo = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { fontSize = 14 };
        for (int n = 0; n < cacheLimites.Count; n++)
        {
            Bounds b = cacheLimites[n];
            int i = cacheIndices[n];
            Color c = coloresZona[n % coloresZona.Length];

            // Caja de la zona
            Gizmos.color = new Color(c.r, c.g, c.b, 0.6f);
            Gizmos.DrawWireCube(b.center, b.size);

            // Cámara: arriba en la zona, mirando hacia abajo al centro
            Vector3 pos = new Vector3(b.center.x, Mathf.Min(b.min.y + b.size.y * alturaEnZona, b.max.y - distanciaAlTecho - 0.5f), b.center.z);
            Vector3 hacia = new Vector3(b.center.x, b.min.y, b.center.z) + (new Vector3(b.size.x, 0f, 0f) * 0.15f) - pos;
            Gizmos.color = c;
            Gizmos.DrawSphere(pos, Mathf.Max(0.4f, b.size.magnitude * 0.006f));
            Matrix4x4 antes = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, Quaternion.LookRotation(hacia.normalized, Vector3.up), Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, 50f, Mathf.Max(2f, b.size.y * 0.25f), 0.3f, 1.6f);
            Gizmos.matrix = antes;

            estilo.normal.textColor = c;
            string nombre = i < nombresZonas.Length ? nombresZonas[i] : zonas[i];
            UnityEditor.Handles.Label(pos + Vector3.up * Mathf.Max(1f, b.size.y * 0.05f), "CAM " + (n + 1) + " · " + nombre, estilo);
        }
    }
#endif

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || jugador == null) return;
        Gizmos.color = new Color(0.3f, 0.9f, 1f);
        Gizmos.DrawWireSphere(destino, 0.3f);
        Gizmos.DrawLine(destino, jugador.position + alturaMirada);
    }
}

// Una zona de la casa con su cámara de vigilancia
public class ZonaCamara
{
    public string etiqueta;
    public int numero;
    public Bounds limites;   // toda la zona (para saber si el robot está en ella)
    public Bounds interior;  // donde puede estar la cámara sin salirse del cuarto
    public Vector3 montaje;
}
