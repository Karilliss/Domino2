
using System.Collections.Generic;
using System.Linq;

namespace DominoGame
{
    public class GameState
    {
        private List<DominoPiece> _placedPieces;
        private List<DominoPiece> _availablePieces;
        private int _hintsUsed;
        private int _movesCount;
        private Difficulty _currentDifficulty;
        private double _elapsedTime;
        private bool _isGameCompleted;
        public int MaxHints { get; private set; }

        public GameState()
        {
            _placedPieces = new List<DominoPiece>();
            _availablePieces = new List<DominoPiece>();
            _hintsUsed = 0;
            _movesCount = 0;
            _currentDifficulty = Difficulty.Medium;
            _elapsedTime = 0;
            _isGameCompleted = false;
            MaxHints = 5;
            InitializeAvailablePieces();
        }

        public IReadOnlyList<DominoPiece> PlacedPieces => _placedPieces.AsReadOnly();
        public IReadOnlyList<DominoPiece> AvailablePieces => _availablePieces.AsReadOnly();
        public int HintsUsed => _hintsUsed;
        public int MovesCount => _movesCount;
        public Difficulty CurrentDifficulty => _currentDifficulty;
        public double ElapsedTime => _elapsedTime;
        public bool IsGameCompleted => _isGameCompleted;
        public int RemainingPiecesCount => _availablePieces.Count(p => !_placedPieces.Any(p2 => p2.Equals(p)));
        public HashSet<int> UsedSums => new HashSet<int>(_placedPieces.Select(p => p.Sum));

        private void InitializeAvailablePieces()
        {
            _availablePieces.Clear();
            for (int i = 1; i <= 6; i++)
            {
                for (int j = i + 1; j <= 6; j++)
                {
                    _availablePieces.Add(new DominoPiece(i, j));
                }
            }
        }

        public void Reset()
        {
            _placedPieces.Clear();
            _availablePieces.Clear();
            _hintsUsed = 0;
            _movesCount = 0;
            _elapsedTime = 0;
            _isGameCompleted = false;
            MaxHints = 5;
            InitializeAvailablePieces();
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            _currentDifficulty = difficulty;
            MaxHints = difficulty switch
            {
                Difficulty.Easy => 7,
                Difficulty.Medium => 5,
                Difficulty.Hard => 3,
                _ => 5
            };
            _hintsUsed = 0;
        }

        public void IncrementHintsUsed() => _hintsUsed++;
        public void IncrementMovesCount() => _movesCount++;
        public void AddPlacedPiece(DominoPiece piece) => _placedPieces.Add(piece);
        public void RemovePlacedPiece(DominoPiece piece) => _placedPieces.Remove(piece);
        public void SetGameCompleted(bool completed) => _isGameCompleted = completed;
        public void UpdateElapsedTime(double time) => _elapsedTime = time;
        public void ClearPieces() => InitializeAvailablePieces();
        public void AddAvailablePieces(List<DominoPiece> pieces)
        {
            foreach (var piece in pieces)
            {
                if (!_availablePieces.Any(p => p.Equals(piece)))
                    _availablePieces.Add(piece);
            }
        }
    }
}
