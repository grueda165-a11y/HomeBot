using UnityEngine;
using TMPro;

// Muestra el número de intento. Empieza en 1 y sube cada vez
// que el jugador muere o se reinicia la escena.
public class ContadorIntentos : MonoBehaviour
{
    public TMP_Text texto;
    public string prefijo = "Intento: ";

    private int mostrado = -1;

    void Awake()
    {
        if (texto == null)
            texto = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (texto == null || VidaJugador.Intentos == mostrado) return;

        mostrado = VidaJugador.Intentos;
        texto.text = prefijo + mostrado;
    }
}
