using System;
using System.Collections.Generic;
using System.Drawing;

namespace JpegModule {
    public static class JpegCodec {
        private const int BlockSize = 8;
        private static readonly Matrix Rgb2YCbCr_Factor = new Matrix(
            new double[]{ 0.299, 0.587, 0.114 },
            new double[]{ -0.168736, -0.331264, 0.5 },
            new double[]{ 0.5, -0.418688, -0.081312 }
        );
        private static readonly Vector Rgb2YCbCr_Offset = new Vector(0, 128, 128);
        private static readonly Matrix YCbCr2Rgb_Factor = new Matrix(
            new double[] { 1, 0, 1.402 },
            new double[] { 1, -0.344136, -0.714136 },
            new double[] { 1, 1.772, 0 }
        );
        private static readonly Matrix QuantizationMatrix = new Matrix(
            new double[] { 16, 11, 10, 16, 24, 40, 51, 61 },
            new double[] { 12, 12, 14, 19, 26, 58, 60, 55 },
            new double[] { 14, 13, 16, 24, 40, 57, 69, 56 },
            new double[] { 14, 17, 22, 29, 51, 87, 80, 62 },
            new double[] { 18, 22, 37, 56, 68, 109, 103, 77 },
            new double[] { 24, 35, 55, 64, 81, 104, 113, 92 },
            new double[] { 49, 64, 78, 87, 103, 121, 120, 101 },
            new double[] { 72, 92, 95, 98, 112, 100, 103, 99 }
        );

        public static List<RlePair>[][][] Encode(Bitmap bmp) {
            YCbCrImage yCbCr = RgbToYCbCr(bmp);
            Dct(yCbCr.Y);
            Dct(yCbCr.Cb);
            Dct(yCbCr.Cr);
            var rle = new List<RlePair>[][][] {
                RunLengthEncode(yCbCr.Y),
                RunLengthEncode(yCbCr.Cb),
                RunLengthEncode(yCbCr.Cr)
            };

            return rle;
        }

        public static Bitmap Decode(List<RlePair>[][][] rle) {
            var yCbCr = new YCbCrImage(
                RunLengthDecode(rle[0]),
                RunLengthDecode(rle[1]),
                RunLengthDecode(rle[2])
            );
            InverseDct(yCbCr.Y);
            InverseDct(yCbCr.Cb);
            InverseDct(yCbCr.Cr);

            return YCbCrToRgb(yCbCr);
        }

        #region Encoding
        public static YCbCrImage RgbToYCbCr(Bitmap bmp) {
            var y = new Matrix(bmp.Height, bmp.Width);
            var cb = new Matrix(bmp.Height, bmp.Width);
            var cr = new Matrix(bmp.Height, bmp.Width);

            for (int i = 0; i < bmp.Height; i++) {
                for (int j = 0; j < bmp.Width; j++) {
                    Color color = bmp.GetPixel(j, i);
                    var rgb = new Vector(color.R, color.G, color.B);
                    Vector yCbCr = Rgb2YCbCr_Factor * rgb + Rgb2YCbCr_Offset;

                    y[i][j] = Math.Round(yCbCr[0]);
                    cb[i][j] = Math.Round(yCbCr[1]);
                    cr[i][j] = Math.Round(yCbCr[2]);
                }
            }

            return new YCbCrImage(y, cb, cr);
        }

        public static void Dct(Matrix matrix) {
            int hBlocks = matrix.ColCount / BlockSize;
            int vBlocks = matrix.RowCount / BlockSize;

            // Shift from [0, 255] to [-128, 127] with center at 0
            for (int i = 0; i < matrix.RowCount; i++) {
                for (int j = 0; j < matrix.ColCount; j++) {
                    matrix[i][j] -= 128;
                }
            }

            for (int blockI = 0; blockI < vBlocks; blockI++) {
                for (int blockJ = 0; blockJ < hBlocks; blockJ++) {
                    var dctBlock = new Matrix(BlockSize, BlockSize);

                    for (int u = 0; u < BlockSize; u++) {
                        for (int v = 0; v < BlockSize; v++) {
                            dctBlock[u][v] = Dct(matrix, blockI, blockJ, u, v);
                        }
                    }

                    for (int i = 0; i < BlockSize; i++) {
                        for (int j = 0; j < BlockSize; j++) {
                            matrix[i + blockI * BlockSize][j + blockJ * BlockSize] = Math.Round(dctBlock[i][j] / QuantizationMatrix[i][j]);
                        }
                    }
                }
            }
        }

