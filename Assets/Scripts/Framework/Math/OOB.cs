using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OOB
{
	public Vector3 center;
	public Vector3 extents;

	public Quaternion orientation;

	public OOB(Vector3 center, Vector3 extents, Quaternion orientation)
	{
		this.center = center;
		this.extents = extents;
		this.orientation = orientation;
	}

	override public string ToString()
	{
		return "center:" + this.center + ", extents:" + this.extents + ", " + "orientation: " + this.orientation;
	}
}