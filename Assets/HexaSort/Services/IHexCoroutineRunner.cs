using System.Collections;
using UnityEngine;

namespace BeNice.HexaSort.Services
{
    public interface IHexCoroutineRunner
    {
        Coroutine Run(IEnumerator routine);
        void Stop(Coroutine coroutine);
    }
}
