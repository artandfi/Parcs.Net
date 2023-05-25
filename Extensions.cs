using System.Collections.Generic;
using System.Linq;

namespace JpegModule {
    public static class Extensions {
        public static bool ArrayEqual<T>(this List<T>[] first, List<T>[] second) {
            if (first == null || second == null || first.Length != second.Length) {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return first.Zip(second, (x1, y1) => x1.Zip(y1, (x2, y2) => comparer.Equals(x2, y2)).All(x => x)).All(x => x);
        }
        
        public static bool ArrayEqual<T>(this List<T>[][] first, List<T>[][] second) {
            if (first == second) {
                return true;
            }
            if (first == null || second == null || first.Length != second.Length) {
                return false;
            }

            return first.Zip(second, (x, y) => x.ArrayEqual(y)).All(x => x);
        }
    }
}
