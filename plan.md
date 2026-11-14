# Consistent Hashing Tutorial Plan

Based on *System Design Interview* by Alex Xu, Chapter 5 (Design Consistent Hashing).
Language and stack: C# / .NET 8, NUnit for tests, NSubstitute for mocking.

## How we work

- Claude explains the background and gives one step at a time.
- You write the code and the tests yourself.
- Tell Claude when a step is ready. Claude reads the code, runs `dotnet test`, reviews it and gives the next step.
- Claude only gives hints, not full solutions, unless you ask.

## The idea in short

- Problem: `hash(key) % N` remaps almost all keys when the server count `N` changes, causing mass cache misses.
- Ring: positions `0 .. 2^32 - 1`, wrapping around at the end.
- Servers are hashed onto the ring. A key is hashed onto the same ring and walks **clockwise** to the first server.
- Adding or removing a server only moves the keys next to it (about `1/N` of all keys).
- Virtual nodes: each server is placed on the ring many times so the load spreads evenly.

## Progress

### Done

- [x] **Step 1: Hash function.** `Hashing.GenerateHash(string)` returns a `uint` from the first 4 bytes of MD5, read with `BinaryPrimitives.ReadUInt32LittleEndian`. Tests for same input, different input and 4-byte size.
- [x] **Step 2: Ring data structure.** `ConsistentHashingRing` with a private `SortedList<uint, Node>` and a read-only `Count`. Test for an empty ring.
- [x] **Step 3: `AddServer`.** Hashes the server name and stores it.
  - Adding the same server twice does nothing.
  - On a collision with a different server, the position is probed forward (`hash++`) until a free slot is found.
  - `IHashing` interface extracted so a fake hasher can force collisions (NSubstitute).
  - Tests: count after add, duplicate add, collision probing, probed server added twice.

### Open

- [ ] **Step 4: `RemoveServer`** (next)
  - Find the server by its **name among the stored values** (`IndexOfValue` + `RemoveAt`), not by re-hashing. A probed server does not sit at its own hash position.
  - Decide what happens for an unknown server (ignore or throw) and test it.
  - Tests: add then remove gives `Count == 0`; unknown server; collision case (`server1` and `server2` both at 100, remove `server2`, `server1` stays at 100); add, remove, add again.
- [ ] **Step 5: `GetServer(key)`, the clockwise lookup**
  - Hash the key, then find the first position `>=` the hash (binary search over the sorted keys).
  - Wrap around to the first position if the hash is past the last one.
  - Decide what an empty ring does (throw or return nothing) and test it.
  - Tests: key between two servers, key exactly on a server, key past the last server (wrap-around), single server, empty ring.
  - Likely replaces the test-only `GetNode(uint)` helper.
- [ ] **Step 6: Virtual nodes**
  - Place each server at many positions, for example by hashing `"server-A#0"`, `"server-A#1"`, and so on.
  - `RemoveServer` must remove all positions of a server.
  - Think about what changes: `Count` (positions vs servers), and the duplicate check by `ContainsValue` per name (probably needs a separate set of server names).
  - Collision handling now matters more because there are many more positions.
  - Tests: positions per server, removing a server removes all its virtual nodes, lookup still returns the real server name.
- [ ] **Step 7: Experiments**
  - Distribution: hash many keys, count keys per server, compare with and without virtual nodes.
  - Redistribution: how many keys change owner when a server is added or removed (expect about `1/N`, versus almost all with `% N`).
  - Can be a small demo in `Program.cs` or tests with generous tolerances.
- [ ] **Step 8: Wrap-up**
  - Review edge cases and test coverage.
  - Discuss real-world extras from the chapter and beyond: data migration or pre-warming when nodes change, replication to the next nodes on the ring, hotspot or celebrity problem.
  - Optional: widen the ring to `ulong`, or replace probing with salting.

## Design decisions so far

| Topic | Decision | Why |
|---|---|---|
| Hash function | MD5, first 4 bytes, little-endian `uint` | Even spread, stable across runs (`string.GetHashCode` is randomized per process). Little-endian pinned explicitly. |
| Ring size | `2^32` | Fits a `uint`; collisions possible but rare. |
| Ring storage | `SortedList<uint, Node>` | Keys stay sorted, so binary search works for the lookup. |
| Collisions | Detect, then probe forward (`hash++`) | Guarantees uniqueness without overwriting another server. Keys may share positions, only server positions must be unique. |
| Duplicate add | Ignored | Adding a server that is already there leaves the ring as the caller wanted. |
| Testability | `IHashing` + NSubstitute | Lets tests force hash values and collisions. |

## Open notes and cleanups

- Last commit: `719ad42 empty hashRing`. The `AddServer` work, `IHashing` and the namespace fix in tests are **not committed yet**.
- `NodeType.Client` is unused. The ring only holds servers; consider removing the enum or the extra `Node` type.
- `GetNode(uint)` is a test helper that needs an exact position. Expect it to be replaced in Step 5.
- `hash++` wraps from `uint.MaxValue` to 0 (unchecked arithmetic), which suits a ring. The loop would only run forever if all 2^32 positions were taken (not realistic).
- `.idea/` and `*.DotSettings.user` are git-ignored.

## Useful commands

```
dotnet test                              # run all tests
dotnet add ConsistentHashingTests package <name>   # add a test package
```
