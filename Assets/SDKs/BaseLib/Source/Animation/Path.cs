#region Using Statements
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#endregion

/// <summary>
/// ------------------------------------------------------------------------
/// Path.cs
/// ------------------------------------------------------------------------
/// The Path class defines a series a WayPoints which units must 'patrol'
/// when spawned.
/// ------------------------------------------------------------------------
/// ©2011 Boon Cotter, All Rights Reserved
/// ------------------------------------------------------------------------
/// </summary>

namespace BaseLib
{

public class Path : MonoBehaviour
{
	#region Fields
	
	/// <summary>
	/// The number of samples per curve span for converting to spline.
	/// </summary>
	[SerializeField]
	private int m_sampleResolution = 10;
	public int SampleResolution
	{
		get { return m_sampleResolution; }
		set { m_sampleResolution = (int)Mathf.Clamp(value, 1, 32); }
	}
	
	/// <summary>
	/// A collection of wayPoints for the path.
	/// </summary>
	public List<WayPoint> Waypoints;
	
	/// <summary>
	/// The nodes array is used to store path points at build time when the Catmull-Rom
	/// curve is constructed from the Waypoints collection.
	/// </summary>
	private List<Vector3> m_nodes;
	public List<Vector3> Nodes
	{
		get { return m_nodes; }
		private set { m_nodes = value; }
	}
	
	/// <summary>
	/// Get the length of the path.
	/// </summary>
	public float Length { get { return GetPathLength(); } }
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Local initialize.
	/// </summary>
	private void Awake()
	{
		if (Waypoints == null || Waypoints.Count == 0)
			Debug.LogError("[PATH] Has no Waypoints.");
		
		CatmullRom(WaypointsToVectors(), out m_nodes, SampleResolution, true);
	}
	
	/// <summary>
	/// Convert the Waypoint collection into a Vector3 array.
	/// </summary>
	private List<Vector3> WaypointsToVectors()
	{
		List<Vector3> results = new List<Vector3>();
		foreach (WayPoint wp in Waypoints)
			results.Add(wp.transform.position);
		return results;
	}
		                           
	/// <summary>
	/// Returns the length of the path.
	/// </summary>
	private float GetPathLength()
	{
#if UNITY_EDITOR
		CatmullRom(WaypointsToVectors(), out m_nodes, SampleResolution, true);	
#endif
		float result = 0f;
		
		if (Nodes != null && Nodes.Count >= 2)
			for (int n = 0; n < Nodes.Count - 1; n++)
				result += Vector3.Distance(Nodes[n], Nodes[n+1]);
		
		return result;
	}
	
	#endregion
	
	/// <summary>
	/// Draw the path's curve.
	/// </summary>
	private void OnDrawGizmos()
	{
		if (Waypoints == null || Waypoints.Count < 2)
			return;
		
		for (int n = 0; n < Waypoints.Count; n ++)
			if (Waypoints[n] == null)
				Waypoints.RemoveAt(n);
		
		Gizmos.color = Color.red;
		
		List<Vector3> coords = new List<Vector3>();
		foreach (WayPoint wp in Waypoints)
			coords.Add(wp.transform.position);
		
		List<Vector3> curve;
		if (CatmullRom(coords, out curve, SampleResolution, true))
			for (int n = 0; n < curve.Count - 1; n++)
				Gizmos.DrawLine(curve[n], curve[n + 1]);
	}


    ////
    ///
    /// Takes an array of input coordinates, converts that array into a Catmull-Rom spline,
    /// and then samples the resulting spline according to the specified sample count (per span),
    /// populating the output array with the newly sampled coordinates. The returned boolean
    /// indicates whether the operation was successful (true) or not (false).
    /// NOTE: The first and last points specified are used to describe curvature and will be dropped
    /// from the resulting spline.
    ///
    public static bool CatmullRom(List<Vector3> inCoordinates, out List<Vector3> outCoordinates, int samples, bool includeEndPoints)
    {
        if ((!includeEndPoints && inCoordinates.Count < 4) || (includeEndPoints && inCoordinates.Count < 2))
        {
            outCoordinates = null;
            return false;
        }

        if (includeEndPoints && inCoordinates.Count >= 2)
        {
            inCoordinates.Insert(0, inCoordinates[0]);
            inCoordinates.Insert(inCoordinates.Count - 1, inCoordinates[inCoordinates.Count - 1]);
        }

        List<Vector3> results = new List<Vector3>();

        for (int n = 1; n < inCoordinates.Count - 2; n++)
            for (int i = 0; i < samples; i++)
                results.Add(PointOnCurve(inCoordinates[n - 1], inCoordinates[n], inCoordinates[n + 1], inCoordinates[n + 2], (1f / samples) * i));

        results.Add(inCoordinates[inCoordinates.Count - 2]);
        outCoordinates = results;

        return true;
    }
    ///
    /// Return a point on the curve between P1 and P2 with P0 and P4 describing curvature, at
    /// the normalized distance t.
    ///
    public static Vector3 PointOnCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 result = new Vector3();
        float t0 = ((-t + 2f) * t - 1f) * t * 0.5f;
        float t1 = (((3f * t - 5f) * t) * t + 2f) * 0.5f;
        float t2 = ((-3f * t + 4f) * t + 1f) * t * 0.5f;
        float t3 = ((t - 1f) * t * t) * 0.5f;
        result.x = p0.x * t0 + p1.x * t1 + p2.x * t2 + p3.x * t3;
        result.y = p0.y * t0 + p1.y * t1 + p2.y * t2 + p3.y * t3;
        result.z = p0.z * t0 + p1.z * t1 + p2.z * t2 + p3.z * t3;
        return result;
    }

}

}