using ConsistentHashing;

namespace ConsistentHashingTests;

public class HashingTests
{
    private Hashing _hashingAlgorithm;
    
    [OneTimeSetUp]
    public void Setup()
    {
        _hashingAlgorithm = new Hashing();
    }

    [Test]
    public void HashesOfTheSameStringsAreEqual()
    {
        const string string1 = "Hello World";
        const string string2 = "Hello World";
        var hash1 = _hashingAlgorithm.GenerateHash(string1);
        var hash2 = _hashingAlgorithm.GenerateHash(string2);
        Assert.That(hash1, Is.EqualTo(hash2));
    }
    
    [Test]
    public void HashesOfTheDifferentStringsAreNotEqual()
    {
        const string string1 = "Hello World";
        const string string2 = "Hello";
        var hash1 = _hashingAlgorithm.GenerateHash(string1);
        var hash2 = _hashingAlgorithm.GenerateHash(string2);
        Assert.That(hash2, Is.Not.EqualTo(hash1));
    }
    
    [Test]
    public void HashIs4BytesLong()
    {
        const string string1 = "Hello World";
        var hash1 = _hashingAlgorithm.GenerateHash(string1);
        var bytes = BitConverter.GetBytes(hash1);
        Assert.That(bytes, Has.Length.EqualTo(4));
    }
}