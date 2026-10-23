using UnityEngine;

public class Moving_Object : MonoBehaviour
{
    [Header("Configuración de la plataforma")]
    [Tooltip("Altura mínima de la plataforma en unidades del mundo")]
    [SerializeField] private float minHeight = 0f;

    [Tooltip("Altura máxima de la plataforma en unidades del mundo")]
    [SerializeField] private float maxHeight = 0f;

    [Tooltip("Velocidad de movimiento en unidades / s")]
    [SerializeField] private float speed = 2f;

    private bool movingUp = true;
    private Vector3 initialPosition;
    private Vector3 lastPosition;
    private Movimiento_Player player;

    void Start()
    {
        initialPosition = transform.position;
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        Vector3 movement = movingUp ? Vector3.up : Vector3.down;
        movement *= speed * Time.deltaTime;

        transform.Translate(movement);

        float maxY = initialPosition.y + maxHeight;
        float minY = initialPosition.y + minHeight;

        if (transform.position.y > maxY)
        {
            transform.position = new Vector3(transform.position.x, maxY, transform.position.z);
            movingUp = false;
        }
        else if (transform.position.y < minY)
        {
            transform.position = new Vector3(transform.position.x, minY, transform.position.z);
            movingUp = true;
        }

        Vector3 platformVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        if (player != null)
            player.SetExternalVelocity(platformVelocity);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 basePos = Application.isPlaying ? initialPosition : transform.position;

        Gizmos.color = Color.red;
        Vector3 minPoint = new Vector3(basePos.x, basePos.y + minHeight, basePos.z);
        Vector3 maxPoint = new Vector3(basePos.x, basePos.y + maxHeight, basePos.z);

        Gizmos.DrawLine(minPoint, maxPoint);
        Gizmos.DrawWireSphere(minPoint, 0.2f);
        Gizmos.DrawWireSphere(maxPoint, 0.2f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Movimiento_Player p))
            player = p;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Movimiento_Player p) && p == player)
        {
            player.SetExternalVelocity(Vector3.zero);
            player = null;
        }
    }
}