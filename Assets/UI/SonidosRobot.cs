using UnityEngine;

// Genera por código los sonidos del robot que se apaga, para no depender de archivos de audio.
// Si prefieres usar tus propios audios, asígnalos en PantallaMuerte y estos no se usan.
public static class SonidosRobot
{
    const int Frecuencia = 44100;

    // Chisporroteo de circuito: chasquidos de ruido que se van apagando.
    public static AudioClip Chispazo(float duracion = 0.9f, int semilla = 7)
    {
        System.Random rnd = new System.Random(semilla);
        int n = (int)(duracion * Frecuencia);
        float[] datos = new float[n];

        int pos = 0;
        while (pos < n)
        {
            float t = (float)pos / n;
            int largo = (int)(Frecuencia * (0.004f + rnd.NextDouble() * 0.03f));
            float vol = (float)(0.4 + rnd.NextDouble() * 0.6) * (1f - t * 0.8f);
            float anterior = 0f;

            for (int i = 0; i < largo && pos + i < n; i++)
            {
                float ruido = (float)(rnd.NextDouble() * 2 - 1);
                float agudo = ruido - anterior;            // filtro paso alto simple: suena más "eléctrico"
                anterior = ruido;
                float env = Mathf.Exp(-6f * i / largo);
                datos[pos + i] += agudo * vol * env * 0.6f;
            }

            pos += largo + (int)(Frecuencia * rnd.NextDouble() * 0.06f * (0.3f + t));
        }

        return Crear("Chispazo", datos);
    }

    // Zumbido que baja de tono, como un motor/circuito perdiendo energía.
    public static AudioClip Apagado(float duracion = 0.9f)
    {
        int n = (int)(duracion * Frecuencia);
        float[] datos = new float[n];
        float fase = 0f, fase2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(780f, 45f, Mathf.Pow(t, 0.6f));
            fase += 2f * Mathf.PI * f / Frecuencia;
            fase2 += 2f * Mathf.PI * f * 0.5f / Frecuencia;

            // Onda cuadrada suavizada + subarmónico: suena a juguete electrónico
            float onda = Mathf.Clamp(Mathf.Sin(fase) * 3f, -1f, 1f) * 0.6f + Mathf.Sin(fase2) * 0.4f;
            float env = (1f - t) * Mathf.Min(1f, i / (Frecuencia * 0.01f));
            datos[i] = onda * env * 0.35f;
        }

        return Crear("Apagado", datos);
    }

    // Piezas metálicas de juguete cayendo y rebotando.
    public static AudioClip PiezasCayendo(float duracion = 1.4f, int semilla = 11)
    {
        System.Random rnd = new System.Random(semilla);
        int n = (int)(duracion * Frecuencia);
        float[] datos = new float[n];

        // Relaciones de frecuencia no armónicas = timbre metálico
        float[] parciales = { 1f, 2.76f, 5.40f, 8.93f };

        int piezas = 3;
        for (int p = 0; p < piezas; p++)
        {
            float t0 = 0.02f + p * 0.12f + (float)rnd.NextDouble() * 0.05f;
            float baseF = 900f + (float)rnd.NextDouble() * 1400f;
            float hueco = 0.22f + (float)rnd.NextDouble() * 0.08f;
            float vol = 0.5f - p * 0.1f;

            // Cada pieza rebota varias veces, cada vez más rápido y más bajo
            for (int r = 0; r < 5; r++)
            {
                Golpe(datos, t0, baseF * (1f + r * 0.03f), vol, parciales);
                t0 += hueco;
                hueco *= 0.6f;
                vol *= 0.55f;
                if (t0 >= duracion) break;
            }
        }

        return Crear("PiezasCayendo", datos);
    }

    // Melodía corta de victoria: arpegio que sube, con un toque de juguete.
    public static AudioClip Victoria()
    {
        float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };   // Do Mi Sol Do Mi
        float paso = 0.12f, cola = 0.9f;
        int n = (int)((paso * notas.Length + cola) * Frecuencia);
        float[] datos = new float[n];
        for (int k = 0; k < notas.Length; k++)
        {
            int inicio = (int)(k * paso * Frecuencia);
            float dur = k == notas.Length - 1 ? cola : paso * 1.8f;
            int largo = (int)(dur * Frecuencia);
            for (int i = 0; i < largo && inicio + i < n; i++)
            {
                float t = (float)i / Frecuencia;
                float onda = Mathf.Sin(2f * Mathf.PI * notas[k] * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * notas[k] * 2f * t) * 0.2f;
                float env = Mathf.Min(1f, i / (Frecuencia * 0.005f)) * Mathf.Exp(-t * (k == notas.Length - 1 ? 3f : 9f));
                datos[inicio + i] += onda * env * 0.3f;
            }
        }
        return Crear("Victoria", datos);
    }

    // Clic de botón de juguete: "bip" corto que sube de tono.
    public static AudioClip Clic()
    {
        int n = (int)(0.09f * Frecuencia);
        float[] datos = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            fase += 2f * Mathf.PI * Mathf.Lerp(700f, 1300f, t) / Frecuencia;
            float onda = Mathf.Clamp(Mathf.Sin(fase) * 2.5f, -1f, 1f);
            datos[i] = onda * Mathf.Exp(-t * 5f) * 0.3f;
        }
        return Crear("Clic", datos);
    }

    // Tic suave al pasar el mouse por un botón.
    public static AudioClip Tic()
    {
        int n = (int)(0.035f * Frecuencia);
        float[] datos = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Frecuencia;
            datos[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 120f) * 0.25f;
        }
        return Crear("Tic", datos);
    }

    static void Golpe(float[] datos, float inicio, float f, float vol, float[] parciales)
    {
        int i0 = (int)(inicio * Frecuencia);
        int largo = (int)(0.18f * Frecuencia);

        for (int i = 0; i < largo && i0 + i < datos.Length; i++)
        {
            float t = (float)i / Frecuencia;
            float s = 0f;
            for (int k = 0; k < parciales.Length; k++)
                s += Mathf.Sin(2f * Mathf.PI * f * parciales[k] * t) * Mathf.Exp(-t * (18f + k * 14f)) / (k + 1);
            datos[i0 + i] += s * vol * 0.5f;
        }
    }

    static AudioClip Crear(string nombre, float[] datos)
    {
        for (int i = 0; i < datos.Length; i++)
            datos[i] = Mathf.Clamp(datos[i], -1f, 1f);

        AudioClip clip = AudioClip.Create(nombre, datos.Length, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
