namespace ConsistentHashing;

public class ConsistentHashingRing
{
    private readonly SortedList<uint, string> _hashRing = new();
    public int Count => _hashRing.Count;
}