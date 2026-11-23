using UnityEngine;

// CÁMARA VIEJA (ya no se usa). La reemplaza CamaraPTZ.
// Este componente quedó en el Main Camera de la escena; se deja vacío solo para que Unity
// no muestre "Missing Script". Al dar Play se quita solo. Puedes quitarlo del Main Camera
// cuando quieras (clic derecho en el componente > Remove Component).
public class CameraFollow : MonoBehaviour
{
    [HideInInspector] public Transform target;
    [HideInInspector] public Vector3 offset;

    void Awake()
    {
        Destroy(this);
    }
}
