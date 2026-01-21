using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfindingManager : MonoBehaviour
{
    public static PathfindingManager Instance { get; private set; }

    [SerializeField] private Tilemap _groundTilemap;

    [SerializeField] private LayerMask _blockMask;

    private PathGrid _grid;
    private AStar _astar;

    public PathGrid Grid => _grid;
    public AStar AStar => _astar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_groundTilemap == null)
        {
            return;
        }

        _grid = new PathGrid(_groundTilemap, _blockMask);
        _astar = new AStar(_grid);
    }
}
