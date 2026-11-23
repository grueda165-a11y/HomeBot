using UnityEditor;

// Las herramientas del menú HomeBot arman escenas: no pueden usarse mientras el juego está en Play,
// porque lo que se crea en Play se pierde al detenerlo (y los botones quedan sin conectar).
public static class HerramientasHomeBot
{
    public static bool FueraDePlay(string titulo)
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return true;

        EditorUtility.DisplayDialog(titulo,
            "Primero detén el juego (botón ■ de Play arriba) y vuelve a usar esta opción.\n\n" +
            "Lo que se arma mientras el juego está en Play se pierde al detenerlo.", "OK");
        return false;
    }

    // Grisea las opciones del menú HomeBot mientras el juego está en Play
    [MenuItem("HomeBot/Crear escena del menú de inicio", true)]
    [MenuItem("HomeBot/Armar HUD", true)]
    [MenuItem("HomeBot/Armar menú de pausa", true)]
    [MenuItem("HomeBot/Cámara PTZ/Configurar", true)]
    [MenuItem("HomeBot/Cámara PTZ/Agregar punto donde estoy mirando", true)]
    static bool SoloFueraDePlay() => !EditorApplication.isPlaying;
}
