using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Commands
{
	public const string LeftDown = "LeftDown";
	public const string LeftUp = "LeftUp";
	public const string RightDown = "RightDown";
	public const string RightUp = "RightUp";
}

public class StickClient : MonoBehaviour
{
	public GameObject Sphere;

	protected Material _sphereMtl;
	protected UdpSender _udpSender;

    // Use this for initialization
    void Start()
    {
		_sphereMtl = Sphere.GetComponent<MeshRenderer>().material;
		Debug.Assert(_sphereMtl != null);

		this.Bind(out _udpSender);
		_udpSender.Init(23456);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnLeftButtonDown()
    {
		_sphereMtl.color = Color.green;
		_udpSender.Broadcast(Commands.LeftDown);
    }

    public void OnLeftButtonUp()
    {
		_sphereMtl.color = Color.blue;
		_udpSender.Broadcast(Commands.LeftUp);
    }

    public void OnRightButtonDown()
    {
		_sphereMtl.color = Color.red;
		_udpSender.Broadcast(Commands.RightDown);
    }
	
    public void OnRightButtonUp()
    {
		_sphereMtl.color = Color.blue;
		_udpSender.Broadcast(Commands.RightUp);
    }

}
