using Shared.resources;

namespace GameServer.realm.worlds.logic;

public class GuildHall : World {
    public GuildHall(RealmManager manager, WorldTemplateData template, Client client)
        : base(manager, template) {
        GuildId = client.Account.GuildId;
    }

    public int GuildId { get; }

    public override bool AllowedAccess(Client client) {
        return base.AllowedAccess(client) && client.Account.GuildId == GuildId;
    }

    public override void Init() { }

    public override string SelectMap(WorldTemplateData template) {
        return template.Maps[Manager.Database.GetGuild(GuildId)?.Level ?? 0];
    }
}