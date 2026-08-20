using System.Collections;

namespace BeNice.HexaSort.Services
{
    public interface IHexReactionAnimator
    {
        IEnumerator PlayOperation(HexReactionOperation operation, float speedMultiplier);
    }
}
