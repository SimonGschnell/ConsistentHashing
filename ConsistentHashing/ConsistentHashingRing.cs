namespace ConsistentHashing;

public enum NodeType
{
    Server,
    Client
}

public record Node
{
    public NodeType NodeType { get; set; }
    public required string NodeName { get; set; }
}

public class ConsistentHashingRing(IHashing hashingAlgorithm)
{
    private readonly SortedList<uint, Node> _hashRing = new();
    public int Count => _hashRing.Count;

    public Node GetNode(uint key)
    {
        return _hashRing[key];
    }

    public void AddServer(string serverId)
    {
        if (_hashRing.ContainsValue(new Node {NodeType = NodeType.Server, NodeName = serverId}))
        {
            return;
        }
        var hash = hashingAlgorithm.GenerateHash(serverId);
        while (HasHashCollision(_hashRing, hash))
        {
            hash++;
        }
        _hashRing.Add(hash, new Node{NodeType = NodeType.Server, NodeName = serverId});
    }

    private static bool HasHashCollision(SortedList<uint, Node> hashRing, uint hash)
    {
        return hashRing.ContainsKey(hash);
    }
}