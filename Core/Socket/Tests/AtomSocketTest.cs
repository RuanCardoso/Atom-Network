using Atom.Core;
using UnityEngine;

public class AtomSocketTest : MonoBehaviour
{
    private readonly AtomStream _atomStream = new();

    int money = 100;
    private void Awake()
    {
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        _atomStream.Write(10);
        _atomStream.Write((short)20);
        _atomStream.Write((ushort)30);
        _atomStream.Write((string)"40");
        _atomStream.Write((byte)50);
        _atomStream.Write((float)60.1f);
        _atomStream.Write((double)70.1);
        _atomStream.Position = 0;
        _atomStream.Read(out int intValue);
        _atomStream.Read(out short shortValue);
        _atomStream.Read(out ushort ushortValue);
        _atomStream.Read(out string stringValue);
        _atomStream.Read(out byte byteValue);
        _atomStream.Read(out float floatValue);
        _atomStream.Read(out double doubleValue);
        Debug.Log($"intValue: {intValue}, shortValue: {shortValue}, ushortValue: {ushortValue}, stringValue: {stringValue}, byteValue: {byteValue}, floatValue: {floatValue}, doubleValue: {doubleValue}");
        _atomStream.Position = 0;
    }
}