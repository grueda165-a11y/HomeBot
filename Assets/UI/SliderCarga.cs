using UnityEngine;
using UnityEngine.UI;

// Llena la barra verde del slider recortándola (fillAmount) en vez de estirarla,
// así las rayas diagonales no se deforman.
[RequireComponent(typeof(Slider))]
public class SliderCarga : MonoBehaviour
{
    public Image carga;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(_ => Actualizar());
    }

    void OnEnable() { if (slider != null) Actualizar(); }
    void Start() { Actualizar(); }

    void Actualizar()
    {
        if (carga != null) carga.fillAmount = slider.normalizedValue;
    }
}
