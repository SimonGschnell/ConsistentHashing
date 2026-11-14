using ConsistentHashing;

namespace ConsistentHashingTests;

public class ConsistentHashingRingTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void KeysAreSortedInTheRing()
    {
        var ring = new ConsistentHashingRing();
        Assert.That(ring.Count, Is.EqualTo(0));
    }
}