        public static List<RlePair>[][] RunLengthEncode(Matrix matrix) {
            int vBlocks = matrix.RowCount / BlockSize;
            int hBlocks = matrix.ColCount / BlockSize;
            var rleBlocks = new List<RlePair>[vBlocks][];
            for (int i = 0; i < vBlocks; i++) {
                rleBlocks[i] = new List<RlePair>[hBlocks];
            }

            for (int blockI = 0; blockI < vBlocks; blockI++) {
                for (int blockJ = 0; blockJ < hBlocks; blockJ++) {
                    rleBlocks[blockI][blockJ] = RunLengthEncodeBlock(matrix, blockI, blockJ);
                }
            }

            return rleBlocks;
        }

        private static List<RlePair> RunLengthEncodeBlock(Matrix matrix, int blockI, int blockJ) {
            int i = 0, j = 0;
            int minI = blockI * BlockSize;
            int minJ = blockJ * BlockSize;
            int maxI = (blockI + 1) * BlockSize;
            int maxJ = (blockJ + 1) * BlockSize;
            var encoding = new List<RlePair> { new RlePair((int)matrix[i][j]) };

            // Right (down), down-left (stop), down (right), up-right 
            while (i != maxI - 1 || j != maxJ - 1) {
                if (j + 1 < maxJ) {
                    j += 1;
                }
                else if (i + 1 < maxI) {
                    i += 1;
                }

                AddToRle(encoding, (int)matrix[i][j]);

                while (i < maxI - 1 && j > minJ) {
                    i += 1;
                    j -= 1;
                    AddToRle(encoding, (int)matrix[i][j]);
                }

                if (i + 1 < maxI) {
                    i += 1;
                }
                else if (j + 1 < maxJ) {
                    j += 1;
                }

                AddToRle(encoding, (int)matrix[i][j]);

                while (i > minI && j + 1 < maxJ) {
                    i -= 1;
                    j += 1;
                    AddToRle(encoding, (int)matrix[i][j]);
                }
            }

            return encoding;
        }

        private static void AddToRle(List<RlePair> encoding, int value) {
            if (encoding[encoding.Count - 1].Value == value) {
                encoding[encoding.Count - 1].Freq += 1;
            }
            else {
                encoding.Add(new RlePair(value));
            }
        }

        private static double Dct(Matrix matrix, int blockI, int blockJ, int u, int v) {
            double sum = 0;
            for (int i = 0; i < BlockSize; i++) {
                for (int j = 0; j < BlockSize; j++) {
                    double elem = matrix[i + blockI * BlockSize][j + blockJ * BlockSize];
                    double cosProduct = Math.Cos((2 * i + 1) * u * Math.PI / 16) * Math.Cos((2 * j + 1) * v * Math.PI / 16);
                    sum += elem * cosProduct;
                }
            }

            return sum * Alpha(u) * Alpha(v) / 4;
        }
        #endregion

        #region Decoding
        public static Matrix RunLengthDecode(List<RlePair>[][] rleBlocks) {
            int vBlocks = rleBlocks.GetLength(0);
            int hBlocks = rleBlocks[0].GetLength(0);
            var matrix = new Matrix(vBlocks * BlockSize, hBlocks * BlockSize);

            for (int blockI = 0; blockI < vBlocks; blockI++) {
                for (int blockJ = 0; blockJ < hBlocks; blockJ++) {
                    RunLengthDecodeBlock(matrix, rleBlocks, blockI, blockJ);
                }
            }

            return matrix;
        }

