namespace GameServer.realm.worlds;

public static class DynamicWorld {
    private static readonly List<Type> Worlds;

    static DynamicWorld() {
        Worlds = new List<Type>();

        var type = typeof(World);
        var worlds = type.Assembly.GetTypes().Where(t => type.IsAssignableFrom(t) && type != t);
        foreach (var i in worlds)
            Worlds.Add(i);
    }

    //public static void TryGetWorld(RealmManager manager, WorldTemplateData template, Client client, out World world)
    //{
    //    world = null;

    //    switch (template.StaticId)
    //    {
    //        //case World.Tutorial:
    //        //    world = new Tutorial(template, client);
    //        //    break;
    //        case World.Nexus:
    //            world = new Nexus(template, client);
    //            break;
    //        case World.Test:
    //            world = new Test(template);
    //            break;
    //        case World.Vault:
    //            world = new Vault(template, client);
    //            break;
    //        case World.Realm:
    //            world = new RealmOfTheMadGod(template, client);
    //            break;
    //        case World.GuildHall:
    //            world = new GuildHall(template, client);
    //            break;
    //    }
    //}
}