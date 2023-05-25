using System;
using System.Linq;

namespace JpegModule {

    public class Matrix {
        public double[][] Array { get; private set; }
        public int RowCount { get { return Array.GetLength(0); } }
        public int ColCount { get { return Array[0].GetLength(0); } }

        public Matrix(params double[][] array) {
            Array = array;
        }

        public Matrix(int rowCount, int colCount) {
            Array = new double[rowCount][];
            for (int i = 0; i < rowCount; i++) {
                Array[i] = new double[colCount];
            }
        }

        public double[] this[int i] {
            get { return Array[i]; }
            set { Array[i] = value; }
        }

        public static Matrix operator +(Matrix first, Matrix second) {
            if (first.ColCount != second.ColCount || first.RowCount != second.RowCount) {
                throw new ArgumentException("Dimensions of the matrices don't match");
            }

            return new Matrix(first.Array.Zip(second.Array, (x1, y1) => x1.Zip(y1, (x2, y2) => x2 + y2).ToArray()).ToArray());
        }

        public static Matrix operator -(Matrix first, Matrix second) {
            if (first.ColCount != second.ColCount || first.RowCount != second.RowCount) {
                throw new ArgumentException("Dimensions of the matrices don't match");
            }

            return new Matrix(first.Array.Zip(second.Array, (x1, y1) => x1.Zip(y1, (x2, y2) => x2 - y2).ToArray()).ToArray());
        }

        public static Matrix operator *(Matrix first, Matrix second) {
            if (first.ColCount != second.RowCount) {
                throw new ArgumentException("First matrix's width must be equal to second matrix's height");
            }

            var res = new Matrix(first.RowCount, second.ColCount);
            for (int i = 0; i < first.RowCount; i++) {
                for (int j = 0; j < second.ColCount; j++) {
                    double sum = 0;

                    for (int k = 0; k < first.ColCount; k++) {
                        sum += first[i][k] * second[k][j];
                    }

                    res[i][j] = sum;
                }
            }

            return res;
        }

        public static Vector operator *(Matrix matrix, Vector vector) {
            return (Vector)(matrix * (Matrix)vector);
        }

        public static explicit operator Matrix(Vector vector) {
            return new Matrix(vector.Array.Select(x => new double[] { x }).ToArray());
        }

        public override string ToString() {
            return string.Join("\n", Array.Select(x => string.Join(" ", x)));
        }

        public override bool Equals(object obj) {
            var second = (Matrix)obj;
            return (
                this.RowCount == second.RowCount &&
                this.ColCount == second.ColCount &&
                this.Array.Zip(second.Array, (x1, y1) => x1.Zip(y1, (x2, y2) => x2 == y2).All(x => x)).All(x => x)
            );
        }
    }
}
