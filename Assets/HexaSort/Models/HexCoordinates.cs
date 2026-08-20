using System;
using UnityEngine;

namespace BeNice.HexaSort.Models
{
    [Serializable]
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public static readonly HexCoordinates[] Directions =
        {
            new HexCoordinates(1, 0),
            new HexCoordinates(1, -1),
            new HexCoordinates(0, -1),
            new HexCoordinates(-1, 0),
            new HexCoordinates(-1, 1),
            new HexCoordinates(0, 1)
        };

        [SerializeField] private readonly int _q;
        [SerializeField] private readonly int _r;

        public int Q => _q;
        public int R => _r;

        public HexCoordinates(int q, int r)
        {
            _q = q;
            _r = r;
        }

        public HexCoordinates GetNeighbor(int directionIndex)
        {
            if (directionIndex < 0 || directionIndex >= Directions.Length)
                throw new ArgumentOutOfRangeException(nameof(directionIndex));

            return this + Directions[directionIndex];
        }

        public Vector3 ToLocalPosition(float cellSize, float spacing)
        {
            var radius = cellSize + spacing;
            var x = radius * Mathf.Sqrt(3f) * (_q + _r * 0.5f);
            var z = radius * 1.5f * _r;
            return new Vector3(x, 0f, z);
        }

        public bool Equals(HexCoordinates other) => _q == other._q && _r == other._r;

        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (_q * 397) ^ _r;
            }
        }

        public override string ToString() => $"({_q},{_r})";

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b) =>
            new HexCoordinates(a._q + b._q, a._r + b._r);

        public static bool operator ==(HexCoordinates left, HexCoordinates right) => left.Equals(right);

        public static bool operator !=(HexCoordinates left, HexCoordinates right) => !left.Equals(right);
    }
}
