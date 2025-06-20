using System;
using System.Windows.Forms;
using DominoGame;
using System.Collections.Generic;

namespace DominoGameUI
{
    public class LeaderboardForm : Form
    {
        public LeaderboardForm(IReadOnlyList<LeaderboardEntry> entries)
        {
            this.Text = "Leaderboard";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            InitializeComponents(entries);
        }

        private void InitializeComponents(IReadOnlyList<LeaderboardEntry> entries)
        {
            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            listView.Columns.Add("Rank", 50);
            listView.Columns.Add("Player", 100);
            listView.Columns.Add("Difficulty", 80);
            listView.Columns.Add("Time (s)", 60);
            listView.Columns.Add("Moves", 60);
            listView.Columns.Add("Hints", 50);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(entry.PlayerName);
                item.SubItems.Add(entry.Difficulty.ToString());
                item.SubItems.Add(entry.Time.ToString("F2"));
                item.SubItems.Add(entry.Moves.ToString());
                item.SubItems.Add(entry.Hints.ToString());
                listView.Items.Add(item);
            }

            var closeButton = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            closeButton.Click += (s, e) => this.Close();

            this.Controls.Add(listView);
            this.Controls.Add(closeButton);
        }

        private void InitializeComponent()
        {

        }
    }
}