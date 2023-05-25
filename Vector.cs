using System;
using System.Linq;

namespace JpegModule {
    public class Vector {
        public double[] Array { get; private set; }
        public int Dim { get { return Array.Length; } }

        public Vector(params double[] array) {
            Array = array;
        }

        public Vector(int dim) {
            Array = new double[dim];
        }

        public double this[int i] {
            get { return Array[i]; }
            set { Array[i] = value; }
        }

        public static Vector operator +(Vector first, Vector second) {
            if (first.Dim != second.Dim) {
                throw new ArgumentException("Dimensions of the vectors don't match");
            }

            return new Vector(first.Array.Zip(second.Array, (x, y) => x + y).ToArray());
        }

        public static Vector operator -(Vector first, Vector second) {
            if (first.Dim != second.Dim) {
                throw new ArgumentException("Dimensions of the vectors don't match");
            }

            return new Vector(first.Array.Zip(second.Array, (x, y) => x - y).ToArray());
        }

        public static explicit operator Vector(Matrix matrix) {
            if (matrix.ColCount != 1) {
                throw new ArgumentException("Cannot convert matrix to vector: more than 1 column");
            }

            return new Vector(matrix.Array.Select(x => x[0]).ToArray());
        }

        public override string ToString() {
            return string.Join(" ", Array);
        }

        public override bool Equals(object obj) {
            var other = (Vector)obj;
            return this.Array.Zip(other.Array, (x, y) => x == y).All(x => x);
        }
    }
}
