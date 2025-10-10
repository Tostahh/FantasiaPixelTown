using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapPathfinder : MonoBehaviour
{
    public Tilemap walkableTilemap;
    public Tilemap obstacleTilemap;

    private Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1,0,0),
        new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0),
        new Vector3Int(0,-1,0)
    };

    public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
    {
        Vector3Int start = walkableTilemap.WorldToCell(startWorld);
        Vector3Int target = walkableTilemap.WorldToCell(targetWorld);

        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        Node startNode = new Node(start, null, 0, GetHeuristic(start, target));
        openSet.Enqueue(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue();
            if (current.position == target)
                return RetracePath(current);

            closedSet.Add(current.position);

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current.position + dir;
                if (closedSet.Contains(neighbor) || !IsWalkable(neighbor)) continue;

                float gCost = current.gCost + 1;
                float hCost = GetHeuristic(neighbor, target);
                Node neighborNode = new Node(neighbor, current, gCost, hCost);
                openSet.Enqueue(neighborNode);
            }
        }

        return null; // no path found
    }

    public Vector3 GetRandomWalkableWorldPosition()
    {
        if (walkableTilemap == null) return Vector3.zero;
        BoundsInt bounds = walkableTilemap.cellBounds;
        List<Vector3Int> walkableTiles = new List<Vector3Int>();

        foreach (Vector3Int pos in bounds.allPositionsWithin)
            if (IsWalkable(pos))
                walkableTiles.Add(pos);

        if (walkableTiles.Count == 0) return Vector3.zero;

        Vector3Int randomTile = walkableTiles[Random.Range(0, walkableTiles.Count)];
        return walkableTilemap.GetCellCenterWorld(randomTile);
    }

    // --- NEW: Random walkable tile near a center ---
    public Vector3 GetRandomWalkableWorldPositionNear(Vector3 center, int radius)
    {
        Vector3Int centerCell = walkableTilemap.WorldToCell(center);
        List<Vector3Int> candidates = new List<Vector3Int>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cell = centerCell + new Vector3Int(x, y, 0);
                if (IsWalkable(cell))
                    candidates.Add(cell);
            }
        }

        if (candidates.Count == 0) return Vector3.zero;
        Vector3Int chosenCell = candidates[Random.Range(0, candidates.Count)];
        return walkableTilemap.GetCellCenterWorld(chosenCell);
    }

    public Vector3 GetClosestWalkableTile(Vector3 fromWorld)
    {
        Vector3Int fromCell = walkableTilemap.WorldToCell(fromWorld);

        // Search expanding radius until walkable tile is found
        int radius = 0;
        while (radius < 20) // max radius
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int cell = fromCell + new Vector3Int(x, y, 0);
                    if (IsWalkable(cell))
                        return walkableTilemap.GetCellCenterWorld(cell);
                }
            }
            radius++;
        }

        return fromWorld; // fallback
    }

    private float GetHeuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector3> RetracePath(Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(walkableTilemap.GetCellCenterWorld(current.position));
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private bool IsWalkable(Vector3Int pos)
    {
        return walkableTilemap.HasTile(pos) && !obstacleTilemap.HasTile(pos);
    }

    public bool IsWalkable(Vector3 worldPos)
    {
        Vector3Int cell = walkableTilemap.WorldToCell(worldPos);
        return IsWalkable(cell);
    }

    private class Node : System.IComparable<Node>
    {
        public Vector3Int position;
        public Node parent;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;

        public Node(Vector3Int pos, Node parent, float g, float h)
        {
            position = pos;
            this.parent = parent;
            gCost = g;
            hCost = h;
        }

        public int CompareTo(Node other)
        {
            return fCost.CompareTo(other.fCost);
        }
    }

    private class PriorityQueue<T> where T : System.IComparable<T>
    {
        private List<T> data = new List<T>();
        public int Count => data.Count;

        public void Enqueue(T item)
        {
            data.Add(item);
            int ci = data.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (data[ci].CompareTo(data[pi]) >= 0) break;
                (data[ci], data[pi]) = (data[pi], data[ci]);
                ci = pi;
            }
        }

        public T Dequeue()
        {
            int li = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[li];
            data.RemoveAt(li);
            li--;
            int pi = 0;

            while (true)
            {
                int ci = pi * 2 + 1;
                if (ci > li) break;
                int rc = ci + 1;
                if (rc <= li && data[rc].CompareTo(data[ci]) < 0) ci = rc;
                if (data[pi].CompareTo(data[ci]) <= 0) break;
                (data[pi], data[ci]) = (data[ci], data[pi]);
                pi = ci;
            }

            return frontItem;
        }
    }
}
