using UnityEngine;

public class Linterna : MonoBehaviour
{
    public Light luzLinterna;
    [Tooltip("Luces extra (las de atrás del robot). Se prenden y apagan junto con la linterna.")]
    public Light[] lucesExtra;
    public KeyCode teclaLinterna = KeyCode.L;
    public bool empiezaEncendida = false;

    void Start()
    {
        PonerEstado(empiezaEncendida);
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaLinterna) && luzLinterna != null)
        {
            PonerEstado(!luzLinterna.enabled);
        }
    }

    void PonerEstado(bool encendida)
    {
        if (luzLinterna != null)
            luzLinterna.enabled = encendida;

        if (lucesExtra == null) return;
        foreach (Light luz in lucesExtra)
        {
            if (luz != null) luz.enabled = encendida;
        }
    }
}