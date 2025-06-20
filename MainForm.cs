using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using DominoGame;

namespace DominoGameUI
{
    public partial class MainForm : Form
    {
        private DominoGame.DominoGame _game;
        private TableLayoutPanel _gridPanel;
        private FlowLayoutPanel _dominoPanel;
        private Label _statusLabel;
        private Label _progressLabel;
        private ComboBox _difficultyCombo;
        private Button _newGameButton;
        private Button _resetButton;
        private Button _autoSolveButton;
        private Button _checkSolutionButton;
        private Button _hintButton;
        private Button _saveButton;
        private Button _loadButton;
        private Button _leaderboardButton;
        private Button _cancelButton;
        private DominoControl _selectedDomino;
        private Position _selectedCell;
        private DominoGame.Orientation _selectedOrientation;
        private System.Windows.Forms.Timer _gameTimer;
        private List<Domino> _placedDominoes = new List<Domino>();
        private HashSet<Domino> _selectedDominoes = new HashSet<Domino>();
        private Stack<Domino> _moveStack = new Stack<Domino>();
        private Panel _controlPanel;
        private const int GRID_SIZE = 9;
        private const int CellSize = 50;
        private const int TileWidth = 45;
        private const int TileHeight = 90;
        private const int DotRadius = 6;
        private const int Margin = 10;

        public MainForm()
        {
            _game = new DominoGame.DominoGame();
            InitializeComponents();
            InitializeGrid();
            InitializeDominoPanel();
            UpdateProgressDisplay();
        }

        private void InitializeComponents()
        {
            this.Text = "Domino Puzzle Game";
            this.Size = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            _controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100
            };

