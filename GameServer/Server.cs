using System.Net;
using System.Net.Sockets;
using GameServer.realm;
using Shared;

namespace GameServer;

public class Server {
    
    private Queue<Client> _clientPool;
    private Socket _listenSocket;
    private RealmManager _manager;

    public Server(RealmManager manager, int port, int maxConn) {
        SLog.Info("Starting server...");
        _manager = manager;
        _clientPool = new Queue<Client>(maxConn);
        for (var i = 0; i < maxConn; i++)
            _clientPool.Enqueue(new Client(this, _manager));

        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(endpoint);
        _listenSocket.Listen(128);
        SLog.Info("Listening on port {0}...", port);
        Accept();
    }

    private async void Accept() {
        while (true) {
            Socket socket;
            if ((socket = await _listenSocket.AcceptAsync(CancellationToken.None)) != null)
                _clientPool.Dequeue().Reset(socket);
        }
    }

    public void Disconnect(Client client) {
        try {
            if (client.Socket.Connected) {
                client.Socket.Shutdown(SocketShutdown.Both);
                client.Socket.Close();
            }

            _clientPool.Enqueue(client);
        }
        catch (Exception e) {
            if (e is not SocketException se || se.SocketErrorCode != SocketError.NotConnected &&
                se.SocketErrorCode != SocketError.Shutdown)
                SLog.Error(e);
        }
    }

    public void Stop() {
        SLog.Info("Stopping server...");
        try {
            _listenSocket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e) {
            if (e is not SocketException se || se.SocketErrorCode != SocketError.NotConnected)
                SLog.Error(e);
        }

        _listenSocket.Close();
        foreach (var i in _manager.Clients.Keys.ToArray())
            i.Disconnect();
    }
}