using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    public int port = 5005; // Must match sender
    public bool lookingAway = false;

    void Start()
    {
        udpClient = new UdpClient(port);
        udpClient.BeginReceive(ReceiveCallback, null);
        Debug.Log("UDP Listener started on port " + port);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
        byte[] bytes = udpClient.EndReceive(ar, ref ip);
        string message = Encoding.UTF8.GetString(bytes);

        if (message == "LOOKING_AWAY")
        {
            lookingAway = true;
            Debug.Log("lookingAway = TRUE");
        }
        else if (message == "LOOKING")
        {
            lookingAway = false;
            Debug.Log("lookingAway = FALSE");
        }

        // Keep listening
        udpClient.BeginReceive(ReceiveCallback, null);
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
    }
}