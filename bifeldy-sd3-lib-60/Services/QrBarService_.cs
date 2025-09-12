/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Qr Bar Code
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

using QRCoder;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using ZXing.Common;
using ZXing.ImageSharp;

namespace bifeldy_sd3_lib_60.Services {

    public interface IQrBarService {
        Image AddBackground(Image qrImage, Image bgImage, float bgBrightness = 2f, float bgOpacity = 0.5f);
        Image AddCaptionBar(Image qrImage, string caption, Color textColor, Color textBackgroundColor);
        Image AddCaptionQr(Image qrImage, string caption, Color textColor, Color textBackgroundColor);
        Image AddLogo(Image qrImage, Image logoImg, double logoScale, bool drawCircleBg = true);
        Image GenerateBarCode(string content, int minWidthPx = 512, int heightPx = 192, bool withPadding = true);
        string ReadTextFromQrAndBarCode(Image bitmapImage);
        Image GenerateQrCode(string content, int version = -1, int minSizePx = 512);
    }

    [SingletonServiceRegistration]
    public sealed class CQrBarService : IQrBarService {

        private static readonly int MAX_RETRY = 5;

        public CQrBarService() {
            //
        }

        private void DrawCaption(Image<Rgba32> img, string caption, int fontSize, int fontPadding, Color textColor, Color textBackgroundColor) {
            Font font = SystemFonts.CreateFont("Arial", fontSize);

            var textOptions = new RichTextOptions(font) {
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = img.Width - (fontPadding * 2),
                WordBreaking = WordBreaking.BreakAll
            };

            FontRectangle textSize = TextMeasurer.MeasureSize(caption, textOptions);

            int newHeight = img.Height + (int)textSize.Height + (fontPadding * 2);
            int newWidth = Math.Max(img.Width, (int)textSize.Width + (fontPadding * 2));

            using (var extended = new Image<Rgba32>(newWidth, newHeight, Color.White)) {
                int qrX = (newWidth - img.Width) / 2;

                var txtLoc = new PointF((newWidth - textSize.Width) / 2, img.Height);
                var txtBgBox = new RectangleF(
                    txtLoc.X - 2, txtLoc.Y - 2,
                    textSize.Width + (fontPadding / 2) + 2,
                    textSize.Height + (fontPadding / 2) + 2
                );

                extended.Mutate(x => {
                    _ = x.DrawImage(img, new Point(qrX, 0), 1f);
                    _ = x.Fill(textBackgroundColor, txtBgBox);
                    _ = x.DrawText(caption, font, textColor, txtLoc);
                });

                img.Mutate(x => {
                    _ = x.Resize(extended.Width, extended.Height);
                    _ = x.DrawImage(extended, new Point(0, 0), 1f);
                });
            }
        }

        private void CopyFrameMetadata(ImageFrame srcFrame, ImageFrame destFrame) {
            if (srcFrame.Metadata.GetGifMetadata() is { } srcGif) {
                GifFrameMetadata outGif = destFrame.Metadata.GetGifMetadata();

                outGif.FrameDelay = srcGif.FrameDelay;
                outGif.DisposalMethod = srcGif.DisposalMethod;
            }

            if (srcFrame.Metadata.GetWebpMetadata() is { } srcWebp) {
                WebpFrameMetadata outWebp = destFrame.Metadata.GetWebpMetadata();

                outWebp.FrameDelay = srcWebp.FrameDelay;
            }
        }

        private void CopyLoopMetadata(Image srcImage, Image destImage) {
            if (srcImage.Metadata.GetGifMetadata() is { } gifMeta) {
                destImage.Metadata.GetGifMetadata().RepeatCount = gifMeta.RepeatCount;
            }

            if (srcImage.Metadata.GetWebpMetadata() is { } webpMeta) {
                destImage.Metadata.GetWebpMetadata().RepeatCount = webpMeta.RepeatCount;
            }
        }

        private Image<Rgba32> ProcessAnimatedFrames(Image srcImage, Action<Image<Rgba32>, ImageFrame> perFrameAction) {
            Image<Rgba32> output = null;

            foreach (ImageFrame frame in srcImage.Frames) {
                using (var frameImage = new Image<Rgba32>(frame.Width, frame.Height)) {
                    _ = frameImage.Frames.AddFrame(frame);

                    frameImage.Frames.RemoveFrame(0);

                    perFrameAction(frameImage, frame);

                    if (output == null) {
                        output = frameImage.Clone();

                        this.CopyLoopMetadata(srcImage, output);
                        this.CopyFrameMetadata(frame, output.Frames.RootFrame);
                    }
                    else {
                        ImageFrame<Rgba32> added = output.Frames.AddFrame(frameImage.Frames.RootFrame);

                        this.CopyFrameMetadata(frame, added);
                    }
                }
            }

            return output ?? srcImage.CloneAs<Rgba32>();
        }

