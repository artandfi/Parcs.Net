using System;
using System.Drawing;
using System.Threading;
using Parcs;

namespace JpegModule {
    public class JpegEncoder : IModule {
        public void Run(ModuleInfo info, CancellationToken token = default(CancellationToken)) {
            var bmp = (Bitmap)info.Parent.ReadObject(typeof(Bitmap));
            var encoded = JpegCodec.Encode(bmp);
            Console.WriteLine($"{encoded}");
            info.Parent.WriteData(Serializer.Serialize(encoded));
        }
    }
}
