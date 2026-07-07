using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ArrowEmitter : MonoBehaviour
{
    [Header("Trap Tiles")]
    [SerializeField] TileBase leftTrapTile;
    [SerializeField] TileBase rightTrapTile;

    [Header("Arrow")]
    [SerializeField] GameObject arrowPrefab;
    [SerializeField] float spawnOffset = 0.4f;
    [SerializeField] float fireInterval = 2f;

    Tilemap _tilemap;
    readonly List<TrapSpawn> _traps = new();
    Coroutine _fireRoutine;

    struct TrapSpawn
    {
        public Vector3 Position;
        public Vector2 Direction;
    }

    void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    void OnEnable()
    {
        CacheTrapCells();
        _fireRoutine = StartCoroutine(FireLoop());
    }

    void OnDisable()
    {
        if (_fireRoutine != null)
        {
            StopCoroutine(_fireRoutine);
            _fireRoutine = null;
        }
    }

    void CacheTrapCells()
    {
        _traps.Clear();

        if (_tilemap == null)
            return;

        BoundsInt bounds = _tilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = _tilemap.GetTile(cell);
            if (tile == null)
                continue;

            Vector2 direction = GetDirection(tile);
            if (direction == Vector2.zero)
                continue;

            _traps.Add(new TrapSpawn
            {
                Position = _tilemap.GetCellCenterWorld(cell),
                Direction = direction
            });
        }
    }

    Vector2 GetDirection(TileBase tile)
    {
        if (tile == leftTrapTile)
            return Vector2.left;

        if (tile == rightTrapTile)
            return Vector2.right;

        return Vector2.zero;
    }

    IEnumerator FireLoop()
    {
        yield return new WaitForSeconds(2f);

        var wait = new WaitForSeconds(fireInterval);

        while (true)
        {
            SpawnAll();
            yield return wait;
        }
    }

    void SpawnAll()
    {
        if (arrowPrefab == null)
            return;

        foreach (TrapSpawn trap in _traps)
        {
            Vector3 spawnPos = trap.Position + (Vector3)(trap.Direction * spawnOffset);
            GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            arrow.GetComponent<ArrowProjectile>().Launch(trap.Direction);
        }
    }
}
