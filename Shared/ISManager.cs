using NLog;
using Timer = System.Timers.Timer;

namespace Shared;

public class ISManager : InterServerChannel, IDisposable {
    private const int PingPeriod = 2000;
    private const int ServerTimeout = 30000;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly object _dicLock = new();
    private readonly Dictionary<string, int> _lastUpdateTime = new();
    private readonly Dictionary<string, ServerInfo> _servers = new();

    private readonly ServerConfig _settings;

    private readonly Timer _tmr = new(PingPeriod);
    private long _lastPing;

    public EventHandler NewServer;
    public EventHandler ServerPing;
    public EventHandler ServerQuit;

    public ISManager(Database db, ServerConfig settings)
        : base(db, settings.serverInfo.instanceId) {
        SLog.Info("Server's Id is {0}", settings.serverInfo.instanceId);

        _settings = settings;

        // kind of fucked up to do this, but can't really think of another way
        db.SetISManager(this);

        // listen to "network" communications
        AddHandler<NetworkMsg>(Channel.Network, HandleNetwork);

        // tell other servers listening that we've join the network
        Publish(Channel.Network, new NetworkMsg {
            Code = NetworkCode.Join,
            Info = _settings.serverInfo
        });
    }

    public void Dispose() {
        Publish(Channel.Network, new NetworkMsg {
            Code = NetworkCode.Quit,
            Info = _settings.serverInfo
        });
    }

    public void Run() {
        _tmr.Elapsed += (sender, e) => Tick(PingPeriod);
        _tmr.Start();
    }

    public void Tick(int elapsedMs) {
        using (TimedLock.Lock(_dicLock)) {
            // update running time
            _lastPing += elapsedMs;
            foreach (var s in _lastUpdateTime.Keys.ToArray())
                _lastUpdateTime[s] += elapsedMs;

            if (_lastPing < PingPeriod)
                return;
            _lastPing = 0;

            // notify other servers we're still alive. Update info in the process.
            Publish(Channel.Network, new NetworkMsg {
                Code = NetworkCode.Ping,
                Info = _settings.serverInfo
            });

            // check for server timeouts
            foreach (var s in _lastUpdateTime.Where(s => s.Value > ServerTimeout).ToArray()) {
                var sInfo = _servers[s.Key];
                SLog.Info("{0} ({1}) timed out.", sInfo.name, s.Key);
                RemoveServer(s.Key);

                // invoke server quit event
                var networkMsg = new NetworkMsg {
                    Code = NetworkCode.Timeout,
                    Info = sInfo
                };
                ServerQuit?.Invoke(this,
                    new InterServerEventArgs<NetworkMsg>(s.Key, networkMsg));
            }
        }
    }

    private void HandleNetwork(object sender, InterServerEventArgs<NetworkMsg> e) {
        using (TimedLock.Lock(_dicLock)) {
            switch (e.Content.Code) {
                case NetworkCode.Join:
                    if (AddServer(e.InstanceId, e.Content.Info)) {
                        SLog.Info("{0} ({1}, {2}) joined the network.",
                            e.Content.Info.name, e.Content.Info.type, e.InstanceId);

                        // make new server aware of this server
                        Publish(Channel.Network, new NetworkMsg {
                            Code = NetworkCode.Join,
                            Info = _settings.serverInfo
                        });

                        NewServer?.Invoke(this, e);
                    }
                    else {
                        UpdateServer(e.InstanceId, e.Content.Info);
                    }

                    break;

                case NetworkCode.Ping:
                    if (!_servers.ContainsKey(e.InstanceId))
                        SLog.Info("{0} ({1}, {2}) re-joined the network.",
                            e.Content.Info.name, e.Content.Info.type, e.InstanceId);
                    UpdateServer(e.InstanceId, e.Content.Info);
                    ServerPing?.Invoke(this, e);
                    break;

                case NetworkCode.Quit:
                    SLog.Info("{0} ({1}, {2}) left the network.",
                        e.Content.Info.name, e.Content.Info.type, e.InstanceId);
                    RemoveServer(e.InstanceId);
                    ServerQuit?.Invoke(this, e);
                    break;
            }
        }
    }

    private bool AddServer(string instanceId, ServerInfo info) {
        if (_servers.ContainsKey(instanceId))
            return false;

        UpdateServer(instanceId, info);
        return true;
    }

    private void UpdateServer(string instanceId, ServerInfo info) {
        _servers[instanceId] = info;
        _lastUpdateTime[instanceId] = 0;
    }

    private void RemoveServer(string instanceId) {
        _servers.Remove(instanceId);
        _lastUpdateTime.Remove(instanceId);
    }

    public ServerInfo[] GetServerList() {
        using (TimedLock.Lock(_dicLock)) {
            return _servers.Values.ToArray();
        }
    }

    public string[] GetServerGuids() {
        using (TimedLock.Lock(_dicLock)) {
            return _servers.Keys.ToArray();
        }
    }

    public ServerInfo GetServerInfo(string instanceId) {
        using (TimedLock.Lock(_dicLock)) {
            return _servers.ContainsKey(instanceId)
                ? _servers[instanceId]
                : null;
        }
    }
}