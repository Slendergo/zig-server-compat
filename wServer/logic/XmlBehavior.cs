using common;
using System.Reflection;
using System.Xml.Linq;
using wServer.logic;
using wServer.logic.loot;

namespace wServer.logic;

public class XmlBehaviorEntry
{
    public readonly string Id;
    public readonly IStateChildren[] Behaviors;
    public readonly MobDrops[] Loots;

    public XmlBehaviorEntry(XElement e, string id)
    {
        Id = id;
            
        var behavTypes = Assembly.GetCallingAssembly().GetTypes()
            .Where(type => typeof(IStateChildren).IsAssignableFrom(type) && !type.IsInterface)
            .Select(type => type).ToArray();
        var lootTypes = Assembly.GetCallingAssembly().GetTypes()
            .Where(type => typeof(MobDrops).IsAssignableFrom(type) && !type.IsInterface)
            .Select(type => type).ToArray();
            
        var children = new List<IStateChildren>();
        var behaviors = new List<IStateChildren>();
        var loots = new List<MobDrops>();
            
        if (e.Elements("State").Any())
            ParseStates(e, behavTypes, ref children);

        if (e.Elements().Any(elem => behavTypes.Any(type => type.Name == elem.Name.ToString())))
            ParseBehaviors(e, behavTypes, ref children);

        var state = (IStateChildren)Activator.CreateInstance(behavTypes.Single(x => x.Name == "State"),
            "root", children.ToArray());
        behaviors.Add(state);
        children.Clear();
            
        foreach (var i in e.Elements().Where(elem => lootTypes.Any(type => type.Name == elem.Name.ToString()))) {
            var behavior = (MobDrops)Activator.CreateInstance(lootTypes.Single(x => x.Name == i.Name.ToString()), i);
            if (behavior is null)
                continue;

            loots.Add(behavior);
        }
            
        Behaviors = behaviors.ToArray();
        Loots = loots.ToArray();
    }

    private static void ParseStates(XElement e, Type[] behavTypes, ref List<IStateChildren> behaviors)
    {
        var children = new List<IStateChildren>();
        foreach (var i in e.Elements("State"))
        {
            if (i.Elements("State").Any())
                ParseStates(i, behavTypes, ref children);

            if (i.Elements().Any(elem => behavTypes.Any(type => type.Name == elem.Name.ToString())))
                ParseBehaviors(i, behavTypes, ref children);

            var state = (IStateChildren)Activator.CreateInstance(behavTypes.Single(x => x.Name == "State"),
                i.GetAttribute<string>("id"), children.ToArray());
            behaviors.Add(state);
            children.Clear();
        }
    }

    private static void ParseBehaviors(XElement e, Type[] behavTypes, ref List<IStateChildren> behaviors)
    {
        var children = new List<IStateChildren>();
        foreach (var i in e.Elements().Where(elem => behavTypes.Any(type => type.Name == elem.Name.ToString())))
        {
            if (i.Elements().Any(elem => behavTypes.Any(type => type.Name == elem.Name.ToString())))
                ParseBehaviors(i, behavTypes, ref children);

            var name = i.Attribute("behavior") != null ? i.GetAttribute<string>("behavior") : i.Name.ToString();
            IStateChildren behavior;
            if (children.Count > 0)
                behavior = (IStateChildren)Activator.CreateInstance(behavTypes.Single(x => x.Name == name), i,
                    children.ToArray());
            else
                behavior = (IStateChildren)Activator.CreateInstance(behavTypes.Single(x => x.Name == name), i);
            behaviors.Add(behavior);
            children.Clear();
        }
    }
}