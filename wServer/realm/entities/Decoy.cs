﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Game;
using wServer.logic;
using wServer.networking.packets.outgoing;

namespace wServer.realm.entities
{
    class Decoy : StaticObject, IPlayer
    {
        static Random rand = new Random();

        Player player;
        int duration;
        Vector2 direction;
        float speed;

        Vector2 GetRandDirection()
        {
            double angle = rand.NextDouble() * 2 * Math.PI;
            return new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
            );
        }
        public Decoy(Player player, int duration, float tps)
            : base(player.Manager, 0x0715, duration, true, true, true)
        {
            this.player = player;
            this.duration = duration;
            this.speed = tps;

            var history = player.TryGetHistory(1);
            if (history == null)
                direction = GetRandDirection();
            else
            {
                direction = new Vector2(player.X - history.Value.X, player.Y - history.Value.Y);
                if (direction.LengthSquared() == 0)
                    direction = GetRandDirection();
                else
                    direction.Normalize();
            }
        }

        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            stats[StatsType.Texture1] = player.Texture1;
            stats[StatsType.Texture2] = player.Texture2;
            base.ExportStats(stats);
        }

        bool exploded = false;
        public override void Tick(RealmTime time)
        {
            if (HP > duration - 2000)
            {
                this.ValidateAndMove(
                    X + direction.X * speed * time.ElaspedMsDelta / 1000,
                    Y + direction.Y * speed * time.ElaspedMsDelta / 1000
                );
            }
            if (HP < 250 && !exploded)
            {
                exploded = true;
                Owner.BroadcastPacketNearby(new ShowEffect()
                {
                    EffectType = EffectType.AreaBlast,
                    Color = new ARGB(0xffff0000),
                    TargetObjectId = Id,
                    Pos1 = new Position() { X = 1 }
                }, this, null);
            }
            base.Tick(time);
        }

        public void Damage(int dmg, Entity src) { }

        public bool IsVisibleToEnemy() { return true; }
    }
}
