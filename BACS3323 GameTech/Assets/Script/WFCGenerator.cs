using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Tile;

public class WFCGenerator
{
    static readonly Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public int dimensions;
    public Tile[] tileObjects;
    public Cell cellObj;

    public Tile startTile;
    public Tile bossTile;

    List<Cell> grid = new();
    List<Vector2Int> mainPath = new();
    Dictionary<Vector2Int, Tile> placed = new();

    Vector2Int startPos;
    Vector2Int bossPos;

    // =========================
    // BACKTRACK STACK
    // =========================
    class Snapshot
    {
        public Dictionary<Vector2Int, Tile[]> optionsSnapshot = new();
        public Dictionary<Vector2Int, Tile> placedSnapshot = new();
    }

    class Decision
    {
        public Vector2Int pos;
        public List<Tile> remainingOptions;
    }

    Stack<Snapshot> snapshots = new();
    Stack<Decision> decisions = new();

    public Dictionary<Vector2Int, Tile> Generate(int size, int seed)
    {
        this.dimensions = size;
        Random.InitState(seed);

        bool success = false;

        for (int attempt = 0; attempt < 20 && !success; attempt++)
        {
            ResetAll();

            InitGrid();
            GenerateAdjacency();
            GenerateMainPath();
            CollapseMainPath();

            success = SolveWithBacktracking();
        }

        return placed; // ⭐ 返回数据，不是Spawn
    }

    // =========================
    // RESET
    // =========================
    void ResetAll()
    {
        grid.Clear();
        mainPath.Clear();
        placed.Clear();

        snapshots.Clear();
        decisions.Clear();
    }

    // =========================
    // INIT GRID
    // =========================
    void InitGrid()
    {
        for (int y = 0; y < dimensions; y++)
        {
            for (int x = 0; x < dimensions; x++)
            {
                Cell c = new Cell();
                c.gridPos = new Vector2Int(x, y);
                c.collapsed = false;
                c.tileOptions = tileObjects.ToArray();
                grid.Add(c);
            }
        }
    }

    // =========================
    // ADJACENCY
    // =========================
    void GenerateAdjacency()
    {
        foreach (var t in tileObjects)
        {
            t.upNeighbours = tileObjects.Where(o => t.up && o.down).ToArray();
            t.downNeighbours = tileObjects.Where(o => t.down && o.up).ToArray();
            t.leftNeighbours = tileObjects.Where(o => t.left && o.right).ToArray();
            t.rightNeighbours = tileObjects.Where(o => t.right && o.left).ToArray();
        }
    }

    // =========================
    // MAIN PATH
    // =========================
    void GenerateMainPath()
    {
        PickStartAndBoss();

        mainPath = GeneratePathBFS(startPos, bossPos);
    }

    void CollapseMainPath()
    {
        for (int i = 0; i < mainPath.Count; i++)
        {
            var pos = mainPath[i];

            if (!InBounds(pos))
            {
                Debug.LogError("Path out of bounds: " + pos);
                return;
            }

            var cell = GetCell(pos);

            Tile t;

            if (i == 0)
                t = startTile;
            else if (i == mainPath.Count - 1)
                t = bossTile;
            else
                t = PickPathTile(pos, i);

            cell.collapsed = true;
            cell.tileOptions = new Tile[] { t };

            placed[pos] = t;
        }
    }

    Tile PickPathTile(Vector2Int pos, int i)
    {
        List<Vector2Int> requiredDirs = new();

        

        if (i > 0)
            requiredDirs.Add(pos - mainPath[i - 1]);

        if (i < mainPath.Count - 1)
        {
            Vector2Int dirToNext = mainPath[i + 1] - pos;
            requiredDirs.Add(dirToNext);

            // 如果 next 是 boss → 检查 boss 是否能接
            if (i + 1 == mainPath.Count - 1)
            {
                if (!bossTile.HasConnection(-dirToNext))
                    return null;
            }
        }

        bool isStart = i == 0;
        bool isEnd = i == mainPath.Count - 1;

        var valid = tileObjects
            .Where(t => t.IsStrictPath(requiredDirs, isStart, isEnd))
            .Where(t =>
            {
                if (isStart) return t.tileType == Tile.TileType.Start;
                if (isEnd) return t.tileType == Tile.TileType.Boss;
                return t.tileType != Tile.TileType.Start &&
                       t.tileType != Tile.TileType.Boss;
            })
            .ToList();

        if (valid.Count == 0)
        {
            Debug.LogError("❌ No valid path tile at " + pos);
            return null;
        }

        return valid[Random.Range(0, valid.Count)];
    }

    void PickStartAndBoss()
    {
        startPos = new Vector2Int(
            Random.Range(0, dimensions),
            Random.Range(0, dimensions)
        );

        do
        {
            bossPos = new Vector2Int(
                Random.Range(0, dimensions),
                Random.Range(0, dimensions)
            );

        } while (bossPos == startPos);
    }

