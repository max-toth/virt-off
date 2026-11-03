using System.Text;

// Кодирование строкового id (V2) внутри кадров: [idLen:1][id UTF-8...].
public static class IdCodec
{
    // Пишет idLen(1) + UTF-8(id) в buf начиная с offset, возвращает новый offset.
    public static int Write(byte[] buf, int offset, string id)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(id ?? "");
        buf[offset] = (byte)bytes.Length;
        System.Array.Copy(bytes, 0, buf, offset + 1, bytes.Length);
        return offset + 1 + bytes.Length;
    }

    // Читает idLen(1) + id, возвращает строку, next = offset после id.
    public static string Read(byte[] buf, int offset, out int next)
    {
        int len = buf[offset];
        string id = Encoding.UTF8.GetString(buf, offset + 1, len);
        next = offset + 1 + len;
        return id;
    }

    // Устойчивый выбор модели аватара по строковому id (детерминированно в сессии).
    public static int StableIndex(string id, int count)
    {
        unchecked
        {
            int h = 17;
            foreach (char c in id) h = h * 31 + c;
            return (h & 0x7fffffff) % count;
        }
    }
}
