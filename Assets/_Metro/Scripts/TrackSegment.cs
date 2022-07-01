using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;


public class TrackSegment : MonoBehaviour
{
    public TransportLine line;
    public Station stationA;
    public Station stationB;
    public int linkIndex; // where in line am I

    public Vector3 start;
    public Vector3 cp1;
    public Vector3 cp2;
    public Vector3 end;
    public Color color;

    public bool editing = false;
    public int lengthOfLine = 20;

    Vector3[] cp;
    Mesh mesh;
    LineRenderer lineRenderer;
    MeshCollider meshCollider;
    

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        mesh = new Mesh();

        lineRenderer.material = Resources.Load("Materials/M_IOWire") as Material;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        lineRenderer.SetPropertyBlock(block);
        lineRenderer.widthMultiplier = 0.01f;
        lineRenderer.positionCount = lengthOfLine+1;

        cp = new Vector3[]{
            new Vector3(0f,0f,0f),
            new Vector3(0f,0f,0f),
            new Vector3(0f,0f,0f),
            new Vector3(0f,0f,0f)
        };

    }

    // Update is called once per frame
    void Update()
    {   
        if(!editing) return;

        var dist = 0.1f;
        cp[0] = start;
        cp[1] = cp1; //+ start.right * 0.25f * dist;
        cp[2] = cp2; //- end.right * 0.25f * dist;
        cp[3] = end;

        for (int i = 0; i <= lengthOfLine; i++)
        {
            Vector3 p = Interpolate(i*1.0f/lengthOfLine);
            lineRenderer.SetPosition(i, p); 
        }

        lineRenderer.BakeMesh(mesh, true);
        meshCollider.sharedMesh = mesh;
    }

    Vector3 Interpolate(float t){
        var t1 = 1.0f - t;
        return t1*t1*t1*cp[0] + 3f*t1*t1*t*cp[1] + 3f*t1*t*t*cp[2] + t*t*t*cp[3];
    }

    Vector3 Derivative(float t){
        var t1 = 1.0f - t;
        return 3f*t1*t1*(cp[1]-cp[0]) + 6f*t1*t*(cp[2]-cp[1]) + 3f*t*t*(cp[3]-cp[2]);
    }





    
}
