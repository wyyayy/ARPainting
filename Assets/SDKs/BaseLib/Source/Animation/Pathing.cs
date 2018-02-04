using UnityEngine;
using System;
using System.Collections;

namespace BaseLib
{ 

public class Pathing : MonoBehaviour
{
	#region Fields
	
	/// <summary>
	/// Object linear movement speed in units/sec.
	/// </summary>
	public float Speed = 1f;
	
	/// <summary>
	/// The path to which the object is attached.
	/// </summary>
	public Path AttachedPath;
	
	/// <summary>
	/// The index of the last WayPoint the object reached.
	/// </summary>
	private int CurrentWaypointIndex;
	
	/// <summary>
	/// Called when the object reaches the end of the path.
	/// </summary>
	public EventHandler ReachedEndOfPath;
	
	/// <summary>
	/// True if the object has reached the end of the path.
	/// </summary>
	public bool AtEndOfPath
	{
		get { return CurrentWaypointIndex >= AttachedPath.Nodes.Count - 1; }
	}
	
	
	#endregion
	
	#region Methods
	
    void Start()
    {
        AttachToPath(this.AttachedPath);
    }

	/// <summary>
	/// Attach the object to the specified path.
	/// </summary>
	public void AttachToPath(Path path)
	{
		if (path == null)
			return;
		
		AttachedPath = path;
		transform.position = AttachedPath.Nodes[0];
		CurrentWaypointIndex = 0;
		StartCoroutine(CRPathing());
	}
	
	/// <summary>
	/// Detach the object.
	/// </summary>
	public void DetachFromPath()
	{
		StopAllCoroutines();
	}
	
	/// <summary>
	/// Return the distance between the current position and the next waypoint.
	/// </summary>
	private float DistanceToNextWaypoint()
	{
		if (CurrentWaypointIndex == AttachedPath.Nodes.Count - 1)
			return 0f;
		else
			return Vector3.Distance(transform.position, AttachedPath.Nodes[CurrentWaypointIndex + 1]);
	}
	
	/// <summary>
	/// Move the object along the path.
	/// </summary>
	private IEnumerator CRPathing()
	{
		while (!AtEndOfPath)
		{
			float distanceToTravel = Speed * Time.deltaTime;
			float distanceToNextWaypoint = DistanceToNextWaypoint();
			
			while (distanceToNextWaypoint < distanceToTravel && !AtEndOfPath)
			{
				distanceToTravel -= distanceToNextWaypoint;
				transform.position = AttachedPath.Nodes[CurrentWaypointIndex + 1];
				CurrentWaypointIndex += 1;
				distanceToNextWaypoint = DistanceToNextWaypoint();
			}
			
			if (AtEndOfPath)
				transform.position = AttachedPath.Nodes[AttachedPath.Nodes.Count - 1];
			else
				transform.position += Vector3.Normalize(AttachedPath.Nodes[CurrentWaypointIndex + 1] - transform.position) * distanceToTravel;
				
			yield return null;
		}
		
		if (ReachedEndOfPath != null)
			ReachedEndOfPath(this, null);
		
		DetachFromPath();
		
		yield return null;
	}
	
	#endregion
}

}