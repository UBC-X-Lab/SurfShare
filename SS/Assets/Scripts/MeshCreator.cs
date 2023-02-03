using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DelaunatorSharp;

using TriangleNet.Geometry;
using TriangleNet.Meshing;

public static class MeshCreator
{
    public static Mesh CreateMesh(Vector2[] poly_vertices, int[] vertices_count, Vector3[] world_vertices, Vector3 heightNormal, float meshHeight = 0.01f)
    {
        int[] delaunay_triangles;

        var polygon = new Polygon();

        int index = 0;
        bool is_hole = false;
        foreach (int count in vertices_count)
        {
            Vertex[] vertices = new Vertex[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vertex(poly_vertices[index].x, poly_vertices[index].y);
                index++;
            }
            polygon.Add(new Contour(vertices), is_hole);
            is_hole = true;
        }

        // int[] delaunay_triangles;
        IMesh meshTemp = polygon.Triangulate();

        delaunay_triangles = new int[meshTemp.Triangles.Count * 3];
        index = 0;
        foreach (TriangleNet.Topology.Triangle t in meshTemp.Triangles)
        {
            delaunay_triangles[index * 3 + 2] = t.GetVertexID(0);
            delaunay_triangles[index * 3 + 1] = t.GetVertexID(1);
            delaunay_triangles[index * 3] = t.GetVertexID(2);
            index++;
        }

        Debug.Log("New Triangulation Success!");

        Mesh mesh = new Mesh();

        Vector3[] mesh_vertices = new Vector3[poly_vertices.Length * 2];
        //Vector2[] meshUV = new Vector2[poly_vertices.Length];

        // assign poly_vertices for bottom and top
        for (int i = 0; i < mesh_vertices.Length; i++)
        {
            int poly_vertices_index = i % poly_vertices.Length;
            mesh_vertices[i] = world_vertices[poly_vertices_index]; // offset to counter HL intrinsic errors
            if (i >= poly_vertices.Length)
            {
                mesh_vertices[i] += heightNormal * meshHeight;
            }
            // Debug.Log(mesh_vertices[i].x + "," + mesh_vertices[i].y + "," + mesh_vertices[i].z);
        }

        mesh.vertices = mesh_vertices;
        //mesh.uv = meshUV;

        int[] meshTriangle = new int[delaunay_triangles.Length * 2 + 6 * poly_vertices.Length]; // make room for triangles for the top/bottom face + 6 triangle vertice each side
        // assign the bottom, as is
        for (int i = 0; i < delaunay_triangles.Length; i++)
        {
            meshTriangle[i] = delaunay_triangles[delaunay_triangles.Length - 1 - i];
        }
        //assign the top, but offsetting
        for (int i = delaunay_triangles.Length; i < delaunay_triangles.Length * 2; i++)
        {
            meshTriangle[i] = delaunay_triangles[i - delaunay_triangles.Length] + poly_vertices.Length;
        }
        // assign the side
        int vertice_start = 0;
        for (int i = 0; i < vertices_count.Length; i++)
        {
            for (int j = 0; j < vertices_count[i]; j++)
            {
                int start = delaunay_triangles.Length * 2 + (vertice_start + j) * 6;
                meshTriangle[start] = vertice_start + (j + 1) % vertices_count[i] + poly_vertices.Length; // 2
                meshTriangle[start + 1] = vertice_start + j + poly_vertices.Length; // 1
                meshTriangle[start + 2] = vertice_start + j; // 0
                meshTriangle[start + 3] = vertice_start + j; // 0
                meshTriangle[start + 4] = vertice_start + (j + 1) % vertices_count[i]; // 3
                meshTriangle[start + 5] = vertice_start + (j + 1) % vertices_count[i] + poly_vertices.Length; // 2
            }
            vertice_start += vertices_count[i];
        }

        //for (int i = 0; i < poly_vertices.Length; i++)
        //{
        //    int start = delaunay_triangles.Length * 2 + i * 6; // start index in meshTriangle
        //    // i, i + poly_vertices.Length, i + 1 + poly_vertices.Length, i + 1;

        //    // to face outward, reverse
        //    meshTriangle[start] = (i + 1) % poly_vertices.Length + poly_vertices.Length; // 2
        //    meshTriangle[start + 1] = i + poly_vertices.Length; // 1
        //    meshTriangle[start + 2] = i; // 0
        //    meshTriangle[start + 3] = i; // 0
        //    meshTriangle[start + 4] = (i + 1) % poly_vertices.Length; // 3
        //    meshTriangle[start + 5] = (i + 1) % poly_vertices.Length + poly_vertices.Length; // 2
        //}

        mesh.triangles = meshTriangle;
        mesh.RecalculateNormals();
        mesh.Optimize();

        return mesh;
    }
}
