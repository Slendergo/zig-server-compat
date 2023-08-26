using common.resources;
using wServer.networking;

namespace wServer.realm.worlds.logic;

public class GuildHall : World
{
    public int GuildId { get; private set; }

    public GuildHall(RealmManager manager, WorldTemplateData template, Client client) 
        : base(manager, template)
    {
        GuildId = client.Account.GuildId;
    }

    public override bool AllowedAccess(Client client) => base.AllowedAccess(client) && client.Account.GuildId == GuildId;

    public override void Init()
    {
    }

    public override string SelectMap(WorldTemplateData template) => template.Maps[Manager.Database.GetGuild(GuildId)?.Level ?? 0];
}