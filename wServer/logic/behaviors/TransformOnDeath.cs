﻿using common;
using common.resources;
using System.Xml.Linq;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.behaviors;

class TransformOnDeath : Behavior
{
    ushort target;
    int min;
    int max;
    float probability;

    public TransformOnDeath(XElement e)
    {
        target = GetObjType(e.ParseString("@target"));
        min = e.ParseInt("@min", 1);
        max = e.ParseInt("@max", 1);
        probability = e.ParseFloat("@probability", 1);
    }

    public TransformOnDeath(string target, int min = 1, int max = 1, double probability = 1)
    {
        this.target = GetObjType(target);
        this.min = min;
        this.max = max;
        this.probability = (float)probability;
    }

    protected internal override void Resolve(State parent)
    {
        parent.Death += (sender, e) =>
        {
            if (e.Host.CurrentState.Is(parent) &&
                Random.NextDouble() < probability)
            {
                if (Entity.Resolve(e.Host.Manager, target) is Portal
                    && e.Host.Owner.IdName.Contains("Arena"))
                {
                    return;
                }
                int count = Random.Next(min, max + 1);
                for (int i = 0; i < count; i++)
                {
                    Entity entity = Entity.Resolve(e.Host.Manager, target);

                    entity.Move(e.Host.X, e.Host.Y);

                    if (e.Host is Enemy && entity is Enemy && (e.Host as Enemy).Spawned)
                    {
                        (entity as Enemy).Spawned = true;
                        (entity as Enemy).ApplyConditionEffect(new ConditionEffect()
                        {
                            Effect = ConditionEffectIndex.Invisible,
                            DurationMS = -1
                        });
                    }

                    e.Host.Owner.EnterWorld(entity);
                }
            }
        };
    }
    protected override void TickCore(Entity host, RealmTime time, ref object state)
    { }
}