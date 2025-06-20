using System;
using System.Collections.Generic;

namespace DominoGame
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public class DominoPiece : IEquatable<DominoPiece>, IComparable<DominoPiece>
    {
        private static int _nextId = 0;
        public int Value1 { get; private set; }
        public int Value2 { get; private set; }
        public int Sum => Value1 + Value2;
        public Position Position { get; private set; }
        public Orientation Orientation { get; private set; }
        public bool IsPlaced { get; private set; }
        public int Id { get; private set; }

        public DominoPiece(int value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
            Position = new Position(-1, -1);
            Orientation = Orientation.Horizontal;
            IsPlaced = false;
            Id = _nextId++;
        }

        public void Place(Position pos, Orientation orient)
        {
            Position = pos;
            Orientation = orient;
            IsPlaced = true;
        }

        public void Remove()
        {
            Position = new Position(-1, -1);
            IsPlaced = false;
        }

        public List<Position> GetOccupiedPositions()
        {
            var positions = new List<Position> { Position };
            if (IsPlaced && Position.IsValid())
            {
                positions.Add(Orientation == Orientation.Horizontal
                    ? new Position(Position.Row, Position.Col + 1)
                    : new Position(Position.Row + 1, Position.Col));
            }
            return positions;
        }

        public bool CanBePlacedAt(Position pos, Orientation orient, int gridSize)
        {
            if (!pos.IsValidForGrid(gridSize)) return false;
            Position secondPos = orient == Orientation.Horizontal
                ? new Position(pos.Row, pos.Col + 1)
                : new Position(pos.Row + 1, pos.Col);
            return secondPos.IsValidForGrid(gridSize);
        }

        public (int, int) GetCanonicalForm() =>
            (Math.Min(Value1, Value2), Math.Max(Value1, Value2));

        public bool Equals(DominoPiece other)
        {
            if (other == null) return false;
            var thisCanon = GetCanonicalForm();
            var otherCanon = other.GetCanonicalForm();
            return thisCanon == otherCanon;
        }

        public int CompareTo(DominoPiece other)
        {
            var thisCanon = GetCanonicalForm();
            var otherCanon = other.GetCanonicalForm();
            if (thisCanon.Item1 != otherCanon.Item1)
                return thisCanon.Item1.CompareTo(otherCanon.Item1);
            return thisCanon.Item2.CompareTo(otherCanon.Item2);
        }

        public override bool Equals(object obj) => Equals(obj as DominoPiece);
        public override int GetHashCode() => GetCanonicalForm().GetHashCode();
        public override string ToString() => $"[{Value1},{Value2}]";
        public static void ResetIdCounter() => _nextId = 0;
    }
}