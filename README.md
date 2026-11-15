# ConsistentHashing

A small consistent hashing implementation in C# / .NET 8, built as a learning project based on
*System Design Interview* by Alex Xu, Chapter 5 (Design Consistent Hashing).

## How it works

- Servers are hashed onto a ring of positions `0 .. 2^32 - 1` (MD5, first 4 bytes).
- A key is hashed onto the same ring and belongs to the first server found clockwise, wrapping around at the end. The lookup uses a binary search over the sorted positions.
- Each server is placed on the ring many times (virtual nodes, `"server1#0"`, `"server1#1"`, ...) so the keys spread evenly.
- Adding or removing a server only moves the keys next to it, about `1/N` of all keys, instead of almost all keys with `hash % N`.

## Run

```
dotnet test                              # run the tests (NUnit, NSubstitute)
dotnet run --project ConsistentHashing   # run the demo
```

The demo prints two experiments:

- **Distribution:** how evenly 100,000 keys spread over 5 servers with 1, 10, 100 and 500 virtual nodes.
- **Redistribution:** how many keys change server when a 6th server is added, compared with `hash % N`.

## Project layout

- `ConsistentHashing/ConsistentHashingRing.cs`: the ring (`AddServer`, `RemoveServer`, clockwise lookup).
- `ConsistentHashing/Hashing.cs`: the hash function behind the `IHashing` interface.
- `ConsistentHashing/Program.cs`: the experiments demo.
- `ConsistentHashingTests/`: unit tests. A fake `IHashing` forces exact positions and collisions.
