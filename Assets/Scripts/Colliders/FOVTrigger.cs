using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class FOVTrigger : MonoBehaviour
{
    [Header("FOV Shape")]
    [SerializeField] private float nearWidth = 0.2f;   // Small opening at camera origin
    [SerializeField] private float nearHeight = 0.15f;
    [SerializeField] private float farWidth = 8f;     // Wide end at max range
    [SerializeField] private float farHeight = 6f;
    [SerializeField] private float range = 10f;    // How far the FOV reaches

    [Header("Detection")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers;            // What blocks vision
    [SerializeField] private string targetTag = "Player";      // Leave empty to detect all

    [Header("Events")]
    public UnityEvent<GameObject> OnObjectSpotted;  // Fires when an object enters (and is visible)
    public UnityEvent<GameObject> OnObjectLost;     // Fires when an object leaves or is occluded

    // Read from other scripts: which objects are currently visible
    public IReadOnlyCollection<GameObject> VisibleObjects => _visibleObjects;

    private readonly HashSet<GameObject> _visibleObjects = new HashSet<GameObject>();
    private readonly HashSet<GameObject> _objectsInVolume = new HashSet<GameObject>();

    // ─────────────────────────────────────────────────────────────
    //  Collider Generation
    // ─────────────────────────────────────────────────────────────

    [ContextMenu("Generate FOV Collider")]
    public void GenerateCollider()
    {
        MeshCollider mc = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = CreateFOVMesh();
        mc.convex = true;   // Required for trigger physics
        mc.isTrigger = true;   // Makes it a trigger, not a solid wall
    }

    private Mesh CreateFOVMesh()
    {
        Mesh mesh = new Mesh { name = "FOVMesh" };

        float hnw = nearWidth / 2f;
        float hnh = nearHeight / 2f;
        float hfw = farWidth / 2f;
        float hfh = farHeight / 2f;

        // +Z = forward (the direction the "camera" is looking)
        // Near face sits at z=0 (camera origin), far face at z=range
        Vector3[] vertices = new Vector3[]
        {
            // Near face — small square at z = 0
            new Vector3(-hnw, -hnh, 0),      // 0: Near Bottom-Left
            new Vector3( hnw, -hnh, 0),      // 1: Near Bottom-Right
            new Vector3( hnw,  hnh, 0),      // 2: Near Top-Right
            new Vector3(-hnw,  hnh, 0),      // 3: Near Top-Left

            // Far face — large square at z = range
            new Vector3(-hfw, -hfh, range),  // 4: Far Bottom-Left
            new Vector3( hfw, -hfh, range),  // 5: Far Bottom-Right
            new Vector3( hfw,  hfh, range),  // 6: Far Top-Right
            new Vector3(-hfw,  hfh, range),  // 7: Far Top-Left
        };

        int[] triangles = new int[]
        {
            // Near face (normal faces -Z, back toward the camera)
            0, 3, 2,   0, 2, 1,
            // Far face (normal faces +Z, forward)
            4, 5, 6,   4, 6, 7,
            // Bottom face
            0, 1, 5,   0, 5, 4,
            // Top face
            3, 7, 6,   3, 6, 2,
            // Left face
            0, 4, 7,   0, 7, 3,
            // Right face
            1, 2, 6,   1, 6, 5,
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

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
            Debug.Log($"[FOV] Spotted: {obj.name}");
            OnObjectSpotted?.Invoke(obj);
        }
    }

    private void Lose(GameObject obj)
    {
        if (_visibleObjects.Remove(obj))
        {
            Debug.Log($"[FOV] Lost sight of: {obj.name}");
            OnObjectLost?.Invoke(obj);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Gizmos — visualise the FOV in the Scene view
    // ─────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (TryGetComponent<MeshCollider>(out MeshCollider mc) && mc.sharedMesh != null)
        {
            // Cyan = nothing spotted, Red = target(s) detected
            Gizmos.color = _visibleObjects.Count > 0 ? Color.red : Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(mc.sharedMesh);
        }

        // Draw a yellow ray to each currently-visible object
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        foreach (GameObject obj in _visibleObjects)
            if (obj != null)
                Gizmos.DrawLine(transform.position, obj.transform.position);
    }
}
