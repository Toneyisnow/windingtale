using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Rendering;

namespace WindingTale.FightObjects
{
    /// <summary>
    /// The meshes of one creature's fight frames, read from the Fight_NNN_mesh.bytes that
    /// Tools/Vox_to_Obj/obj_to_fightpack.py packs the frame OBJs into.
    ///
    /// Consecutive frames are mostly the same drawing, so the pack stores every distinct
    /// triangle once, grouped by which frames use it, and each frame is a list of groups;
    /// see the script for the byte layout. Meshes come out in the OBJ's own coordinates,
    /// which FightModel3D.MeasureAxes sorts out like it does for the imported OBJs.
    ///
    /// Packs are shared between the bodies showing the same creature and freed when the
    /// last of them lets go (Acquire / Release).
    /// </summary>
    public class FightMeshPack
    {
        private const uint Magic = 0x4D465457;   // "WTFM"
        private const int Version = 1;

        private static readonly Dictionary<int, FightMeshPack> loaded = new Dictionary<int, FightMeshPack>();

        private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private int users;

        public Texture Palette { get; private set; }

        /// <summary>The pack of <paramref name="animationId"/>, or null when it has none.</summary>
        public static FightMeshPack Acquire(int animationId, string folder, string digits)
        {
            FightMeshPack pack;
            if (!loaded.TryGetValue(animationId, out pack) || !pack.IsAlive())
            {
                TextAsset bytes = Resources.Load<TextAsset>(folder + "Fight_" + digits + "_mesh");
                if (bytes == null)
                {
                    return null;
                }
                pack = new FightMeshPack();
                try
                {
                    pack.Read(bytes.bytes);
                }
                catch (Exception e)
                {
                    Debug.LogError("[FightMeshPack] cannot read " + folder + ": " + e.Message);
                    pack.Destroy();
                    return null;
                }
                finally
                {
                    Resources.UnloadAsset(bytes);
                }
                pack.Palette = Resources.Load<Texture2D>(folder + "Fight_" + digits + "_palette");
                loaded[animationId] = pack;
            }
            pack.users++;
            return pack;
        }

        public static void Release(int animationId, FightMeshPack pack)
        {
            if (pack == null || --pack.users > 0)
            {
                return;
            }
            pack.Destroy();
            FightMeshPack current;
            if (loaded.TryGetValue(animationId, out current) && current == pack)
            {
                loaded.Remove(animationId);
            }
        }

        /// <summary>The mesh of a frame model (OBJ file name, with or without extension).</summary>
        public Mesh Get(string file)
        {
            Mesh mesh;
            meshes.TryGetValue(Path.GetFileNameWithoutExtension(file), out mesh);
            return mesh;
        }

        private bool IsAlive()
        {
            foreach (Mesh mesh in meshes.Values)
            {
                return mesh != null;
            }
            return true;
        }

