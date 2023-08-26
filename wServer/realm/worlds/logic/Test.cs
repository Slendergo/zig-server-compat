using common.resources;
using terrain;

namespace wServer.realm.worlds.logic;

public class Test : World
{
    public bool JsonLoaded { get; private set; }

    public Test(RealmManager manager, WorldTemplateData template)
        : base(manager, template)
    {
    }

    public override void Init() { }

    public void LoadJson(string json)
    {
        if (!JsonLoaded)
        {
            FromJson(json);
            JsonLoaded = true;
        }
    }
}