using System;
using System.Collections.Generic;
using System.Linq;

namespace DominoGame
{
    public class GameGrid
    {
        private readonly int _gridSize = 9;
        private readonly int[][] _grid;
        private readonly int[][] _dominoGrid;
        private readonly Dictionary<Position, int> _constraintCache;
        private bool _cacheValid;
        private readonly List<DominoPiece> _placedPieces;

        public GameGrid(int gridSize, List<DominoPiece> placedPieces)
        {
            if (gridSize != 9) throw new ArgumentException("Grid size must be 9x9.");
            _grid = new int[_gridSize][].Select(_ => new int[_gridSize]).ToArray();
            _dominoGrid = new int[_gridSize][].Select(_ => new int[_gridSize].Select(_ => -1).ToArray()).ToArray();
            _constraintCache = new Dictionary<Position, int>();
            _cacheValid = false;
            _placedPieces = placedPieces ?? throw new ArgumentNullException(nameof(placedPieces));
        }

        public int GridSize => _gridSize;
        public int[][] Grid => _grid;
        public int[][] DominoGrid => _dominoGrid;

        public void Clear()
        {
            for (int i = 0; i < _gridSize; i++)
            {
                Array.Fill(_grid[i], 0);
                Array.Fill(_dominoGrid[i], -1);
            }
            _constraintCache.Clear();
            _cacheValid = false;
        }

        public bool CanPlacePiece(DominoPiece piece, Position position, Orientation orientation, HashSet<int> usedSums)
        {
            if (!position.IsValidForGrid(_gridSize)) return false;

            Position secondPos = orientation == Orientation.Horizontal
                ? new Position(position.Row, position.Col + 1)
                : new Position(position.Row + 1, position.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) return false;

            if (_grid[position.Row][position.Col] > 0 || _grid[secondPos.Row][secondPos.Col] > 0)
                return false;

            if (_dominoGrid[position.Row][position.Col] != -1 || _dominoGrid[secondPos.Row][secondPos.Col] != -1)
                return false;

            if (usedSums.Contains(piece.Sum)) return false;

            return WouldMaintainRowColumnUniqueness(piece, position, orientation) &&
                   !WouldTouchOtherPieces(position, orientation);
        }

        public void PlacePiece(DominoPiece piece, Position position, Orientation orientation, int pieceId)
        {
            if (!position.IsValidForGrid(_gridSize)) throw new ArgumentException("Invalid position for piece placement.");
            Position secondPos = orientation == Orientation.Horizontal
                ? new Position(position.Row, position.Col + 1)
                : new Position(position.Row + 1, position.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) throw new ArgumentException("Second position out of bounds.");

            _dominoGrid[position.Row][position.Col] = pieceId;
            _dominoGrid[secondPos.Row][secondPos.Col] = pieceId;
            _grid[position.Row][position.Col] = piece.Sum;
            _grid[secondPos.Row][secondPos.Col] = piece.Sum;

            _constraintCache.Clear();
            _cacheValid = false;
        }

        public bool RemovePiece(Position position, DominoPiece piece)
        {
            if (!position.IsValidForGrid(_gridSize)) return false;

            int pieceId = _dominoGrid[position.Row][position.Col];
            if (pieceId == -1) return false;

            foreach (var pos in piece.GetOccupiedPositions())
            {
                if (!pos.IsValidForGrid(_gridSize)) continue; // Skip invalid positions
                _dominoGrid[pos.Row][pos.Col] = -1;
                _grid[pos.Row][pos.Col] = 0;
            }

            _constraintCache.Clear();
            _cacheValid = false;
            return true;
        }

        public void UpdatePieceIds()
        {
            var newDominoGrid = new int[_gridSize][].Select(_ => new int[_gridSize].Select(_ => -1).ToArray()).ToArray();
            for (int i = 0; i < _placedPieces.Count; i++)
            {
                foreach (var pos in _placedPieces[i].GetOccupiedPositions())
                {
                    if (pos.IsValidForGrid(_gridSize))
                    {
                        if (newDominoGrid[pos.Row][pos.Col] != -1)
                            throw new InvalidOperationException("Piece position conflict detected");
                        newDominoGrid[pos.Row][pos.Col] = i;
                    }
                }
            }
            Array.Copy(newDominoGrid, _dominoGrid, newDominoGrid.Length);
        }

        public int CalculateConstraintValue(int row, int col)
        {
            if (!new Position(row, col).IsValidForGrid(_gridSize)) return 0;

            if (!_cacheValid)
            {
                _constraintCache.Clear();
                _cacheValid = true;
            }

            var pos = new Position(row, col);
            if (_constraintCache.TryGetValue(pos, out int value))
                return value;

            int sum = 0;
            var adjacentPieces = new HashSet<int>();

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;

                    int newRow = row + dr;
                    int newCol = col + dc;

                    if (newRow >= 0 && newRow < _gridSize && newCol >= 0 && newCol < _gridSize)
                    {
                        int pieceId = _dominoGrid[newRow][newCol];
                        if (pieceId != -1 && pieceId < _placedPieces.Count && !adjacentPieces.Contains(pieceId))
                        {
                            adjacentPieces.Add(pieceId);
                            sum += _placedPieces[pieceId].Sum;
                        }
                    }
                }
            }

            _constraintCache[pos] = sum;
            return sum;
        }

        private bool WouldTouchOtherPieces(Position position, Orientation orientation)
        {
            if (!position.IsValidForGrid(_gridSize)) return true;

            var checkPositions = new List<Position>();
            Position secondPos = orientation == Orientation.Horizontal
                ? new Position(position.Row, position.Col + 1)
                : new Position(position.Row + 1, position.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) return true;

            if (orientation == Orientation.Horizontal)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 2; dc++)
                    {
                        checkPositions.Add(new Position(position.Row + dr, position.Col + dc));
                    }
                }
            }
            else
            {
                for (int dr = -1; dr <= 2; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        checkPositions.Add(new Position(position.Row + dr, position.Col + dc));
                    }
                }
            }

            return checkPositions.Any(checkPos =>
                checkPos.IsValidForGrid(_gridSize) &&
                _dominoGrid[checkPos.Row][checkPos.Col] != -1);
        }

        private bool WouldMaintainRowColumnUniqueness(DominoPiece piece, Position position, Orientation orientation)
        {
            if (!position.IsValidForGrid(_gridSize)) return false;

            Position secondPos = orientation == Orientation.Horizontal
                ? new Position(position.Row, position.Col + 1)
                : new Position(position.Row + 1, position.Col);

            if (!secondPos.IsValidForGrid(_gridSize)) return false;

            var affectedRows = new HashSet<int>();
            var affectedCols = new HashSet<int>();

            if (orientation == Orientation.Horizontal)
            {
                affectedRows.Add(position.Row);
                affectedCols.Add(position.Col);
                affectedCols.Add(position.Col + 1);
            }
            else
            {
                affectedRows.Add(position.Row);
                affectedRows.Add(position.Row + 1);
                affectedCols.Add(position.Col);
            }

            foreach (int row in affectedRows)
            {
                var rowDigits = new HashSet<int>();
                for (int c = 0; c < _gridSize; c++)
                {
                    int pieceId = _dominoGrid[row][c];
                    if (pieceId != -1 && pieceId < _placedPieces.Count)
                    {
                        var p = _placedPieces[pieceId];
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
                    int pieceId = _dominoGrid[r][col];
                    if (pieceId != -1 && pieceId < _placedPieces.Count)
                    {
                        var p = _placedPieces[pieceId];
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