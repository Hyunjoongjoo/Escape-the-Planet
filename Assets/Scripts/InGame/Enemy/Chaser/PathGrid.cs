using UnityEngine;
using UnityEngine.Tilemaps;

public class PathGrid
{
    private readonly Tilemap _ground;
    private readonly LayerMask _blockMask;
    private readonly BoundsInt _bounds;

    private readonly bool[,] _walkable;

    public BoundsInt Bounds => _bounds;

    public PathGrid(Tilemap ground, LayerMask blockMask)
    {
        _ground = ground;
        _blockMask = blockMask;

        _bounds = ground.cellBounds;

        int w = _bounds.size.x;
        int h = _bounds.size.y;

        _walkable = new bool[w, h];

        Build();
    }

    private void Build()
    {
        Vector3 cellSize3 = _ground.layoutGrid.cellSize;
        Vector2 cellSize = new Vector2(cellSize3.x, cellSize3.y);

        Vector2 boxSize = cellSize * 0.98f;

        foreach (Vector3Int cell in _bounds.allPositionsWithin)
        {
            int ix = cell.x - _bounds.xMin;
            int iy = cell.y - _bounds.yMin;

            if (!_ground.HasTile(cell))
            {
                _walkable[ix, iy] = false;
                continue;
            }

            Vector2 center = _ground.GetCellCenterWorld(cell);

            bool prev = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;

            Collider2D hit = Physics2D.OverlapBox(center, boxSize, 0f, _blockMask);

            Physics2D.queriesHitTriggers = prev;

            _walkable[ix, iy] = (hit == null);
        }
    }

    public bool IsWalkable(Vector3Int cell)
    {
        int ix = cell.x - _bounds.xMin;
        int iy = cell.y - _bounds.yMin;

        if (ix < 0 || iy < 0 || ix >= _bounds.size.x || iy >= _bounds.size.y)
        {
            return false;
        }

        return _walkable[ix, iy];
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return _ground.WorldToCell(worldPos);
    }

    public Vector3 CellToWorldCenter(Vector3Int cell)
    {
        return _ground.GetCellCenterWorld(cell);
    }
}
