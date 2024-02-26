using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AcidicBosses.Helpers;

/**
 * Increments through a given list, returning to the beginning when getting to the end.
 */
public class Counter<T>
{
    private IEnumerable<T> List { get; }
    private int Index { get; set; }

    public Counter(IEnumerable<T> list)
    {
        List = list;
        Index = 0;
    }

    public T Get()
    {
        return List.ElementAt(Index);
    }

    public void Next()
    {
        Index = (Index + 1) % List.Count();
    }

    public void SendData(BinaryWriter binaryWriter)
    {
        binaryWriter.Write7BitEncodedInt(Index);
    }

    public void RecieveData(BinaryReader binaryReader)
    {
        Index = binaryReader.Read7BitEncodedInt();
    }
}