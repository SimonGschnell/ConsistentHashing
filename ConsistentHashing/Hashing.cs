using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ConsistentHashing;

public interface IHashing
{
    uint GenerateHash(string input);
}

public class Hashing : IHashing
{
    public uint GenerateHash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var md5Hash = MD5.HashData(inputBytes);
        // md5 generates 16 bytes hash
        // by converting it to uint we use 4 bytes, 32 bits, 0 - 2"32-1 places.
        return BinaryPrimitives.ReadUInt32LittleEndian(md5Hash);
    }
}