﻿using System;
using System.Collections.Generic;
using common.resources;

namespace wServer.realm.entities
{
    public abstract class Character : Entity
    {
        public static Random Random = new Random();

        private readonly SV<int> _hp;
        private readonly SV<int> _maximumHP;

        public int HP
        {
            get { return _hp.GetValue(); }
            set { _hp.SetValue(value); }
        }
        public int MaximumHP
        {
            get { return _maximumHP.GetValue(); }
            set { _maximumHP.SetValue(value); }
        }

        protected Character(RealmManager manager, ushort objType)
            : base(manager, objType)
        {
            _hp = new SV<int>(this, StatsType.HP, 0);
            _maximumHP = new SV<int>(this, StatsType.MaximumHP, 0);

            if (ObjectDesc != null)
            {
                if (ObjectDesc.SizeStep != 0)
                {
                    var step = Random.Next(0, (ObjectDesc.MaxSize - ObjectDesc.MinSize) / ObjectDesc.SizeStep + 1) * ObjectDesc.SizeStep;
                    SetDefaultSize(ObjectDesc.MinSize + step);
                }
                else
                    SetDefaultSize(ObjectDesc.MinSize);

                SetConditions();

                HP = ObjectDesc.MaxHP;
                MaximumHP = HP;
            }
        }

        private void SetConditions()
        {
            if (ObjectDesc.StasisImmune)
                ApplyConditionEffect(ConditionEffectIndex.StasisImmune);
            if (ObjectDesc.StunImmune)
                ApplyConditionEffect(ConditionEffectIndex.StunImmune);
        }

        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            stats[StatsType.HP] = HP;
            if (!(this is Player))
                stats[StatsType.MaximumHP] = MaximumHP;
            base.ExportStats(stats);
        }
    }
}
