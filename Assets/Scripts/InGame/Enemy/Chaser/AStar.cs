using UnityEngine;
using System.Collections.Generic;

public class AStar
{
    private readonly PathGrid _grid;

    private static readonly Vector3Int[] _dirs8 =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),

        new Vector3Int(1, 1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(-1, -1, 0),
    };

    public AStar(PathGrid grid)
    {
        _grid = grid;
    }

    private int Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        int diag = Mathf.Min(dx, dy);
        int straight = Mathf.Abs(dx - dy);

        return diag * 14 + straight * 10;
    }

    public bool TryFindPath(Vector3Int start, Vector3Int goal, int maxExpand, List<Vector3Int> outPath)
    {
        outPath.Clear();

        if (!_grid.IsWalkable(start) || !_grid.IsWalkable(goal))
        {
            return false;
        }

        var open = new MinHeap<Vector3Int>();
        var openSet = new HashSet<Vector3Int>();
        var closed = new HashSet<Vector3Int>();

        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int>();

        gScore[start] = 0;

        int startF = Heuristic(start, goal);
        open.Enqueue(start, startF);
        openSet.Add(start);

        int expandCount = 0;

        while (open.Count > 0)
        {
            Vector3Int current = open.Dequeue();
            openSet.Remove(current);

            if (closed.Contains(current))
            {
                continue;
            }

            closed.Add(current);

            if (current == goal)
            {
                ReconstructPath(cameFrom, current, outPath);
                return true;
            }

            expandCount++;
            if (expandCount > maxExpand)
            {
                return false;
            }

            foreach (var d in _dirs8)
            {
                Vector3Int next = current + d;

                if (d.x != 0 && d.y != 0)
                {
                    Vector3Int side1 = current + new Vector3Int(d.x, 0, 0);
                    Vector3Int side2 = current + new Vector3Int(0, d.y, 0);

                    if (!_grid.IsWalkable(side1) || !_grid.IsWalkable(side2))
                    {
                        continue;
                    }
                }

                if (!_grid.IsWalkable(next))
                {
                    continue;
                }

                if (closed.Contains(next))
                {
                    continue;
                }

                int stepCost = (d.x != 0 && d.y != 0) ? 14 : 10;
                int tentativeG = gScore[current] + stepCost;

                if (gScore.TryGetValue(next, out int oldG) && tentativeG >= oldG)
                {
                    continue;
                }

                cameFrom[next] = current;
                gScore[next] = tentativeG;

                int f = tentativeG + Heuristic(next, goal);

                if (!openSet.Contains(next))
                {
                    open.Enqueue(next, f);
                    openSet.Add(next);
                }
            }
        }

        return false;
    }

    private void ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current, List<Vector3Int> outPath)
    {
        outPath.Clear();
        outPath.Add(current);

        while (cameFrom.TryGetValue(current, out Vector3Int prev))
        {
            current = prev;
            outPath.Add(current);
        }

        outPath.Reverse();
    }
}
