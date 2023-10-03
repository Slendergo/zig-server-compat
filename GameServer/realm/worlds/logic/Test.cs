using Shared.resources;

namespace GameServer.realm.worlds.logic;

public class Test : World {
    public Test(RealmManager manager, WorldTemplateData template)
        : base(manager, template) { }

    public bool JsonLoaded { get; private set; }

    public override void Init() { }

    public void LoadJson(string json) {
        if (!JsonLoaded) {
            FromJson(json);
            JsonLoaded = true;
        }
    }
}