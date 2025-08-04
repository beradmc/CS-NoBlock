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

namespace NoBlock;

[MinimumApiVersion(80)]
public class NoBlockPlugin : BasePlugin
{
    public override string ModuleName => "NoBlock Plugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "beratfps";
    public override string ModuleDescription => "Allows players to pass through each other without collision";

    private readonly NoBlockManager _noBlockManager;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _initializationTimer;

    public NoBlockPlugin()
    {
        _noBlockManager = new NoBlockManager(this);
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        
        _initializationTimer = AddTimer(2.0f, () => 
        {
            try
            {
                ApplyNoBlockToAllPlayers();
            }
            catch (Exception)
            {
            }
        });
    }

    public override void Unload(bool hotReload)
    {
        _initializationTimer?.Kill();
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsValidPlayer(player)) return HookResult.Continue;

        AddTimer(0.1f, () => _noBlockManager.ApplyNoBlockToPlayer(player));
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsValidPlayer(player)) return HookResult.Continue;

        AddTimer(0.1f, () => _noBlockManager.ApplyNoBlockToPlayer(player));
        return HookResult.Continue;
    }

    private bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && 
               player.IsValid && 
               player.PlayerPawn?.Value != null && 
               player.PlayerPawn.Value.IsValid &&
               player.TeamNum > 1;
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
                    AddTimer(0.1f, () => _noBlockManager.ApplyNoBlockToPlayer(player));
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

    public NoBlockManager(NoBlockPlugin plugin)
    {
        _plugin = plugin;
    }

    public void ApplyNoBlockToPlayer(CCSPlayerController player)
    {
        if (player?.PlayerPawn?.Value == null) return;

        try
        {
            var pawn = player.PlayerPawn.Value;
            if (!pawn.IsValid || !player.PawnIsAlive) return;

            var collision = pawn.Collision;
            if (collision == null) return;
            
            collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            CollisionRulesChanged(pawn);
        }
        catch (Exception)
        {
        }
    }

    private void CollisionRulesChanged(CCSPlayerPawn pawn)
    {
        try
        {
            VirtualFunction.CreateVoid<CCSPlayerPawn>(pawn.Handle, GameData.GetOffset("CollisionRulesChanged"))(pawn);
        }
        catch (Exception)
        {
        }
    }
}
