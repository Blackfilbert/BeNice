namespace BeNice.HexaSort.Models
{
    public sealed class HexCellModel
    {
        public HexCoordinates Coordinates { get; }
        public HexStackModel Stack { get; private set; }
        public bool HasStack => Stack != null && !Stack.IsEmpty;

        public HexCellModel(HexCoordinates coordinates)
        {
            Coordinates = coordinates;
        }

        public void SetStack(HexStackModel stack)
        {
            Stack = stack;
        }

        public void RemoveStack()
        {
            Stack = null;
        }
    }
}
