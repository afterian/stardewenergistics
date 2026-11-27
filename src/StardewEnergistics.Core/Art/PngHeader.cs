using System.Buffers.Binary;

namespace StardewEnergistics.Core.Art;

/// <summary>Reads a PNG's IHDR chunk (size and colour type) without decoding pixels.</summary>
public sealed record PngHeader(int Width, int Height, int BitDepth, int ColorType)
{
    public const int ColorTypeIndexed = 3;
    public const int ColorTypeRgba = 6;

    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>Colour types that can hold transparency the way the game needs: RGBA or indexed (with tRNS).</summary>
    public bool SupportsTransparency => ColorType is ColorTypeRgba or ColorTypeIndexed;

    public static PngHeader Read(Stream stream)
    {
        Span<byte> buf = stackalloc byte[33];
        int read = 0;
        while (read < buf.Length)
        {
            int n = stream.Read(buf[read..]);
            if (n == 0)
                throw new InvalidDataException("File is too short to be a PNG.");
            read += n;
        }

        if (!buf[..8].SequenceEqual(Signature))
            throw new InvalidDataException("Not a PNG file (bad signature).");
        if (buf[12] != 'I' || buf[13] != 'H' || buf[14] != 'D' || buf[15] != 'R')
            throw new InvalidDataException("PNG has no IHDR chunk first.");

        int width = BinaryPrimitives.ReadInt32BigEndian(buf[16..20]);
        int height = BinaryPrimitives.ReadInt32BigEndian(buf[20..24]);
        return new PngHeader(width, height, buf[24], buf[25]);
    }

    public static PngHeader Read(string path)
    {
        using var f = File.OpenRead(path);
        return Read(f);
    }
}
