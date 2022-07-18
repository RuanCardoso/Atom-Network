using Atom.Core;
using UnityEngine;

public class AtomSocketTest : MonoBehaviour
{
    private readonly AtomStream _atomStream = new();

    private void Awake()
    {
    }

    private void Start()
    {

    }

    int money = 1;
    private void Update()
    {
        money.Write(_atomStream);
    }
}