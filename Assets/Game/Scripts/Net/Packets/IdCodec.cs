using System.Text;

namespace Game.Scripts.Net.Packets
{
    public static class IdCodec
    {
        public static int Write(byte[] buffer, int offset, string id)
        {
            id ??= string.Empty;
            var length = Encoding.UTF8.GetBytes(id, 0, id.Length, buffer, offset + 1);
            buffer[offset] = (byte)length;
            return offset + 1 + length;
        }

        public static string Read(byte[] buffer, int offset, out int next)
        {
            int length = buffer[offset];
            next = offset + 1 + length;
            return Encoding.UTF8.GetString(buffer, offset + 1, length);
        }
    }
}
