using Atom.Core;
using Atom.Core.Interface;
using Atom.Core.Wrappers;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using static Atom.Core.AtomGlobal;

[DefaultExecutionOrder(-5)]
public class AtomModule : MonoBehaviour, ISocket
{
    private AtomSocket _server;
    private AtomSocket _client;

    private void Awake()
    {
#if UNITY_SERVER || UNITY_EDITOR
        _server = new(this);
        _server.Initialize("0.0.0.0", 5055);
#endif
#if !UNITY_SERVER || UNITY_EDITOR
        _client = new(this);
        _client.Initialize("0.0.0.0", 5056);
#endif
    }

    void Start()
    {
        Debug.Log(GetFreePort());
        string[] address = Conf.Addresses[0].Split(':');
        _client.Connect(address[0], int.Parse(address[1]), this);
    }

    void Update()
    {

    }

    public void OnMessageCompleted(AtomStream reader, AtomStream writer, int playerId, EndPoint endPoint, Channel channel, Target target, Operation operation, bool isServer)
    {
        throw new System.NotImplementedException();
    }

    public int GetFreePort()
    {
        UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 0));
        IPEndPoint endPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
        int port = endPoint.Port;
        udpClient.Close();
        return port;
    }

    private void OnApplicationQuit()
    {
        _client.Close();
        _server.Close();
    }
}