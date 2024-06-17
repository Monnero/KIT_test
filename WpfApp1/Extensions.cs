using System;
using System.Collections.Generic;

public static class ListExtensions
{
    private static Random random = new Random();

    public static void Shuffle<T>(this List<T> list, List<int> numbers)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}