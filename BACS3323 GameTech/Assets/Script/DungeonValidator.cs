using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonValidator
{
    public bool Validate(Dictionary<Vector2Int, Tile> placed, int size)
    {
        return IsConnected(placed, size);
    }

    bool IsConnected(Dictionary<Vector2Int, Tile> placed, int size)
    {
        var visited = new HashSet<Vector2Int>();
        var q = new Queue<Vector2Int>();

        // 找 Start
        var start = placed.First(p => p.Value.tileType == Tile.TileType.Start).Key;

        q.Enqueue(start);
        visited.Add(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var tile = placed[cur];

            foreach (var dir in new[] {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right })
            {
                if (!tile.HasConnection(dir)) continue;

                var next = cur + dir;

                if (!placed.ContainsKey(next)) continue;

                var nt = placed[next];

                if (!nt.HasConnection(-dir)) continue;

                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    q.Enqueue(next);
                }
            }
        }

        // 找 Boss
        var boss = placed.First(p => p.Value.tileType == Tile.TileType.Boss).Key;

        return visited.Contains(boss);
    }
}