using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace RuntimeModelLoaders.Loaders
{
    /// <summary>
    /// Minimalistischer OBJ-Importer, der Vertices, Normals, UVs und Face-Indizes einliest
    /// und ein Unity-Mesh erzeugt. Unterstützt triangulierte und polygonale Faces
    /// (Polygon-Fans) sowie Indexformate v/vt/vn oder v//vn.
    /// </summary>
    public static class ObjImporter
    {
        private struct ObjIndex : IEquatable<ObjIndex>
        {
            public int v;
            public int vt;
            public int vn;

            public bool Equals(ObjIndex other) => v == other.v && vt == other.vt && vn == other.vn;
            public override int GetHashCode() => HashCode.Combine(v, vt, vn);
        }

        public static Mesh Parse(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"OBJ-Datei nicht gefunden: {path}");
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            var meshVertices = new List<Vector3>();
            var meshNormals = new List<Vector3>();
            var meshUVs = new List<Vector2>();
            var meshTriangles = new List<int>();
            var indexMap = new Dictionary<ObjIndex, int>();

            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                switch (parts[0])
                {
                    case "v":
                        vertices.Add(ParseVector3(parts));
                        break;
                    case "vn":
                        normals.Add(ParseVector3(parts));
                        break;
                    case "vt":
                        uvs.Add(ParseVector2(parts));
                        break;
                    case "f":
                        AddFace(parts, vertices, uvs, normals, meshVertices, meshUVs, meshNormals, meshTriangles, indexMap);
                        break;
                }
            }

            var mesh = new Mesh
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            mesh.SetVertices(meshVertices);
            mesh.SetTriangles(meshTriangles, 0);

            if (meshUVs.Count == meshVertices.Count)
                mesh.SetUVs(0, meshUVs);

            if (meshNormals.Count == meshVertices.Count)
                mesh.SetNormals(meshNormals);
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(
            IReadOnlyList<string> parts,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<Vector2> uvs,
            IReadOnlyList<Vector3> normals,
            List<Vector3> meshVertices,
            List<Vector2> meshUVs,
            List<Vector3> meshNormals,
            List<int> meshTriangles,
            Dictionary<ObjIndex, int> indexMap)
        {
            // parts[0] ist "f"
            var faceIndices = new List<int>();
            for (var i = 1; i < parts.Count; i++)
            {
                var token = parts[i];
                var indices = token.Split('/');

                var objIndex = new ObjIndex
                {
                    v = ParseIndex(indices, 0, vertices.Count),
                    vt = ParseIndex(indices, 1, uvs.Count),
                    vn = ParseIndex(indices, 2, normals.Count)
                };

                if (!indexMap.TryGetValue(objIndex, out var meshIndex))
                {
                    meshIndex = meshVertices.Count;
                    indexMap.Add(objIndex, meshIndex);
                    meshVertices.Add(vertices[objIndex.v]);

                    if (objIndex.vt >= 0)
                        meshUVs.Add(uvs[objIndex.vt]);
                    if (objIndex.vn >= 0)
                        meshNormals.Add(normals[objIndex.vn]);
                }

                faceIndices.Add(meshIndex);
            }

            // Triangulation per Fan: (0, i, i+1)
            for (var i = 1; i < faceIndices.Count - 1; i++)
            {
                meshTriangles.Add(faceIndices[0]);
                meshTriangles.Add(faceIndices[i]);
                meshTriangles.Add(faceIndices[i + 1]);
            }
        }

        private static int ParseIndex(IReadOnlyList<string> indices, int position, int count)
        {
            if (indices.Count <= position || string.IsNullOrEmpty(indices[position]))
                return -1;

            var idx = int.Parse(indices[position], CultureInfo.InvariantCulture);
            if (idx < 0)
                idx = count + idx; // negative Indizes sind relativ zum Listenende
            else
                idx -= 1; // OBJ ist 1-basiert

            return Mathf.Clamp(idx, 0, count - 1);
        }

        private static Vector3 ParseVector3(IReadOnlyList<string> parts)
        {
            return new Vector3(
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture));
        }

        private static Vector2 ParseVector2(IReadOnlyList<string> parts)
        {
            return new Vector2(
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }
}
