using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;


public class TrackSpline : MonoBehaviour
{

    public TransportLine line;

    public List<Vector3> points = new List<Vector3>();
    public List<Vector3> cp = new List<Vector3>();
    public List<float> lengths = new List<float>();

    public Color color;

    public bool update = true;
    public int curveResolution = 20;

    Mesh mesh;
    LineRenderer lineRenderer;
    MeshCollider meshCollider;
    

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        mesh = new Mesh();

        lineRenderer.material = Resources.Load("Materials/MRTKTransparent") as Material;
        var block = new MaterialPropertyBlock();
        block.SetColor("_Color", color);
        lineRenderer.SetPropertyBlock(block);
        lineRenderer.widthMultiplier = 0.01f;

    }

    // Update is called once per frame
    void Update()
    {   
        if(!update) return;
        if(points.Count == 0) return;

        UpdateLengths();
        UpdateControlPoints();

        lineRenderer.positionCount = (points.Count-1)*curveResolution;

        for(int i = 0; i < lineRenderer.positionCount; i++){
            Vector3 p = GetPosition(i*1.0f/(lineRenderer.positionCount-1));
            lineRenderer.SetPosition(i, p); 
        }

        lineRenderer.BakeMesh(mesh, true);
        meshCollider.sharedMesh = mesh;
    }

    void UpdateLengths(){
        lengths.Clear();
        for(int i = 0; i < points.Count - 1; i++){
            var p0 = points[i];
            var p1 = points[i+1];

            lengths.Add(Vector3.Distance(p0,p1));
        }
    }

    void UpdateControlPoints(){
        cp.Clear();
        cp.Add(points[0]);
        for(int i = 0; i < points.Count - 1; i++){
            var p0 = points[i];
            var p1 = points[i+1];

            // default line
            var cp0 = p0 + (p1-p0)*0.05f;
            var cp1 = p1 + (p0-p1)*0.05f;

            // look back to i-1 
            if(i > 0){ 
                var p00 = points[i-1];
                var v = Vector3.Normalize(p1 - p00);
                cp0 = p0 + v * 0.1f;

            }
            // look forward to i+2
            if(i < points.Count - 2){
                var p2 = points[i+2];
                var v = Vector3.Normalize(p2 - p0);
                cp1 = p1 - v * 0.1f;
            }
            cp.Add(cp0);
            cp.Add(cp1);
            cp.Add(p1);
        }
    }


    public Vector3 GetPosition(float t){
        int i;
		if (t >= 1f) {
			t = 1f;
			i = cp.Count - 4;
		}
		else {
			t = Mathf.Clamp01(t) * (points.Count - 1);
			i = (int)t;
			t -= i;
			i *= 3;
		}

        return Interpolate(cp[i], cp[i+1], cp[i+2], cp[i+3], t);
    }
    
    // public Vector3 GetDistance(float t){
    //     int i;
	// 	if (t >= 1f) {
	// 		t = 1f;
	// 		i = cp.Count - 4;
	// 	}
	// 	else {
	// 		t = Mathf.Clamp01(t) * (points.Count - 1);
	// 		i = (int)t;
	// 		t -= i;
	// 		i *= 3;
	// 	}

    //     return Interpolate(cp[i], cp[i+1], cp[i+2], cp[i+3], t);
    // }

    public Vector3 GetVelocity(float t){
        int i;
		if (t >= 1f) {
			t = 1f;
			i = cp.Count - 4;
		}
		else {
			t = Mathf.Clamp01(t) * (points.Count - 1);
			i = (int)t;
			t -= i;
			i *= 3;
		}

        return Derivative(cp[i], cp[i+1], cp[i+2], cp[i+3], t);
    }

    // float Distance(float t){
    //     int i;
	// 	if (t >= 1f) {
	// 		t = 1f;
	// 		i = cp.Count - 4;
	// 	}
	// 	else {
	// 		t = Mathf.Clamp01(t) * (points.Count - 1);
	// 		i = (int)t;
	// 		t -= i;
	// 		i *= 3;
	// 	}

    //     return GetPoint(cp[i], cp[i+1], cp[i+2], cp[i+3], t);
    // }

    Vector3 Interpolate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t){
        var t1 = 1.0f - t;
        return t1*t1*t1*p0 + 3f*t1*t1*t*p1 + 3f*t1*t*t*p2 + t*t*t*p3;
    }

    Vector3 Derivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t){
        var t1 = 1.0f - t;
        return 3f*t1*t1*(p1-p0) + 6f*t1*t*(p2-p1) + 3f*t*t*(p3-p2);
    }





    
}
