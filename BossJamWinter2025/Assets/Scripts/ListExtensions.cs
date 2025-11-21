using System.Collections.Generic;
using System;

public static class IListExtensions {
    public static T GetRandom<T>(this IList<T> list) {
        if (list.Count <= 0) {
            new ArgumentException("Unable to get a random element from an empty list");
        }

        var index = UnityEngine.Random.Range(0, list.Count - 1);
        return list[index];
    }
}
