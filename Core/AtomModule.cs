using Atom.Core;
using Atom.Core.Interface;
using Atom.Core.Wrappers;
using System.Net;
using UnityEngine;
using static Atom.Core.AtomGlobal;

[DefaultExecutionOrder(-5)]
public class AtomModule : MonoBehaviour, ISocket
{
    private AtomSocket _server;
    private AtomSocket _client;

    public void OnMessageCompleted(AtomStream reader, AtomStream writer, int playerId, EndPoint endPoint, Channel channel, Target target, Operation operation, bool isServer)
    {
        throw new System.NotImplementedException();
    }

    private void Awake()
    {
        _server = new(this);
        _server.Initialize("0.0.0.0", 5055);
        _client = new(this);
        _client.Initialize("0.0.0.0", 5056);
    }

    void Start()
    {
        string[] address = Conf.Addresses[0].Split(':');
        _client.Connect(address[0], int.Parse(address[1]), this);
    }

    void Update()
    {

    }
}
