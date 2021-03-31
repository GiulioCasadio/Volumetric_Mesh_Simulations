using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TetMesh : AbstractMesh
{
    private Mesh unityMesh;
    private MeshFilter filter;
    private MeshRenderer meshRenderer;

    /*private static int count = 0;
    private static float sum = 0;*/
    private int isFell = 16;
    
    private void Init()
    {
        // Creo il primo figlio con la mesh completa
        GameObject firstSon = new GameObject("firstSon");
        firstSon.AddComponent<Rigidbody>().mass = 10;
        firstSon.transform.parent = this.gameObject.transform;
        firstSon.AddComponent<MeshSplitter>();

        visiblePolys = firstSon.GetComponent<MeshSplitter>().GetVisiblePolys();
        vertices = firstSon.GetComponent<MeshSplitter>().GetVertices();
        oldNewVertices = firstSon.GetComponent<MeshSplitter>().GetOldNewVertices();

        /*count++;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();*/

        LoadMesh();

        /*stopwatch.Stop();

        sum += stopwatch.Elapsed.Milliseconds;
        Text time = GameObject.Find("Time").GetComponent<Text>();
        time.text = "Time: " + sum/count;
        stopwatch.Reset();*/

        if (!meshLoaded) { return; }
        ComputeFaces();
        ComputeAdjacencies();

        // Setto posizione, rotazione e scala del nuovo oggetto
        firstSon.transform.SetPositionAndRotation(this.gameObject.transform.position, this.gameObject.transform.rotation);
        firstSon.transform.localScale = new Vector3(1, 1, 1);

        if (filter == null && meshRenderer == null)
        {
            filter = firstSon.AddComponent<MeshFilter>();
            meshRenderer = firstSon.AddComponent<MeshRenderer>();
            unityMesh = new Mesh();
            filter.mesh = unityMesh;
        }

        RenderSurface();
        meshRenderer.material = material == null ? new Material(Shader.Find("Diffuse")) : material;

        // Se viene lanciato da un cannone
        if (transform.parent!=null && transform.parent.name.Equals("SpawnPoint"))
        {
            firstSon.GetComponent<Rigidbody>().AddForce(transform.forward * transform.parent.GetComponent<FireMortar>().GetSpeed());
            firstSon.AddComponent<StillPosition>();

            switch (filePath)
            {
                case "Assets/Mesh/AppleLow.mesh":
                    firstSon.gameObject.transform.Rotate(new Vector3(270, 0, 0));
                    break;
                case "Assets/Mesh/PearLow.mesh":
                    firstSon.gameObject.transform.Rotate(new Vector3(180, 0, 0));
                    break;
                case "Assets/Mesh/BananaLow.mesh":
                    firstSon.gameObject.transform.Rotate(new Vector3(180, 0, 0));
                    break;
                case "Assets/Mesh/PumpkinLow.mesh":
                    firstSon.gameObject.transform.Rotate(new Vector3(90, 0, 0));
                    break;
            }
        } else if (SceneManager.GetActiveScene().name == "WallUndestroyable")
        {
            firstSon.GetComponent<Rigidbody>().AddForce(transform.forward * 15000);
            firstSon.GetComponent<Rigidbody>().mass = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!meshLoaded || filePath != _currentPath)
        {
            if (Application.isPlaying || (Application.isEditor && filePath != ""))
            {
                Init();
            }
        }
    }

    void ComputeFaces()
    {
        foreach(List<int> poly in polys)
        {
            faces.Add(new List<int>(new int[] {poly[0], poly[2], poly[1]}));
            faces.Add(new List<int>(new int[] { poly[0], poly[1], poly[3] }));
            faces.Add(new List<int>(new int[] { poly[1], poly[2], poly[3] }));
            faces.Add(new List<int>(new int[] { poly[0], poly[3], poly[2] }));

        }

        Debug.Log("num Faces " + faces.Count());

    }

    void ComputeAdjacencies() {
        Dictionary<int, int> supportDict = new Dictionary<int, int>();
        Dictionary<int, int> supportDictForFaces = new Dictionary<int, int>();
        adjP2P = new List<List<int>>();
        adjF2F = new List<List<int>>();
        for (int i=0; i<polys.Count(); i++)
        {
            adjP2P.Add(new List<int>());
        }
        for (int i = 0; i < faces.Count(); i++)
        {
            adjF2F.Add(new List<int>());
        }

        int f = 0;
        foreach(List<int> face_tmp in faces)
        {  
            int tetIdx = f / 4;
            List<int> face_ = new List<int>(face_tmp);
            face_.Sort();
            string stringToHash = string.Join(",", face_.ToArray());
            int face = stringToHash.GetHashCode();
            
            if (supportDict.ContainsKey(face))
            {
                adjP2P[tetIdx].Add(supportDict[face]);
                adjP2P[supportDict[face]].Add(tetIdx);
                adjF2F[f].Add(supportDictForFaces[face]);
                adjF2F[supportDictForFaces[face]].Add(f);

            }
            else
            { 
                supportDict[face] = tetIdx;
                supportDictForFaces[face] = f;
            }

            f++;
        }
    }

    void RenderSurface()
    {
        unityMesh.Clear();
        SF2Poly.Clear();
        unityMesh.vertices = vertices.ToArray();
        List<int> triangles = new List<int>();

        int idx = 0;
        int visibleIdx = 0;
        foreach(List<int> face in faces)
        {
            int polyIdx = idx / 4;
            if (visiblePolys[polyIdx])
            {
                if (adjF2F[idx].Count() == 0 || !visiblePolys[adjF2F[idx][0]/4])
                {
                    foreach (int f in face)
                    {
                        triangles.Add(f);
                    }
                    SF2Poly[visibleIdx] = polyIdx;
                    visibleIdx++;
                }
            }
            idx++;
        }
        Debug.Log("num Surface Faces " + triangles.Count()/3);
        unityMesh.triangles = triangles.ToArray();
        unityMesh.RecalculateNormals();
        unityMesh.RecalculateBounds();
    }

    public new void Slice(double x, double y, double z)
    {
        base.Slice(x, y, z);
        RenderSurface();
    }

    public void polyVisibilitySwitch(int idx, bool isVisible)
    {
        visiblePolys[idx] = isVisible;
        RenderSurface();

    }

    public new void SplitMesh(GameObject current)
    {
        base.SplitMesh(current);
        Destroy(current);
    }

    public new void PlaneSlice(float a, float b, float c, float d)
    {
        base.PlaneSlice(a, b, c, d);
        RenderSurface();
    }

    public new void PlaneSlice(float a, float b, float c, float d, float dim)
    {
        base.PlaneSlice(a, b, c, d, dim);
        RenderSurface();
    }

    public void PlaneCutter(float a, float b, float c, float d, float dim, GameObject meshSplitted)
    {
        MeshFilter filter = meshSplitted.GetComponent<MeshFilter>();
        base.PlaneCutter(a, b, c, d, dim);
        RenderSurface();
        filter.mesh = unityMesh;
    }

    public int GetStatus() { return isFell; }

    public void UpdateStatus() { isFell --; }
}
