using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DominoGame
{
    public class GameManager
    {
        private GameGrid _gameGrid;
        private GameState _gameState;
        private PuzzleGenerator _puzzleGenerator;
        private Leaderboard _leaderboard;
        private const int GRID_SIZE = 9;

        public GameManager()
        {
            _gameState = new GameState();
            _gameGrid = new GameGrid(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _puzzleGenerator = new PuzzleGenerator(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _leaderboard = new Leaderboard();
        }

        public GameManager(int gridSize)
        {
            if (gridSize != GRID_SIZE)
                throw new ArgumentException($"Grid size must be {GRID_SIZE}x{GRID_SIZE}.");
            _gameState = new GameState();
            _gameGrid = new GameGrid(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _puzzleGenerator = new PuzzleGenerator(GRID_SIZE, _gameState.PlacedPieces.ToList());
            _leaderboard = new Leaderboard();
        }

        public bool IsGameCompleted => _gameState.IsGameCompleted;
        public int HintsUsed => _gameState.HintsUsed;
        public int MaxHints => _gameState.MaxHints;
        public int MovesCount => _gameState.MovesCount;
        public Difficulty CurrentDifficulty => _gameState.CurrentDifficulty;
        public int GridSize => _gameGrid.GridSize;
        public double ElapsedTime => _gameState.ElapsedTime;
        public int[][] Grid => _gameGrid.Grid;
        public IReadOnlyList<DominoPiece> AvailablePieces => _gameState.AvailablePieces;
        public IReadOnlyList<DominoPiece> PlacedPieces => _gameState.PlacedPieces;
        public int RemainingPiecesCount => _gameState.RemainingPiecesCount;

        public bool GenerateNewGame(Difficulty difficulty)
        {
            _gameState.Reset();
            _gameGrid.Clear();
            _gameState.SetDifficulty(difficulty);
            bool result = _puzzleGenerator.GeneratePuzzle(_gameState, difficulty);
            if (result)
            {
                foreach (var piece in _puzzleGenerator.InitialPieces)
                {
                    var newPiece = new DominoPiece(piece.Value1, piece.Value2);
                    newPiece.Place(piece.Position, piece.Orientation);
                    _gameState.AddPlacedPiece(newPiece);
                    _gameGrid.PlacePiece(newPiece, piece.Position, piece.Orientation, _gameState.PlacedPieces.Count - 1);
                }
                _gameState.UpdateElapsedTime(0);
            }
            return result;
        }

        public bool PlacePiece(DominoPiece piece, Position position, Orientation orientation, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!position.IsValidForGrid(_gameGrid.GridSize))
            {
                errorMessage = "Invalid position for piece placement.";
                return false;
            }

            var usedSums = new HashSet<int>(_gameState.UsedSums);

            Position secondPos = orientation == Orientation.Horizontal
                ? new Position(position.Row, position.Col + 1)
                : new Position(position.Row + 1, position.Col);

            if (!secondPos.IsValidForGrid(_gameGrid.GridSize))
            {
                errorMessage = "Second position out of bounds.";
                return false;
            }

            if (_gameGrid.Grid[position.Row][position.Col] > 0 || _gameGrid.Grid[secondPos.Row][secondPos.Col] > 0)
            {
                errorMessage = "Cannot place domino in a numbered cell.";
                return false;
            }

            if (!_gameGrid.CanPlacePiece(piece, position, orientation, usedSums))
            {
                errorMessage = "Invalid placement: violates game rules.";
                return false;
            }

            var newPiece = new DominoPiece(piece.Value1, piece.Value2);
            newPiece.Place(position, orientation);

            _gameState.AddPlacedPiece(newPiece);
            _gameState.IncrementMovesCount();
            _gameGrid.PlacePiece(newPiece, position, orientation, _gameState.PlacedPieces.Count - 1);

            if (_gameState.PlacedPieces.Count * 2 == _gameGrid.GridSize * _gameGrid.GridSize)
            {
                bool isValid = IsValidSolution(out string validationError);
                _gameState.SetGameCompleted(isValid);
                if (isValid)
                {
                    _leaderboard.AddResult("Player", _gameState.CurrentDifficulty, _gameState.ElapsedTime,
                        _gameState.MovesCount, _gameState.HintsUsed);
                }
                else
                {
                    errorMessage = validationError;
                }
            }

            return true;
        }

        public bool RemovePiece(Position position, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!position.IsValidForGrid(_gameGrid.GridSize))
            {
                errorMessage = "Invalid position.";
                return false;
            }

            int pieceId = _gameGrid.DominoGrid[position.Row][position.Col];
            if (pieceId == -1 || pieceId >= _gameState.PlacedPieces.Count)
            {
                errorMessage = "No piece at the specified position.";
                return false;
            }

            var piece = _gameState.PlacedPieces[pieceId];
            if (!_gameGrid.RemovePiece(position, piece))
            {
                errorMessage = "Failed to remove piece.";
                return false;
            }

            _gameState.RemovePlacedPiece(piece);
            _gameState.IncrementMovesCount();
            _gameState.SetGameCompleted(false);
            _gameGrid.UpdatePieceIds();

            return true;
        }

        public bool MovePiece(Position from, Position to1, Position to2, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!from.IsValidForGrid(_gameGrid.GridSize))
            {
                errorMessage = "Invalid source position.";
                return false;
            }

            int pieceId = _gameGrid.DominoGrid[from.Row][from.Col];
            if (pieceId == -1 || pieceId >= _gameState.PlacedPieces.Count)
            {
                errorMessage = "No piece at source position.";
                return false;
            }

            var piece = _gameState.PlacedPieces[pieceId];
            if (!_gameGrid.RemovePiece(from, piece))
            {
                errorMessage = "Failed to remove piece from source.";
                return false;
            }

            var newOrientation = to1.Row == to2.Row ? Orientation.Horizontal : Orientation.Vertical;
            var tempPiece = new DominoPiece(piece.Value1, piece.Value2);
            tempPiece.Place(to1, newOrientation);

            var usedSums = new HashSet<int>(_gameState.UsedSums);
            if (!_gameGrid.CanPlacePiece(tempPiece, to1, newOrientation, usedSums))
            {
                _gameGrid.PlacePiece(piece, piece.Position, piece.Orientation, pieceId);
                errorMessage = "Invalid destination: violates game rules.";
                return false;
            }

            piece.Place(to1, newOrientation);
            _gameGrid.PlacePiece(piece, to1, newOrientation, pieceId);
            _gameState.IncrementMovesCount();

            return true;
        }

        public bool AutoSolve(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_puzzleGenerator.HasSolution)
            {
                errorMessage = "No solution available.";
                return false;
            }

            foreach (var piece in _gameState.PlacedPieces.ToList())
                RemovePiece(piece.Position, out _);

            foreach (var solutionPiece in _puzzleGenerator.SolutionPieces)
            {
                if (!PlacePiece(solutionPiece, solutionPiece.Position, solutionPiece.Orientation, out errorMessage))
                    return false;
            }

            return true;
        }

        public bool SaveGame(string filename, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var file = new BinaryWriter(File.Open(filename, FileMode.Create)))
                {
                    file.Write(_gameGrid.GridSize);
                    file.Write((int)_gameState.CurrentDifficulty);
                    file.Write(_gameState.HintsUsed);
                    file.Write(_gameState.MovesCount);
                    file.Write(_gameState.ElapsedTime);

                    foreach (var row in _gameGrid.Grid)
                        foreach (var cell in row)
                            file.Write(cell);

                    file.Write(_gameState.PlacedPieces.Count);
                    foreach (var piece in _gameState.PlacedPieces)
                    {
                        file.Write(piece.Value1);
                        file.Write(piece.Value2);
                        file.Write(piece.Position.Row);
                        file.Write(piece.Position.Col);
                        file.Write((int)piece.Orientation);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to save game: {ex.Message}";
                return false;
            }
        }

        public bool LoadGame(string filename, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var file = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    int gridSize = file.ReadInt32();
                    if (gridSize != GRID_SIZE)
                    {
                        errorMessage = "Grid size mismatch. Expected 9x9 grid.";
                        return false;
                    }

                    _gameGrid = new GameGrid(GRID_SIZE, _gameState.PlacedPieces.ToList());
                    _gameState.SetDifficulty((Difficulty)file.ReadInt32());
                    _gameState.Reset();
                    _gameGrid.Clear();
                    int hints = file.ReadInt32();
                    int moves = file.ReadInt32();
                    _gameState.UpdateElapsedTime(file.ReadDouble());
                    for (int i = 0; i < hints; i++) _gameState.IncrementHintsUsed();
                    for (int i = 0; i < moves; i++) _gameState.IncrementMovesCount();

                    for (int i = 0; i < _gameGrid.GridSize; i++)
                        for (int j = 0; j < _gameGrid.GridSize; j++)
                            _gameGrid.Grid[i][j] = file.ReadInt32();

                    int placedCount = file.ReadInt32();
                    for (int i = 0; i < placedCount; i++)
                    {
                        int v1 = file.ReadInt32();
                        int v2 = file.ReadInt32();
                        var pos = new Position(file.ReadInt32(), file.ReadInt32());
                        var orient = (Orientation)file.ReadInt32();

                        var piece = new DominoPiece(v1, v2);
                        if (!PlacePiece(piece, pos, orient, out errorMessage))
                            return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load game: {ex.Message}";
                return false;
            }
        }

        public bool IsValidSolution(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (_gameState.PlacedPieces.Count * 2 != _gameGrid.GridSize * _gameGrid.GridSize)
            {
                errorMessage = "Not all cells are covered by pieces.";
                return false;
            }

            for (int row = 0; row < _gameGrid.GridSize; row++)
            {
                for (int col = 0; col < _gameGrid.GridSize; col++)
                {
                    if (_gameGrid.Grid[row][col] > 0 && _gameGrid.DominoGrid[row][col] == -1)
                    {
                        int calculated = _gameGrid.CalculateConstraintValue(row, col);
                        if (calculated != _gameGrid.Grid[row][col])
                        {
                            errorMessage = $"Incorrect sum at cell ({row}, {col}). Expected {calculated}, got {_gameGrid.Grid[row][col]}.";
                            return false;
                        }
                    }
                }
            }

            if (!CheckRowColumnUniqueness(out string uniquenessError))
            {
                errorMessage = uniquenessError;
                return false;
            }

            return true;
        }

        private bool CheckRowColumnUniqueness(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                for (int row = 0; row < _gameGrid.GridSize; row++)
                {
                    var rowDigits = new HashSet<int>();
                    for (int col = 0; col < _gameGrid.GridSize; col++)
                    {
                        int pieceId = _gameGrid.DominoGrid[row][col];
                        if (pieceId != -1 && pieceId < _gameState.PlacedPieces.Count)
                        {
                            var piece = _gameState.PlacedPieces[pieceId];
                            if (row == piece.Position.Row && col == piece.Position.Col)
                            {
                                if (!rowDigits.Add(piece.Value1) || !rowDigits.Add(piece.Value2))
                                {
                                    errorMessage = $"Duplicate value in row {row}.";
                                    return false;
                                }
                            }
                        }
                    }
                }

                for (int col = 0; col < _gameGrid.GridSize; col++)
                {
                    var colDigits = new HashSet<int>();
                    for (int row = 0; row < _gameGrid.GridSize; row++)
                    {
                        int pieceId = _gameGrid.DominoGrid[row][col];
                        if (pieceId != -1 && pieceId < _gameState.PlacedPieces.Count)
                        {
                            var piece = _gameState.PlacedPieces[pieceId];
                            if (row == piece.Position.Row && col == piece.Position.Col)
                            {
                                if (!colDigits.Add(piece.Value1) || !colDigits.Add(piece.Value2))
                                {
                                    errorMessage = $"Duplicate value in column {col}.";
                                    return false;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error checking uniqueness: {ex.Message}";
                return false;
            }
        }
    }
}