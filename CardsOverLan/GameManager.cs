﻿using CardsOverLan.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan
{
    internal sealed class GameManager
    {
        private const string PacksDirectory = "packs";
        private const string SettingsFilePath = "settings.json";

        public static GameManager Instance { get; }

        public GameSettings Settings { get; }

        public CardGame Game { get; }

        private readonly List<Pack> _packs;

        static GameManager()
        {
            Instance = new GameManager();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Load()
        {
        }

        private GameManager()
        {
            _packs = new List<Pack>();

            // Load the settings
            Settings = GameSettings.FromFile(SettingsFilePath);

            // Load all the decks
            foreach (var deckPath in Directory.EnumerateFiles(PacksDirectory, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var pack = JsonConvert.DeserializeObject<Pack>(File.ReadAllText(deckPath));
                    _packs.Add(pack);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load deck '{deckPath}': {ex}");
                }
            }

            Game = new CardGame(_packs, Settings);

            Console.WriteLine("\n========= GAME STARTING =========\n");
            Console.WriteLine($"Player limit: [{Settings.MinPlayers}, {Settings.MaxPlayers}]");
            Console.WriteLine($"Hand size: {Settings.HandSize}");
            Console.WriteLine($"Perma-Czar: {Settings.PermanentCzar}");
            Console.WriteLine($"Points to win: {Settings.MaxPoints}");
            Console.WriteLine($"Cards: {Game.BlackCardCount + Game.WhiteCardCount} ({Game.WhiteCardCount}x white, {Game.BlackCardCount}x black)");
            Console.WriteLine();
            Console.WriteLine($"Packs:\n{_packs.Select(d => $"        [{d}]").Aggregate((c, n) => $"{c}\n{n}")}");
            Console.WriteLine("\n=================================\n");

            Game.GameStateChanged += OnGameStateChanged;
            Game.RoundStarted += OnGameRoundStarted;
            Game.StageChanged += OnGameStageChanged;
            Game.RoundEnded += OnGameRoundEnded;
            Game.GameEnded += OnGameEnded;
        }

        public object GetGameInfoObject()
        {
            return new
            {
                server_name = Settings.ServerName,
                min_players = Settings.MinPlayers,
                current_player_count = Game.PlayerCount,
                max_players = Settings.MaxPlayers,
                white_card_count = Game.WhiteCardCount,
                black_card_count = Game.BlackCardCount,
                pack_info = _packs.Select(p => new { id = p.Id, name = p.Name })
            };
        }

        private void OnGameEnded(Player[] winners)
        {
            Console.WriteLine($"Game ended. Winners: {winners.Select(w => w.ToString()).Aggregate((c, n) => $"{c}, {n}")}");
        }

        private void OnGameRoundEnded(int round, Player roundWinner)
        {
            Console.WriteLine($"Round {round} ended: {roundWinner?.ToString() ?? "Nobody"} wins!");
        }

        private void OnGameStageChanged(in GameStage oldStage, in GameStage currentStage)
        {
            Console.WriteLine($"Stage changed: {oldStage} -> {currentStage}");
        }

        private void OnGameRoundStarted()
        {
            Console.WriteLine($"ROUND {Game.Round}:");
            Console.WriteLine($"Current black card: {Game.CurrentBlackCard}");
            Console.WriteLine($"Judge is {Game.Judge}");
        }

        private void OnGameStateChanged()
        {
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Console.Title = $"Cards Over LAN Server ({Game.PlayerCount})";
        }
    }
}