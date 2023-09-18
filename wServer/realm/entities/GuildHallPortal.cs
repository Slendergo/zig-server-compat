namespace wServer.realm.entities;

internal class GuildHallPortal : StaticObject {
    public GuildHallPortal(RealmManager manager, ushort objType, int? life)
        : base(manager, objType, life, false, true, false) { }
}