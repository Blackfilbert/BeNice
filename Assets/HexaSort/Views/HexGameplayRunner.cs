using System;
using System.Collections;
using System.Collections.Generic;
using BeNice.HexaSort.Scopes;
using BeNice.HexaSort.Services;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexGameplayRunner : MonoBehaviour, IHexCoroutineRunner
    {
        [SerializeField] private GameplayLifetimeScope _gameplayScope;

        private void Start()
        {
            _gameplayScope.InitializeForLuna();
        }

        public Coroutine Run(IEnumerator routine)
        {
#if LUNA_PLAYABLE
            return StartCoroutine(RunFlattened(routine));
#else
            return StartCoroutine(routine);
#endif
        }

        public void Stop(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

#if LUNA_PLAYABLE
        private static IEnumerator RunFlattened(IEnumerator root)
        {
            if (root == null)
                yield break;

            var routines = new Stack<IEnumerator>();
            routines.Push(root);
            try
            {
                while (routines.Count > 0)
                {
                    var current = routines.Peek();
                    if (!current.MoveNext())
                    {
                        (current as IDisposable)?.Dispose();
                        routines.Pop();
                        continue;
                    }

                    var yielded = current.Current;
                    if (yielded is IEnumerator nested)
                    {
                        routines.Push(nested);
                        continue;
                    }

                    yield return yielded;
                }
            }
            finally
            {
                while (routines.Count > 0)
                    (routines.Pop() as IDisposable)?.Dispose();
            }
        }
#endif
    }
}
