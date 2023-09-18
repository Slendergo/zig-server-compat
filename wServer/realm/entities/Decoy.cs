using Mono.Game;

namespace wServer.realm.entities;

internal class Decoy : StaticObject, IPlayer {
    private static Random rand = new();
    private Vector2 direction;
    private int duration;

    private bool exploded;

    private Player player;
    private float speed;

    public Decoy(Player player, int duration, float tps)
        : base(player.Manager, 0x0715, duration, true, true, true) {
        this.player = player;
        this.duration = duration;
        speed = tps;

        var history = player.TryGetHistory(1);
        if (history == null) {
            direction = GetRandDirection();
        }
        else {
            direction = new Vector2(player.X - history.Value.X, player.Y - history.Value.Y);
            if (direction.LengthSquared() == 0)
                direction = GetRandDirection();
            else
                direction.Normalize();
        }
    }

    public void Damage(int dmg, Entity src) { }

    public bool IsVisibleToEnemy() {
        return true;
    }

    private Vector2 GetRandDirection() {
        var angle = rand.NextDouble() * 2 * Math.PI;
        return new Vector2(
            (float) Math.Cos(angle),
            (float) Math.Sin(angle)
        );
    }

    protected override void ExportStats(IDictionary<StatsType, object> stats) {
        stats[StatsType.Texture1] = player.Texture1;
        stats[StatsType.Texture2] = player.Texture2;
        base.ExportStats(stats);
    }

    public override void Tick(RealmTime time) {
        if (HP > duration - 2000)
            ValidateAndMove(
                X + direction.X * speed * time.ElapsedMsDelta / 1000,
                Y + direction.Y * speed * time.ElapsedMsDelta / 1000
            );
        if (HP < 250 && !exploded) {
            exploded = true;

            foreach (var otherPlayer in Owner.Players.Values)
                if (otherPlayer.DistSqr(this) < Player.RadiusSqr)
                    otherPlayer.Client.SendShowEffect(EffectType.AreaBlast, Id, new Position {X = 1}, new Position(),
                        new ARGB(0xffff0000));
        }

        base.Tick(time);
    }
}