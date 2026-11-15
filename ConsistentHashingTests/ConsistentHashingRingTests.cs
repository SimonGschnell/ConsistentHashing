using ConsistentHashing;
using NSubstitute;

namespace ConsistentHashingTests;

public class ConsistentHashingRingTests
{
    private ConsistentHashingRing _hashingRing;
    
    [SetUp]
    public void Setup()
    {
        _hashingRing = new ConsistentHashingRing(new Hashing(), 1);
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
        hashing.GenerateHash(Arg.Any<string>()).Returns(100u);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        Assert.That(_hashingRing.Count, Is.EqualTo(2));
        Assert.That(_hashingRing.GetServer(100), Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetServer(101), Is.EqualTo("server2"));
    }
    
    [Test]
    public void SameServerIsNotAddedAfterHandlingHashCollisionOnce()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash(Arg.Any<string>()).Returns(100u);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        _hashingRing.AddServer("server2");
        Assert.That(_hashingRing.Count, Is.EqualTo(2));
        Assert.That(_hashingRing.GetServer(100), Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetServer(101), Is.EqualTo("server2"));
    }
    
    [Test]
    public void RemoveServerCorrectlyUpdatesCount()
    {
        _hashingRing.AddServer("server1");
        _hashingRing.RemoveServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void RemovesServerCorrectlyWhenThereWasAHashCollision()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash(Arg.Any<string>()).Returns(100u);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        _hashingRing.RemoveServer("server2");
        Assert.That(_hashingRing.Count, Is.EqualTo(1));
        Assert.That(_hashingRing.GetServer(100), Is.EqualTo("server1"));
    }
    
    [Test]
    public void AddingRemovingAndAddingAgainWorksCorrectly()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash(Arg.Any<string>()).Returns(100u);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.RemoveServer("server1");
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(1));
        Assert.That(_hashingRing.GetServer(100), Is.EqualTo("server1"));
    }
    
    [Test]
    public void RemovingNotPresentServerInTheRingDoesNotThrow()
    {
        Assert.DoesNotThrow(()=>_hashingRing.RemoveServer("server1"));
        Assert.That(_hashingRing.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void GetNodeFromRingDoesNotThrowWhenTheSearchedKeyIsMissing()
    {
        Assert.DoesNotThrow(()=>_hashingRing.GetServer(123));
    }
    
    // Single server at 100, key at 110. The key is past the last server, so the lookup wraps around the ring to the only server.
    [Test]
    public void KeyAfterTheOnlyServerWrapsAroundToIt()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash(Arg.Any<string>()).Returns(100u);
        hashing.GenerateHash("client1").Returns(110u);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("client1"), Is.EqualTo("server1"));
    }
    
    // Single server at 100, key at 50. The first position clockwise from 50 is the server, with no wrap needed.
    [Test]
    public void KeyBeforeTheOnlyServerGoesToIt()
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash(Arg.Any<string>()).Returns(100u);
        hashing.GenerateHash("client1").Returns(50u);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("client1"), Is.EqualTo("server1"));
    }

    // Helper: a ring with server1, server2 and server3 at positions 100, 200 and 300 (one virtual node each). The hash of "key" is the given keyHash. Stubs use the virtual node names ("server1#0" and so on) because that is what gets hashed.
    private void CreateRingWithThreeServers(uint keyHash)
    {
        IHashing hashing = Substitute.For<IHashing>();
        hashing.GenerateHash("server1#0").Returns(100u);
        hashing.GenerateHash("server2#0").Returns(200u);
        hashing.GenerateHash("server3#0").Returns(300u);
        hashing.GenerateHash("key").Returns(keyHash);
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        _hashingRing.AddServer("server3");
    }

    // Clockwise lookup with servers at 100, 200 and 300. Covers a key before the first server, between two servers, exactly on the first, a middle and the last server, and after the last server (wraps to the first). This is the case that exercises the binary search.
    [TestCase(50u, "server1", TestName = "Key before the first server goes to the first server")]
    [TestCase(150u, "server2", TestName = "Key between two servers goes to the next one clockwise")]
    [TestCase(100u, "server1", TestName = "Key exactly on the first server goes to it")]
    [TestCase(200u, "server2", TestName = "Key exactly on a middle server goes to it")]
    [TestCase(300u, "server3", TestName = "Key exactly on the last server goes to it")]
    [TestCase(350u, "server1", TestName = "Key after the last server wraps to the first server")]
    public void FindsCorrectServerWithMultipleServers(uint keyHash, string expectedServer)
    {
        CreateRingWithThreeServers(keyHash);
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("key"), Is.EqualTo(expectedServer));
    }

    // With no servers on the ring a lookup has nothing to return and gives null.
    [Test]
    public void EmptyRingReturnsNoServer()
    {
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("key"), Is.Null);
    }

    // A key at 150 belongs to server2 (200). After server2 is removed the key moves on to the next server clockwise, server3 (300).
    [Test]
    public void KeyGoesToTheNextServerAfterTheOwnerIsRemoved()
    {
        CreateRingWithThreeServers(150u);
        _hashingRing.RemoveServer("server2");
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("key"), Is.EqualTo("server3"));
    }

    // ---- Step 6: virtual nodes ----

    // Helper: builds a fake hasher that returns a chosen position for each given name, so tests control exactly where virtual nodes land.
    private IHashing StubHashing(params (string Id, uint Hash)[] positions)
    {
        var hashing = Substitute.For<IHashing>();
        foreach (var (id, hash) in positions)
        {
            hashing.GenerateHash(id).Returns(hash);
        }
        return hashing;
    }

    // With 3 virtual nodes per server, one AddServer call creates 3 positions (100, 200, 300) and each of them resolves to the real server name.
    [Test]
    public void AddingAServerCreatesOnePositionPerVirtualNode()
    {
        var hashing = StubHashing(("server1#0", 100u), ("server1#1", 200u), ("server1#2", 300u));
        _hashingRing = new ConsistentHashingRing(hashing, 3);
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(3));
        Assert.That(_hashingRing.GetServer(100), Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetServer(200), Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetServer(300), Is.EqualTo("server1"));
    }

    // Adding a server a second time must not create its virtual nodes again.
    [Test]
    public void AddingTheSameServerTwiceDoesNotAddVirtualNodesAgain()
    {
        var hashing = StubHashing(("server1#0", 100u), ("server1#1", 200u), ("server1#2", 300u));
        _hashingRing = new ConsistentHashingRing(hashing, 3);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(3));
    }

    // Removing a server removes every one of its virtual nodes, not just one.
    [Test]
    public void RemovingAServerRemovesAllItsVirtualNodes()
    {
        var hashing = StubHashing(("server1#0", 100u), ("server1#1", 200u), ("server1#2", 300u));
        _hashingRing = new ConsistentHashingRing(hashing, 3);
        _hashingRing.AddServer("server1");
        _hashingRing.RemoveServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(0));
        Assert.That(_hashingRing.GetServer(100), Is.Null);
    }

    // server1 and server2 interleave on the ring. After removing server1, only server2 positions (200 and 400) are left and the key at 150 goes to server2.
    [Test]
    public void RemovingOneServerLeavesTheVirtualNodesOfOtherServersAlone()
    {
        var hashing = StubHashing(
            ("server1#0", 100u), ("server1#1", 300u),
            ("server2#0", 200u), ("server2#1", 400u),
            ("key", 150u));
        _hashingRing = new ConsistentHashingRing(hashing, 2);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        _hashingRing.RemoveServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(2));
        Assert.That(_hashingRing.GetServer(200), Is.EqualTo("server2"));
        Assert.That(_hashingRing.GetServer(400), Is.EqualTo("server2"));
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("key"), Is.EqualTo("server2"));
    }

    // The key at 250 walks clockwise to position 300, which is virtual node #1 of server1. The lookup must return "server1", not "server1#1".
    [Test]
    public void LookupReturnsTheRealServerNameWhenTheKeyLandsOnAVirtualNode()
    {
        var hashing = StubHashing(
            ("server1#0", 100u), ("server1#1", 300u),
            ("server2#0", 200u), ("server2#1", 400u),
            ("key", 250u));
        _hashingRing = new ConsistentHashingRing(hashing, 2);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        // 250 walks clockwise to 300, which is virtual node #1 of server1.
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("key"), Is.EqualTo("server1"));
    }

    // The virtual nodes of two different servers hash to 100. The second is probed to 101 and a key at 101 resolves to the second server.
    [Test]
    public void CollisionBetweenVirtualNodesOfDifferentServersIsProbed()
    {
        var hashing = StubHashing(("server1#0", 100u), ("server2#0", 100u), ("key", 101u));
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server2");
        Assert.That(_hashingRing.Count, Is.EqualTo(2));
        Assert.That(_hashingRing.GetServer(100), Is.EqualTo("server1"));
        Assert.That(_hashingRing.GetServer(101), Is.EqualTo("server2"));
        Assert.That(_hashingRing.GetNextServerClockwiseForClient("key"), Is.EqualTo("server2"));
    }

    // Removing "server1" must not remove "server10". Servers are matched by exact name, not by substring.
    [Test]
    public void RemovingAServerDoesNotRemoveServersWhoseNameContainsItsName()
    {
        var hashing = StubHashing(("server1#0", 100u), ("server10#0", 200u));
        _hashingRing = new ConsistentHashingRing(hashing, 1);
        _hashingRing.AddServer("server1");
        _hashingRing.AddServer("server10");
        _hashingRing.RemoveServer("server1");
        Assert.That(_hashingRing.Count, Is.EqualTo(1));
        Assert.That(_hashingRing.GetServer(200), Is.EqualTo("server10"));
    }
}