using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Volumen de música y de efectos del juego. Se guarda entre partidas (PlayerPrefs)
// y se aplica solo a todos los AudioSource de cualquier escena.
//  - Música: AudioSource que se repiten (loop) o que tengan CanalAudio = Musica.
//  - Efectos: todo lo demás (pasos, chispas, pila, botones...).
public static class AjustesAudio
{
    const string ClaveMusica = "HomeBot_VolumenMusica";
    const string ClaveEfectos = "HomeBot_VolumenEfectos";

    public static float Musica
    {
        get => PlayerPrefs.GetFloat(ClaveMusica, 0.8f);
        set { PlayerPrefs.SetFloat(ClaveMusica, Mathf.Clamp01(value)); PlayerPrefs.Save(); Aplicar(); }
    }

    public static float Efectos
    {
        get => PlayerPrefs.GetFloat(ClaveEfectos, 0.8f);
        set { PlayerPrefs.SetFloat(ClaveEfectos, Mathf.Clamp01(value)); PlayerPrefs.Save(); Aplicar(); }
    }

    // Volumen que tenía cada fuente antes de aplicarle el ajuste
    static readonly Dictionary<AudioSource, float> volumenOriginal = new Dictionary<AudioSource, float>();

    public static void Aplicar()
    {
        float musica = Musica, efectos = Efectos;

        foreach (AudioSource s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!volumenOriginal.TryGetValue(s, out float original))
            {
                original = s.volume;
                volumenOriginal[s] = original;
            }

            CanalAudio canal = s.GetComponent<CanalAudio>();
            bool esMusica = canal != null ? canal.canal == CanalAudio.Tipo.Musica : s.loop;
            s.volume = original * (esMusica ? musica : efectos);
        }
    }

    // ---------- Se aplica solo al cargar escenas y cada segundo (para fuentes nuevas) ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        volumenOriginal.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;

        GameObject go = new GameObject("AjustesAudio");
        go.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(go);
        go.AddComponent<Aplicador>();

        Aplicar();
    }

    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        // Quitar fuentes destruidas de la escena anterior
        var muertas = new List<AudioSource>();
        foreach (var par in volumenOriginal) if (par.Key == null) muertas.Add(par.Key);
        foreach (var m in muertas) volumenOriginal.Remove(m);

        Aplicar();
    }

    class Aplicador : MonoBehaviour
    {
        float proximo;
        void Update()
        {
            if (Time.unscaledTime < proximo) return;
            proximo = Time.unscaledTime + 1f;
            Aplicar();
        }
    }
}
