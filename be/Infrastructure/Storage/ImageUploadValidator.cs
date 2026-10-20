using System.Buffers.Binary;
using be.Application.Abstractions.Storage;

namespace be.Infrastructure.Storage;

internal static class ImageUploadValidator
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    internal static async Task<byte[]> ReadBoundedAsync(MediaUpload file, long maximum, CancellationToken cancellationToken)
    {
        await using var source = file.OpenReadStream();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int count;
        while ((count = await source.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (output.Length + count > maximum)
                throw new InvalidOperationException("File size exceeds the configured limit.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    private static void ValidateGif(byte[] data)
    {
        void Invalid() => throw new InvalidOperationException("GIF file is incomplete or corrupted.");
        var offset = 13;
        if ((data[10] & 0x80) != 0) offset += 3 * (1 << ((data[10] & 7) + 1));
        var images = 0;
        void Blocks()
        {
            while (true)
            {
                if (offset >= data.Length) Invalid();
                var size = data[offset++];
                if (size == 0) return;
                offset += size;
                if (offset > data.Length) Invalid();
            }
        }
        while (offset < data.Length)
        {
            var block = data[offset++];
            if (block == 0x3b)
            {
                if (images == 0 || offset != data.Length) Invalid();
                return;
            }
            if (block == 0x21)
            {
                if (offset >= data.Length) Invalid();
                offset++; // Extension label, then bounded subblocks.
                Blocks();
            }
            else if (block == 0x2c)
            {
                if (offset + 9 >= data.Length) Invalid();
                var left = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, 2));
                var top = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + 2, 2));
                var width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + 4, 2));
                var height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + 6, 2));
                if (width == 0 || height == 0 || left + width > BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6, 2)) || top + height > BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(8, 2))) Invalid();
                var packed = data[offset + 8];
                offset += 9;
                if ((packed & 0x80) != 0) offset += 3 * (1 << ((packed & 7) + 1));
                if (offset >= data.Length || data[offset] < 2 || data[offset] > 8) Invalid();
                offset++; // LZW minimum code size; encoded bytes remain opaque.
                Blocks();
                images++;
            }
            else Invalid();
        }
        Invalid();
    }

        internal static (string ContentType, string Extension, int Width, int Height) Validate(byte[] data, string? declaredContentType, string extension, bool allowGif = false)
        {
            if (data.Length >= 24 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            {
                ValidateDeclaredType(declaredContentType, extension, "image/png", ".png");
                ValidatePng(data);
                return ("image/png", ".png", BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4)));
            }

            if (data.Length >= 10 && data[0] == 0xFF && data[1] == 0xD8)
            {
                ValidateDeclaredType(declaredContentType, extension, "image/jpeg", ".jpg", ".jpeg");
                if (data[^2] != 0xFF || data[^1] != 0xD9)
                {
                    throw new InvalidOperationException("JPEG file is incomplete or corrupted.");
                }

                using var jpegStream = new MemoryStream(data, writable: false);
                var dimensions = ReadJpegDimensions(jpegStream);
                return ("image/jpeg", extension is ".jpeg" ? ".jpeg" : ".jpg", dimensions.Width, dimensions.Height);
            }

            if (data.Length >= 30 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            {
                ValidateDeclaredType(declaredContentType, extension, "image/webp", ".webp");
                var dimensions = ReadWebPDimensions(data);
                return ("image/webp", ".webp", dimensions.Width, dimensions.Height);
            }

            if (allowGif && data.Length >= 14 && (data.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || data.AsSpan(0, 6).SequenceEqual("GIF89a"u8)))
            {
                ValidateDeclaredType(declaredContentType, extension, "image/gif", ".gif");
                ValidateGif(data);
                return ("image/gif", ".gif", BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6, 2)), BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(8, 2)));
            }

            throw new InvalidOperationException("Unsupported or invalid image file. Allowed formats are JPEG, PNG, and WebP.");
        }

        private static void ValidateDeclaredType(string? declaredContentType, string extension, string detectedContentType, params string[] extensions)
        {
            if (!AllowedContentTypes.Contains(declaredContentType ?? string.Empty) || !string.Equals(declaredContentType, detectedContentType, StringComparison.OrdinalIgnoreCase) || !extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The file extension and content type must match the uploaded image data.");
            }
        }

        private static void ValidatePng(byte[] data)
        {
            var signature = new byte[]
            {
                0x89,
                0x50,
                0x4E,
                0x47,
                0x0D,
                0x0A,
                0x1A,
                0x0A
            };
            if (data.Length < 33 || !data.AsSpan(0, signature.Length).SequenceEqual(signature))
            {
                throw new InvalidOperationException("PNG file is incomplete or corrupted.");
            }

            var offset = signature.Length;
            var sawHeader = false;
            var sawEnd = false;
            while (offset + 12 <= data.Length)
            {
                var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
                if (length > int.MaxValue || (long)offset + 12 + length > data.Length)
                {
                    throw new InvalidOperationException("PNG file is incomplete or corrupted.");
                }

                var chunkLength = (int)length;
                var typeOffset = offset + 4;
                var chunkType = System.Text.Encoding.ASCII.GetString(data, typeOffset, 4);
                var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(typeOffset + 4 + chunkLength, 4));
                var actualCrc = ComputePngCrc(data.AsSpan(typeOffset, 4 + chunkLength));
                if (expectedCrc != actualCrc)
                {
                    throw new InvalidOperationException("PNG file is incomplete or corrupted.");
                }

                if (!sawHeader)
                {
                    if (chunkType != "IHDR" || chunkLength != 13)
                    {
                        throw new InvalidOperationException("PNG file is incomplete or corrupted.");
                    }

                    sawHeader = true;
                }

                offset += 12 + chunkLength;
                if (chunkType == "IEND")
                {
                    sawEnd = chunkLength == 0;
                    break;
                }
            }

            if (!sawHeader || !sawEnd || offset != data.Length)
            {
                throw new InvalidOperationException("PNG file is incomplete or corrupted.");
            }
        }

        private static uint ComputePngCrc(ReadOnlySpan<byte> data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var value in data)
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
                }
            }

            return ~crc;
        }

        private static (int Width, int Height) ReadWebPDimensions(byte[] data)
        {
            var declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4, 4));
            if ((long)declaredLength + 8 > data.Length)
            {
                throw new InvalidOperationException("WebP file is incomplete or corrupted.");
            }

            var chunkType = System.Text.Encoding.ASCII.GetString(data, 12, 4);
            if (chunkType == "VP8X" && data.Length >= 30)
            {
                var width = 1 + data[24] + (data[25] << 8) + (data[26] << 16);
                var height = 1 + data[27] + (data[28] << 8) + (data[29] << 16);
                return (width, height);
            }

            if (chunkType == "VP8 " && data.Length >= 30 && data[23] == 0x9D && data[24] == 0x01 && data[25] == 0x2A)
            {
                var width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(26, 2)) & 0x3FFF;
                var height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(28, 2)) & 0x3FFF;
                return (width, height);
            }

            if (chunkType == "VP8L" && data.Length >= 25 && data[20] == 0x2F)
            {
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(21, 4));
                var width = (int)(bits & 0x3FFF) + 1;
                var height = (int)((bits >> 14) & 0x3FFF) + 1;
                return (width, height);
            }

            throw new InvalidOperationException("Unable to decode WebP dimensions.");
        }

        private static (int Width, int Height) ReadJpegDimensions(Stream stream)
        {
            stream.Position = 2;
            while (stream.Position < stream.Length)
            {
                if (stream.ReadByte() != 0xFF)
                {
                    break;
                }

                var marker = stream.ReadByte();
                var lengthBytes = new byte[2];
                if (stream.Read(lengthBytes, 0, 2) != 2)
                {
                    break;
                }

                var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);
                if (segmentLength < 2)
                {
                    break;
                }

                if (marker is >= 0xC0 and <= 0xC3)
                {
                    stream.ReadByte();
                    var heightBytes = new byte[2];
                    var widthBytes = new byte[2];
                    stream.Read(heightBytes, 0, 2);
                    stream.Read(widthBytes, 0, 2);
                    return (BinaryPrimitives.ReadUInt16BigEndian(widthBytes), BinaryPrimitives.ReadUInt16BigEndian(heightBytes));
                }

                stream.Position += segmentLength - 2;
            }

            throw new InvalidOperationException("Unable to decode JPEG dimensions.");
        }
}
