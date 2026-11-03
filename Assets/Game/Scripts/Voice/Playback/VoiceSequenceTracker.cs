namespace Game.Scripts.Voice.Playback
{
    /// <summary>Номера кадров голоса одного отправителя: находит потерянные кадры и отбрасывает дубликаты и опоздавшие.</summary>
    public sealed class VoiceSequenceTracker
    {
        /// <summary>Больше кадров подряд не маскируется: на долгой паузе синтез Opus звучит хуже тишины.</summary>
        public const int MaxConcealedFrames = 3;

        /// <summary>Кадр, отстающий от последнего не больше чем на столько, считается опоздавшим.
        /// Больший откат — перезапуск отправителя: счёт начинается заново.</summary>
        public const int LateWindow = 16;

        public const int Drop = -1;

        private const int SequenceRange = ushort.MaxValue + 1;

        private bool _hasLast;
        private ushort _last;

        /// <returns>Сколько потерянных кадров перед этим замаскировать (0..MaxConcealedFrames), или Drop — дубликат или опоздал.</returns>
        public int Accept(ushort sequence)
        {
            if (!_hasLast)
            {
                _hasLast = true;
                _last = sequence;
                return 0;
            }

            var delta = (ushort)(sequence - _last);
            if (delta == 0 || delta >= SequenceRange - LateWindow) return Drop;

            _last = sequence;
            var lost = delta - 1;
            return lost <= MaxConcealedFrames ? lost : 0;
        }
    }
}
