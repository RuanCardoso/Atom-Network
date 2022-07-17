using Atom.Core;
using UnityEngine;

namespace NeutronNetwork.Tests
{
    public class Tests : MonoBehaviour
    {
        AtomStream atomStream = new();

        private void Start()
        {

        }

        private void Update()
        {
            atomStream.Write("Ruan Cardoso");
            atomStream.Write("Geissy");
            atomStream.Position = 0;
            for (int i = 0; i < 2; i++)
            {
                atomStream.Read(out string str);
                Debug.Log(str);
            }
            atomStream.Position = 0;
        }
    }
}