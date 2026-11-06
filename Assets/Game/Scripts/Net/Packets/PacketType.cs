namespace Game.Scripts.Net.Packets
{
    public static class PacketType
    {
        public const byte Voice = 0x01;
        public const byte Position = 0x02;
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
        public const byte Disconnect = 0x0D;
        public const byte PlayerLeft = 0x0E;
        public const byte PropPush = 0x0F;
        public const byte SetName = 0x10;
        public const byte PlayerName = 0x11;
        public const byte IdAssigned = 0xFE;
        public const byte Hello = 0xFF;
    }
}
