using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Parcs;
using log4net;

namespace JpegModule {
    public class JpegMainModule : MainModule {
        private const string OutputFileName = "decoded.bmp";
        private static readonly ILog _log = LogManager.GetLogger(typeof(JpegMainModule));
        private static CommandLineOptions _options;

        public static void Main(string[] args) {
            log4net.Config.XmlConfigurator.Configure();
            _options = new CommandLineOptions();

            if (args != null && !CommandLine.Parser.Default.ParseArguments(args, _options)) {
                throw new ArgumentException($@"Cannot parse the arguments. Possible usage: {_options.GetUsage()}");
            }

            (new JpegMainModule()).RunModule(_options);
        }

        public override void Run(ModuleInfo info, CancellationToken token = default(CancellationToken)) {
            Bitmap bmp;
            try {
                bmp = new Bitmap(_options.BmpFileName);
            }
            catch (FileNotFoundException e) {
                _log.Error("Bitmap file not found.", e);
                return;
            }

            int[] possiblePointNumbers = { 1, 2, 4 };
            if (!possiblePointNumbers.Contains(_options.PointsNum)) {
                _log.Error($"Running on {_options.PointsNum} points is not supported. Supported numbers of points: {string.Join(" ", possiblePointNumbers)}");
                return;
            }

            _log.Info($"Starting JPEG module on {_options.PointsNum} points");

            var points = new IPoint[_options.PointsNum];
            var channels = new IChannel[_options.PointsNum];
            for (int i = 0; i < _options.PointsNum; i++) {
                points[i] = info.CreatePoint();
                channels[i] = points[i].CreateChannel();
                points[i].ExecuteClass(typeof(JpegEncoder).ToString());
            }

            var rles = new string[4];
            DateTime time = DateTime.Now;
            _log.Info("Waiting for result...");

            switch (_options.PointsNum) {
                case 1:
                    channels[0].WriteObject(bmp);
                    
                    LogSendingTime(time);
                    rles[0] = channels[0].ReadString();
                    break;
                case 2:
                    channels[0].WriteObject(bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height / 2), bmp.PixelFormat));
                    channels[1].WriteObject(bmp.Clone(new Rectangle(0, bmp.Height / 2, bmp.Width, bmp.Height / 2), bmp.PixelFormat));

                    LogSendingTime(time);
                    rles[0] = new Lazy<string>(channels[0].ReadString).Value;
                    rles[1] = new Lazy<string>(channels[1].ReadString).Value;
                    break;
                case 4:
                    channels[0].WriteObject(bmp.Clone(new Rectangle(0, 0, bmp.Width / 2, bmp.Height / 2), bmp.PixelFormat));
                    channels[1].WriteObject(bmp.Clone(new Rectangle(bmp.Width / 2, 0, bmp.Width / 2, bmp.Height / 2), bmp.PixelFormat));
                    channels[2].WriteObject(bmp.Clone(new Rectangle(0, bmp.Height / 2, bmp.Width / 2, bmp.Height / 2), bmp.PixelFormat));
                    channels[3].WriteObject(bmp.Clone(new Rectangle(bmp.Width / 2, bmp.Height / 2, bmp.Width / 2, bmp.Height / 2), bmp.PixelFormat));

                    LogSendingTime(time);
                    rles[0] = new Lazy<string>(channels[0].ReadString).Value;
                    rles[1] = new Lazy<string>(channels[1].ReadString).Value;
                    rles[2] = new Lazy<string>(channels[2].ReadString).Value;
                    rles[3] = new Lazy<string>(channels[3].ReadString).Value;
                    break;
            }

            LogResultFoundTime(time);
            SaveOutput(rles.Take(_options.PointsNum).Select(x => Serializer.Deserialize(x)).ToArray(), _options.PointsNum);
        }

        private static void LogSendingTime(DateTime time) {
            _log.Info($"Sending finished: time = {Math.Round((DateTime.Now - time).TotalSeconds, 3)}");
        }

        private static void LogResultFoundTime(DateTime time) {
            _log.Info($"Result found: time = {Math.Round((DateTime.Now - time).TotalSeconds, 3)}, saving the result to the file {OutputFileName}");
        }

        private static void SaveOutput(List<RlePair>[][][][] rles, int pointsNum) {
            switch (pointsNum) {
                case 1:
                    JpegCodec.Decode(rles[0]).Save(OutputFileName, ImageFormat.Bmp);
                    break;
                case 2:
                    var bmp1 = JpegCodec.Decode(rles[0]);
                    var bmp2 = JpegCodec.Decode(rles[1]);
                    var bmp = MergeBitmapsByHeight(bmp1, bmp2);
                    bmp.Save(OutputFileName, ImageFormat.Bmp);
                    break;
                case 4:
                    var bitmap1 = JpegCodec.Decode(rles[0]);
                    var bitmap2 = JpegCodec.Decode(rles[1]);
                    var bitmap3 = JpegCodec.Decode(rles[2]);
                    var bitmap4 = JpegCodec.Decode(rles[3]);
                    var leftBmp = MergeBitmapsByHeight(bitmap1, bitmap3);
                    var rightBmp = MergeBitmapsByHeight(bitmap2, bitmap4);
                    var bitmap = MergeBitmapsByWidth(leftBmp, rightBmp);
                    bitmap.Save(OutputFileName, ImageFormat.Bmp);
                    break;
            }
        }

        private static Bitmap MergeBitmapsByHeight(Bitmap bmp1, Bitmap bmp2) {
            var bmp = new Bitmap(bmp1.Width, bmp1.Height + bmp2.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(bmp1, 0, 0);
            g.DrawImage(bmp2, 0, bmp1.Height);

            return bmp;
        }

        private static Bitmap MergeBitmapsByWidth(Bitmap bmp1, Bitmap bmp2) {
            var bmp = new Bitmap(bmp1.Width + bmp2.Width, bmp1.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(bmp1, 0, 0);
            g.DrawImage(bmp2, bmp1.Width, 0);

            return bmp;
        }
    }
}
