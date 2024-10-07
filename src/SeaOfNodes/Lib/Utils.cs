using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Lib;

public static class Utils
{
    /// <summary>
    /// Delete an item from a list from position <paramref name="i"/>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public static T del<T>(List<T> list, int i)
    {
        if ((uint)i >= list.Count)
        {
            return default!;
        }
        int iLast = list.Count - 1;
        T item = list[i];
        T last = list[iLast];
        list.RemoveAt(iLast);
        if (i < list.Count)
            list[i] = last;
        return item;
    }
}
