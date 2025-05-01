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

        // Player Settings
        public PlayerButtons[] LastPlayerButtons = new PlayerButtons[33];
        public int[] TotalJumpsDid = new int[33];
        
        public int AdditionalsJumpAvailable = 0;

        public PluginConfig Config { get; set; } = new();

        public override void Load(bool hotReload)
        {
        }

        public void SetVars()
        {
            // Removing exists events
            RemoveListener<Listeners.OnTick>(OnTickExecuted);
            // Register the listener if we have at least 1 additional jump
            if (Config.AdditionalsJump > 0)
            {
                RegisterListener<Listeners.OnTick>(OnTickExecuted);
            }

            // Convert additoonal jumps + 1 (num of jumps + first jump player did)
            AdditionalsJumpAvailable = Config.AdditionalsJump + 1;
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // Redirect to this function
            SetVars();
        }

        private void OnTickExecuted()
        {
            var players = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected && p.Team > CsTeam.Spectator);

            // Vars used latetly in loop
            PlayerButtons LastButtons;
            int Jumps = 0;
            int Slot = 0;

            // Loop through enumerate & filtred players 
            foreach (var player in players)
            {
                if (player == null) continue;

                Slot = player.Slot;
                LastButtons = LastPlayerButtons[Slot];
                Jumps = TotalJumpsDid[Slot];

                var buttons = player.Buttons;

                // In jump (Prvent holding spaces by check if the last button was space)
                if ((buttons & PlayerButtons.Jump) != 0 && (LastButtons & PlayerButtons.Jump) == 0)
                {
                    // Reached max jumps
                    if (Jumps != AdditionalsJumpAvailable)
                    {
                        var pawn = player?.Pawn?.Value;
                        var Origin = pawn?.AbsVelocity;
                        if (pawn != null && Origin != null)
                        {
                            pawn.AbsVelocity.Z = 300;
                            TotalJumpsDid[Slot] += 1;
                        }
                    }
                }

                // Need to reset user jumps when he hit the ground? Yep
                if (Jumps != 0 && player?.PlayerPawn?.Value?.Flags != null && ((PlayerFlags)player.PlayerPawn.Value.Flags & PlayerFlags.FL_ONGROUND) != 0)
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
        public HookResult OnClientSpawn(EventPlayerSpawn @event, GameEventInfo info)
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
