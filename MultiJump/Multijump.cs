using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace MultiJump
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("additional_jumps")] public int AdditionalsJump { get; set; } = 0;
    }

    public class Multijump : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "MultiJump";
        public override string ModuleAuthor => "AMG";
        public override string ModuleVersion => "1.0";
        public override string ModuleDescription => "Allow multiple jumps for everyone";

        // Pllayer Settings
        
        public PlayerButtons[] LastPlayerButtons = new PlayerButtons[33];
        public int[] TotalJumpsDid = new int[33];
        

        public int AdditionalsJumpAvailable = 0;

        

        public PluginConfig Config { get; set; } = new();

        public override void Load(bool hotReload)
        {
            // Delete everything from the dict on load
        }

        public void SetVars()
        {
            // Don't want additionals jumps? Remove OnTick Listener to save up mem.
            if (Config.AdditionalsJump <= 0)
            {
                RemoveListener<Listeners.OnTick>(OnTickExecuted);
            }
            else
            {
                RegisterListener<Listeners.OnTick>(OnTickExecuted);
            }
            AdditionalsJumpAvailable = Config.AdditionalsJump + 1;
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // Redirect to this function
            SetVars();
        }

        public void OnTickExecuted()
        {
            var players = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && p.Team > CsTeam.Spectator);

            PlayerButtons LastButtons;
            int Jumps = 0;
            int Slot = 0;

            foreach (var player in players)
            {
                if (player == null) continue;

                Slot = player.Slot;
                LastButtons = LastPlayerButtons[Slot];
                Jumps = TotalJumpsDid[Slot];

                var buttons = player.Buttons;

                // In jump
                if ((buttons & PlayerButtons.Jump) != 0 && (LastButtons & PlayerButtons.Jump) == 0)
                {
                    // Reached max jumps
                    if (Jumps != AdditionalsJumpAvailable)
                    {
                        var pawn = player?.Pawn?.Value;
                        var Origin = pawn?.AbsVelocity;
                        if (pawn != null && Origin != null)
                        {
                            pawn.Teleport(null, null, new Vector(Origin.X, Origin.Y, Random.Shared.Next(300, 320)));
                            TotalJumpsDid[Slot] += 1;
                        }
                    }
                }

                if (Jumps != 0 && player?.PlayerPawn?.Value?.OnGroundLastTick != null && player.PlayerPawn.Value.OnGroundLastTick)
                    TotalJumpsDid[Slot] = 0;

                LastPlayerButtons[Slot] = buttons;
            }
        }

        [ConsoleCommand("css_reloadconfig")]
        [CommandHelper(0, "", CommandUsage.SERVER_ONLY)]
        public void OnConfigReload(CCSPlayerController?player, CommandInfo info)
        {
            Config.Reload();
            SetVars();
            Logger.LogInformation("Config Reloaded!");
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnClietSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return 0;

            // Reset vars on full connect
            TotalJumpsDid[player.Slot] = 0;
            LastPlayerButtons[player.Slot] = 0;

            return 0;
        }
    }
}
