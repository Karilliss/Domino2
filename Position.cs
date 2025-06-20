using System;

namespace DominoGame
{
    public struct Position : IEquatable<Position>, IComparable<Position>
    {
        public int Row { get; set; }
        public int Col { get; set; }

        public Position(int row = -1, int col = -1)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(Position other) => Row == other.Row && Col == other.Col;

        public int CompareTo(Position other)
        {
            if (Row != other.Row) return Row.CompareTo(other.Row);
            return Col.CompareTo(other.Col);
        }

        public bool IsValid() => Row >= 0 && Col >= 0;

        public bool IsValidForGrid(int gridSize) =>
            Row >= 0 && Row < gridSize && Col >= 0 && Col < gridSize;

        public override int GetHashCode() => HashCode.Combine(Row, Col);

        public static bool operator ==(Position left, Position right) => left.Equals(right);

        public static bool operator !=(Position left, Position right) => !left.Equals(right);

        public override bool Equals(object obj) => obj is Position other && Equals(other);
    }
}