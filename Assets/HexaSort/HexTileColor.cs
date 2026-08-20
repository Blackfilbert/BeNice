using System;
using UnityEngine;

namespace BeNice.HexaSort
{
    [Serializable]
    public enum HexTileColor
    {
        Red,
        Green,
        Blue,
        Yellow,
        Purple,
        White,
        Sea
    }

    [Serializable]
    public struct HexTileColorMaterial
    {
        [SerializeField] private HexTileColor _color;
        [SerializeField] private Material _material;

        public HexTileColor Color => _color;
        public Material Material => _material;
    }
}
