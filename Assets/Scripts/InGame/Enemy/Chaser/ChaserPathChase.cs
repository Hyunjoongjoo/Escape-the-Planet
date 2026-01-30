using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(EnemyController))]
public class ChaserPathChase : MonoBehaviour
{
    [SerializeField] private Transform _target;

    [SerializeField] private float _repathInterval = 0.35f;
    [SerializeField] private int _maxExpand = 2500;
    [SerializeField] private int _repathCellThreshold = 2;
    [SerializeField] private float _waypointReachDist = 0.08f;

    [SerializeField] private bool _isForced = false;
    [SerializeField] private float _fallbackSpeedMultiplier = 1.0f;

    [SerializeField] private int _goalFixRadius = 4;

    [SerializeField] private int _forcedSearchRadius = 16;
    [SerializeField] private int _forcedSearchSamples = 120;

    [SerializeField] private float _agentRadius = 0.45f;
    [SerializeField] private float _lookAhead = 0.6f;

    private EnemyController _enemy;

    private PathGrid _grid;
    private AStar _astar;

    private readonly List<Vector3Int> _cellPath = new List<Vector3Int>();
    private int _pathIndex = 0;

    private float _repathTimer = 0f;
    private Vector3Int _lastGoalCell;

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
    }
    

    private IEnumerator Start()
    {
        yield return null;

        if (PathfindingManager.Instance == null)
        {
            enabled = false;
            yield break;
        }

        _grid = PathfindingManager.Instance.Grid;
        _astar = PathfindingManager.Instance.AStar;

        if (_grid == null || _astar == null)
        {
            enabled = false;
            yield break;
        }

        if (_target != null)
        {
            _lastGoalCell = _grid.WorldToCell(_target.position);
            _repathTimer = 0f;
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _isForced = false;

        if (_grid != null && _target != null)
        {
            _lastGoalCell = _grid.WorldToCell(_target.position);
            _repathTimer = 0f;
        }
    }

    public void ClearTarget()
    {
        _target = null;
        _isForced = false;

        _cellPath.Clear();
        _pathIndex = 0;

        if (_enemy != null)
        {
            _enemy.StopMove();
        }
    }

    public void ForceChase(Transform target)
    {
        if (target == null)
        {
            return;
        }

        _target = target;
        _isForced = true;

        if (_grid != null)
        {
            _lastGoalCell = _grid.WorldToCell(target.position);
        }

        _repathTimer = 0f;
    }



    private void FixedUpdate()
    {
        if (_enemy.State.current == EnemyState.State.Dead)
        {
            return;
        }

        if (_grid == null || _astar == null)
        {
            return;
        }

        if (_target == null)
        {
            _cellPath.Clear();
            _pathIndex = 0;

            _enemy.StopMove();

            return;
        }

        Vector3Int startCell = _grid.WorldToCell(transform.position);

        Vector3Int rawGoalCell = _grid.WorldToCell(_target.position);

        Vector3Int goalCell;
        bool goalOk = TryFixGoalCell(rawGoalCell, out goalCell);

        if (!goalOk)
        {
            if (_isForced)
            {
                ForcedFallbackMove();
            }
            return;
        }

        _repathTimer -= Time.fixedDeltaTime;

        bool needRepath = false;

        if (_repathTimer <= 0f)
        {
            needRepath = true;
        }

        int goalMove = Mathf.Abs(goalCell.x - _lastGoalCell.x) + Mathf.Abs(goalCell.y - _lastGoalCell.y);
        if (goalMove >= _repathCellThreshold)
        {
            needRepath = true;
        }

        if (needRepath)
        {
            _repathTimer = _repathInterval;
            _lastGoalCell = goalCell;

            bool found = _astar.TryFindPath(startCell, goalCell, _maxExpand, _cellPath);

            if (found)
            {
                _pathIndex = 0;
            }
            else
            {
                _cellPath.Clear();
                _pathIndex = 0;

                if (_isForced)
                {
                    Vector3Int reachableGoal;
                    bool foundAlt = TryFindReachableGoalNearTarget(startCell, goalCell, out reachableGoal);

                    if (foundAlt)
                    {
                        bool altFound = _astar.TryFindPath(startCell, reachableGoal, _maxExpand, _cellPath);
                        if (altFound)
                        {
                            _pathIndex = 0;
                            return;
                        }
                    }

                    ForcedFallbackMove();
                }

                return;
            }
        }

        FollowPath();
    }

    private void FollowPath()
    {
        if (_cellPath.Count == 0)
        {
            if (_isForced)
            {
                ForcedFallbackMove();
            }
            return;
        }

        int nextIndex = Mathf.Clamp(_pathIndex + 1, 0, _cellPath.Count - 1);
        Vector3 nextWorld = _grid.CellToWorldCenter(_cellPath[nextIndex]);

        Vector2 to = (Vector2)nextWorld - (Vector2)transform.position;
        float dist = to.magnitude;

        if (dist <= _waypointReachDist)
        {
            _pathIndex = nextIndex;

            if (_pathIndex >= _cellPath.Count - 1)
            {
                _repathTimer = 0f;

                if (_isForced)
                {
                    ForcedFallbackMove();
                }
            }

            return;
        }

        _enemy.ApplyExternalMove(to.normalized, 1f);
    }

    private bool TryFixGoalCell(Vector3Int goal, out Vector3Int fixedGoal)
    {
        fixedGoal = goal;

        if (_grid.IsWalkable(goal))
        {
            return true;
        }

        int bestDist = int.MaxValue;
        Vector3Int best = goal;

        for (int y = -_goalFixRadius; y <= _goalFixRadius; y++)
        {
            for (int x = -_goalFixRadius; x <= _goalFixRadius; x++)
            {
                Vector3Int c = new Vector3Int(goal.x + x, goal.y + y, 0);

                if (!_grid.IsWalkable(c))
                {
                    continue;
                }

                int d = Mathf.Abs(x) + Mathf.Abs(y);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;

                    if (bestDist == 1)
                    {
                        fixedGoal = best;
                        return true;
                    }
                }
            }
        }

        if (bestDist == int.MaxValue)
        {
            return false;
        }

        fixedGoal = best;
        return true;
    }

    private bool TryFindReachableGoalNearTarget(Vector3Int startCell, Vector3Int targetCell, out Vector3Int bestGoal)
    {
        bestGoal = targetCell;

        int checkedCount = 0;
        int bestDist = int.MaxValue;
        Vector3Int best = targetCell;

        for (int r = 1; r <= _forcedSearchRadius; r++)
        {
            for (int y = -r; y <= r; y++)
            {
                int x1 = r - Mathf.Abs(y);
                int x2 = -x1;

                Vector3Int c1 = new Vector3Int(targetCell.x + x1, targetCell.y + y, 0);
                Vector3Int c2 = new Vector3Int(targetCell.x + x2, targetCell.y + y, 0);

                if (TryCandidate(startCell, targetCell, c1, ref bestDist, ref best, ref checkedCount))
                {
                    bestGoal = best;
                    return true;
                }

                if (x2 != x1)
                {
                    if (TryCandidate(startCell, targetCell, c2, ref bestDist, ref best, ref checkedCount))
                    {
                        bestGoal = best;
                        return true;
                    }
                }

                if (checkedCount >= _forcedSearchSamples)
                {
                    break;
                }
            }

            if (checkedCount >= _forcedSearchSamples)
            {
                break;
            }
        }

        if (bestDist == int.MaxValue)
        {
            return false;
        }

        bestGoal = best;
        return true;
    }

    private bool TryCandidate(
        Vector3Int startCell,
        Vector3Int targetCell,
        Vector3Int candidate,
        ref int bestDist,
        ref Vector3Int best,
        ref int checkedCount)
    {
        checkedCount++;

        if (!_grid.IsWalkable(candidate))
        {
            return false;
        }

        List<Vector3Int> tmp = new List<Vector3Int>();
        bool ok = _astar.TryFindPath(startCell, candidate, _maxExpand, tmp);

        if (!ok)
        {
            return false;
        }

        int distToTarget = Mathf.Abs(candidate.x - targetCell.x) + Mathf.Abs(candidate.y - targetCell.y);

        if (distToTarget < bestDist)
        {
            bestDist = distToTarget;
            best = candidate;

            if (bestDist == 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ForcedFallbackMove()
    {
        if (_target == null)
        {
            return;
        }

        Vector2 pos = _enemy.Rigidbody.position;
        Vector2 dir = ((Vector2)_target.position - pos).normalized;

        if (dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        LayerMask blockMask = Physics2D.GetLayerCollisionMask(gameObject.layer);

        RaycastHit2D hit = Physics2D.CircleCast(pos, _agentRadius, dir, _lookAhead, blockMask);

        if (hit.collider != null)
        {
            Vector2 slide = Vector2.Perpendicular(hit.normal).normalized;

            float a = Vector2.Dot(slide, dir);
            float b = Vector2.Dot(-slide, dir);

            if (b > a)
            {
                slide = -slide;
            }

            _enemy.ApplyExternalMove(slide, _fallbackSpeedMultiplier);
            return;
        }

        _enemy.ApplyExternalMove(dir, _fallbackSpeedMultiplier);
    }
}