        public static void InverseDct(Matrix matrix) {
            int hBlocks = matrix.ColCount / BlockSize;
            int vBlocks = matrix.RowCount / BlockSize;

            for (int i = 0; i < matrix.RowCount; i++) {
                for (int j = 0; j < matrix.ColCount; j++) {
                    matrix[i][j] *= QuantizationMatrix[i % BlockSize][j % BlockSize];
                }
            }

            for (int blockI = 0; blockI < vBlocks; blockI++) {
                for (int blockJ = 0; blockJ < hBlocks; blockJ++) {
                    var block = new Matrix(BlockSize, BlockSize);

                    for (int x = 0; x < BlockSize; x++) {
                        for (int y = 0; y < BlockSize; y++) {
                            block[x][y] = InverseDct(matrix, blockI, blockJ, x, y);
                        }
                    }

                    for (int i = 0; i < BlockSize; i++) {
                        for (int j = 0; j < BlockSize; j++) {
                            matrix[i + blockI * BlockSize][j + blockJ * BlockSize] = Clip(Math.Round(block[i][j]) + 128, 0, 255);
                        }
                    }
                }
            }
        }

        public static Bitmap YCbCrToRgb(YCbCrImage yCbCrImage) {
            var bmp = new Bitmap(yCbCrImage.Y.ColCount, yCbCrImage.Y.RowCount);

            for (int i = 0; i < bmp.Height; i++) {
                for (int j = 0; j < bmp.Width; j++) {
                    var yCbCr = new Vector(yCbCrImage.Y[i][j], yCbCrImage.Cb[i][j], yCbCrImage.Cr[i][j]);
                    Vector rgb = YCbCr2Rgb_Factor * (yCbCr - Rgb2YCbCr_Offset);

                    bmp.SetPixel(j, i, Color.FromArgb(
                        (int)Clip(rgb[0], 0, 255),
                        (int)Clip(rgb[1], 0, 255),
                        (int)Clip(rgb[2], 0, 255)
                    ));
                }
            }

            return bmp;
        }

        private static void RunLengthDecodeBlock(Matrix matrix, List<RlePair>[][] rleBlocks, int blockI, int blockJ) {
            int i = 0, j = 0;
            int minI = blockI * BlockSize;
            int minJ = blockJ * BlockSize;
            int maxI = (blockI + 1) * BlockSize;
            int maxJ = (blockJ + 1) * BlockSize;
            List<RlePair> encoding = rleBlocks[blockI][blockJ];
            matrix[i][j] = GetNextFromRle(encoding);

            // Right (down), down-left (stop), down (right), up-right 
            while (i != maxI - 1 || j != maxJ - 1) {
                if (j + 1 < maxJ) {
                    j += 1;
                }
                else if (i + 1 < maxI) {
                    i += 1;
                }

                matrix[i][j] = GetNextFromRle(encoding);

                while (i < maxI - 1 && j > minJ) {
                    i += 1;
                    j -= 1;
                    matrix[i][j] = GetNextFromRle(encoding);
                }

                if (i + 1 < maxI) {
                    i += 1;
                }
                else if (j + 1 < maxJ) {
                    j += 1;
                }

                matrix[i][j] = GetNextFromRle(encoding);

                while (i > minI && j + 1 < maxJ) {
                    i -= 1;
                    j += 1;
                    matrix[i][j] = GetNextFromRle(encoding);
                }
            }
        }

        private static int GetNextFromRle(List<RlePair> encoding) {
            if (encoding[0].Freq == 0) {
                encoding.RemoveAt(0);
            }

            encoding[0].Freq -= 1;
            return encoding[0].Value;
        }

        private static double InverseDct(Matrix matrix, int blockI, int blockJ, int x, int y) {
            double sum = 0;
            for (int i = 0; i < BlockSize; i++) {
                for (int j = 0; j < BlockSize; j++) {
                    double scale = Alpha(i) * Alpha(j);
                    double dctCoefficient = matrix[i + blockI * BlockSize][j + blockJ * BlockSize];
                    double cosProduct = Math.Cos((2 * x + 1) * i * Math.PI / 16) * Math.Cos((2 * y + 1) * j * Math.PI / 16);
                    sum += scale * dctCoefficient * cosProduct;
                }
            }

            return sum / 4;
        }
        #endregion

        private static double Alpha(int u) => u == 0 ? 1 / Math.Sqrt(2) : 1;
        
        private static double Clip(double value, double lowerBound, double upperBound) {
            return value < lowerBound ? lowerBound : (value > upperBound ? upperBound : value);
        }
    }
}
