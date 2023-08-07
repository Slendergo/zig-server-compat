using System.Linq;
using common.resources;
using wServer.realm.entities;

namespace wServer.realm
{
    class BoostStatManager
    {
        private readonly StatsManager _parent;
        private readonly Player _player;
        private readonly SV<int>[] _boostSV;
        private readonly int[] _boost;
        private readonly ActivateBoost[] _activateBoost;

        public ActivateBoost[] ActivateBoost => _activateBoost;
        public int this[int index] => _boost[index];

        public BoostStatManager(StatsManager parent)
        {
            _parent = parent;
            _player = parent.Owner;

            _boost = new int[StatsManager.NumStatTypes];
            _boostSV = new SV<int>[_boost.Length];
            for (var i = 0; i < _boostSV.Length; i++)
                _boostSV[i] = new SV<int>(_player, StatsManager.GetBoostStatType(i), _boost[i], i != 0 && i != 1);
            _activateBoost = new ActivateBoost[_boost.Length];
            for (int i = 0; i < _activateBoost.Length; i++)
                _activateBoost[i] = new ActivateBoost();
            ReCalculateValues();
        }

        protected internal void ReCalculateValues(InventoryChangedEventArgs e = null)
        {
            for (var i = 0; i < _boost.Length; i++)
                _boost[i] = 0;

            ApplyEquipBonus(e);
            ApplyActivateBonus(e);

            for (var i = 0; i < _boost.Length; i++)
                _boostSV[i].SetValue(_boost[i]);
        }

        private void ApplyEquipBonus(InventoryChangedEventArgs e)
        {
            for (var i = 0; i < 4; i++)
            {
                if (_player.Inventory[i] == null)
                    continue;

                foreach (var b in _player.Inventory[i].StatsBoost)
                    IncrementBoost((StatsType)b.Key, b.Value);
            }
        }

        private void ApplyActivateBonus(InventoryChangedEventArgs e)
        {
            for (var i = 0; i < _activateBoost.Length; i++)
            {
                // set boost
                var b = _activateBoost[i].GetBoost();
                _boost[i] += b;
            }
        }

        private void IncrementBoost(StatsType stat, int amount)
        {
            var i = StatsManager.GetStatIndex(stat);
            if (_parent.Base[i] + amount < 1)
            {
                amount = (i == 0) ? -_parent.Base[i] + 1 : -_parent.Base[i];
            }
            _boost[i] += amount;
        }

        private void FixedStat(StatsType stat, int value)
        {
            var i = StatsManager.GetStatIndex(stat);
            _boost[i] = value - _parent.Base[i];
        }
    }
}
