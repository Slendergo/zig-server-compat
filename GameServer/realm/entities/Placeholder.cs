﻿namespace GameServer.realm.entities;

internal class Placeholder : StaticObject {
    public Placeholder(RealmManager manager, int life)
        : base(manager, 0x070f, life, true, true, false) {
        SetDefaultSize(0);
    }
}