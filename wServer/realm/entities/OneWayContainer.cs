using common;

namespace wServer.realm.entities;

internal class OneWayContainer : Container {
    public OneWayContainer(RealmManager manager, ushort objType,
        int? life, bool dying, RInventory dbLink = null) : base(manager, objType, life, dying, dbLink) { }

    public OneWayContainer(RealmManager manager, ushort id)
        : base(manager, id) { }
}