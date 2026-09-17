using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace WinComputerUse.Native
{
    public static class CaptureService
    {
        public static Dictionary<string, object> Capture(long hwndVal, string format, int quality, string outFile)
        {
            return DesktopManager.RunOnInteractiveDesktop(() =>
            {
                Rectangle bounds;
                IntPtr hWnd = new IntPtr(hwndVal);

                if (hwndVal != 0 && WindowService.IsWindow(hWnd))
                {
                    WindowService.ActivateWindow(hwndVal);
                    RECT rect = WindowService.GetBounds(hWnd);
                    bounds = new Rectangle(rect.Left, rect.Top, Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top));
                }
                else
                {
                    bounds = SystemInformation.VirtualScreen;
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        bounds = Screen.PrimaryScreen.Bounds;
                    }
                }

                using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                    }

                    ImageFormat imgFormat = ImageFormat.Jpeg;
                    ImageCodecInfo encoder = GetEncoder(ImageFormat.Jpeg);
                    EncoderParameters encParams = new EncoderParameters(1);
                    encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);

                    if (format.ToLowerInvariant() == "png")
                    {
                        imgFormat = ImageFormat.Png;
                        encoder = GetEncoder(ImageFormat.Png);
                        encParams = null;
                    }

                    string dataUrl;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        if (encParams != null)
                            bmp.Save(ms, encoder, encParams);
                        else
                            bmp.Save(ms, imgFormat);

                        byte[] bytes = ms.ToArray();
                        string mime = format.ToLowerInvariant() == "png" ? "image/png" : "image/jpeg";
                        dataUrl = "data:" + mime + ";base64," + Convert.ToBase64String(bytes);
                    }

                    if (!string.IsNullOrEmpty(outFile))
                    {
                        string dir = Path.GetDirectoryName(outFile);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        if (encParams != null)
                            bmp.Save(outFile, encoder, encParams);
                        else
                            bmp.Save(outFile, imgFormat);
                    }

                    return new Dictionary<string, object>()
                    {
                        { "width", bounds.Width },
                        { "height", bounds.Height },
                        { "originX", bounds.X },
                        { "originY", bounds.Y },
                        { "dataUrl", dataUrl },
                        { "filepath", outFile ?? "" }
                    };
                }
            });
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }
    }
}
