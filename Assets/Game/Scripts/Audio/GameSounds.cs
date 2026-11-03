using System.Collections.Generic;
using UnityEngine;

// Номера уходят в сеть — новые значения добавлять только в конец
public enum Sfx : byte { Punch, Slap, Whoosh, Impact, Miss }

// Звуки драки и предметов.
// Свои файлы кладутся в Assets/Resources/Sounds/ — имя должно начинаться с punch / slap / whoosh / impact / miss
// (например punch_1.wav, punch_2.wav — тогда выбирается случайный). Пока файлов нет, играет сгенерированный звук
// (кроме miss — удара по воздуху: без файла он просто молчит).
public static class GameSounds
{
    const int Rate = 44100;

    static readonly Dictionary<Sfx, AudioClip[]> clips = new Dictionary<Sfx, AudioClip[]>();
    static bool loaded;

    public static void Play(Sfx sfx, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var variants = Get(sfx);
        if (variants.Length == 0) return;
        var clip = variants[Random.Range(0, variants.Length)];

        var go = new GameObject($"Sfx_{sfx}");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = pitch * Random.Range(0.92f, 1.08f);
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1.5f;
        src.maxDistance = 30f;
        src.Play();
        Object.Destroy(go, clip.length / Mathf.Max(0.1f, src.pitch) + 0.1f);
    }

    // Без перезагрузки домена (Enter Play Mode Options) статика переживает выход из Play Mode,
    // а созданные кодом клипы при этом уничтожаются — сбрасываем кэш при каждом запуске
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache()
    {
        clips.Clear();
        loaded = false;
    }

    static AudioClip[] Get(Sfx sfx)
    {
        if (!loaded) Load();
        foreach (var c in clips[sfx])
            if (c == null) { Load(); break; } // клип выгрузили — загрузить заново
        return clips[sfx];
    }

    static void Load()
    {
        loaded = true;
        clips.Clear();
        var all = Resources.LoadAll<AudioClip>("Sounds");
        var report = new System.Text.StringBuilder($"[GameSounds] В Resources/Sounds найдено клипов: {all.Length}");
        foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
        {
            string prefix = sfx.ToString().ToLowerInvariant();
            var list = new List<AudioClip>();
            foreach (var c in all)
                if (c.name.ToLowerInvariant().StartsWith(prefix)) list.Add(c);
            report.Append($"\n  {prefix}: ");
            if (list.Count == 0 && sfx == Sfx.Miss)
                report.Append("нет (без звука)");
            else if (list.Count == 0)
            {
                list.Add(Generate(sfx));
                report.Append("заглушка");
            }
            else
                foreach (var c in list) report.Append($"{c.name} ({c.length:F2} с, {c.loadState}) ");
            clips[sfx] = list.ToArray();
        }
        Debug.Log(report);
    }

    // ---- Заглушки, синтезированные кодом ----

    static AudioClip Generate(Sfx sfx)
    {
        var rnd = new System.Random((int)sfx + 17);
        float Noise() => (float)rnd.NextDouble() * 2f - 1f;

        float length = sfx switch { Sfx.Punch => 0.28f, Sfx.Slap => 0.2f, Sfx.Whoosh => 0.32f, _ => 0.35f };
        int n = (int)(length * Rate);
        var data = new float[n];
        float lp = 0, prevNoise = 0, phase = 0;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float s;
            switch (sfx)
            {
                case Sfx.Punch:
                {
                    // Глухой удар: падающий по частоте бас + короткий шумовой щелчок через ФНЧ
                    float freq = Mathf.Lerp(110f, 45f, t / length);
                    phase += 2f * Mathf.PI * freq / Rate;
                    lp += (Noise() - lp) * 0.25f;
                    s = Mathf.Sin(phase) * Mathf.Exp(-18f * t) + lp * 1.8f * Mathf.Exp(-55f * t);
                    break;
                }
                case Sfx.Slap:
                {
                    // Лещ: резкий высокочастотный хлопок (шум через ФВЧ) с быстрым затуханием
                    float nz = Noise();
                    float hp = nz - prevNoise;
                    prevNoise = nz;
                    s = (hp * 0.8f + nz * 0.4f) * Mathf.Exp(-38f * t)
                        + Mathf.Sin(2f * Mathf.PI * 1900f * t) * 0.15f * Mathf.Exp(-60f * t);
                    break;
                }
                case Sfx.Whoosh:
                {
                    // Взмах: шум с плавающим ФНЧ и колоколообразной огибающей
                    float k = Mathf.Lerp(0.03f, 0.25f, Mathf.Sin(Mathf.PI * t / length));
                    lp += (Noise() - lp) * k;
                    s = lp * 2.2f * Mathf.Sin(Mathf.PI * t / length);
                    break;
                }
                default:
                {
                    // Стук предмета: несколько затухающих мод + щелчок
                    s = Mathf.Sin(2f * Mathf.PI * 170f * t) * Mathf.Exp(-22f * t)
                        + Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.6f * Mathf.Exp(-35f * t)
                        + Mathf.Sin(2f * Mathf.PI * 780f * t) * 0.35f * Mathf.Exp(-50f * t)
                        + Noise() * 0.5f * Mathf.Exp(-90f * t);
                    break;
                }
            }
            // Короткая атака, чтобы не было щелчка в начале
            data[i] = Mathf.Clamp(s * Mathf.Clamp01(t * 2000f) * 0.7f, -1f, 1f);
        }

        var clip = AudioClip.Create($"gen_{sfx}", n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
