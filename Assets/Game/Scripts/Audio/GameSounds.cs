using System;
using Game.Scripts.Audio.Models;
using Game.Scripts.Audio.Synthesis;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer.Unity;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Game.Scripts.Audio
{
    /// <summary>Пул 3D-источников для коротких эффектов. Клипы — из Resources/Sounds, а если их нет — синтезированные.</summary>
    public sealed class GameSounds : IInitializable, IDisposable
    {
        private const string ResourcesFolder = "Sounds";
        private const int PoolSize = 16;
        private const float PitchJitter = 0.08f;
        private const int SeedOffset = 17;

        private static readonly string[] SfxNames = Enum.GetNames(typeof(Sfx));

        private readonly AudioClip[][] _clips = new AudioClip[SfxNames.Length][];
        private GameObject _root;
        private AudioSource[] _pool;
        private int _nextSource;

        /// <summary>3D-звук в точке; к pitch добавляется случайный разброс ±8%. На сервере без графики ничего не делает.</summary>
        public void Play(Sfx sfx, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            var index = (int)sfx;
            if (_pool == null || (uint)index >= (uint)_clips.Length) return;

            var variants = _clips[index];
            if (variants.Length == 0) return;

            var source = _pool[_nextSource];
            _nextSource = (_nextSource + 1) % _pool.Length;

            source.transform.position = position;
            source.clip = variants[Random.Range(0, variants.Length)];
            source.volume = Mathf.Clamp01(volume);
            source.pitch = pitch * Random.Range(1f - PitchJitter, 1f + PitchJitter);
            source.Play();
        }

        public void Initialize()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

            LoadClips();
            CreatePool();
        }

        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root);
            _pool = null;
        }

        private void LoadClips()
        {
            for (var i = 0; i < _clips.Length; i++)
            {
                var sfx = (Sfx)i;
                var clips = Resources.LoadAll<AudioClip>($"{ResourcesFolder}/{SfxNames[i]}");

                if (clips.Length == 0 && SfxSynthesizerFactory.TryCreate(sfx, out var synthesizer))
                    clips = new[] { ProceduralClip.Create($"gen_{sfx}", synthesizer, i + SeedOffset) };

                _clips[i] = clips;
            }
        }

        private void CreatePool()
        {
            _root = new GameObject(nameof(GameSounds));

            _pool = new AudioSource[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                var child = new GameObject($"Source {i}");
                child.transform.SetParent(_root.transform, false);

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
