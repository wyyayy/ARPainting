using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetDetectionUtil : MonoBehaviour
{
    string _IP;

    UdpClient _sender;
    UdpClient _receiver;

    int _remotePort = 19876;
    int _localPort = 29784;

    public void StartHost()
    {
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            Debug.LogWarning("No network available!");
            return;
        }

        //_IP = GetLocalIPAddress();
        _IP = "ddddd";

        _sender = new UdpClient(_localPort, AddressFamily.InterNetwork);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, _remotePort);
        _sender.Connect(groupEP);

        //SendData ();
        InvokeRepeating("_broadcastMsg", 0, 5f);
    }

    void _broadcastMsg()
    {
        string customMessage = _IP;

        if (customMessage != "")
        {
            int ret = _sender.Send(Encoding.ASCII.GetBytes(customMessage), customMessage.Length);
            Debug.Log("Send: " + ret);
        }
    }

    public void StartClient()
    {
        try
        {
            if (_receiver == null)
            {
                _receiver = new UdpClient(_remotePort);
                _receiver.BeginReceive(new AsyncCallback(_receiveData), null);
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }

    private void _receiveData(IAsyncResult result)
    {
        var receiveIPGroup = new IPEndPoint(IPAddress.Any, _remotePort);

        byte[] received;
        if (_receiver != null)
        {
            received = _receiver.EndReceive(result, ref receiveIPGroup);
        }
        else
        {
            return;
        }
        _receiver.BeginReceive(new AsyncCallback(_receiveData), null);
        string receivedString = Encoding.ASCII.GetString(received);
        Debug.Log("Received from :" + receiveIPGroup.Address + ", Data: " + receivedString);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.H))
        {
            if(null == _sender)
            {
                StartHost();
                Debug.Log("Host started");
            }
        }
        else if(Input.GetKeyDown(KeyCode.C))
        {
            if(null == _receiver)
            {
                StartClient();
                Debug.Log("Client started");
            }
        }        
    }

    ///---

    public static string GetLocalIPAddress()
    {
        var hostName = Dns.GetHostName();
        var host = Dns.GetHostEntry(hostName);
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
}
