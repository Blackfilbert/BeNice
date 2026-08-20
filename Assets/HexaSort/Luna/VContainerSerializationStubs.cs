#if LUNA_PLAYABLE
using System;
using UnityEngine;

namespace VContainer.Unity
{
    [Serializable]
    public struct ParentReference
    {
        [SerializeField] public string TypeName;
    }
}
#endif
