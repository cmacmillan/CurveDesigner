using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace ChaseMacMillan.CurveDesigner
{
#if UNITY_EDITOR
    ///This is from https://wiki.unity3d.com/index.php/ExportOBJ
    public static class ObjMeshExporter
    {
        public static void DoExport(GameObject gameObject, bool makeSubmeshes)
        {
            string meshName = gameObject.name;
            string fileName = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");

            ObjExporterScript.Start();

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                                + "\n#" + System.DateTime.Now.ToLongDateString()
                                + "\n#" + System.DateTime.Now.ToLongTimeString()
                                + "\n#-------"
                                + "\n\n");

            Transform t = gameObject.transform;

            Vector3 originalPosition = t.position;
            t.position = Vector3.zero;

            if (!makeSubmeshes)
            {
                meshString.Append("g ").Append(t.name).Append("\n");
            }
            meshString.Append(processTransform(t, makeSubmeshes));

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                WriteToFile(meshString.ToString(), fileName);
                Debug.Log("Exported Mesh: " + fileName);
            }
            else
            {
                Debug.Log("Mesh export cancelled");
            }

            t.position = originalPosition;

            ObjExporterScript.End();
        }

        static string processTransform(Transform t, bool makeSubmeshes)
        {
            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + t.name
                            + "\n#-------"
                            + "\n");

            if (makeSubmeshes)
            {
                meshString.Append("g ").Append(t.name).Append("\n");
            }

            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf)
            {
                meshString.Append(ObjExporterScript.MeshToString(mf, t));
            }

            for (int i = 0; i < t.childCount; i++)
            {
                meshString.Append(processTransform(t.GetChild(i), makeSubmeshes));
            }

            return meshString.ToString();
        }

        static void WriteToFile(string s, string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.Write(s);
            }
        }

        private static class ObjExporterScript
        {
            private static int StartIndex = 0;

            public static void Start()
            {
                StartIndex = 0;
            }
            public static void End()
            {
                StartIndex = 0;
            }

            public static string MeshToString(MeshFilter mf, Transform t)
            {
                Vector3 s = t.localScale;
                Vector3 p = t.localPosition;
                Quaternion r = t.localRotation;

                int numVertices = 0;
                Mesh m = mf.sharedMesh;
                if (!m)
                {
                    return "####Error####";
                }
                Material[] mats = mf.GetComponent<MeshRenderer>().sharedMaterials;

                StringBuilder sb = new StringBuilder();

                foreach (Vector3 vv in m.vertices)
                {
                    Vector3 v = t.TransformPoint(vv);
                    numVertices++;
                    sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
                }
                sb.Append("\n");
                foreach (Vector3 nn in m.normals)
                {
                    Vector3 v = r * nn;
                    sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, -v.y, v.z));
                }
                sb.Append("\n");
                foreach (Vector3 v in m.uv)
                {
                    sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
                }
                for (int material = 0; material < m.subMeshCount; material++)
                {
                    sb.Append("\n");
                    sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                    sb.Append("usemap ").Append(mats[material].name).Append("\n");

                    int[] triangles = m.GetTriangles(material);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                            triangles[i] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex, triangles[i + 2] + 1 + StartIndex));
                    }
                }

                StartIndex += numVertices;
                return sb.ToString();
            }
        }
    }
#endif
}
