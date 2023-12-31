﻿using GameServer.realm.entities.player;

namespace GameServer.realm.entities;

public class Pet : Entity, IPlayer {
    public Pet(RealmManager manager, Player player, ushort objType) : base(manager, objType) {
        PlayerOwner = player;
    }

    public Player PlayerOwner { get; set; }

    public void Damage(int dmg, Entity src) { }

    public bool IsVisibleToEnemy() {
        return false;
    }
}