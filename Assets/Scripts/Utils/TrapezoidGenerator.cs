using UnityEngine;

public class TrapezoidGenerator : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Collider Generation
    // ─────────────────────────────────────────────────────────────

    [SerializeField] private float nearWidth = 0.2f;   // Small opening at camera origin
    [SerializeField] private float nearHeight = 0.15f;
    [SerializeField] private float farWidth = 8f;     // Wide end at max range
    [SerializeField] private float farHeight = 6f;
    [SerializeField] private float range = 10f;    // How far the FOV reaches

    [ContextMenu("Generate FOV Collider")]
    public void GenerateCollider()
    {
        MeshCollider mc = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = CreateFOVMesh();
        mc.convex = true;   // Required for trigger physics
        mc.isTrigger = true;   // Makes it a trigger, not a solid wall
        this.transform.rotation = transform.parent.rotation;
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
}
