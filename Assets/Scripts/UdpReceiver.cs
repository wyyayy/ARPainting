using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UdpReceiver : MonoBehaviour
{
    public System.Action<string> OnMessage;

    protected UdpClient _udpClient;
    protected IPEndPoint _receiveIPGroup;
    protected int _port;

    protected bool _inReceiving;
    protected Queue<string> _msgQueue;

    void Start()
    {
        _msgQueue = new Queue<string>();
    }

    void Update()
    {
        lock(this)
        {
            while(0 != _msgQueue.Count)
            {
                var msg = _msgQueue.Dequeue();
                if(OnMessage != null)
                {
                    OnMessage(msg);
                }
            }
        }    
    }

    public IPAddress GetHostIP()
    {
        if(null == _udpClient) return null;
        else
        {
            Debug.Assert(_receiveIPGroup != null);
            return _receiveIPGroup.Address;
        }
    }

    public void StartReceive(int port)
    {        
        lock(this)
        {
            try
            {
                StopReceive();

                Debug.Assert(null == _udpClient);
                _port = port;
                _udpClient = new UdpClient(_port);
                _receiveIPGroup = new IPEndPoint(IPAddress.Any, _port);

                _udpClient.BeginReceive(_receiveData, null);
                _inReceiving = true;

                Debug.Log("Start receiving...");
            }
            catch (SocketException e)
            {
                Debug.Log(e.Message);
            }
        }     
    }

    public void StopReceive()
    {
        lock(this)
        {        
            if(!_inReceiving) return;

            _udpClient.Close();
            _receiveIPGroup = null;
            _inReceiving = false;

            Debug.Log(0 == _msgQueue.Count);
            _msgQueue.Clear();
        }
    }

    private void _receiveData(IAsyncResult result)
    {
        Debug.Log("......");
        lock(this)
        {           
            if(null == _udpClient)
            {
                Debug.Assert(!_inReceiving);
                return;
            }

            byte[] receivedBytes;
            receivedBytes = _udpClient.EndReceive(result, ref _receiveIPGroup);
            string receivedMsg = Encoding.ASCII.GetString(receivedBytes);
            _msgQueue.Enqueue(receivedMsg);

            _udpClient.BeginReceive(_receiveData, null);  
        }
    }

}