        private void Destroy()
        {
            foreach (Mesh mesh in meshes.Values)
            {
                if (mesh == null)
                {
                    continue;
                }
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
            meshes.Clear();
        }

        private void Read(byte[] compressed)
        {
            byte[] data;
            using (MemoryStream input = new MemoryStream(compressed))
            using (DeflateStream inflate = new DeflateStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream(compressed.Length * 8))
            {
                inflate.CopyTo(output);
                data = output.ToArray();
            }

            int pos = 0;
            if (ReadU32(data, ref pos) != Magic || ReadU16(data, ref pos) != Version)
            {
                throw new InvalidDataException("not a fight mesh pack");
            }

            int uvCount = ReadU16(data, ref pos);
            Vector2[] uvTable = new Vector2[uvCount];
            for (int i = 0; i < uvCount; i++)
            {
                uvTable[i] = new Vector2(ReadF32(data, ref pos), ReadF32(data, ref pos));
            }
            Vector3 min = new Vector3(ReadF32(data, ref pos), ReadF32(data, ref pos), ReadF32(data, ref pos));
            Vector3 step = new Vector3(ReadF32(data, ref pos), ReadF32(data, ref pos), ReadF32(data, ref pos));

            int vertexCount = (int)ReadU32(data, ref pos);
            Vector3[] positions = new Vector3[vertexCount];
            for (int axis = 0; axis < 3; axis++)
            {
                int q = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    q = (q + ReadU16(data, ref pos)) & 0xFFFF;
                    Vector3 p = positions[i];
                    p[axis] = min[axis] + q * step[axis];
                    positions[i] = p;
                }
            }
            Vector3[] normals = new Vector3[vertexCount];
            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3 n = normals[i];
                    n[axis] = (sbyte)data[pos++] / 127f;
                    normals[i] = n;
                }
            }
            for (int i = 0; i < vertexCount; i++)
            {
                normals[i].Normalize();
            }
            Vector2[] uvs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                int index = uvCount <= 256 ? data[pos++] : ReadU16(data, ref pos);
                uvs[i] = uvTable[index];
            }

            int triangleCount = (int)ReadU32(data, ref pos);
            int[] indices = new int[triangleCount * 3];
            int previous = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                uint z = ReadVarint(data, ref pos);
                previous += (int)(z >> 1) ^ -(int)(z & 1);
                indices[i] = previous;
            }

            int groupCount = (int)ReadU32(data, ref pos);
            int[] groupStart = new int[groupCount + 1];
            for (int g = 0; g < groupCount; g++)
            {
                groupStart[g + 1] = groupStart[g] + (int)ReadU32(data, ref pos);
            }

            // Per model: gather its groups' triangles, renumber the pool vertices they use.
            int[] local = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                local[i] = -1;
            }
            List<int> used = new List<int>();
            List<int> triangles = new List<int>();
            int modelCount = ReadU16(data, ref pos);
            for (int m = 0; m < modelCount; m++)
            {
                int nameLength = data[pos++];
                string name = System.Text.Encoding.UTF8.GetString(data, pos, nameLength);
                pos += nameLength;

                used.Clear();
                triangles.Clear();
                int refs = (int)ReadU32(data, ref pos);
                int group = 0;
                for (int r = 0; r < refs; r++)
                {
                    group += (int)ReadVarint(data, ref pos);
                    for (int i = groupStart[group] * 3; i < groupStart[group + 1] * 3; i++)
                    {
                        int v = indices[i];
                        if (local[v] < 0)
                        {
                            local[v] = used.Count;
                            used.Add(v);
                        }
                        triangles.Add(local[v]);
                    }
                }

                Vector3[] p = new Vector3[used.Count];
                Vector3[] n = new Vector3[used.Count];
                Vector2[] t = new Vector2[used.Count];
                for (int i = 0; i < used.Count; i++)
                {
                    int v = used[i];
                    p[i] = positions[v];
                    n[i] = normals[v];
                    t[i] = uvs[v];
                    local[v] = -1;
                }

                Mesh mesh = new Mesh();
                mesh.name = name;
                mesh.indexFormat = used.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.vertices = p;
                mesh.normals = n;
                mesh.uv = t;
                mesh.SetTriangles(triangles, 0, true);
                mesh.UploadMeshData(true);
                meshes[name] = mesh;
            }
        }

        private static int ReadU16(byte[] data, ref int pos)
        {
            int v = data[pos] | (data[pos + 1] << 8);
            pos += 2;
            return v;
        }

        private static uint ReadU32(byte[] data, ref int pos)
        {
            uint v = BitConverter.ToUInt32(data, pos);
            pos += 4;
            return v;
        }

        private static float ReadF32(byte[] data, ref int pos)
        {
            float v = BitConverter.ToSingle(data, pos);
            pos += 4;
            return v;
        }

        private static uint ReadVarint(byte[] data, ref int pos)
        {
            uint v = 0;
            int shift = 0;
            while (true)
            {
                byte b = data[pos++];
                v |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return v;
                }
                shift += 7;
            }
        }
    }
}
