using ConsistentHashing;
using NSubstitute;

namespace ConsistentHashingTests;

public class ConsistentHashingRingTests
{
    private ConsistentHashingRing _hashingRing;
    
    [SetUp]
    public void Setup()
    {
        _hashingRing = new ConsistentHashingRing(new Hashing());
    }

    [Test]
    public void ANewConsistentHashRingIsEmpty()
    {
        Assert.That(_hashingRing.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void AddingServerToConsistentHashRingUpdatesCount()
    {
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void AddingTheSameServerDoesNothing()
    {
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void HashingCollisionOfDifferentServersUpdatesHashCorrectly()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash("server1").Returns(100u);
        hashing.GenerateHash("server2").Returns(100u);
        _hashingRing = new ConsistentHashingRing(hashing);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        Assert.That(_hashingRing.Count, Is.EqualTo(2));
        Assert.That(_hashingRing.GetNode(100).NodeName, Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetNode(101).NodeName, Is.EqualTo("server2"));
    }
    
    [Test]
    public void SameServerIsNotAddedAfterHandlingHashCollisionOnce()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash("server1").Returns(100u);
        hashing.GenerateHash("server2").Returns(100u);
        _hashingRing = new ConsistentHashingRing(hashing);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        _hashingRing.AddServer("server2");
        Assert.That(_hashingRing.Count, Is.EqualTo(2));
        Assert.That(_hashingRing.GetNode(100).NodeName, Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetNode(101).NodeName, Is.EqualTo("server2"));
    }
}