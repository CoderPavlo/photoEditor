using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace photoEditor.Tools
{
    static public class Hist
    {
        static public void setHistogram(Image selectedImage, StackPanel toolsPanel)
        {
            Image image = GeneralTools.imageProperties();
            toolsPanel.Children.Add(image);
            image.Source = histogram(GeneralTools.ImageToBitmapImage(selectedImage));
        }

        static public void setEqualizeHistogram(Image selectedImage, StackPanel toolsPanel)
        {
            Image image = GeneralTools.imageProperties();
            toolsPanel.Children.Add(image);
            selectedImage.Source = EqualizeImage(GeneralTools.ImageToBitmapImage(selectedImage), image);
        }

        public static BitmapImage histogram(BitmapImage selectedImage)
        {
            WriteableBitmap writableBitmap = new WriteableBitmap(selectedImage);

            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;
            const int size = 256;
            double[] xData = new double[size];
            for (int i = 0; i < size; i++)
                xData[i] = i;
            double[] yData = new double[size];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    System.Windows.Media.Color pixelColor = writableBitmap.GetPixel(x, y);
                    yData[pixelColor.R] += 1;

                }
            }
            for (int i = 0; i < size; i++)
                yData[i] /= width * height;
            var plt = new ScottPlot.Plot(800, 800);
            plt.AddBar(yData, xData, System.Drawing.Color.Gray);

            plt.SetAxisLimits(yMin: 0);

            System.Drawing.Bitmap bitmap = plt.GetBitmap();

            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                BitmapImage sourceImage = new BitmapImage();
                sourceImage.BeginInit();
                sourceImage.StreamSource = stream;
                sourceImage.CacheOption = BitmapCacheOption.OnLoad;
                sourceImage.EndInit();

                return sourceImage;
            }
        }

        static BitmapImage EqualizeImage(BitmapImage sourceImage, Image image)
        {
            int width = sourceImage.PixelWidth;
            int height = sourceImage.PixelHeight;

            FormatConvertedBitmap grayImage = new FormatConvertedBitmap(sourceImage, PixelFormats.Gray8, null, 0);
            int[] histogram = new int[256];
            int totalPixels = width * height;

            byte[] pixels = new byte[width * height];
            grayImage.CopyPixels(pixels, width, 0);

            for (int i = 0; i < pixels.Length; i++)
            {
                histogram[pixels[i]]++;
            }

            int[] cumulativeHistogram = new int[256];
            cumulativeHistogram[0] = histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cumulativeHistogram[i] = cumulativeHistogram[i - 1] + histogram[i];
            }

            byte[] equalizedPixels = new byte[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                equalizedPixels[i] = (byte)(255 * cumulativeHistogram[pixels[i]] / totalPixels);
            }


            BitmapSource equalizedImage = BitmapSource.Create(width, height, sourceImage.DpiX, sourceImage.DpiY, PixelFormats.Gray8, null, equalizedPixels, width);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = null;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.CreateOptions = BitmapCreateOptions.None;
            bitmapImage.DecodePixelHeight = equalizedImage.PixelHeight;
            bitmapImage.DecodePixelWidth = equalizedImage.PixelWidth;
            bitmapImage.StreamSource = ConvertBitmapSourceToStream(equalizedImage);
            bitmapImage.EndInit();

            const int size = 256;
            double[] xData = new double[size];
            for (int i = 0; i < size; i++)
                xData[i] = i;
            double[] yData = new double[size];

            for (int i = 0; i < equalizedPixels.Length; i++)
            {
                yData[equalizedPixels[i]]++;
            }

            for (int i = 0; i < size; i++)
                yData[i] /= width * height;
            var plt = new ScottPlot.Plot(800, 800);
            plt.AddBar(yData, xData, System.Drawing.Color.Gray);

            plt.SetAxisLimits(yMin: 0);

            System.Drawing.Bitmap bitmap = plt.GetBitmap();

            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                BitmapImage sImage = new BitmapImage();
                sImage.BeginInit();
                sImage.StreamSource = stream;
                sImage.CacheOption = BitmapCacheOption.OnLoad;
                sImage.EndInit();
                image.Source = sImage;
            }

            return bitmapImage;
        }

        static Stream ConvertBitmapSourceToStream(BitmapSource bitmapSource)
        {
            MemoryStream stream = new MemoryStream();

            BitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);

            return stream;
        }

    }
}