        private void DrawLogo(Image<Rgba32> img, Image<Rgba32> logo, Point drawStart, PointF circleCenter, int circleDiameter, bool drawCircleBg, bool drawBorder = true, float borderThickness = 4f) {
            img.Mutate(x => {
                if (drawCircleBg) {
                    float radius = circleDiameter / 2f;
                    var circle = new EllipsePolygon(circleCenter, radius);

                    _ = x.Fill(Color.White, circle);

                    if (drawBorder && borderThickness > 0) {
                        _ = x.Draw(Color.Black, borderThickness, circle);
                    }
                }

                _ = x.DrawImage(logo, drawStart, 1f);
            });
        }

        private Image<Rgba32> SetWhiteAsTransparent(Image img, ref Rgba32[] pixels) {
            Image<Rgba32> imgClone = img.CloneAs<Rgba32>();

            imgClone.CopyPixelDataTo(pixels);

            byte whiteThreshold = 250;
            for (int i = 0; i < pixels.Length; i++) {
                ref Rgba32 px = ref pixels[i];

                if (px.R >= whiteThreshold && px.G >= whiteThreshold && px.B >= whiteThreshold) {
                    px.A = 0;
                }
            }

            return imgClone;
        }

        /* ** */

        public Image AddBackground(Image qrImage, Image bgImage, float bgBrightness = 2f, float bgOpacity = 0.5f) {
            int pixelCount = qrImage.Width * qrImage.Height;
            var pixels = new Rgba32[pixelCount];

            using (Image<Rgba32> qrImageClone = this.SetWhiteAsTransparent(qrImage, ref pixels)) {
                using (var transparentQrImage = Image.LoadPixelData<Rgba32>(pixels, qrImageClone.Width, qrImageClone.Height)) {
                    if (bgImage.Frames.Count > 1) {
                        return this.ProcessAnimatedFrames(bgImage, (frameImage, frame) => {
                            frameImage.Mutate(x => {
                                _ = x.Resize(new ResizeOptions {
                                    Size = new Size(qrImageClone.Width, qrImageClone.Height),
                                    Mode = ResizeMode.Crop
                                });

                                _ = x.Brightness(bgBrightness);
                                _ = x.Opacity(bgOpacity);
                                _ = x.DrawImage(transparentQrImage, new Point(0, 0), 1.0f);
                            });
                        });
                    }
                    else {
                        Image<Rgba32> bgClone = bgImage.CloneAs<Rgba32>();

                        bgClone.Mutate(x => {
                            _ = x.Resize(new ResizeOptions {
                                Size = new Size(qrImageClone.Width, qrImageClone.Height),
                                Mode = ResizeMode.Crop
                            });

                            _ = x.Brightness(bgBrightness);
                            _ = x.Opacity(bgOpacity);
                            _ = x.DrawImage(transparentQrImage, new Point(0, 0), 1.0f);
                        });

                        return bgClone;
                    }
                }
            }
        }

        public Image AddCaptionBar(Image qrImage, string caption, Color textColor, Color textBackgroundColor) {
            int pixelCount = qrImage.Width * qrImage.Height;
            var pixels = new Rgba32[pixelCount];

            using (Image<Rgba32> qrImageClone = this.SetWhiteAsTransparent(qrImage, ref pixels)) {
                if (qrImageClone.Frames.Count > 1) {
                    return this.ProcessAnimatedFrames(qrImageClone, (frameImage, frame) => {
                        int fontSize = (int)(frameImage.Width * 0.04f);
                        int fontPadding = fontSize / 2;

                        this.DrawCaption(frameImage, caption, fontSize, fontPadding, textColor, textBackgroundColor);
                    });
                }
                else {
                    Image<Rgba32> output = qrImageClone.CloneAs<Rgba32>();

                    int fontSize = (int)(output.Width * 0.04f);
                    int fontPadding = fontSize / 2;

                    this.DrawCaption(output, caption, fontSize, fontPadding, textColor, textBackgroundColor);

                    return output;
                }
            }
        }

        public Image AddCaptionQr(Image qrImage, string caption, Color textColor, Color textBackgroundColor) {
            int pixelCount = qrImage.Width * qrImage.Height;
            var pixels = new Rgba32[pixelCount];

            using (Image<Rgba32> qrImageClone = this.SetWhiteAsTransparent(qrImage, ref pixels)) {
                if (qrImageClone.Frames.Count > 1) {
                    return this.ProcessAnimatedFrames(qrImageClone, (frameImage, frame) => {
                        int fontSize = (int)(frameImage.Width * 0.03f);
                        int fontPadding = fontSize / 2;

                        this.DrawCaption(frameImage, caption, fontSize, fontPadding, textColor, textBackgroundColor);
                    });
                }
                else {
                    Image<Rgba32> output = qrImageClone.CloneAs<Rgba32>();

                    int fontSize = (int)(output.Width * 0.03f);
                    int fontPadding = fontSize / 2;

                    this.DrawCaption(output, caption, fontSize, fontPadding, textColor, textBackgroundColor);

                    return output;
                }
            }
        }

