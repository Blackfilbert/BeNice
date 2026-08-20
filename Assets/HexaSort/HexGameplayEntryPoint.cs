#if !LUNA_PLAYABLE
using VContainer.Unity;

namespace BeNice.HexaSort
{
    public sealed class HexGameplayEntryPoint : IStartable
    {
        private readonly HexGameplayController _controller;

        public HexGameplayEntryPoint(HexGameplayController controller)
        {
            _controller = controller;
        }

        public void Start()
        {
            _controller.Initialize();
        }
    }
}
#endif
