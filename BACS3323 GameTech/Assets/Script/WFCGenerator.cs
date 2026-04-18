using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static RoomData;
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

    public Tile startTile;
    public Tile bossTile;
    public Tile emptyTile;

    List<Cell> grid = new();
    public List<Vector2Int> mainPath = new();
    Dictionary<Vector2Int, Tile> placed = new();

    HashSet<Vector2Int> branchCells = new();
    List<List<Vector2Int>> branches = new();
    HashSet<Vector2Int> branchRoots = new();

    Vector2Int startPos;
    Vector2Int bossPos;

    int iteration = 0;
    int branchCount;
    int branchLength;

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

    public Dictionary<Vector2Int, Tile> Generate(int size, int seed, float difficulty, bool enableBranches)
    {
        this.dimensions = size;
        Random.InitState(seed);

        DungeonValidator validator = new DungeonValidator();

        bool success = false;

        for (int attempt = 0; attempt < 50 && !success; attempt++)
        {
            ResetAll();

            InitGrid();
            GenerateMainPath();

            Debug.Log($"Attempt {attempt}");

            if (enableBranches)
            {
                branchCount = Mathf.RoundToInt(difficulty * 5);
                branchLength = Mathf.RoundToInt(Mathf.Lerp(2, 6, difficulty));

                GenerateBranches(branchCount, branchLength);
            }

            if (!CollapseMainPath())
                continue;

            Debug.Log($"Main Path SUCCESS at attempt {attempt}");


            if (enableBranches)
            {
                CollapseBranches();
            }

            Debug.Log($"Branches SUCCESS at attempt {attempt}");

            // ⭐ WFC
            if (!SolveWithBacktracking())
                continue;

            // ⭐ Validator
            if (!validator.Validate(placed, dimensions))
                continue;

            // ⭐ 必须有 start / boss
            if (!placed.Values.Any(t => t.tileType == TileType.Start) ||
                !placed.Values.Any(t => t.tileType == TileType.Boss))
                continue;

            success = true;
        }

        if (!success)
        {
            Debug.LogError("❌ Generation FAILED after retries");
            return new Dictionary<Vector2Int, Tile>();
        }

        // 用Empty填满所有格子
        foreach (var cell in grid)
        {
            if (!placed.ContainsKey(cell.gridPos))
            {
                placed[cell.gridPos] = emptyTile;
            }
        }


        return placed;
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

        branchCells.Clear();
        branches.Clear();
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

        mainPath = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new();

        Vector2Int current = startPos;
        mainPath.Add(current);
        visited.Add(current);

        int targetLength = Random.Range(dimensions * 2, dimensions * 3);

        while (current != bossPos || mainPath.Count < targetLength)
        {
            var neighbors = directions
                .Select(d => current + d)
                .Where(p => InBounds(p) && !visited.Contains(p))
                .ToList();

            if (neighbors.Count == 0)
                break;

            // bias toward boss（关键）
            neighbors = neighbors
                .OrderBy(p => Vector2Int.Distance(p, bossPos))
                .ToList();

            current = neighbors[Random.Range(0, Mathf.Min(2, neighbors.Count))];

            mainPath.Add(current);
            visited.Add(current);

            // 防止死循环
            if (mainPath.Count > dimensions * dimensions)
                break;
        }

        if (!mainPath.Contains(bossPos))
            mainPath.Add(bossPos);
    }

    // 将主路径上的格子直接坍缩为特定 Tile，保证主路径正确
    bool CollapseMainPath()
    {
        for (int i = 0; i < mainPath.Count; i++)
        {
            var pos = mainPath[i];
            var cell = GetCell(pos);

            List<Vector2Int> requiredDirs = new();

            if (i > 0)
                requiredDirs.Add(mainPath[i - 1] - pos);

            if (i < mainPath.Count - 1)
                requiredDirs.Add(mainPath[i + 1] - pos);

            Tile t;

            if (i == 0)
                t = FixTileToMatch(startTile, requiredDirs);
            else if (i == mainPath.Count - 1)
                t = FixTileToMatch(bossTile, requiredDirs);
            else
            {
                bool isBranchRoot = branchRoots.Contains(pos);

                Vector2Int? branchDir = null;

                if (isBranchRoot)
                {
                    var branch = branches.FirstOrDefault(b => b.Contains(pos));

                    if (branch != null)
                    {
                        var first = branch[0];

                        if (Vector2Int.Distance(first, pos) == 1)
                            branchDir = first - pos;
                    }
                }

                t = FindMatch(requiredDirs, isBranchRoot, branchDir);
            }

            if (t == null)
            {
                Debug.LogError($"❌ Main path failed at {pos}");
                return false;
            }

            cell.collapsed = true;
            cell.tileOptions = new Tile[] { t };
            placed[pos] = t;
        }

        return true;
    }

    Tile FixTileToMatch(Tile baseTile, List<Vector2Int> dirs)
    {
        if (dirs.All(d => baseTile.HasConnection(d)))
            return baseTile;

        Debug.LogError("❌ Start/Boss方向不匹配");
        return null;
    }

    Tile FindMatch(List<Vector2Int> dirs, bool allowBranch, Vector2Int? extraDir = null)
    {
        var candidates = new List<Tile>();

        foreach (var t in tileObjects)
        {
            if (t.tileType == TileType.Start || t.tileType == TileType.Boss)
                continue;

            if (!dirs.All(d => t.HasConnection(d)))
                continue;

            int count = CountConnections(t);

            if (allowBranch)
            {
                if (count == dirs.Count + 1)
                {
                    if (extraDir.HasValue && !t.HasConnection(extraDir.Value))
                        continue;

                    candidates.Add(t);
                }
            }
            else
            {
                // 严格等于（保证主路径直线）
                if (count == dirs.Count)
                    candidates.Add(t);
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    int CountConnections(Tile t)
    {
        int c = 0;
        if (t.up) c++;
        if (t.down) c++;
        if (t.left) c++;
        if (t.right) c++;
        return c;
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

    // =========================
    // BRANCHES
    // =========================

    void GenerateBranches(int branchCount = 3, int maxLength = 4)
    {
        for (int i = 0; i < branchCount; i++)
        {
            var candidates = mainPath.Skip(1).Take(mainPath.Count - 2)
                        .Where(p => !branchRoots.Contains(p))
                        .ToList();

            if (candidates.Count == 0) return;

            var start = candidates[Random.Range(0, candidates.Count)];
            branchRoots.Add(start);

            // 找一个方向延伸（第一格必须贴 main path）
            var possibleDirs = directions
                .Select(d => new { dir = d, pos = start + d })
                .Where(x => InBounds(x.pos) &&
                            !mainPath.Contains(x.pos) &&
                            !branchCells.Contains(x.pos))
                .ToList();

            if (possibleDirs.Count == 0)
                continue;

            var first = possibleDirs[Random.Range(0, possibleDirs.Count)];

            List<Vector2Int> branch = new();

            Vector2Int current = first.pos;
            branch.Add(current);
            branchCells.Add(current);

            // 关键：继续往外长
            for (int l = 1; l < maxLength; l++)
            {
                var neighbors = directions
                    .Select(d => current + d)
                    .Where(p => InBounds(p) &&
                                !mainPath.Contains(p) &&
                                !branchCells.Contains(p))
                    .ToList();

                if (neighbors.Count == 0)
                    break;

                var next = neighbors[Random.Range(0, neighbors.Count)];

                branch.Add(next);
                branchCells.Add(next);

                current = next;
            }

            if (branch.Count > 0)
                branches.Add(branch);
        }
    }

    void CollapseBranches()
    {
        foreach (var branch in branches)
        {
            for (int i = 0; i < branch.Count; i++)
            {
                var pos = branch[i];
                var cell = GetCell(pos);

                List<Vector2Int> requiredDirs = new();

                // ⭐ 前一个节点（branch内部连接）
                if (i > 0)
                    requiredDirs.Add(branch[i - 1] - pos);

                // ⭐ 第一个点要连接 main path
                if (i == 0)
                {
                    var start = mainPath
                        .First(p => directions.Any(d => p + d == pos));

                    requiredDirs.Add(start - pos);
                }

                var t = FindMatch(requiredDirs, true);

                if (t == null)
                {
                    Debug.LogError("❌ Branch collapse failed");
                    continue;
                }

                cell.collapsed = true;
                cell.tileOptions = new Tile[] { t };
                placed[pos] = t;
            }
        }
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

            Tile chosen = GetWeightedRandom(options);
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
            .Where(c => !c.collapsed && branchCells.Contains(c.gridPos))
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

    Tile GetWeightedRandom(List<Tile> options)
    {
        float total = options.Sum(t => t.weight);
        float r = Random.Range(0, total);

        float cumulative = 0;

        foreach (var t in options)
        {
            cumulative += t.weight;
            if (r <= cumulative)
                return t;
        }

        return options[0];
    }

    
}