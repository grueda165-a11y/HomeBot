using UnityEngine;

public class Linterna : MonoBehaviour
{
    public Light luzLinterna;               // arrastra aquí la Spot Light
    public KeyCode teclaLinterna = KeyCode.L;
    public bool empiezaEncendida = false;

    void Start()
    {
        if (luzLinterna != null)
            luzLinterna.enabled = empiezaEncendida;
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaLinterna) && luzLinterna != null)
        {
            luzLinterna.enabled = !luzLinterna.enabled;
        }
    }
}