namespace JpegModule {
    public class RlePair {
        public int Value { get; set; }
        public int Freq { get; set; }

        public RlePair(int value, int freq = 1) {
            Value = value;
            Freq = freq;
        }

        public override bool Equals(object obj) {
            var other = (RlePair)obj;
            return this.Value == other.Value && this.Freq == other.Freq;
        }
    }
}
