using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AcidicBosses.Helpers;

public class Counter<T>
{
    public IEnumerable<T> list { get; set; }
    public int index { get; private set; }

    public Counter(IEnumerable<T> list)
    {
        this.list = list;
        index = 0;
    }

    public T Get()
    {
        return list.ElementAt(index);
    }

    public void Next()
    {
        index = (index + 1) % list.Count();
    }

    public void SendData(BinaryWriter binaryWriter)
    {
        binaryWriter.Write7BitEncodedInt(index);
    }

    public void RecieveData(BinaryReader binaryReader)
    {
        index = binaryReader.Read7BitEncodedInt();
    }
}