namespace Game.Scripts.Net
{
    public static class PacketType
    {
        public const byte Attack = 0x03;
        public const byte PropState = 0x04;
        public const byte Sound = 0x05;
        public const byte PropManifest = 0x06;
        public const byte PropPickupRequest = 0x07;
        public const byte PropRelease = 0x08;
        public const byte PropManifestAck = 0x09;
        public const byte PropGrant = 0x0A;
        public const byte PropReleaseEvent = 0x0B;
        public const byte PropSnapshot = 0x0C;
    }
}
