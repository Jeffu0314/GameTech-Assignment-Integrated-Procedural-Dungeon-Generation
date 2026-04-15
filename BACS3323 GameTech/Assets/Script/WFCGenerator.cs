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
    public List<Vector2Int> mainPath = new();
    Dictionary<Vector2Int, Tile> placed = new();

    Vector2Int startPos;
    Vector2Int bossPos;

    int iteration = 0;

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
    // MAIN PATH
    // =========================
    void GenerateMainPath()
    {
        PickStartAndBoss();

        mainPath = GeneratePathBFS(startPos, bossPos);
    }

    // 将主路径上的格子直接坍缩为特定 Tile，保证主路径正确
    void CollapseMainPath()
    {
        for (int i = 0; i < mainPath.Count; i++)
        {
            Debug.Log($"Collapsing main path cell {i}/{mainPath.Count}: {mainPath[i]}");
            var pos = mainPath[i];

            if (!InBounds(pos))
            {
                Debug.LogError("Path out of bounds: " + pos);
                return;
            }

            var cell = GetCell(pos);

            Tile t;

            // Start 和 Boss 直接放特定 Tile，其他格子通过 PickPathTile 挑选满足连接要求的 Tile
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

    // 随机放置 Start 和 Boss，保证它们不重叠
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

    // BFS 寻路，生成主路径
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
            Debug.Log("BFS visiting: " + cur);

            if (cur == goal)
                break;

            foreach (var dir in directions)
            {
                // 这里不需要检查 placed，因为主路径生成时格子都是空的
                var next = cur + dir;

                Debug.Log($"Checking neighbor: {next} from {cur}");
                if (!InBounds(next)) continue;

                Debug.Log($"In bounds: {next}");
                if (visited.Contains(next)) continue;

                Debug.Log($"Adding to queue: {next}");
                visited.Add(next);
                parent[next] = cur;
                q.Enqueue(next);
            }
        }

        // 从 goal 回溯到 start
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
            iteration++;
            Debug.Log($"WFC iteration = {iteration}");
            if (iteration > dimensions * dimensions * 10)
            {
                Debug.LogError("WFC failed - too many iterations");
                return false;
            }

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

                if (!placed.ContainsKey(cur)) continue;

                var source = placed[cur];

                int before = cell.tileOptions.Length;
                
                List<Tile> valid = new();

                foreach (var t in cell.tileOptions)
                {
                    bool ok = false;

                    if (dir == Vector2Int.up)
                        ok = source.up && t.down;
                    else if (dir == Vector2Int.down)
                        ok = source.down && t.up;
                    else if (dir == Vector2Int.left)
                        ok = source.left && t.right;
                    else if (dir == Vector2Int.right)
                        ok = source.right && t.left;

                    if (ok)
                        valid.Add(t);
                }

                if (valid.Count == 0)
                    return false;

                cell.tileOptions = valid.ToArray();

                if (valid.Count < before)
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

        if (decisions.Count > 500)
        {
            Debug.LogError("Too many backtracks");
            return false;
        }

        RestoreSnapshot();

        if (decisions.Count > 0)
        {
            var last = decisions.Pop();

            if (last.remainingOptions.Count == 0)
                return Backtrack();

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
    // 保存当前状态到快照栈
    void SaveSnapshot()
    {
        Snapshot s = new Snapshot();

        foreach (var c in grid)
            s.optionsSnapshot[c.gridPos] = c.tileOptions.ToArray();

        foreach (var p in placed)
            s.placedSnapshot[p.Key] = p.Value;

        snapshots.Push(s);
    }

    // 恢复到上一个快照状态
    void RestoreSnapshot()
    {
        if (snapshots.Count == 0) return;

        var s = snapshots.Pop();

        placed = new Dictionary<Vector2Int, Tile>(s.placedSnapshot);

        foreach (var c in grid)
        {
            c.collapsed = false; // ⭐必须恢复

            if (s.optionsSnapshot.ContainsKey(c.gridPos))
                c.tileOptions = s.optionsSnapshot[c.gridPos].ToArray();
        }

        // 重新标记 collapsed（非常重要）
        foreach (var p in placed)
        {
            var cell = GetCell(p.Key);
            if (cell != null)
            {
                cell.collapsed = true;
                cell.tileOptions = new Tile[] { p.Value };
            }
        }
    }

    // =========================
    // UTIL
    // =========================
    // 获取最低熵的格子（未坍缩，选项最少）
    Cell GetLowestEntropyCell()
    {
        return grid
            .Where(c => !c.collapsed)
            .OrderBy(c => c.tileOptions.Length)
            .FirstOrDefault();
    }

    // 检查坐标是否在网格内
    bool InBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < dimensions &&
               p.y >= 0 && p.y < dimensions;
    }

    // 获取格子对象
    Cell GetCell(Vector2Int p)
    {
        if (!InBounds(p))
        {
            Debug.LogError($"Out of bounds: {p}");
            return null;
        }

        return grid[p.x + p.y * dimensions];
    }


}