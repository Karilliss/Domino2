using System;
using System.Collections.Generic;
using System.Linq;

namespace DominoGame
{
    public class PuzzleGenerator
    {
        private readonly int _gridSize = 9;
        private readonly Random _rng;
        private readonly GameGrid _solutionGrid;
        private readonly List<DominoPiece> _solutionPieces;
        private readonly List<DominoPiece> _initialPieces;
        private bool _hasSolution;
        private const int MAX_GENERATION_ATTEMPTS = 100;

        public PuzzleGenerator(int gridSize, List<DominoPiece> placedPieces)
        {
            if (gridSize != 9) throw new ArgumentException("Grid size must be 9x9.");
            _rng = new Random();
            _solutionGrid = new GameGrid(_gridSize, placedPieces);
            _solutionPieces = new List<DominoPiece>();
            _initialPieces = new List<DominoPiece>();
            _hasSolution = false;
        }

        public bool HasSolution => _hasSolution;
        public IReadOnlyList<DominoPiece> SolutionPieces => _solutionPieces.AsReadOnly();
        public IReadOnlyList<DominoPiece> InitialPieces => _initialPieces.AsReadOnly();
        public GameGrid SolutionGrid => _solutionGrid;

        public bool GeneratePuzzle(GameState gameState, Difficulty difficulty)
        {
            for (int attempt = 0; attempt < MAX_GENERATION_ATTEMPTS; attempt++)
            {
                if (GenerateSolution(gameState, difficulty))
                {
                    GenerateInitialPieces(gameState, difficulty);
                    GenerateConstraintGrid();
                    ApplyDifficultySettings(difficulty);
                    return true;
                }
            }
            return GenerateSimplifiedPuzzle(gameState, difficulty);
        }

        private bool GenerateSolution(GameState gameState, Difficulty difficulty)
        {
            _solutionGrid.Clear();
            _solutionPieces.Clear();
            _initialPieces.Clear();
            var shuffledPieces = gameState.AvailablePieces.OrderBy(_ => _rng.Next()).ToList();
            return BacktrackSolution(0, shuffledPieces);
        }

        private bool BacktrackSolution(int pieceIndex, List<DominoPiece> pieces)
        {
            if (pieceIndex >= pieces.Count || pieceIndex >= (_gridSize * _gridSize) / 2)
            {
                _hasSolution = true;
                return true;
            }

            var piece = pieces[pieceIndex];
            var possiblePlacements = new List<(Position, Orientation)>();

            for (int row = 0; row < _gridSize; row++)
            {
                for (int col = 0; col < _gridSize; col++)
                {
                    var pos = new Position(row, col);
                    if (CanPlacePieceInSolution(piece, pos, Orientation.Horizontal))
                        possiblePlacements.Add((pos, Orientation.Horizontal));
                    if (CanPlacePieceInSolution(piece, pos, Orientation.Vertical))
                        possiblePlacements.Add((pos, Orientation.Vertical));
                }
            }

            possiblePlacements = possiblePlacements.OrderBy(_ => _rng.Next()).ToList();

            foreach (var (pos, orient) in possiblePlacements)
            {
                PlacePieceInSolution(piece, pos, orient, pieceIndex);
                if (CheckRowColumnUniquenessForPlacement(pos, orient, piece))
                {
                    if (BacktrackSolution(pieceIndex + 1, pieces))
                        return true;
                }
                RemovePieceFromSolution(pos, orient);
            }

            return false;
        }

