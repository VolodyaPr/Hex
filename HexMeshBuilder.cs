using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMeshBuilder : MonoBehaviour
{
    [SerializeField] private float radius = 0.9f;
    [SerializeField] private float height = 0.2f;

    void Awake()
    {
        BuildHexMesh();
    }

    void BuildHexMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "HexMesh";

        Vector3[] vertices = new Vector3[14];
        int[] triangles = new int[72];

        vertices[12] = new Vector3(0, height / 2f, 0);
        vertices[13] = new Vector3(0, -height / 2f, 0);

        for (int i = 0; i < 6; i++)
        {
            float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            vertices[i] = new Vector3(x, height / 2f, z);
            vertices[i + 6] = new Vector3(x, -height / 2f, z);
        }

        int triIndex = 0;

        for (int i = 0; i < 6; i++)
        {
            triangles[triIndex++] = 12;
            triangles[triIndex++] = (i + 1) % 6;
            triangles[triIndex++] = i;
        }

        for (int i = 0; i < 6; i++)
        {
            triangles[triIndex++] = 13;
            triangles[triIndex++] = i + 6;
            triangles[triIndex++] = ((i + 1) % 6) + 6;
        }

        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            triangles[triIndex++] = i;
            triangles[triIndex++] = next;
            triangles[triIndex++] = i + 6;

            triangles[triIndex++] = next;
            triangles[triIndex++] = next + 6;
            triangles[triIndex++] = i + 6;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<MeshCollider>();
        }
        collider.sharedMesh = mesh;
    }
}
