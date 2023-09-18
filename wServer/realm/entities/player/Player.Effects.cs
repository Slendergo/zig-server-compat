using common.resources;

namespace wServer.realm.entities;

partial class Player {
    private float _bleeding;
    private int _canTpCooldownTime;
    private float _healing;

    private int _newbieTime;

    public bool IsVisibleToEnemy() {
        if (HasConditionEffect(ConditionEffects.Paused))
            return false;
        if (HasConditionEffect(ConditionEffects.Invisible))
            return false;
        if (_newbieTime > 0)
            return false;
        return true;
    }

    private void HandleEffects(RealmTime time) {
        if (HasConditionEffect(ConditionEffects.Healing) && !HasConditionEffect(ConditionEffects.Sick)) {
            if (_healing > 1) {
                HP = Math.Min(Stats[0], HP + (int) _healing);
                _healing -= (int) _healing;
            }

            _healing += 28 * (time.ElapsedMsDelta / 1000f);
        }

        if (HasConditionEffect(ConditionEffects.Quiet) && MP > 0)
            MP = 0;

        if (HasConditionEffect(ConditionEffects.Bleeding) && HP > 1) {
            if (_bleeding > 1) {
                HP -= (int) _bleeding;
                if (HP < 1)
                    HP = 1;
                _bleeding -= (int) _bleeding;
            }

            _bleeding += 28 * (time.ElapsedMsDelta / 1000f);
        }

        if (HasConditionEffect(ConditionEffects.NinjaSpeedy)) {
            MP = Math.Max(0, (int) (MP - 10 * time.ElapsedMsDelta / 1000f));

            if (MP == 0)
                ApplyConditionEffect(ConditionEffectIndex.NinjaSpeedy, 0);
        }

        if (_newbieTime > 0) {
            _newbieTime -= time.ElapsedMsDelta;
            if (_newbieTime < 0)
                _newbieTime = 0;
        }

        if (_canTpCooldownTime > 0) {
            _canTpCooldownTime -= time.ElapsedMsDelta;
            if (_canTpCooldownTime < 0)
                _canTpCooldownTime = 0;
        }
    }

    private bool CanHpRegen() {
        if (HasConditionEffect(ConditionEffects.Sick))
            return false;
        if (HasConditionEffect(ConditionEffects.Bleeding))
            return false;
        return true;
    }

    private bool CanMpRegen() {
        if (HasConditionEffect(ConditionEffects.Quiet) ||
            HasConditionEffect(ConditionEffects.NinjaSpeedy))
            return false;

        return true;
    }

    internal void SetNewbiePeriod() {
        _newbieTime = 3000;
    }

    internal void SetTPDisabledPeriod() {
        _canTpCooldownTime = 10 * 1000; // 10 seconds
    }

    public bool TPCooledDown() {
        if (_canTpCooldownTime > 0)
            return false;
        return true;
    }
}