        private void GenerateInitialPieces(GameState gameState, Difficulty difficulty)
        {
            _initialPieces.Clear();
            int totalCells = _gridSize * _gridSize;
            int piecesToPlace = difficulty switch
            {
                Difficulty.Easy => totalCells / 4, // 20 pieces
                Difficulty.Medium => totalCells / 6, // ~13 pieces
                Difficulty.Hard => totalCells / 8, // ~10 pieces
                _ => totalCells / 6
            };

            // Ensure piecesToPlace doesn't exceed available grid space
            piecesToPlace = Math.Min(piecesToPlace, (totalCells / 2));

            var availablePieces = gameState.AvailablePieces.OrderBy(_ => _rng.Next()).ToList();
            int placedCount = 0;

            for (int row = 0; row < _gridSize && placedCount < piecesToPlace; row++)
            {
                for (int col = 0; col < _gridSize && placedCount < piecesToPlace; col++)
                {
                    var pos = new Position(row, col);
                    var orientations = new[] { Orientation.Horizontal, Orientation.Vertical };
                    orientations = orientations.OrderBy(_ => _rng.Next()).ToArray();

                    foreach (var orient in orientations)
                    {
                        if (placedCount >= piecesToPlace) break;

                        // Check if both positions are valid before attempting placement
                        Position secondPos = orient == Orientation.Horizontal
                            ? new Position(row, col + 1)
                            : new Position(row + 1, col);

                        if (!pos.IsValidForGrid(_gridSize) || !secondPos.IsValidForGrid(_gridSize))
                        {
                            Console.WriteLine($"Skipping invalid position: ({pos.Row},{pos.Col}), orient={orient}, secondPos=({secondPos.Row},{secondPos.Col})");
                            continue;
                        }

                        var piece = availablePieces.FirstOrDefault(p => !_initialPieces.Any(p2 => p2.Equals(p)) &&
                                                                       CanPlacePieceInSolution(p, pos, orient));
                        if (piece != null && CheckRowColumnUniquenessForPlacement(pos, orient, piece))
                        {
                            var newPiece = new DominoPiece(piece.Value1, piece.Value2);
                            newPiece.Place(pos, orient);
                            _initialPieces.Add(newPiece);
                            Console.WriteLine($"Placed piece at ({pos.Row},{pos.Col}), orient={orient}, values=({piece.Value1},{piece.Value2})");
                            PlacePieceInSolution(newPiece, pos, orient, _initialPieces.Count - 1);
                            placedCount++;
                            if (orient == Orientation.Horizontal)
                                col++; // Skip next column to avoid overlap
                            else
                                row++; // Skip next row to avoid overlap
                        }
                    }
                }
            }

            // Adjust available pieces
            var remainingCells = totalCells - (_initialPieces.Count * 2);
            var neededPieces = remainingCells / 2;
            var additionalPieces = availablePieces
                .Where(p => !_initialPieces.Any(p2 => p2.Equals(p)) && !_solutionPieces.Any(p2 => p2.Equals(p)))
                .Take(neededPieces)
                .Select(p => new DominoPiece(p.Value1, p.Value2))
                .ToList();
            gameState.ClearPieces();
            gameState.AddAvailablePieces(additionalPieces);
            gameState.AddAvailablePieces(_initialPieces.Select(p => new DominoPiece(p.Value1, p.Value2)).ToList());
        }

