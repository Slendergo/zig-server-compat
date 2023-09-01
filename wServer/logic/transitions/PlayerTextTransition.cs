using common;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using wServer.realm;
using wServer.realm.entities;

namespace wServer.logic.transitions;

class PlayerTextTransition : Transition
{
    //State storage: none

    private readonly double? _distSqr;
    private readonly string _regex;
    private readonly bool _setAttackTarget;
    private readonly bool _ignoreCase;

    private bool _transition;
    private Player _player;

    public PlayerTextTransition(XElement e)
    : base(e.ParseString("@targetState", "root"))
    {
        _regex = e.ParseString("@regex");
        _distSqr = e.ParseNFloat("@dist") == null ? null : e.ParseNFloat("@dist") * e.ParseNFloat("@dist");
        _setAttackTarget = e.ParseBool("@setAttackTarget");
        _ignoreCase = e.ParseBool("@ignoreCase");
    }

    public PlayerTextTransition(
        string targetState, 
        string regex, 
        double? dist = null,
        bool setAttackTarget = false,
        bool ignoreCase = true)
        : base(targetState)
    {
        if (dist != null)
            _distSqr = dist.Value * dist.Value;
        _regex = regex;
        _setAttackTarget = setAttackTarget;
        _ignoreCase = ignoreCase;
    }

    protected override bool TickCore(Entity host, RealmTime time, ref object state)
    {
        if (_transition == false ||
            host.Owner == null ||
            _player == null ||
            !host.Owner.Players.Values.Contains(_player))
        {
            return false;
        }
            
        host.AttackTarget = _setAttackTarget ? _player : null;

        if (_distSqr != null)
            return MathsUtils.DistSqr(_player.X, _player.Y, host.X, host.Y) <= _distSqr;
        return true;
    }

    public void OnChatReceived(Player player, string text)
    {
        var rgx = (_ignoreCase)
            ? new Regex(_regex, RegexOptions.IgnoreCase)
            : new Regex(_regex);

        var match = rgx.Match(text);
        if (!match.Success)
        {
            _transition = false;
            _player = null;
            return;
        }

        _transition = true;
        _player = player;
    }
}