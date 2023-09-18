using System.Xml.Linq;

namespace common.resources;

public class AppSettings {
    public readonly int MaxStackablePotions;
    public readonly NewAccounts NewAccounts;
    public readonly NewCharacters NewCharacters;

    public readonly int UseExternalPayments;
    public readonly XElement Xml;

    public AppSettings(string dir) {
        var e = XElement.Parse(Utils.Read(dir));
        Xml = e;
        UseExternalPayments = e.GetValue<int>("UseExternalPayments");
        MaxStackablePotions = e.GetValue<int>("MaxStackablePotions");

        var newAccounts = e.Element("NewAccounts");
        NewAccounts = new NewAccounts(e.Element("NewAccounts"));
        newAccounts.Remove(); // don't export with /app/init

        var newCharacters = e.Element("NewCharacters");
        NewCharacters = new NewCharacters(e.Element("NewCharacters"));
        newCharacters.Remove();
    }
}

public class NewAccounts {
    public readonly bool ClassesUnlocked;
    public readonly int Credits;
    public readonly int Fame;
    public readonly int MaxCharSlot;
    public readonly bool SkinsUnlocked;
    public readonly int SlotCost;
    public readonly CurrencyType SlotCurrency;
    public readonly int VaultCount;

    public NewAccounts(XElement e) {
        MaxCharSlot = e.GetValue("MaxCharSlot", 1);
        VaultCount = e.GetValue("VaultCount", 1);
        Fame = e.GetValue<int>("Fame");
        Credits = e.GetValue<int>("Credits");

        ClassesUnlocked = e.HasElement("ClassesUnlocked");
        SkinsUnlocked = e.HasElement("SkinsUnlocked");

        SlotCost = e.GetValue("SlotCost", 1000);
        SlotCurrency = (CurrencyType) e.GetValue<int>("SlotCurrency");
        if (SlotCurrency != CurrencyType.Fame && SlotCurrency != CurrencyType.Gold)
            SlotCurrency = CurrencyType.Gold;
    }
}

public class NewCharacters {
    public readonly int Level;
    public readonly bool Maxed;

    public NewCharacters(XElement e) {
        Maxed = e.HasElement("Maxed");
        Level = e.GetValue("Level", 1);
    }
}