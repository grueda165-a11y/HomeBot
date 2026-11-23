using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Al darle Play en el editor, el juego SIEMPRE empieza en el menú principal (MenuInicio),
// tengas abierta la escena que tengas. Al pulsar JUGAR se abre la escena que estabas editando
// (por ejemplo HomeBotTesting), así puedes probar cualquier escena pasando por el menú.
// Se puede apagar en: HomeBot > Empezar siempre en el menú.
[InitializeOnLoad]
public static class EmpezarEnMenu
{
    const string RutaMenu = "Assets/Scenes/MenuInicio.unity";
    const string ClaveActivo = "HomeBot_EmpezarEnMenu";
    const string Opcion = "HomeBot/Empezar siempre en el menú";

    // La lee MenuInicio para saber qué escena abrir con JUGAR
    public const string ClaveEscenaParaJugar = "HomeBot_EscenaParaJugar";

    static bool Activo => EditorPrefs.GetBool(ClaveActivo, true);

    static EmpezarEnMenu()
    {
        Aplicar();
        EditorApplication.playModeStateChanged -= AlCambiarModo;
        EditorApplication.playModeStateChanged += AlCambiarModo;
    }

    static void Aplicar()
    {
        SceneAsset menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaMenu);
        EditorSceneManager.playModeStartScene = Activo ? menu : null;
    }

    static void AlCambiarModo(PlayModeStateChange estado)
    {
        if (estado != PlayModeStateChange.ExitingEditMode) return;
        Aplicar();

        // Recordar la escena que estabas editando (si no es el propio menú)
        string ruta = EditorSceneManager.GetActiveScene().path;
        bool esJuego = Activo && !string.IsNullOrEmpty(ruta) && ruta != RutaMenu;
        SessionState.SetString(ClaveEscenaParaJugar, esJuego ? ruta : "");
    }

    [MenuItem(Opcion, false, 100)]
    static void Alternar()
    {
        EditorPrefs.SetBool(ClaveActivo, !Activo);
        Aplicar();
        Debug.Log(Activo ? "Al dar Play se empezará en el menú principal." : "Al dar Play se empezará en la escena abierta.");
    }

    [MenuItem(Opcion, true)]
    static bool AlternarValidar()
    {
        Menu.SetChecked(Opcion, Activo);
        return !EditorApplication.isPlaying;
    }
}