        public Image AddLogo(Image qrImage, Image logoImg, double logoScale, bool drawCircleBg = true) {
            logoScale = Math.Min(Math.Max(logoScale, 0.15), 0.25);

            int targetLogoWidth = (int)(qrImage.Width * logoScale);
            int targetLogoHeight = (int)(qrImage.Height * logoScale);

            using (Image<Rgba32> logo = logoImg.CloneAs<Rgba32>()) {
                logo.Mutate(x => {
                    _ = x.Resize(new ResizeOptions {
                        Size = new Size(targetLogoWidth, targetLogoHeight),
                        Mode = ResizeMode.Max
                    });
                });

                int pixelCount = logo.Width * logo.Height;
                var pixels = new Rgba32[pixelCount];

                using (Image<Rgba32> qrImageClone = this.SetWhiteAsTransparent(logo, ref pixels)) {
                    using (var transparentLogo = Image.LoadPixelData<Rgba32>(pixels, qrImageClone.Width, qrImageClone.Height)) {
                        var logoDrawStart = new Point(
                            (qrImage.Width - transparentLogo.Width) / 2,
                            (qrImage.Height - transparentLogo.Height) / 2
                        );

                        int circleDiameter = (int)(Math.Max(transparentLogo.Width, transparentLogo.Height) * 1.2);

                        int circleX = (qrImage.Width - circleDiameter) / 2;
                        int circleY = (qrImage.Height - circleDiameter) / 2;

                        var logoDrawCircleCenter = new PointF(
                            circleX + (circleDiameter / 2f),
                            circleY + (circleDiameter / 2f)
                        );

                        if (qrImage.Frames.Count > 1) {
                            return this.ProcessAnimatedFrames(qrImage, (frameImage, frame) => {
                                this.DrawLogo(
                                    frameImage,
                                    transparentLogo,
                                    logoDrawStart,
                                    logoDrawCircleCenter,
                                    circleDiameter,
                                    drawCircleBg
                                );
                            });
                        }
                        else {
                            Image<Rgba32> output = qrImage.CloneAs<Rgba32>();

                            this.DrawLogo(output, transparentLogo, logoDrawStart, logoDrawCircleCenter, circleDiameter, drawCircleBg);

                            return output;
                        }
                    }
                }
            }
        }

        /* ** */

        public Image GenerateBarCode(string content, int minWidthPx = 512, int heightPx = 192, bool withPadding = true) {
            var writer = new BarcodeWriter<Rgba32>() {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new EncodingOptions() {
                    PureBarcode = false,
                    NoPadding = true,
                    Margin = 0
                }
            };

            using (Image<Rgba32> generatedImage = writer.Write(content)) {
                int retry = 0;
                int sizeLimit = minWidthPx;

                int padding = withPadding ? 10 : 0;
                var drawLocation = new Point(padding, padding);

                while (retry <= MAX_RETRY) {
                    var res = new Image<Rgba32>(sizeLimit, heightPx, Color.White);

                    using (Image<Rgba32> img = generatedImage.Clone()) {
                        img.Mutate(x => {
                            _ = x.Resize(new Size(sizeLimit - (padding * 2), heightPx - (padding * 2)));
                        });

                        res.Mutate(x => {
                            _ = x.DrawImage(img, drawLocation, 1.0f);
                        });

                        string qrText = this.ReadTextFromQrAndBarCode(res);
                        if (content == qrText) {
                            return res;
                        }

                        retry++;
                        sizeLimit = minWidthPx + (minWidthPx * retry / 2);
                    }

                    res.Dispose();
                }

                throw new Exception("Hasil Bar Code Tidak Terbaca, Mohon Perbesar Resolusi");
            }
        }

        public string ReadTextFromQrAndBarCode(Image bitmapImage) {
            var reader = new BarcodeReader<Rgba32>() {
                AutoRotate = true,
                Options = new DecodingOptions() {
                    PureBarcode = false,
                    TryInverted = true,
                    TryHarder = true
                }
            };

            using (Image<Rgba32> image = bitmapImage.CloneAs<Rgba32>()) {
                ZXing.Result decoded = reader.Decode(image);

                if (decoded != null) {
                    return decoded.Text;
                }
            }

            return null;
        }

        public Image GenerateQrCode(string content, int version = -1, int minSizePx = 512) {
            using (var qrGenerator = new QRCodeGenerator()) {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.L, requestedVersion: version)) {
                    using (var qrCode = new PngByteQRCode(qrCodeData)) {
                        byte[] img = qrCode.GetGraphic(10);

                        int retry = 0;
                        int sizeLimit = minSizePx;

                        while (retry <= MAX_RETRY) {
                            var res = Image.Load(img);

                            res.Mutate(x => {
                                _ = x.Resize(new Size(sizeLimit, sizeLimit));
                            });

                            string qrText = this.ReadTextFromQrAndBarCode(res);
                            if (content == qrText) {
                                return res;
                            }

                            retry++;
                            sizeLimit = minSizePx + (minSizePx * retry / 2);

                            res.Dispose();
                        }

                        throw new Exception("Hasil QR Code Tidak Terbaca, Mohon Perbesar Resolusi");
                    }
                }
            }
        }

    }

}
