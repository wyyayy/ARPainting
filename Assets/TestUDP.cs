using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUDP : MonoBehaviour
{
	public int Port = 23456;
    protected UdpSender _sender;
    protected UdpReceiver _receiver;

    // Use this for initialization
    void Start()
    {
        this.Bind(out _sender);
        this.Bind(out _receiver);

		_sender.Init(this.Port);
		_receiver.StartReceive(this.Port);

		_receiver.OnMessage = msg=>
		{
			Debug.Log("Received: " + msg + " from " + _receiver.GetHostIP());
		};
    }

    // Update is called once per frame
    void Update()
    {
		if(Input.GetKeyDown(KeyCode.S))
		{
			var msg = "aaaa";
			_sender.Broadcast(msg);
        	Debug.Log("Send: " + msg);
		}
    }
}
