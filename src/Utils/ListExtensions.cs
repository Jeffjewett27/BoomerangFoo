using System;
using System.Collections.Generic;
using System.Text;

namespace BoomerangFoo.Utils
{
    internal static class ListExtensions
    {
        private static readonly Random _rng = new Random();

        /// <summary>
        /// In-place Fisher–Yates shuffle.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
