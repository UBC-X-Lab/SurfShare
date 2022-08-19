using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DelaunatorSharp;

public static class MeshCreator
{
    public static Mesh CreateMesh(Vector2[] poly_vertices, float meshHeight = 0.02f)
    {
        IPoint[] points = DelaunatorSharp.Unity.Extensions.DelaunatorExtensions.ToPoints(poly_vertices);
        Delaunator delaunator = new Delaunator(points);

        Mesh mesh = new Mesh();

        Vector3[] mesh_vertices = new Vector3[poly_vertices.Length * 2];
        //Vector2[] meshUV = new Vector2[poly_vertices.Length];

        // assign poly_vertices for bottom and top
        for (int i = 0; i < mesh_vertices.Length; i++)
        {
            int poly_vertices_index = i % poly_vertices.Length;
            float height = i < poly_vertices.Length ? 0 : meshHeight;
            mesh_vertices[i] = new Vector3(poly_vertices[poly_vertices_index].x, height, poly_vertices[poly_vertices_index].y);
            Debug.Log(mesh_vertices[i].x + "," + mesh_vertices[i].y + "," + mesh_vertices[i].x);
        }

        mesh.vertices = mesh_vertices;
        //mesh.uv = meshUV;

        int[] meshTriangle = new int[delaunator.Triangles.Length * 2 + 6 * poly_vertices.Length]; // make room for triangles for the top/bottom face + 6 triangle vertice each side
        // assign the bottom, as is
        for (int i = 0; i < delaunator.Triangles.Length; i++)
        {
            meshTriangle[i] = delaunator.Triangles[delaunator.Triangles.Length - 1 - i];
        }
        //assign the top, but offsetting
        for (int i = delaunator.Triangles.Length; i < delaunator.Triangles.Length * 2; i++)
        {
            meshTriangle[i] = delaunator.Triangles[i - delaunator.Triangles.Length] + poly_vertices.Length;
        }
        // assign the side
        for (int i = 0; i < poly_vertices.Length; i++)
        {
            int start = delaunator.Triangles.Length * 2 + i * 6; // start index in meshTriangle
            // i, i + poly_vertices.Length, i + 1 + poly_vertices.Length, i + 1;

            // to face outward, reverse
            meshTriangle[start] = (i + 1) % poly_vertices.Length + poly_vertices.Length; // 2
            meshTriangle[start + 1] = i + poly_vertices.Length; // 1
            meshTriangle[start + 2] = i; // 0
            meshTriangle[start + 3] = i; // 0
            meshTriangle[start + 4] = (i + 1) % poly_vertices.Length; // 3
            meshTriangle[start + 5] = (i + 1) % poly_vertices.Length + poly_vertices.Length; // 2
        }

        mesh.triangles = meshTriangle;
        mesh.RecalculateNormals();
        mesh.Optimize();

        return mesh;
    }

    public static void UpdateMeshHeight(Mesh mesh, float height)
    {
        Vector3[] new_vertices = mesh.vertices;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            if (new_vertices[i].y != 0)
            {
                new_vertices[i].y = height;
            }
        }
        mesh.vertices = new_vertices;
    }

    public static void SkewMesh(Mesh mesh)
    {
        Vector3[] new_vertices = mesh.vertices;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            if (new_vertices[i].y != 0)
            {
                new_vertices[i] += new Vector3(0.001f, 0, 0);
            }
        }
        mesh.vertices = new_vertices;
    }
}
