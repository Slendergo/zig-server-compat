using common;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using wServer.networking.server;
using wServer.realm;
using wServer.realm.entities;
using wServer.realm.worlds.logic;

namespace wServer.networking
{
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

        public RealmManager Manager { get; private set; }

        static readonly byte[] ServerKey = new byte[] { 0x61, 0x2a, 0x80, 0x6c, 0xac, 0x78, 0x11, 0x4b, 0xa5, 0x01, 0x3c, 0xb5, 0x31 };
        static byte[] _clientKey = new byte[] { 0x81, 0x1f, 0x50, 0x39, 0x1f, 0xb4, 0x55, 0x89, 0x9c, 0xa9, 0xd7, 0x4b, 0x72 };

        public RC4 ReceiveKey { get; private set; }
        public RC4 SendKey { get; private set; }

        private readonly Server _server;
        private readonly CommHandler _handler;


        private volatile ProtocolState _state;
        public ProtocolState State
        {
            get { return _state; }
            internal set { _state = value; }
        }

        public int Id { get; internal set; }
        public DbAccount Account { get; internal set; }
        public DbChar Character { get; internal set; }
        public Player Player { get; internal set; }

        public wRandom Random { get; internal set; }

        //Temporary connection state
        internal int TargetWorld = -1;

        public Socket Skt { get; private set; }
        public string IP { get; private set; }

        internal readonly object DcLock = new object();

        public Client(Server server, RealmManager manager,
           SocketAsyncEventArgs send, SocketAsyncEventArgs receive,
           byte[] clientKey)
        {
            _server = server;
            Manager = manager;
            _clientKey = clientKey;

            ReceiveKey = new RC4(_clientKey);
            SendKey = new RC4(ServerKey);

            _handler = new CommHandler(this, send, receive);
        }

        public void Reset()
        {
            ReceiveKey = new RC4(_clientKey);
            SendKey = new RC4(ServerKey);

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
            if (State == ProtocolState.Disconnected)
                return false;

            if (State == ProtocolState.Ready && Player?.Owner == null)
                return false;

            return true;
        }

        internal void ProcessPacket(Packet pkt)
        {
            using (TimedLock.Lock(DcLock))
            {
                if (State == ProtocolState.Disconnected)
                    return;

                try
                {
                    Log.Trace("Handling packet '{0}'...", pkt.ID);

                    IPacketHandler handler;
                    if (!PacketHandlers.Handlers.TryGetValue(pkt.ID, out handler))
                        Log.Warn("Unhandled packet '{0}'.", pkt.ID);
                    else
                        handler.Handle(this, (IncomingMessage)pkt);
                }
                catch (Exception e)
                {
                    Log.Error("Error when handling packet '{{0}, {1}}'...", pkt.ToString(), e.ToString());
                    Disconnect("Packet handling error.");
                }
            }
        }

        public void Reconnect(Reconnect pkt)
        {
            if (Account == null)
            {
                Disconnect("Tried to reconnect an client with a null account...");
                return;
            }

            Log.Trace("Reconnecting client ({0}) @ {1} to {2}...", Account.Name, IP, pkt.Name);
            Manager.ConMan.AddReconnect(Account.AccountId, pkt);
            SendPacket(pkt);
        }

        public async void SendFailure(string text, int errorId = Failure.MessageWithDisconnect)
        {
            SendPacket(new Failure()
            {
                ErrorId = errorId,
                ErrorDescription = text
            });

            if (errorId == Failure.MessageWithDisconnect ||
                errorId == Failure.ForceCloseGame)
            {
                var t = Task.Delay(1000);
                await t;

                Disconnect();
            }
        }

        public void Disconnect(string reason = "")
        {
            using (TimedLock.Lock(DcLock))
            {
                if (State == ProtocolState.Disconnected)
                    return;

                State = ProtocolState.Disconnected;

                if (!string.IsNullOrEmpty(reason))
                    Log.Trace("Disconnecting client ({0}) @ {1}... {2}",
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
}