        private bool GenerateSimplifiedPuzzle(GameState gameState, Difficulty difficulty)
        {
            _solutionGrid.Clear();
            _solutionPieces.Clear();
            _initialPieces.Clear();
            var shuffledPieces = gameState.AvailablePieces.OrderBy(_ => _rng.Next()).ToList();
            int pieceIndex = 0;

            for (int row = 0; row < _gridSize && pieceIndex < shuffledPieces.Count; row += 2)
            {
                for (int col = 0; col < _gridSize - 1 && pieceIndex < shuffledPieces.Count; col += 2)
                {
                    var pos = new Position(row, col);
                    if (CanPlacePieceInSolution(shuffledPieces[pieceIndex], pos, Orientation.Horizontal))
                    {
                        PlacePieceInSolution(shuffledPieces[pieceIndex], pos, Orientation.Horizontal, pieceIndex);
                        _initialPieces.Add(shuffledPieces[pieceIndex]);
                        Console.WriteLine($"Simplified: Placed piece at ({pos.Row},{pos.Col}), orient=Horizontal");
                        pieceIndex++;
                    }
                }
            }

            for (int col = 0; col < _gridSize && pieceIndex < shuffledPieces.Count; col += 2)
            {
                for (int row = 0; row < _gridSize - 1 && pieceIndex < shuffledPieces.Count; row += 2)
                {
                    var pos = new Position(row, col);
                    if (CanPlacePieceInSolution(shuffledPieces[pieceIndex], pos, Orientation.Vertical))
                    {
                        PlacePieceInSolution(shuffledPieces[pieceIndex], pos, Orientation.Vertical, pieceIndex);
                        _initialPieces.Add(shuffledPieces[pieceIndex]);
                        Console.WriteLine($"Simplified: Placed piece at ({pos.Row},{pos.Col}), orient=Vertical");
                        pieceIndex++;
                    }
                }
            }

            if (pieceIndex >= shuffledPieces.Count / 3)
            {
                _hasSolution = true;
                GenerateConstraintGrid();
                ApplyDifficultySettings(difficulty);
                var remainingCells = _gridSize * _gridSize - (_initialPieces.Count * 2);
                var neededPieces = remainingCells / 2;
                var additionalPieces = shuffledPieces
                    .Where(p => !_initialPieces.Any(p2 => p2.Equals(p)))
                    .Take(neededPieces)
                    .Select(p => new DominoPiece(p.Value1, p.Value2))
                    .ToList();
                gameState.ClearPieces();
                gameState.AddAvailablePieces(additionalPieces);
                gameState.AddAvailablePieces(_initialPieces.Select(p => new DominoPiece(p.Value1, p.Value2)).ToList());
                return true;
            }

            return false;
        }

        private bool CanPlacePieceInSolution(DominoPiece piece, Position pos, Orientation orient)
        {
            if (!piece.CanBePlacedAt(pos, orient, _gridSize)) return false;

            Position secondPos = orient == Orientation.Horizontal
                ? new Position(pos.Row, pos.Col + 1)
                : new Position(pos.Row + 1, pos.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) return false;

            if (_solutionGrid.Grid[pos.Row][pos.Col] > 0 || _solutionGrid.Grid[secondPos.Row][secondPos.Col] > 0)
                return false;

            return !WouldTouchOtherPieces(pos, orient);
        }

