namespace ConsistentHashing;

// virtualNodeCount: how many ring positions each server gets. Experiment A (distribution)
// compares different values: more virtual nodes spread the keys more evenly between servers.
public class ConsistentHashingRing(IHashing hashingAlgorithm, int virtualNodeCount=100)
{
    private readonly SortedList<uint, string> _hashRing = new();
    public int Count => _hashRing.Count;

    public string? GetServer(uint key)
    {
        return _hashRing.ContainsKey(key)
            ? RemoveVirtualNodePostfix(_hashRing[key])
            : null;
    }

    // Places the server on the ring at virtualNodeCount positions ("name#0", "name#1", ...).
    // Experiment B (redistribution) calls this to add a server and see which keys move.
    public void AddServer(string serverId)
    {
        for (var i = 0; i < virtualNodeCount; i++)
        {
            AddVirtualNode($"{serverId}#{i}");
        }
    }

    private void AddVirtualNode(string serverId)
    {
        if (_hashRing.ContainsValue(serverId))
        {
            return;
        }
        var hash = hashingAlgorithm.GenerateHash(serverId);
        while (HasHashCollision(_hashRing, hash))
        {
            hash++;
        }
        _hashRing.Add(hash, serverId);
    }

    // The lookup both experiments use to find the owner of a key: the first server at or
    // after the key's hash going clockwise, wrapping to the first server past the end.
    public string? GetNextServerClockwiseForClient(string clientId)
    {
        if(Count == 0) return null;
        var clientIdHash = hashingAlgorithm.GenerateHash(clientId);

        // Binary search for the first position >= clientIdHash.
        var serverKeys = _hashRing.Keys;
        var low = 0;
        var high = _hashRing.Count;
        while (low < high)
        {
            var mid =  low + (high - low) / 2;
            if (serverKeys[mid] < clientIdHash)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        // Past the last position: wrap around to the first server.
        if (low == serverKeys.Count)
        {
            return RemoveVirtualNodePostfix(_hashRing[serverKeys.First()]);
        }

        return RemoveVirtualNodePostfix(_hashRing[serverKeys[low]]);
    }

    public void RemoveServer(string serverId)
    {
        var matchingNodes = _hashRing.Where(keyValue => RemoveVirtualNodePostfix(keyValue.Value).Equals(serverId)).Select(keyValue => keyValue.Key).ToList();
        foreach (var matchingNode in matchingNodes)
        {
            _hashRing.Remove(matchingNode);
        }
    }

    private static bool HasHashCollision(SortedList<uint, string> hashRing, uint hash)
    {
        return hashRing.ContainsKey(hash);
    }

    private static string RemoveVirtualNodePostfix(string id)
    {
        return id.Split("#").First();
    }
}
