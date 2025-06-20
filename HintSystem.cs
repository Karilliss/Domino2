using System;

namespace DominoGame
{
    public class HintSystem
    {
        private readonly PuzzleGenerator _puzzleGenerator;
        private readonly GameState _gameState;

        public HintSystem(PuzzleGenerator puzzleGenerator, GameState gameState)
        {
            _puzzleGenerator = puzzleGenerator ?? throw new ArgumentNullException(nameof(puzzleGenerator));
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        }

        public bool CanProvideHint => _gameState.HintsUsed < _gameState.MaxHints && _puzzleGenerator.HasSolution;

        public bool GetHint(out Position pos1, out Position pos2, out int value)
        {
            pos1 = new Position();
            pos2 = new Position();
            value = 0;

            if (!CanProvideHint)
                return false;

            foreach (var solutionPiece in _puzzleGenerator.SolutionPieces)
            {
                if (!_gameState.PlacedPieces.Any(p => p.Equals(solutionPiece)))
                {
                    pos1 = solutionPiece.Position;
                    pos2 = solutionPiece.Orientation == Orientation.Horizontal
                        ? new Position(pos1.Row, pos1.Col + 1)
                        : new Position(pos1.Row + 1, pos1.Col);
                    value = solutionPiece.Sum;
                    _gameState.IncrementHintsUsed();
                    return true;
                }
            }

            return false;
        }

        public bool CheckCell(Position position, GameGrid grid, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!position.IsValidForGrid(grid.GridSize))
            {
                errorMessage = "Invalid cell position.";
                return false;
            }

            int calculated = grid.CalculateConstraintValue(position.Row, position.Col);
            if (grid.Grid[position.Row][position.Col] > 0 && calculated != grid.Grid[position.Row][position.Col])
            {
                errorMessage = $"Incorrect sum at cell ({position.Row}, {position.Col}). Expected {grid.Grid[position.Row][position.Col]}, got {calculated}.";
                return false;
            }

            return true;
        }
    }
}