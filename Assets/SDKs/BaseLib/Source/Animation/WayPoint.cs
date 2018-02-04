#region Using Statements
using UnityEngine;
using System.Collections;
#endregion

/// <summary>
/// ------------------------------------------------------------------------
/// WayPoint.cs
/// ------------------------------------------------------------------------
/// WayPoints are used by UnitPaths to define a linear path which units will
/// follow once spawned.
/// ------------------------------------------------------------------------
/// ©2011 Boon Cotter, All Rights Reserved
/// ------------------------------------------------------------------------
/// </summary>
public class WayPoint : MonoBehaviour
{
	#region Fields
	
	private const float GIZMO_RADIUS = 0.1f;
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Draw custom gizmo.
	/// </summary>
	private void OnDrawGizmos()
	{
		Color oldColor = Gizmos.color;
		
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(transform.position, GIZMO_RADIUS);
		
		Gizmos.color = oldColor;
	}
		                  
	#endregion
}