using System;
using UnityEngine;

/// Lives in the hand item, to bridge the placement of the 2 mines. This component is only updated/read by the server, when the server does placement.
public class TPLink : MonoBehaviour
{
    [NonSerialized]
    public int otherTrapNob = -1;
}