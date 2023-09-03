﻿using common;
using NLog;
using System.Net;
using System.Net.Sockets;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using wServer.networking.server;
using wServer.realm;
using wServer.realm.entities;
using wServer.realm.worlds.logic;

namespace wServer.networking;

public enum ProtocolState
{
    Disconnected,
    Connected,
    Handshaked,
    Ready
}

public partial class Client
{
    static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RealmManager Manager { get; }
    
    private readonly Server _server;
    private readonly CommHandler _handler;


    private volatile ProtocolState _state;
    public ProtocolState State
    {
        get => _state;
        internal set => _state = value;
    }

    public int Id { get; internal set; }
    public DbAccount Account { get; internal set; }
    public DbChar Character { get; internal set; }
    public Player Player { get; internal set; }

    public wRandom Random { get; internal set; }
    public uint Seed { get; set; }

    //Temporary connection state
    internal int TargetWorld = -1;

    public Socket Skt { get; private set; }
    public string IP { get; private set; }

    internal readonly object DcLock = new();

    public Client(Server server, RealmManager manager,
        SocketAsyncEventArgs send, SocketAsyncEventArgs receive)
    {
        _server = server;
        Manager = manager;

        _handler = new CommHandler(this, send, receive);
    }

    public void Reset()
    {
        Id = 0; // needed so that inbound packets that are currently queued are discarded.
        Account = null;
        Character = null;
        Player = null;

        // reset client ping/pong values
        _pingTime = -1;
        _pongTime = -1;

        _handler.Reset();
    }

    public void BeginHandling(Socket skt)
    {
        Skt = skt;

        try
        {
            IP = ((IPEndPoint)skt.RemoteEndPoint).Address.ToString();
        }
        catch (Exception e)
        {
            IP = "";
        }

        Log.Trace("Received client @ {0}.", IP);
        _handler.BeginHandling(Skt);
    }

    public void SendPacket(Packet pkt)
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State != ProtocolState.Disconnected)
                _handler.SendPacket(pkt);
        }
    }

    public void SendPackets(IEnumerable<Packet> pkts)
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State != ProtocolState.Disconnected)
                _handler.SendPackets(pkts);
        }
    }

    public bool IsReady()
    {
        switch (State)
        {
            case ProtocolState.Disconnected:
            case ProtocolState.Ready when Player?.Owner == null:
                return false;
            default:
                return true;
        }
    }

    internal void ProcessPacket(Packet pkt)
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State == ProtocolState.Disconnected)
                return;

            try
            {
                Log.Trace("Handling packet '{0}'...", pkt.C2SId);

                if (!PacketHandlers.C2SHandlers.TryGetValue(pkt.C2SId, out var handler))
                    Log.Warn("Unhandled packet '{0}'.", pkt.C2SId);
                else
                    handler.Handle(this, (IncomingMessage)pkt);
            }
            catch (Exception e)
            {
                Log.Error("Error when handling packet '{0}, {1}'...", pkt.ToString(), e.ToString());
                Disconnect("Packet handling error.");
            }
        }
    }

    public bool Reconnecting;

    public void Reconnect(string name, int gameId)
    {
        Reconnecting = true;

        if (Account == null)
        {
            Disconnect("Tried to reconnect an client with a null account...");
            return;
        }

        Log.Trace("Reconnecting client ({0}) @ {1} to {2}...", Account.Name, IP, name);
        ConnectManager.Reconnect(this, gameId);
    }

    public void Disconnect(string reason = "")
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State == ProtocolState.Disconnected)
                return;
            
            _handler.SendPacket(new Failure
            {
                ErrorId = Failure.MessageWithDisconnect,
                ErrorDescription = reason,
            });

            State = ProtocolState.Disconnected;

            if (!string.IsNullOrEmpty(reason))
                Log.Warn("Disconnecting client ({0}) @ {1}... {2}",
                    Account?.Name ?? " ", IP, reason);

            if (Account != null)
                try
                {
                    Save();
                }
                catch (Exception e)
                {
                    var msg = $"{e.Message}\n{e.StackTrace}";
                    Log.Error(msg);
                }

            Manager.Disconnect(this);
            _server.Disconnect(this);
        }
    }

    private void Save() // only when disconnect
    {
        var acc = Account;

        if (Character == null || Player == null || Player.Owner is Test)
        {
            Manager.Database.ReleaseLock(acc);
            return;
        }

        Player.SaveToCharacter();
        Player.Owner?.LeaveWorld(Player);
        acc.RefreshLastSeen();
        acc.FlushAsync();
        Manager.Database.SaveCharacter(acc, Character, Player.FameCounter.ClassStats, true)
            .ContinueWith(t => Manager.Database.ReleaseLock(acc));
    }

    public void Dispose()
    {
        // nothing to do here
    }
}