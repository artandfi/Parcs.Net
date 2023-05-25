using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpegModule {
	// JSON framework didn't work on target VMs, so I wrote a custom serializer.
    public static class Serializer {
        const string lBrace = "{";
        const string rBrace = "}";
        public static string Serialize(List<RlePair>[][][] data) {
            var sb = new StringBuilder();
            sb.Append($"{data[0].GetLength(0)};{data[0][0].GetLength(0)};{data[1].GetLength(0)};{data[1][0].GetLength(0)};{data[2].GetLength(0)};{data[2][0].GetLength(0)};");
            sb.Append("[");
            foreach (var component in data) {
                sb.Append("[");
                foreach (var vblock in component) {
                    sb.Append("[");
                    foreach (var hblock in vblock) {
                        sb.Append("[");
                        foreach (var rlePair in hblock) {
                            sb.Append($"{lBrace}{rlePair.Value} {rlePair.Freq}{rBrace},");
                        }
                        sb.Length--;
                        sb.Append("@]");
                    }
                    sb.Append("]");
                }
                sb.Append("]");
            }
            sb.Append("]");

            return sb.ToString();
        }

        public static List<RlePair>[][][] Deserialize(string serialized) {
            var parts = serialized.Split(';');
            var dims = new int[][] {
                new int[] { int.Parse(parts[0]), int.Parse(parts[1]) },
                new int[] { int.Parse(parts[2]), int.Parse(parts[3]) },
                new int[] { int.Parse(parts[4]), int.Parse(parts[5]) },
            };

            var res = new List<RlePair>[3][][];


            var pairsLists = parts[6].Replace("[", "").Replace("]", "").Split('@');

            int k = 0;
            for (int componentI = 0; componentI < 3; componentI++) {
                res[componentI] = new List<RlePair>[dims[componentI][0]][];

                for (int i = 0; i < dims[componentI][0]; i++) {
                    res[componentI][i] = new List<RlePair>[dims[componentI][1]];

                    for (int j = 0; j < dims[componentI][1]; j++) {
                        var pairsStr = pairsLists[k].Replace("{", "").Replace("}", "").Split(',').ToList();
                        var pairs = pairsStr.Select(x => new RlePair(int.Parse(x.Split(' ')[0]), int.Parse(x.Split(' ')[1]))).ToList();
                        res[componentI][i][j] = pairs;
                        k++;
                    }
                }
            }

            return res;
        }
    }
}
