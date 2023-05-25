namespace JpegModule {
    public struct YCbCrImage {
        public Matrix Y { get; private set; }
        public Matrix Cb { get; private set; }
        public Matrix Cr { get; private set; }

        public YCbCrImage(Matrix y, Matrix cb, Matrix cr) {
            Y = y;
            Cb = cb;
            Cr = cr;
        }

        public override bool Equals(object obj) {
            var other = (YCbCrImage)obj;
            return this.Y.Equals(other.Y) && this.Cb.Equals(other.Cb) && this.Cr.Equals(other.Cr);
        }
    }
}
