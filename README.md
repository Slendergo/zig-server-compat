# Requirements
- Redis
- .NET 7.0+

# Setting up admin rights
Admin privileges are governed by the "admin" and "rank" keys in the database. An example of granting yourself admin in the Redis client, assuming that you are id 1 (the first one to register):

```
hset account.1 admin 1
hset account.1 rank 100
```

# Hosting a server
Make sure that the relevant ports are open (TCP 8080 for App Engine, 2050 for Game Server) and that you have changed the Game Server IPs in GameServer/GameServer.json.

# Support
General discussion and help is available over at https://discord.gg/2XdgUW29rw, in the #zig-help channel.
