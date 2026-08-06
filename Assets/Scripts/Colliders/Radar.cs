using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class Radar : MonoBehaviour
{

    [Header("Detection")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers;            // What blocks vision
    [SerializeField] private string targetTag = "Player";      // Leave empty to detect all

    [Header("Events")]
    public DroneDetectEvent OnObjectDetected = new DroneDetectEvent();  // Fires when an object enters (and is visible)
    //public DroneDetectEvent OnObjectLost = new DroneDetectEvent();

    [Header("Index")]
    public RadarIndexEnum index;

    // Read from other scripts: which objects are currently visible
    public IReadOnlyCollection<GameObject> VisibleObjects => _visibleObjects;

    private readonly HashSet<GameObject> _visibleObjects = new HashSet<GameObject>();
    private readonly HashSet<GameObject> _objectsInVolume = new HashSet<GameObject>();

    // ─────────────────────────────────────────────────────────────
    //  Trigger Callbacks
    // ─────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!MatchesTarget(other)) return;

        _objectsInVolume.Add(other.gameObject);

        // Spot immediately unless we need a clear line of sight
        if (!requireLineOfSight || HasLineOfSight(other))
            Spot(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        // Only needed for dynamic LOS — skip if LOS is disabled
        if (!requireLineOfSight) return;
        if (!MatchesTarget(other)) return;

        bool canSee = HasLineOfSight(other);
        bool isSpotted = _visibleObjects.Contains(other.gameObject);

        if (canSee && !isSpotted) Spot(other.gameObject);
        if (!canSee && isSpotted) Lose(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!MatchesTarget(other)) return;

        _objectsInVolume.Remove(other.gameObject);

        if (_visibleObjects.Contains(other.gameObject))
            Lose(other.gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private bool MatchesTarget(Collider other) =>
        string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag);

    private bool HasLineOfSight(Collider target)
    {
        Vector3 origin = transform.position;
        Vector3 targetPos = target.bounds.center;
        Vector3 direction = targetPos - origin;
        float distance = direction.magnitude;

        // Use all layers if none are specified
        int mask = obstacleLayers == 0 ? ~0 : (int)obstacleLayers;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, mask))
        {
            // LOS is clear only if the first thing we hit IS our target (or its child)
            return hit.collider == target ||
                   hit.collider.transform.IsChildOf(target.transform);
        }

        return true; // Nothing blocked the ray → target is visible
    }

    private void Spot(GameObject obj)
    {
        if (_visibleObjects.Add(obj)) // Add returns false if already present
        {
            OnObjectDetected?.Invoke(DroneDetectEvent.GetFromTransform((uint)index, obj.transform, this.transform));
        }
    }

    private void Lose(GameObject obj)
    {
        if (_visibleObjects.Remove(obj))
        {
            //OnObjectLost?.Invoke(DroneDetectEvent.GetFromTransform((uint)index, obj.transform, this.transform));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Gizmos — visualise the FOV in the Scene view
    // ─────────────────────────────────────────────────────────────

    public void OnDrawGizmos()
    {
        MeshCollider col = GetComponent<MeshCollider>();
        if (col == null || col.sharedMesh == null) return;

        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;

        Mesh mesh = col.sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        HashSet<(int, int)> drawnEdges = new HashSet<(int, int)>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            DrawEdge(triangles[i], triangles[i + 1], vertices, drawnEdges);
            DrawEdge(triangles[i + 1], triangles[i + 2], vertices, drawnEdges);
            DrawEdge(triangles[i + 2], triangles[i], vertices, drawnEdges);
        }

        // ✅ always reset after drawing
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }

    private void DrawEdge(int a, int b, Vector3[] vertices, HashSet<(int, int)> drawn)
    {
        // always store the smaller index first so (a,b) and (b,a) are the same edge
        (int, int) edge = a < b ? (a, b) : (b, a);

        if (drawn.Add(edge))
            Gizmos.DrawLine(vertices[a], vertices[b]);
    }
}
