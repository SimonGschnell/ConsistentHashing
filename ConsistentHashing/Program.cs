namespace ConsistentHashing;

/// <summary>
/// Demo with two experiments that show why consistent hashing and virtual nodes matter.
///   A) Distribution: how evenly do keys spread over the servers?
///   B) Redistribution: how many keys change server when a server is added?
/// </summary>
class Program
{
    private const int KeyCount = 100_000;
    private const int ServerCount = 5;

    static void Main(string[] args)
    {
        var keys = Enumerable.Range(0, KeyCount).Select(i => $"key-{i}").ToList();
        var servers = Enumerable.Range(1, ServerCount).Select(i => $"server{i}").ToList();

        DistributionExperiment(keys, servers);
        RedistributionExperiment(keys, servers);
    }

    // Experiment A: hash many keys, count how many land on each server.
    // With one position per server the arcs between servers are uneven, so some servers
    // get far more keys than others. More virtual nodes average this out.
    private static void DistributionExperiment(List<string> keys, List<string> servers)
    {
        Console.WriteLine($"Experiment A: distribution of {keys.Count:N0} keys over {servers.Count} servers");
        Console.WriteLine($"(ideal share per server: {100.0 / servers.Count:F1}%)");
        Console.WriteLine();
        Console.WriteLine($"{"virtual nodes",14} | {"min share",10} | {"max share",10} | {"max / min",10}");
        Console.WriteLine(new string('-', 54));

        foreach (var virtualNodes in new[] { 1, 10, 100, 500 })
        {
            var ring = CreateRing(servers, virtualNodes);
            var keysPerServer = servers.ToDictionary(server => server, _ => 0);
            foreach (var key in keys)
            {
                keysPerServer[ring.GetNextServerClockwiseForClient(key)!]++;
            }

            var min = keysPerServer.Values.Min() * 100.0 / keys.Count;
            var max = keysPerServer.Values.Max() * 100.0 / keys.Count;
            Console.WriteLine($"{virtualNodes,14} | {min,9:F1}% | {max,9:F1}% | {max / min,10:F2}");
        }
        Console.WriteLine();
    }

    // Experiment B: record which server owns each key, add one server, and count how many
    // keys now have a different owner. Consistent hashing should move only about
    // 1/(N+1) of the keys. The old approach, "hash % number of servers", is shown
    // separately below the table because it works differently and needs an explanation.
    private static void RedistributionExperiment(List<string> keys, List<string> servers)
    {
        var newServer = $"server{servers.Count + 1}";
        var oldCount = servers.Count;
        var newCount = servers.Count + 1;
        Console.WriteLine($"Experiment B: keys that change server when {newServer} is added ({oldCount} -> {newCount} servers)");
        Console.WriteLine($"(ideal for consistent hashing: {100.0 / newCount:F1}% of keys move, the share the new server takes over)");
        Console.WriteLine();
        Console.WriteLine($"{"consistent hashing",-34} | {"keys moved",10} | {"moved",7}");
        Console.WriteLine(new string('-', 58));

        double consistentPercent = 0;
        foreach (var virtualNodes in new[] { 1, 100 })
        {
            var ring = CreateRing(servers, virtualNodes);
            var before = keys.Select(key => ring.GetNextServerClockwiseForClient(key)).ToList();
            ring.AddServer(newServer);
            var after = keys.Select(key => ring.GetNextServerClockwiseForClient(key)).ToList();
            var moved = CountMoved(before, after);
            consistentPercent = moved * 100.0 / keys.Count;
            Console.WriteLine($"{$"{virtualNodes} virtual node(s)",-34} | {moved,10:N0} | {consistentPercent,6:F1}%");
        }
        Console.WriteLine();

        // The old approach, for comparison: no ring, the server number is hash % number of servers.
        var hashing = new Hashing();
        var beforeModulo = keys.Select(key => hashing.GenerateHash(key) % (uint)oldCount).ToList();
        var afterModulo = keys.Select(key => hashing.GenerateHash(key) % (uint)newCount).ToList();
        var movedModulo = CountMoved(beforeModulo, afterModulo);
        var moduloPercent = movedModulo * 100.0 / keys.Count;

        Console.WriteLine($"For comparison, with the standard hash % N approach for chaches, going from {oldCount} to {newCount} servers moves {moduloPercent:F1}% of the keys,");
        Console.WriteLine($"because the divisor changes for every key. That is about {moduloPercent / consistentPercent:F1}x more than with the consistent hashing approach where only {consistentPercent:F1}% of the keys move.");
        Console.WriteLine();
    }

    private static int CountMoved<T>(List<T> before, List<T> after)
    {
        return before.Where((owner, i) => !EqualityComparer<T>.Default.Equals(owner, after[i])).Count();
    }

    private static ConsistentHashingRing CreateRing(List<string> servers, int virtualNodes)
    {
        var ring = new ConsistentHashingRing(new Hashing(), virtualNodes);
        foreach (var server in servers)
        {
            ring.AddServer(server);
        }
        return ring;
    }
}