        private bool WouldTouchOtherPieces(Position pos, Orientation orient)
        {
            var checkPositions = new List<Position>();

            if (orient == Orientation.Horizontal)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 2; dc++)
                    {
                        checkPositions.Add(new Position(pos.Row + dr, pos.Col + dc));
                    }
                }
            }
            else
            {
                for (int dr = -1; dr <= 2; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        checkPositions.Add(new Position(pos.Row + dr, pos.Col + dc));
                    }
                }
            }

            return checkPositions.Any(checkPos =>
                checkPos.IsValidForGrid(_gridSize) &&
                _solutionGrid.DominoGrid[checkPos.Row][checkPos.Col] != -1);
        }

        private void PlacePieceInSolution(DominoPiece piece, Position pos, Orientation orient, int pieceId)
        {
            if (!pos.IsValidForGrid(_gridSize)) return;

            var placedPiece = new DominoPiece(piece.Value1, piece.Value2);
            placedPiece.Place(pos, orient);

            Position secondPos = orient == Orientation.Horizontal
                ? new Position(pos.Row, pos.Col + 1)
                : new Position(pos.Row + 1, pos.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) return;

            _solutionGrid.DominoGrid[pos.Row][pos.Col] = pieceId;
            _solutionGrid.DominoGrid[secondPos.Row][secondPos.Col] = pieceId;
            _solutionGrid.Grid[pos.Row][pos.Col] = piece.Sum;
            _solutionGrid.Grid[secondPos.Row][secondPos.Col] = piece.Sum;

            if (_solutionPieces.Count <= pieceId)
                _solutionPieces.AddRange(Enumerable.Repeat<DominoPiece>(null, pieceId - _solutionPieces.Count + 1));
            _solutionPieces[pieceId] = placedPiece;
        }

        private void RemovePieceFromSolution(Position pos, Orientation orient)
        {
            if (!pos.IsValidForGrid(_gridSize)) return;

            Position secondPos = orient == Orientation.Horizontal
                ? new Position(pos.Row, pos.Col + 1)
                : new Position(pos.Row + 1, pos.Col);

            if (secondPos.IsValidForGrid(_gridSize))
            {
                _solutionGrid.DominoGrid[pos.Row][pos.Col] = -1;
                _solutionGrid.DominoGrid[secondPos.Row][secondPos.Col] = -1;
                _solutionGrid.Grid[pos.Row][pos.Col] = 0;
                _solutionGrid.Grid[secondPos.Row][secondPos.Col] = 0;
            }
        }

        private void GenerateConstraintGrid()
        {
            for (int row = 0; row < _gridSize; row++)
            {
                for (int col = 0; col < _gridSize; col++)
                {
                    if (_solutionGrid.DominoGrid[row][col] == -1)
                    {
                        _solutionGrid.Grid[row][col] = _solutionGrid.CalculateConstraintValue(row, col);
                    }
                }
            }
        }

        private void ApplyDifficultySettings(Difficulty difficulty)
        {
            int cellsToHide = difficulty switch
            {
                Difficulty.Easy => (_gridSize * _gridSize) / 8, // ~10 cells
                Difficulty.Medium => (_gridSize * _gridSize) / 6, // ~13 cells
                Difficulty.Hard => (_gridSize * _gridSize) / 4, // ~20 cells
                _ => (_gridSize * _gridSize) / 6
            };

            var constraintPositions = new List<Position>();
            for (int row = 0; row < _gridSize; row++)
            {
                for (int col = 0; col < _gridSize; col++)
                {
                    if (_solutionGrid.Grid[row][col] > 0 && _solutionGrid.DominoGrid[row][col] == -1)
                        constraintPositions.Add(new Position(row, col));
                }
            }

            // Ensure we don't try to hide more cells than available
            cellsToHide = Math.Min(cellsToHide, constraintPositions.Count);

            constraintPositions = constraintPositions.OrderBy(_ => _rng.Next()).ToList();
            foreach (var pos in constraintPositions.Take(cellsToHide))
                _solutionGrid.Grid[pos.Row][pos.Col] = 0;
        }

        private bool CheckRowColumnUniquenessForPlacement(Position pos, Orientation orient, DominoPiece piece)
        {
            if (!pos.IsValidForGrid(_gridSize)) return false;

            Position secondPos = orient == Orientation.Horizontal
                ? new Position(pos.Row, pos.Col + 1)
                : new Position(pos.Row + 1, pos.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) return false;

            var affectedRows = new HashSet<int>();
            var affectedCols = new HashSet<int>();

            if (orient == Orientation.Horizontal)
            {
                affectedRows.Add(pos.Row);
                affectedCols.Add(pos.Col);
                affectedCols.Add(pos.Col + 1);
            }
            else
            {
                affectedRows.Add(pos.Row);
                affectedRows.Add(pos.Row + 1);
                affectedCols.Add(pos.Col);
            }

            foreach (int row in affectedRows)
            {
                var rowDigits = new HashSet<int>();
                for (int c = 0; c < _gridSize; c++)
                {
                    int pieceId = _solutionGrid.DominoGrid[row][c];
                    if (pieceId != -1 && pieceId < _solutionPieces.Count)
                    {
                        var p = _solutionPieces[pieceId];
                        if (!rowDigits.Add(p.Value1) || !rowDigits.Add(p.Value2))
                            return false;
                    }
                }
                if (rowDigits.Contains(piece.Value1) || rowDigits.Contains(piece.Value2))
                    return false;
            }

            foreach (int col in affectedCols)
            {
                var colDigits = new HashSet<int>();
                for (int r = 0; r < _gridSize; r++)
                {
                    int pieceId = _solutionGrid.DominoGrid[r][col];
                    if (pieceId != -1 && pieceId < _solutionPieces.Count)
                    {
                        var p = _solutionPieces[pieceId];
                        if (!colDigits.Add(p.Value1) || !colDigits.Add(p.Value2))
                            return false;
                    }
                }
                if (colDigits.Contains(piece.Value1) || colDigits.Contains(piece.Value2))
                    return false;
            }

            return true;
        }
    }
}