    List<Vector2Int> GeneratePathBFS(Vector2Int start, Vector2Int goal)
    {
        Queue<Vector2Int> q = new();
        Dictionary<Vector2Int, Vector2Int> parent = new();
        HashSet<Vector2Int> visited = new();

        q.Enqueue(start);
        visited.Add(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            if (cur == goal)
                break;

            foreach (var dir in directions)
            {
                var next = cur + dir;

                if (!InBounds(next)) continue;
                if (visited.Contains(next)) continue;

                visited.Add(next);
                parent[next] = cur;
                q.Enqueue(next);
            }
        }

        // reconstruct path
        List<Vector2Int> path = new();

        if (!parent.ContainsKey(goal))
        {
            Debug.LogError("No path found!");
            return path;
        }

        Vector2Int p = goal;
        path.Add(p);

        while (p != start)
        {
            p = parent[p];
            path.Add(p);
        }

        path.Reverse();
        return path;
    }

    // =========================
    // BACKTRACK WFC CORE
    // =========================
    bool SolveWithBacktracking()
    {
        while (true)
        {
            Cell cell = GetLowestEntropyCell();

            if (cell == null)
                return true;

            Vector2Int pos = cell.gridPos;

            if (cell.collapsed)
                continue;

            List<Tile> options = cell.tileOptions
                .Where(t => t.tileType != TileType.Start &&
                            t.tileType != TileType.Boss).ToList();

            if (options.Count == 0)
            {
                if (!Backtrack())
                    return false;
                continue;
            }

            // save snapshot BEFORE decision
            SaveSnapshot();

            Tile chosen = options[Random.Range(0, options.Count)];
            decisions.Push(new Decision
            {
                pos = pos,
                remainingOptions = options
            });

            ApplyChoice(pos, chosen);

            bool contradiction = !PropagateFrom(pos);

            if (contradiction)
            {
                if (!Backtrack())
                    return false;
            }
        }
    }

    // =========================
    // APPLY CHOICE
    // =========================
    void ApplyChoice(Vector2Int pos, Tile t)
    {
        var cell = GetCell(pos);

        cell.collapsed = true;
        cell.tileOptions = new Tile[] { t };

        placed[pos] = t;
    }

    // =========================
    // PROPAGATION (returns valid/invalid)
    // =========================
    bool PropagateFrom(Vector2Int start)
    {
        Queue<Vector2Int> q = new();
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            foreach (var dir in directions)
            {
                var next = cur + dir;
                if (!InBounds(next)) continue;

                var cell = GetCell(next);
                if (cell.collapsed) continue;

                int before = cell.tileOptions.Length;

                if (!placed.ContainsKey(cur)) continue;

                var source = placed[cur];

                var valid = cell.tileOptions.Where(t =>
                {
                    if (dir == Vector2Int.up) return source.upNeighbours.Contains(t);
                    if (dir == Vector2Int.down) return source.downNeighbours.Contains(t);
                    if (dir == Vector2Int.left) return source.leftNeighbours.Contains(t);
                    if (dir == Vector2Int.right) return source.rightNeighbours.Contains(t);
                    return false;
                }).ToArray();

                if (valid.Length == 0)
                    return false;

                cell.tileOptions = valid;

                if (valid.Length < before)
                    q.Enqueue(next);
            }
        }

        return true;
    }

    // =========================
    // BACKTRACK
    // =========================
    bool Backtrack()
    {
        if (snapshots.Count == 0)
            return false;

        RestoreSnapshot();

        if (decisions.Count > 0)
        {
            var last = decisions.Pop();
            last.remainingOptions.RemoveAt(0);

            if (last.remainingOptions.Count == 0)
                return Backtrack();

            SaveSnapshot();

            ApplyChoice(last.pos, last.remainingOptions[0]);
        }

        return true;
    }

    // =========================
    // SNAPSHOT SYSTEM
    // =========================
    void SaveSnapshot()
    {
        Snapshot s = new Snapshot();

        foreach (var c in grid)
            s.optionsSnapshot[c.gridPos] = c.tileOptions.ToArray();

        foreach (var p in placed)
            s.placedSnapshot[p.Key] = p.Value;

        snapshots.Push(s);
    }

    void RestoreSnapshot()
    {
        if (snapshots.Count == 0) return;

        var s = snapshots.Pop();

        foreach (var c in grid)
        {
            if (s.optionsSnapshot.ContainsKey(c.gridPos))
                c.tileOptions = s.optionsSnapshot[c.gridPos];
        }

        placed = new Dictionary<Vector2Int, Tile>(s.placedSnapshot);
    }

    // =========================
    // UTIL
    // =========================
    Cell GetLowestEntropyCell()
    {
        return grid
            .Where(c => !c.collapsed)
            .OrderBy(c => c.tileOptions.Length)
            .FirstOrDefault();
    }

    bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < dimensions &&
               p.y >= 0 && p.y < dimensions;
    }

    Cell GetCell(Vector2Int p)
    {
        return grid[p.x + p.y * dimensions];
    }

    
}