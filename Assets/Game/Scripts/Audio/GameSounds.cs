using System;
using Game.Scripts.Audio.Synthesis;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Game.Scripts.Audio
{
    public static class GameSounds
    {
        private const string ResourcesFolder = "Sounds";
        private const int PoolSize = 16;
        private const float PitchJitter = 0.08f;
        private const int SeedOffset = 17;

        private static readonly string[] SfxNames = Enum.GetNames(typeof(Sfx));
        private static readonly AudioClip[][] Clips = new AudioClip[SfxNames.Length][];

        private static AudioSource[] _pool;
        private static int _nextSource;

        public static void Play(Sfx sfx, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            var index = (int)sfx;
            if (_pool == null || (uint)index >= (uint)Clips.Length) return;

            var variants = Clips[index];
            if (variants.Length == 0) return;

            var source = _pool[_nextSource];
            _nextSource = (_nextSource + 1) % _pool.Length;

            source.transform.position = position;
            source.clip = variants[Random.Range(0, variants.Length)];
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch * Random.Range(1f - PitchJitter, 1f + PitchJitter);
            source.Play();
        }

        // Без перезагрузки домена (Enter Play Mode Options) статика переживает выход из Play Mode,
        // а пул и сгенерированные клипы уничтожаются.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Array.Clear(Clips, 0, Clips.Length);
            _pool = null;
            _nextSource = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

            LoadClips();
            CreatePool();
        }

        private static void LoadClips()
        {
            for (var i = 0; i < Clips.Length; i++)
            {
                var sfx = (Sfx)i;
                var clips = Resources.LoadAll<AudioClip>($"{ResourcesFolder}/{SfxNames[i]}");

                if (clips.Length == 0 && SfxSynthesizerFactory.TryCreate(sfx, out var synthesizer))
                    clips = new[] { ProceduralClip.Create($"gen_{sfx}", synthesizer, i + SeedOffset) };

                Clips[i] = clips;
            }
        }

        private static void CreatePool()
        {
            var root = new GameObject(nameof(GameSounds));
            Object.DontDestroyOnLoad(root);

            _pool = new AudioSource[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                var child = new GameObject($"Source {i}");
                child.transform.SetParent(root.transform, false);

                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1.5f;
                source.maxDistance = 30f;
                _pool[i] = source;
            }
        }
    }
}
