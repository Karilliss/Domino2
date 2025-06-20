using System.Collections.Generic;

namespace DominoGame
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class DominoGame
    {
        private GameGrid _gameGrid;
        private GameState _gameState;
        private PuzzleGenerator _puzzleGenerator;
        private GameManager _gameManager;
        private HintSystem _hintSystem;
        private Leaderboard _leaderboard;
        private const int GRID_SIZE = 9;

        public DominoGame()
        {
            _gameState = new GameState();
            _gameGrid = new GameGrid(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _puzzleGenerator = new PuzzleGenerator(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _leaderboard = new Leaderboard();
            _gameManager = new GameManager(GRID_SIZE);
            _hintSystem = new HintSystem(_puzzleGenerator, _gameState);
        }

        public bool IsGameCompleted => _gameManager.IsGameCompleted;
        public int HintsUsed => _gameManager.HintsUsed;
        public int MaxHints => _gameManager.MaxHints;
        public int MovesCount => _gameManager.MovesCount;
        public Difficulty CurrentDifficulty => _gameManager.CurrentDifficulty;
        public int GridSize => _gameGrid.GridSize;
        public double ElapsedTime => _gameManager.ElapsedTime;
        public int[][] Grid => _gameGrid.Grid;
        public IReadOnlyList<DominoPiece> AvailablePieces => _gameManager.AvailablePieces;
        public IReadOnlyList<DominoPiece> PlacedPieces => _gameManager.PlacedPieces;
        public int RemainingPiecesCount => _gameManager.RemainingPiecesCount;
        public IReadOnlyList<LeaderboardEntry> LeaderboardEntries => _leaderboard.Entries;

        public bool StartNewGame(Difficulty difficulty)
        {
            _gameState = new GameState();
            _gameGrid = new GameGrid(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _puzzleGenerator = new PuzzleGenerator(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _gameManager = new GameManager(GRID_SIZE);
            _hintSystem = new HintSystem(_puzzleGenerator, _gameState);
            _leaderboard = new Leaderboard();
            _gameState.UpdateElapsedTime(0);
            return _gameManager.GenerateNewGame(difficulty);
        }

        public bool PlacePiece(DominoPiece piece, Position position, Orientation orientation, out string errorMessage)
        {
            return _gameManager.PlacePiece(piece, position, orientation, out errorMessage);
        }

        public bool RemovePiece(Position position, out string errorMessage)
        {
            return _gameManager.RemovePiece(position, out errorMessage);
        }

        public bool MovePiece(Position from, Position to1, Position to2, out string errorMessage)
        {
            return _gameManager.MovePiece(from, to1, to2, out errorMessage);
        }

        public bool RequestHint(out Position pos1, out Position pos2, out int value)
        {
            return _hintSystem.GetHint(out pos1, out pos2, out value);
        }

        public bool CheckCell(Position position, GameGrid grid, out string errorMessage)
        {
            return _hintSystem.CheckCell(position, grid, out errorMessage);
        }

        public bool CheckSolution(out string errorMessage)
        {
            return _gameManager.IsValidSolution(out errorMessage);
        }

        public bool AutoSolve(out string errorMessage)
        {
            return _gameManager.AutoSolve(out errorMessage);
        }

        public bool ResetGame()
        {
            _gameState.UpdateElapsedTime(0);
            return _gameManager.GenerateNewGame(_gameManager.CurrentDifficulty);
        }

        public bool SaveGame(string filename, out string errorMessage)
        {
            return _gameManager.SaveGame(filename, out errorMessage);
        }

        public bool LoadGame(string filename, out string errorMessage)
        {
            return _gameManager.LoadGame(filename, out errorMessage);
        }
    }
}