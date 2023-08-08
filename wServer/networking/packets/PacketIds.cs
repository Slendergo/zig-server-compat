﻿
namespace wServer.networking.packets;

public enum C2SPacketId : byte
{
    AcceptTrade = 0,
    AoeAck = 1,
    Buy = 2,
    CancelTrade = 3,
    ChangeGuildRank = 4,
    ChangeTrade = 5,
    CheckCredits = 6,
    ChooseName = 7,
    Create = 8,
    CreateGuild = 9,
    EditAccountList = 10,
    EnemyHit = 11,
    Escape = 12,
    GotoAck = 13,
    GroundDamage = 14,
    GuildInvite = 15,
    GuildRemove = 16,
    Hello = 17,
    InvDrop = 18,
    InvSwap = 19,
    JoinGuild = 20,
    KeyInfoRequest = 21,
    Load = 22,
    Move = 23,
    OtherHit = 24,
    PlayerHit = 25,
    PlayerShoot = 26,
    PlayerText = 27,
    Pong = 28,
    RequestTrade = 29,
    Reskin = 30,
    SetCondition = 31,
    ShootAck = 32,
    SquareHit = 33,
    Teleport = 34,
    UpdateAck = 35,
    UseItem = 36,
    UsePortal = 37,
    
    Unknown = 255
}
    
public enum S2CPacketId : byte
{
    AccountList = 0,
    AllyShoot = 1,
    Aoe = 2,
    BuyResult = 3,
    ClientStat = 4,
    CreateSuccess = 5,
    Damage = 6,
    Death = 7,
    EnemyShoot = 8,
    Failure = 9,
    File = 10,
    GlobalNotification = 11,
    GoTo = 12,
    GuildResult = 13,
    HatchPet = 14,
    InvResult = 15,
    InvitedToGuild = 16, 
    MapInfo = 17,
    NameResult = 18,
    NewTick = 19,
    Notification = 20, 
    Pic = 21,
    Ping = 22,
    PlaySound = 23, 
    QuestObjId = 24,
    Reconnect = 25,
    ServerPlayerShoot = 26,
    ShowEffect = 27,
    Text = 28,
    TradeAccepted = 29,
    TradeChanged = 30,
    TradeDone = 31,
    TradeRequested = 32,
    TradeStart = 33,
    Update = 34,
    
    Unknown = 255
}