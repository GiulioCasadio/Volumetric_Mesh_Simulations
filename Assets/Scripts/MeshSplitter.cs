using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSplitter : MonoBehaviour
{
    private TetMesh tm;
    public float dim;

    protected List<bool> thisVisiblePolys = new List<bool>();
    protected List<Vector3> thisVertices =  new List<Vector3>();
    protected Dictionary<int, int> thisOldNewVertices = new Dictionary<int, int>();

    private GameObject planeCut;
    private bool isCutting = false;
    private float invulnerability = 0.1f;

    void Start()
    {
        dim = 0.01f;    // Larghezza del taglio

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.RecalculateBounds();

        this.transform.gameObject.AddComponent<MeshCollider>().convex=true;
    }

    void Update()
    {
        invulnerability -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Caso in cui venga tagliata dalla lama
        if (other.gameObject.name == "Lama" &&
            !isCutting &&
            GameObject.Find("LightSaber").GetComponent<SwordAttack>().GetState() &&
            invulnerability < 0)
        {
            isCutting = true;
            planeCut = GameObject.Find("PlaneCutter");
            planeCut.transform.position = other.transform.position;
            planeCut.transform.rotation = other.transform.rotation;
            planeCut.transform.Rotate(90, 0, 0);

            SplitMesh();
        }
        else if ((other.gameObject.name == "Flat" || other.gameObject.name == "Brick" || (other.gameObject.name == "UndestructableBullet(Clone)" && other.gameObject.GetComponent<Rigidbody>().velocity.magnitude > 5)) && transform.parent.GetComponent<TetMesh>().GetStatus()>0)
        {
            transform.parent.GetComponent<TetMesh>().UpdateStatus();
            planeCut = GameObject.Find("PlaneCutter");
            if (transform.parent.GetComponent<TetMesh>().GetStatus() > 15)
                planeCut.transform.position = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            else
                planeCut.transform.position = this.gameObject.transform.position;
            planeCut.transform.rotation = other.transform.rotation;
            planeCut.transform.Rotate(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

            SplitMesh();
        }
    }

    void SplitMesh()
    {
        if (transform.parent.TryGetComponent<TetMesh>(out tm))
        {
            tm.SplitMesh(this.gameObject);
        }
        else
        {
            Debug.LogError("A volume mesh must be attached to this gameObject parent");
        }
    }

    public List<bool> GetVisiblePolys()
    {
        return thisVisiblePolys;
    }

    public List<Vector3> GetVertices()
    {
        return thisVertices;
    }

    public Dictionary<int,int> GetOldNewVertices()
    {
        return thisOldNewVertices;
    }
}
