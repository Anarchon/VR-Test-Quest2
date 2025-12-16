using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RuntimeModelLoaders.Loaders
{
    /// <summary>
    /// Liest ASCII- oder Binary-STL-Dateien und erzeugt ein Unity-Mesh.
    /// Triangle-Winding bleibt unverändert, Normals werden aus der Datei übernommen
    /// oder bei Bedarf neu berechnet.
    /// </summary>
    public static class StlImporter
    {
        public static Mesh Parse(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"STL-Datei nicht gefunden: {path}");

            // Heuristik: ASCII beginnt häufig mit "solid" und ist größer/kleiner als binary-Länge.
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            var isAscii = LooksLikeAscii(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return isAscii ? ParseAscii(stream, path) : ParseBinary(stream, path);
        }

        private static Mesh ParseAscii(Stream stream, string path)
        {
            var reader = new StreamReader(
                stream,
                Encoding.ASCII,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            string? line;
            Vector3 currentNormal = Vector3.zero;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("facet normal", StringComparison.OrdinalIgnoreCase))
                {
                    currentNormal = ParseVector3FromLine(trimmed.Replace("facet normal", string.Empty));
                }
                else if (trimmed.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                {
                    var vertex = ParseVector3FromLine(trimmed.Replace("vertex", string.Empty));
                    vertices.Add(vertex);
                    normals.Add(currentNormal);
                }
                else if (trimmed.StartsWith("endfacet", StringComparison.OrdinalIgnoreCase))
                {
                    var count = vertices.Count;
                    triangles.Add(count - 3);
                    triangles.Add(count - 2);
                    triangles.Add(count - 1);
                }
            }

            return BuildMesh(path, vertices, normals, triangles);
        }

        private static Mesh ParseBinary(Stream stream, string path)
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
            reader.ReadBytes(80); // Header
            var triangleCount = reader.ReadUInt32();

            var vertices = new List<Vector3>((int)triangleCount * 3);
            var normals = new List<Vector3>((int)triangleCount * 3);
            var triangles = new List<int>((int)triangleCount * 3);

            for (var i = 0; i < triangleCount; i++)
            {
                var normal = ReadVector3(reader);
                var v1 = ReadVector3(reader);
                var v2 = ReadVector3(reader);
                var v3 = ReadVector3(reader);

                var baseIndex = vertices.Count;
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                reader.ReadUInt16(); // attribute byte count
            }

            return BuildMesh(path, vertices, normals, triangles);
        }

        private static Mesh BuildMesh(string path, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
        {
            var mesh = new Mesh
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            if (normals.Count == vertices.Count)
                mesh.SetNormals(normals);
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 ParseVector3FromLine(string line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            return new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
        }

        private static bool LooksLikeAscii(Stream stream)
        {
            var buffer = new byte[Math.Min(stream.Length, 1024)];
            stream.Read(buffer, 0, buffer.Length);
            var header = Encoding.ASCII.GetString(buffer).TrimStart();
            if (header.StartsWith("solid", StringComparison.OrdinalIgnoreCase))
            {
                // Binary-Dateien können ebenfalls mit "solid" beginnen; validieren auf Nicht-Druckzeichen
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == 0)
                        return false;
                }
                return true;
            }

            return false;
        }
    }
}
