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
        new Vector2Int(0, 1),   // +Z (up)
        new Vector2Int(0, -1),  // -Z (down)
        new Vector2Int(-1, 0),  // -X (left)
        new Vector2Int(1, 0)    // +X (right)
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
                branchCount = Mathf.RoundToInt(difficulty * 3);
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

            // WFC
            if (!SolveWithBacktracking())
                continue;

            // Validator
            bool valid = validator.Validate(placed, dimensions);

            if (!valid)
            {
                Debug.LogWarning("Soft fail - accepting partial dungeon");
                success = true; //fallback
            }

            // must have start / boss
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

        // use empty tile for unplaced cells
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

        iteration = 0;
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
                // 初始化每个格子，初始状态是未坍缩，选项列表包含所有Tile
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
            // 找到所有未访问过的相邻格子
            var neighbors = directions
                .Select(d => current + d)
                .Where(p => InBounds(p) && !visited.Contains(p))
                .ToList();

            if (neighbors.Count == 0)
                break;

            // 优先选择更靠近Boss的格子，增加主路径直线性和Boss可达性
            // bias toward boss
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


    // 将主路径上的格子直接坍缩为start/boss Tile，保证主路径正确
    // collapse main path first to reduce WFC complexity and ensure start/boss placement
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

            // start
            if (i == 0)
                t = FixTileToMatch(startTile, requiredDirs);
            // Boss 
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

                t = FindMatch(pos, requiredDirs, isBranchRoot, branchDir);
            }

            if (t == null)
            {
                Debug.LogError($"!!! Main path failed at {pos}");
                return false;
            }

            // 坍缩格子，放置Tile
            cell.collapsed = true;
            cell.tileOptions = new Tile[] { t };
            placed[pos] = t;
        }

        return true;
    }

    // Start/Boss必须严格匹配主路径方向，否则直接失败（不允许分叉）
    Tile FixTileToMatch(Tile baseTile, List<Vector2Int> dirs)
    {
        if (dirs.All(d => baseTile.HasConnection(d)))
            return baseTile;

        Debug.LogError("!!! Start/Boss dir not match");
        return null;
    }

    // 在满足主路径连接需求的基础上，优先满足分叉需求（如果是分叉点），但不强制（允许分叉点不分叉）
    Tile FindMatch(Vector2Int pos, List<Vector2Int> dirs, bool allowBranch, Vector2Int? extraDir = null)
    {
        var candidates = new List<Tile>();

        foreach (var t in tileObjects)
        {
            if (t.tileType == TileType.Start || t.tileType == TileType.Boss)
                continue;

            // 边界限制
            if (IsOutOfBoundsConnection(pos, t))
                continue;

            if (!dirs.All(d => t.HasConnection(d)))
                continue;

            int count = CountConnections(t);

            if (allowBranch)
            {
                // 至少满足主路径连接需求，允许有额外连接（分叉连接）
                if (count >= dirs.Count)
                {
                    if (extraDir.HasValue && !t.HasConnection(extraDir.Value))
                        continue;

                    candidates.Add(t);
                }
            }
            else
            {
                // 严格等于 (保证主路径直线)
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

    // 检查Tile是否有朝边界的连接，如果有则不合法
    bool IsOutOfBoundsConnection(Vector2Int pos, Tile t)
    {
        if (pos.y == dimensions - 1 && t.up) return true;
        if (pos.y == 0 && t.down) return true;
        if (pos.x == 0 && t.left) return true;
        if (pos.x == dimensions - 1 && t.right) return true;

        return false;
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
    // on main path (except start/boss), randomly pick some cells as branch roots,
    // then extend a branch in a random valid direction for a random length
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
            // find a direction that can connect to main path and is not blocked
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

            // extend the branch for a random length
            for (int l = 1; l < maxLength; l++)
            {
                // find next direction that is not blocked
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

    // 将分叉点和分叉路径上的格子预先坍缩为满足主路径连接需求的Tile，
    // 优先满足分叉连接需求（如果有），但不强制（允许分叉点不分叉）
    void CollapseBranches()
    {
        foreach (var branch in branches)
        {
            for (int i = 0; i < branch.Count; i++)
            {
                var pos = branch[i];
                var cell = GetCell(pos);

                List<Vector2Int> requiredDirs = new();

                // 前一个节点（branch内部连接）
                if (i > 0)
                    requiredDirs.Add(branch[i - 1] - pos);

                // 第一个点要连接 main path
                if (i == 0)
                {
                    var start = mainPath
                        .First(p => directions.Any(d => p + d == pos));

                    requiredDirs.Add(start - pos);
                }

                bool isEnd = (i == branch.Count - 1);

                Tile t;

                if (isEnd)
                {
                    // 强制 DeadEnd
                    t = tileObjects
                        .Where(x => x.tileType == TileType.DeadEnd &&
                                    requiredDirs.All(d => x.HasConnection(d)))
                        .OrderBy(_ => Random.value)
                        .FirstOrDefault();
                }
                else
                {
                    // 其他格子正常匹配，允许分叉连接但不强制
                    t = FindMatch(pos, requiredDirs, true);
                }

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

            // 选择一个最低熵的格子（未坍缩，选项最少）
            Cell cell = GetLowestEntropyCell();

            // 如果没有格子了，说明所有格子都坍缩了，成功完成
            if (cell == null)
                return true;
            
            Vector2Int pos = cell.gridPos;

            // 如果这个格子已经被分叉预处理过了，就跳过（分叉预处理过的格子已经坍缩了）
            if (cell.collapsed)
                continue;

            // 从选项中随机选择一个，但不能是 Start/Boss
            List<Tile> options = cell.tileOptions
                .Where(t => t.tileType != TileType.Start &&
                            t.tileType != TileType.Boss)
                .ToList();

            // 如果没有选项了，说明之前的选择有问题必须回退
            if (options.Count == 0)
            {
                if (!Backtrack())
                    return false;
                continue;
            }

            // 必须在做出选择之前保存快照，因为ApplyChoice会直接修改placed和cell状态
            SaveSnapshot();

            Tile chosen = GetWeightedRandom(options);
            decisions.Push(new Decision
            {
                pos = pos,
                remainingOptions = options
            });

            
            ApplyChoice(pos, chosen);

            bool contradiction = !PropagateFrom(pos);

            // 如果传播导致矛盾，说明之前的选择有问题必须回退
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
    // 将选定的Tile应用到格子上，更新placed字典
    // Apply the chosen tile to the cell and update the placed dictionary
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
    // 从指定格子开始传播约束，更新相邻格子的选项列表，如果发现某个格子没有选项了则返回false表示矛盾
    // Propagate constraints from the given cell to its neighbors, updating their options.
    bool PropagateFrom(Vector2Int start)
    {
        Queue<Vector2Int> q = new();
        q.Enqueue(start);

        // 传播过程中，如果某个格子的选项列表被更新了，就把它加入队列继续传播，直到没有格子需要更新了
        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            foreach (var dir in directions)
            {
                // 计算相邻格子的坐标
                var next = cur + dir;
                if (!InBounds(next)) continue;

                // 如果相邻格子已经坍缩了，就不需要传播了
                var cell = GetCell(next);
                if (cell.collapsed) continue;

                // 如果相邻格子没有被放置过，说明它还没有被约束过，跳过（分叉预处理过的格子已经坍缩了，不需要传播）
                if (!placed.ContainsKey(cur)) continue;

                // 获取当前格子已放置的Tile，作为约束源
                var source = placed[cur];

                // 记录传播前的选项数量，如果传播后选项数量变少了，说明这个格子被约束了，需要继续传播它的邻居
                int before = cell.tileOptions.Length;
                
                List<Tile> valid = new();

                // 根据连接关系过滤相邻格子的选项列表，只有与当前格子已放置的Tile在dir方向上连接的选项才是有效的
                // Filter the cell by connection
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

                // 如果没有有效选项了，说明之前的选择导致矛盾，必须回退
                if (valid.Count == 0)
                    return false;

                cell.tileOptions = valid.ToArray();

                // 如果选项数量变少了，说明这个格子被约束了，需要继续传播它的邻居
                if (valid.Count < before)
                    q.Enqueue(next);
            }
        }


        return true;
    }

    // =========================
    // BACKTRACK
    // =========================
    // 回退到上一个决策点，恢复快照状态，并从上一个决策点的剩余选项中选择下一个选项继续尝试，如果没有剩余选项了就继续回退
    bool Backtrack()
    {
        // 如果没有快照了，说明已经回退到最初状态了，无法再回退了，失败
        if (snapshots.Count == 0)
            return false;

        // prevent infinite backtracking loop
        if (decisions.Count > 500)
        {
            Debug.LogError("Too many backtracks");
            return false;
        }

        RestoreSnapshot();

        // 从上一个决策点的剩余选项中选择下一个选项继续尝试，如果没有剩余选项了就继续回退
        if (decisions.Count > 0)
        {
            // 取出上一个决策点
            var last = decisions.Pop();

            // 如果上一个决策点没有剩余选项了，说明之前的选择有问题必须继续回退
            if (last.remainingOptions.Count == 0)
                return Backtrack();

            // 从剩余选项中选择下一个选项继续尝试
            last.remainingOptions.RemoveAt(0);

            // 如果上一个决策点没有剩余选项了，说明之前的选择有问题必须继续回退
            if (last.remainingOptions.Count == 0)
                return Backtrack();

            // 在做出选择之前保存快照，因为ApplyChoice会直接修改placed和cell状态
            SaveSnapshot();

            // 将上一个决策点的下一个选项应用到格子上，继续尝试
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
            c.collapsed = false;

            if (s.optionsSnapshot.ContainsKey(c.gridPos))
                c.tileOptions = s.optionsSnapshot[c.gridPos].ToArray();
        }

        // 重新标记 collapsed
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

    // 从选项列表中根据权重随机选择一个Tile
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