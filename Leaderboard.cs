using System;
using System.Collections.Generic;
using System.IO;

namespace DominoGame
{
    public class Leaderboard
    {
        private readonly string _filePath;
        private readonly List<LeaderboardEntry> _entries;

        public Leaderboard(string filePath = "leaderboard.bin")
        {
            _filePath = filePath;
            _entries = LoadLeaderboard();
        }

        public IReadOnlyList<LeaderboardEntry> Entries => _entries.AsReadOnly();

        public void AddResult(string playerName, Difficulty difficulty, double time, int moves, int hints)
        {
            var entry = new LeaderboardEntry
            {
                PlayerName = playerName,
                Difficulty = difficulty,
                Time = time,
                Moves = moves,
                Hints = hints,
                Date = DateTime.Now
            };

            _entries.Add(entry);
            _entries.Sort((a, b) => a.Time.CompareTo(b.Time));
            if (_entries.Count > 10)
                _entries.RemoveRange(10, _entries.Count - 10);

            SaveLeaderboard();
        }

        private List<LeaderboardEntry> LoadLeaderboard()
        {
            try
            {
                if (!File.Exists(_filePath)) return new List<LeaderboardEntry>();

                using (var file = new BinaryReader(File.Open(_filePath, FileMode.Open)))
                {
                    int count = file.ReadInt32();
                    var entries = new List<LeaderboardEntry>();
                    for (int i = 0; i < count; i++)
                    {
                        entries.Add(new LeaderboardEntry
                        {
                            PlayerName = file.ReadString(),
                            Difficulty = (Difficulty)file.ReadInt32(),
                            Time = file.ReadDouble(),
                            Moves = file.ReadInt32(),
                            Hints = file.ReadInt32(),
                            Date = DateTime.FromBinary(file.ReadInt64())
                        });
                    }
                    return entries;
                }
            }
            catch
            {
                return new List<LeaderboardEntry>();
            }
        }

        private void SaveLeaderboard()
        {
            try
            {
                using (var file = new BinaryWriter(File.Open(_filePath, FileMode.Create)))
                {
                    file.Write(_entries.Count);
                    foreach (var entry in _entries)
                    {
                        file.Write(entry.PlayerName);
                        file.Write((int)entry.Difficulty);
                        file.Write(entry.Time);
                        file.Write(entry.Moves);
                        file.Write(entry.Hints);
                        file.Write(entry.Date.ToBinary());
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to save leaderboard.");
            }
        }
    }

    public class LeaderboardEntry
    {
        public string PlayerName { get; set; }
        public Difficulty Difficulty { get; set; }
        public double Time { get; set; }
        public int Moves { get; set; }
        public int Hints { get; set; }
        public DateTime Date { get; set; }
    }
}