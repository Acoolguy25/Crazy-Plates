using Mirror;
using System;
using UnityEngine;

public class DummyTransport : Transport
{
    public override bool Available()
    {
        return true; // Pretend it's always available
    }

    public override void ClientConnect(string address)
    {
        // Immediately invoke connected callback for local simulation
        if (OnClientConnected != null)
            OnClientConnected.Invoke();
    }

    public override bool ClientConnected()
    {
        return true; // Always pretend we're connected
    }

    public override void ClientDisconnect()
    {
        if (OnClientDisconnected != null)
            OnClientDisconnected.Invoke();
    }

    public override void ClientSend(ArraySegment<byte> segment, int channelId = Channels.Reliable)
    {
        // Do nothing
    }

    public override void ServerStart()
    {
        // Instantly say that server started and a local client connected
        OnServerConnectedWithAddress.Invoke(1, "dummy://localhost"); // Fake connectionId 0
    }

    public override void ServerStop()
    {
        OnServerDisconnected.Invoke(1);
    }

    public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId = Channels.Reliable)
    {
        // Do nothing
    }

    public override void ServerDisconnect(int connectionId)
    {
        OnServerDisconnected.Invoke(connectionId);
    }

    public override string ServerGetClientAddress(int connectionId)
    {
        return "localhost";
    }

    public override bool ServerActive()
    {
        return true;
    }

    public override void Shutdown()
    {
        // No shutdown needed
    }

    public override int GetMaxPacketSize(int channelId = Channels.Reliable)
    {
        return 1500;
    }

    public override Uri ServerUri()
    {
        return new Uri("dummy://localhost");
    }
}
