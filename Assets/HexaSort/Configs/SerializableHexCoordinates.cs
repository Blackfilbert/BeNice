using System;
using BeNice.HexaSort.Models;
using UnityEngine;

namespace BeNice.HexaSort.Configs
{
    [Serializable]
    public struct SerializableHexCoordinates
    {
        [SerializeField] private int _q;
        [SerializeField] private int _r;

        public int Q => _q;
        public int R => _r;

        public SerializableHexCoordinates(int q, int r)
        {
            _q = q;
            _r = r;
        }

        public HexCoordinates ToCoordinates() => new HexCoordinates(_q, _r);
    }
}
