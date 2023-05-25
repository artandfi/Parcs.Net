using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpegModule {
    public static class ImageGenerator {
        public static Bitmap GenerateCheckered(int width, int height) {
            var bmp = new Bitmap(width, height);

            bool isBlack;
            for (int i = 0; i < height; i++) {
                isBlack = i % 2 == 0;
                for (int j = 0; j < width; j++) {
                    bmp.SetPixel(j, i, isBlack ? Color.Black : Color.White);
                    isBlack = !isBlack;
                }
            }

            return bmp;
        }

        public static Bitmap GenerateRandomized(int width, int height) {
            var bmp = new Bitmap(width, height);
            var rng = new Random();

            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    bmp.SetPixel(j, i, Color.FromArgb(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256)));
                }
            }

            return bmp;
        }
    }
}
