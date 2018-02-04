using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UdpSender : MonoBehaviour
{
    UdpClient _udpClient;

    protected int _port;
    protected int _localPort = 29784;

    void Start()
    {
        
    }

    public void Init(int port)
    {
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            Debug.LogWarning("No network available!");
            return;
        }

        _port = port;
        _udpClient = new UdpClient(_localPort, AddressFamily.InterNetwork);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, _port);
        _udpClient.Connect(groupEP);
    }

    public void StopHost()
    {
        if(null == _udpClient) return;

        _udpClient.Close();
        _udpClient = null;
    }

    public void Broadcast(string message)
    {
        Debug.Assert(message != null);
        _udpClient.Send(Encoding.ASCII.GetBytes(message), message.Length);
    }
}



