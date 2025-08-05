using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace NoBlock;

[MinimumApiVersion(80)]
public class NoBlockPlugin : BasePlugin
{
    public override string ModuleName => "NoBlock Plugin";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "beratfps";
    public override string ModuleDescription => "Allows players to pass through each other without collision. GitHub: https://github.com/beradmc/CS2-NoBlock";

    private readonly NoBlockManager _noBlockManager;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _initializationTimer;

    public NoBlockPlugin()
    {
        _noBlockManager = new NoBlockManager(this);
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        
        _initializationTimer = AddTimer(2.0f, ApplyNoBlockToAllPlayers);
    }

    public override void Unload(bool hotReload)
    {
        _initializationTimer?.Kill();
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (IsValidPlayer(player))
        {
            Server.NextFrame(() => _noBlockManager.ApplyNoBlockToPlayer(player));
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (IsValidPlayer(player))
        {
            AddTimer(2.0f, () => _noBlockManager.ApplyNoBlockToPlayer(player));
        }
        return HookResult.Continue;
    }

    private bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid && player.TeamNum > 1;
    }

    public void ApplyNoBlockToAllPlayers()
    {
        try
        {
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (IsValidPlayer(player))
                {
                    AddTimer(1.0f, () => _noBlockManager.ApplyNoBlockToPlayer(player));
                }
            }
        }
        catch (Exception)
        {
        }
    }
}

public class NoBlockManager
{
    private readonly NoBlockPlugin _plugin;
    private static readonly WIN_LINUX<int> OnCollisionRulesChangedOffset = new WIN_LINUX<int>(173, 172);

    public NoBlockManager(NoBlockPlugin plugin)
    {
        _plugin = plugin;
    }

    public void ApplyNoBlockToPlayer(CCSPlayerController player)
    {
        try
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive) return;
            
            CCSPlayerPawn? pawn = GetPlayerPawn(player);
            if (pawn == null || !pawn.IsValid) return;

            var collision = pawn.Collision;
            if (collision == null) return;
            
            ApplyCollisionSettings(collision, pawn);
        }
        catch (Exception)
        {
        }
    }

    private void ApplyCollisionSettings(CCollisionProperty collision, CCSPlayerPawn pawn)
    {
        collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
        
        VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(pawn.Handle, OnCollisionRulesChangedOffset.Get());
        collisionRulesChanged.Invoke(pawn.Handle);
    }

    private CCSPlayerPawn? GetPlayerPawn(CCSPlayerController player)
    {
        try
        {
            var playerPawnProperty = typeof(CCSPlayerController).GetProperty("PlayerPawn");
            if (playerPawnProperty != null)
            {
                var playerPawnValue = playerPawnProperty.GetValue(player);
                if (playerPawnValue != null)
                {
                    var valueProperty = playerPawnValue.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        return valueProperty.GetValue(playerPawnValue) as CCSPlayerPawn;
                    }
                }
            }
        }
        catch
        {
            try
            {
                return player.PlayerPawn?.Value;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }
}

public class WIN_LINUX<T>
{
    [JsonPropertyName("Windows")]
    public T Windows { get; private set; }

    [JsonPropertyName("Linux")]
    public T Linux { get; private set; }

    public WIN_LINUX(T windows, T linux)
    {
        this.Windows = windows;
        this.Linux = linux;
    }

    public T Get()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return this.Windows;
        }
        else
        {
            return this.Linux;
        }
    }
}
