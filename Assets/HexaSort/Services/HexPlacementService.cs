using BeNice.HexaSort.Models;

namespace BeNice.HexaSort.Services
{
    public sealed class HexPlacementService
    {
        private readonly HexBoardModel _board;

        public HexPlacementService(HexBoardModel board)
        {
            _board = board;
        }

        public bool CanPlace(HexCoordinates coordinates) => _board.IsCellFree(coordinates);

        public bool TryPlace(HexCoordinates coordinates, HexStackModel stack) =>
            _board.TryPlaceStack(coordinates, stack);
    }
}