            _difficultyCombo = new ComboBox
            {
                Location = new Point(10, 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _difficultyCombo.Items.AddRange(Enum.GetNames(typeof(Difficulty)));
            _difficultyCombo.SelectedIndex = 1; // Default to Medium
            _difficultyCombo.SelectedIndexChanged += (s, e) => StartNewGame();
            _controlPanel.Controls.Add(_difficultyCombo);

            _newGameButton = new Button { Text = "New Game", Location = new Point(120, 10) };
            _newGameButton.Click += NewGameButton_Click;
            _controlPanel.Controls.Add(_newGameButton);

            _resetButton = new Button
            {
                Text = "Reset",
                Location = new Point(200, 10)
            };
            _resetButton.Click += ResetButton_Click;
            _controlPanel.Controls.Add(_resetButton);

            _autoSolveButton = new Button { Text = "Auto Solve", Location = new Point(280, 10) };
            _autoSolveButton.Click += AutoSolveButton_Click;
            _controlPanel.Controls.Add(_autoSolveButton);

            _checkSolutionButton = new Button { Text = "Check Solution", Location = new Point(360, 10) };
            _checkSolutionButton.Click += CheckSolutionButton_Click;
            _controlPanel.Controls.Add(_checkSolutionButton);

            _hintButton = new Button { Text = "Hint", Location = new Point(440, 10) };
            _hintButton.Click += HintButton_Click;
            _controlPanel.Controls.Add(_hintButton);

            _saveButton = new Button { Text = "Save", Location = new Point(520, 10) };
            _saveButton.Click += SaveButton_Click;
            _controlPanel.Controls.Add(_saveButton);

            _loadButton = new Button { Text = "Load", Location = new Point(600, 10) };
            _loadButton.Click += LoadButton_Click;
            _controlPanel.Controls.Add(_loadButton);

            _leaderboardButton = new Button { Text = "Leaderboard", Location = new Point(680, 10) };
            _leaderboardButton.Click += LeaderboardButton_Click;
            _controlPanel.Controls.Add(_leaderboardButton);

            InitializeCancelButton();

            _progressLabel = new Label
            {
                Location = new Point(10, 40),
                AutoSize = true
            };
            _controlPanel.Controls.Add(_progressLabel);

            _statusLabel = new Label
            {
                Location = new Point(10, 70),
                AutoSize = true,
                ForeColor = Color.Red
            };
            _controlPanel.Controls.Add(_statusLabel);

            _gridPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Size = new Size(400, 400),
                RowCount = GRID_SIZE,
                ColumnCount = GRID_SIZE,
                Margin = new Padding(Margin)
            };
            for (int i = 0; i < GRID_SIZE; i++)
            {
                _gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / GRID_SIZE));
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / GRID_SIZE));
            }

            _dominoPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = new Padding(Margin)
            };

            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350
            };
            rightPanel.Controls.Add(_dominoPanel);

            this.Controls.Add(rightPanel);
            this.Controls.Add(_gridPanel);
            this.Controls.Add(_controlPanel);
        }

        private void InitializeCancelButton()
        {
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(760, 10)
            };
            _cancelButton.Click += CancelButton_Click;
            _controlPanel.Controls.Add(_cancelButton);
        }

        private void StartNewGame()
        {
            var difficulty = (Difficulty)_difficultyCombo.SelectedIndex;
            if (_game.StartNewGame(difficulty))
            {
                _placedDominoes.Clear();
                _moveStack.Clear();
                InitializeGrid();
                InitializeDominoPanel();
                UpdateProgressDisplay();
                StartGameTimer();
                SetStatusMessage($"New {difficulty} game started.", Color.Green);
                Console.WriteLine($"Started new game with {difficulty}. Available pieces: {_game.AvailablePieces.Count}");
            }
            else
            {
                SetStatusMessage("Failed to start new game.");
            }
        }

        private void InitializeGrid()
        {
            _gridPanel.Controls.Clear();
            _gridPanel.RowCount = GRID_SIZE;
            _gridPanel.ColumnCount = GRID_SIZE;
            _gridPanel.RowStyles.Clear();
            _gridPanel.ColumnStyles.Clear();
            for (int i = 0; i < GRID_SIZE; i++)
            {
                _gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / GRID_SIZE));
                _gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / GRID_SIZE));
            }
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    var cell = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.White,
                        Tag = new Position(row, col),
                        Margin = new Padding(0),
                        SizeMode = PictureBoxSizeMode.StretchImage
                    };
                    cell.MouseClick += GridCell_MouseClick;
                    _gridPanel.Controls.Add(cell, col, row);
                }
            }
            UpdateGridDisplay();
        }

        private void InitializeDominoPanel()
        {
            _dominoPanel.Controls.Clear();
            foreach (var piece in _game.AvailablePieces)
            {
                // Explicitly filter out duplicates (Value1 == Value2) as a safeguard
                if (piece.Value1 != piece.Value2 && !_game.PlacedPieces.Any(p => p.Equals(piece)))
                {
                    var dominoControl = new DominoControl(piece)
                    {
                        Margin = new Padding(Margin)
                    };
                    dominoControl.Click += DominoControl_Click;
                    _dominoPanel.Controls.Add(dominoControl);
                    Console.WriteLine($"Added DominoControl for piece [{piece.Value1}:{piece.Value2}]");
                }
            }
            if (_dominoPanel.Controls.Count == 0)
            {
                Console.WriteLine("No DominoControls added to panel.");
            }
        }

        private void StartGameTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
            }
            _gameTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _gameTimer.Tick += (s, e) => UpdateProgressDisplay();
            _gameTimer.Start();
        }

        private void UpdateGridDisplay()
        {
            for (int row = 0; row < GRID_SIZE; row++)
            {
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    var cell = (PictureBox)_gridPanel.GetControlFromPosition(col, row);
                    int value = _game.Grid[row][col];
                    var pieceInfo = _game.PlacedPieces
                        .Select((p, idx) => new { Piece = p, Index = idx })
                        .FirstOrDefault(p => p.Piece.GetOccupiedPositions()
                            .Any(pos => pos.Row == row && pos.Col == col));

                    if (pieceInfo != null)
                    {
                        var piece = pieceInfo.Piece;
                        if (piece.Position.Row == row && piece.Position.Col == col)
                        {
                            var bitmap = new Bitmap(CellSize * 2, CellSize);
                            using (Graphics g = Graphics.FromImage(bitmap))
                            {
                                g.Clear(Color.LightGreen);
                                DrawDomino(g, 0, 0, piece.Value1, piece.Value2, piece.Orientation == DominoGame.Orientation.Horizontal);
                            }
                            cell.Image = bitmap;
                            cell.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            cell.Image = null;
                            cell.BackColor = Color.LightGreen;
                        }
                    }
                    else if (value > 0)
                    {
                        cell.Image = null;
                        cell.BackColor = Color.LightGray;
                        using (Graphics g = cell.CreateGraphics())
                        {
                            g.Clear(cell.BackColor);
                            g.DrawString(value.ToString(), this.Font, Brushes.Black, new PointF(10, 10));
                        }
                    }
                    else
                    {
                        cell.Image = null;
                        cell.BackColor = Color.White;
                    }
                }
            }
        }

        private void DrawDomino(Graphics g, int x, int y, int top, int bottom, bool isHorizontal)
        {
            Rectangle rect = new Rectangle(x, y, TileWidth, TileHeight);
            g.FillRectangle(Brushes.LightGray, rect);
            g.DrawRectangle(Pens.Black, rect);

            // Divider line
            g.DrawLine(Pens.Black, x, y + TileHeight / 2, x + TileWidth, y + TileHeight / 2);

            using (Font font = new Font("Arial", 14, FontStyle.Bold))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(top.ToString(), font, Brushes.Black, new RectangleF(x, y, TileWidth, TileHeight / 2), format);
                g.DrawString(bottom.ToString(), font, Brushes.Black, new RectangleF(x, y + TileHeight / 2, TileWidth, TileHeight / 2), format);
            }
        }

        private void UpdateProgressDisplay()
        {
            _progressLabel.Text = $"Placed: {_game.PlacedPieces.Count} | Remaining: {_game.RemainingPiecesCount} | " +
                                  $"Moves: {_game.MovesCount} | Hints: {_game.HintsUsed}/{_game.MaxHints} | " +
                                  $"Time: {(int)_game.ElapsedTime}s";
        }

        private void SetStatusMessage(string message, Color color = default)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = color == default ? Color.Red : color;
        }

        private void GridCell_MouseClick(object sender, MouseEventArgs e)
        {
            var cell = (PictureBox)sender;
            var pos = (Position)cell.Tag;

            if (e.Button == MouseButtons.Right)
            {
                string error = null;
                var piece = _game.PlacedPieces.FirstOrDefault(p => p.GetOccupiedPositions().Any(p => p.Equals(pos)));
                if (piece != null && _game.RemovePiece(pos, out error))
                {
                    var dominoToRemove = _placedDominoes.FirstOrDefault(d =>
                        d.X == pos.Col * CellSize + Margin && d.Y == pos.Row * CellSize + Margin);
                    if (dominoToRemove != null)
                    {
                        _placedDominoes.Remove(dominoToRemove);
                        _moveStack.Clear();
                    }
                    UpdateGridDisplay();
                    InitializeDominoPanel();
                    UpdateProgressDisplay();
                    SetStatusMessage("Piece removed.");
                }
                else
                {
                    SetStatusMessage(error ?? "No domino at selected cell.");
                }
            }
            else if (e.Button == MouseButtons.Left && _selectedDomino != null)
            {
                _selectedCell = pos;
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Place Horizontal").Click += (s, _) => PlaceDomino(DominoGame.Orientation.Horizontal);
                contextMenu.Items.Add("Place Vertical").Click += (s, _) => PlaceDomino(DominoGame.Orientation.Vertical);
                contextMenu.Show(cell, e.Location);
            }
        }

        private void PlaceDomino(DominoGame.Orientation orientation)
        {
            if (_selectedDomino == null || !_selectedCell.IsValid()) return;

            // Validate that the selected domino is not a duplicate
            if (_selectedDomino.Piece.Value1 == _selectedDomino.Piece.Value2)
            {
                SetStatusMessage("Cannot place duplicate piece (e.g., [1:1]).");
                _selectedDomino = null;
                return;
            }

            if (_game.PlacePiece(_selectedDomino.Piece, _selectedCell, orientation, out string error))
            {
                var dominoPos = new Point(_selectedCell.Col * CellSize + Margin, _selectedCell.Row * CellSize + Margin);
                var newDomino = new Domino
                {
                    X = dominoPos.X,
                    Y = dominoPos.Y,
                    Width = orientation == DominoGame.Orientation.Horizontal ? TileWidth * 2 : TileWidth,
                    Height = orientation == DominoGame.Orientation.Horizontal ? TileHeight / 2 : TileHeight
                };
                _placedDominoes.Add(newDomino);
                _moveStack.Push(newDomino);
                UpdateGridDisplay();
                InitializeDominoPanel();
                UpdateProgressDisplay();
                SetStatusMessage("Piece placed.", Color.Green);
                _selectedDomino = null;
                _selectedCell = new Position();
                if (_game.IsGameCompleted)
                {
                    SetStatusMessage("Game completed successfully!", Color.Green);
                }
            }
            else
            {
                SetStatusMessage(error);
            }
        }

        private void DominoControl_Click(object sender, EventArgs e)
        {
            _selectedDomino = (DominoControl)sender;
            _selectedCell = new Position();
            if (_selectedDomino.Piece.Value1 == _selectedDomino.Piece.Value2)
            {
                SetStatusMessage("Duplicate piece selected (e.g., [1:1]). Please select a non-duplicate piece.");
                _selectedDomino = null;
                return;
            }
            SetStatusMessage($"Selected domino [{_selectedDomino.Piece.Value1}:{_selectedDomino.Piece.Value2}]. Click a cell to place.", Color.Blue);
        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            StartNewGame();
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            _placedDominoes.Clear();
            _selectedDominoes.Clear();
            _moveStack.Clear();
            if (_game.ResetGame())
            {
                InitializeGrid();
                InitializeDominoPanel();
                UpdateProgressDisplay();
                StartGameTimer();
                SetStatusMessage("Game reset.", Color.Green);
            }
            else
            {
                SetStatusMessage("Failed to reset game.");
            }
        }

        private void AutoSolveButton_Click(object sender, EventArgs e)
        {
            if (_game.AutoSolve(out string error))
            {
                _placedDominoes.Clear();
                _moveStack.Clear();
                UpdateGridDisplay();
                InitializeDominoPanel();
                UpdateProgressDisplay();
                SetStatusMessage("Game solved automatically.", Color.Green);
            }
            else
            {
                SetStatusMessage(error);
            }
        }

        private void CheckSolutionButton_Click(object sender, EventArgs e)
        {
            if (_game.CheckSolution(out string error))
            {
                SetStatusMessage("Solution is correct!", Color.Green);
            }
            else
            {
                SetStatusMessage(error);
            }
        }

        private void HintButton_Click(object sender, EventArgs e)
        {
            if (_game.RequestHint(out Position pos1, out Position pos2, out int value))
            {
                UpdateGridDisplay();
                InitializeDominoPanel();
                UpdateProgressDisplay();
                SetStatusMessage($"Place domino with sum {value} at ({pos1.Row},{pos1.Col})-({pos2.Row},{pos2.Col}).", Color.Blue);
                HighlightCells(pos1, pos2);
            }
            else
            {
                SetStatusMessage("No hints available or hint limit reached.");
            }
        }

        private void HighlightCells(Position pos1, Position pos2)
        {
            foreach (PictureBox cell in _gridPanel.Controls)
            {
                var pos = (Position)cell.Tag;
                if ((pos.Equals(pos1) || pos.Equals(pos2)) && cell.Image == null)
                {
                    cell.BackColor = Color.Yellow;
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog { Filter = "Domino Game Files|*.dlg" })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (_game.SaveGame(saveDialog.FileName, out string error))
                    {
                        SetStatusMessage("Game saved.", Color.Green);
                    }
                    else
                    {
                        SetStatusMessage(error);
                    }
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog { Filter = "Domino Game Files|*.dlg" })
            {
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    if (_game.LoadGame(openDialog.FileName, out string error))
                    {
                        _placedDominoes.Clear();
                        _moveStack.Clear();
                        InitializeGrid();
                        InitializeDominoPanel();
                        UpdateProgressDisplay();
                        StartGameTimer();
                        SetStatusMessage("Game loaded.", Color.Green);
                    }
                    else
                    {
                        SetStatusMessage(error);
                    }
                }
            }
        }

        private void LeaderboardButton_Click(object sender, EventArgs e)
        {
            var leaderboardForm = new LeaderboardForm(_game.LeaderboardEntries);
            leaderboardForm.ShowDialog();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (_moveStack.Count > 0)
            {
                var lastMove = _moveStack.Pop();
                var piece = _game.PlacedPieces.LastOrDefault(p => p.GetOccupiedPositions()
                    .Any(pos => pos.Row == lastMove.Y / CellSize && pos.Col == lastMove.X / CellSize));
                if (piece != null && _game.RemovePiece(piece.Position, out string error))
                {
                    _placedDominoes.Remove(lastMove);
                    UpdateGridDisplay();
                    InitializeDominoPanel();
                    UpdateProgressDisplay();
                    SetStatusMessage("Last move canceled.", Color.Green);
                }
                else
                {
                    SetStatusMessage("Failed to cancel move.");
                }
            }
            else
            {
                SetStatusMessage("No moves to cancel.");
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            foreach (var d in _placedDominoes)
            {
                e.Graphics.FillRectangle(Brushes.LightGray, d.X, d.Y, d.Width, d.Height);
                e.Graphics.DrawRectangle(Pens.Black, d.X, d.Y, d.Width, d.Height);
                if (_selectedDominoes.Contains(d))
                {
                    e.Graphics.DrawRectangle(Pens.Red, new Rectangle(d.X, d.Y, d.Width, d.Height));
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            Point gridLocation = _gridPanel.PointToClient(this.PointToScreen(e.Location));
            foreach (var d in _placedDominoes)
            {
                Rectangle tileRect = new Rectangle(d.X, d.Y, d.Width, d.Height);
                if (tileRect.Contains(gridLocation))
                {
                    if (_selectedDominoes.Contains(d))
                        _selectedDominoes.Remove(d);
                    else
                        _selectedDominoes.Add(d);
                    Invalidate();
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gameTimer?.Stop();
                _gameTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class Domino
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}