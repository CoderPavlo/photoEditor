using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace photoEditor.Tools
{
    public static class ColorProcessing
    {
        public static void setColorProcessing(BitmapImage originalImage, System.Windows.Controls.Image selectedImage, StackPanel toolsPanel)
        {
            TextBox textBox1 = GeneralTools.textBoxProperties();
            TextBlock textBlock1 = GeneralTools.textBlockProperties("t = ");
            Grid grid = GeneralTools.gridProperties(1, 2, 1, textBlock1, textBox1);
            CheckBox checkBox = GeneralTools.checkBoxProperties("На оригінальне зображення");

            Grid gridLog = GeneralTools.gridProperties(1, 1, 20);
            string[] option = new string[] { "Двохступеневий метод", "Метод вирівнювання" };
            ComboBox comboBox = GeneralTools.comboBoxProperties(option, (sender, e) => comboBoxSelectionChanged(sender, grid, gridLog));
            

            Button button = GeneralTools.buttonProperties("Усунути",
                (sender, e) =>
                {

                    BitmapImage bitmapImage;
                    if (!checkBox.IsChecked.Value)
                        bitmapImage = GeneralTools.ImageToBitmapImage(selectedImage);
                    else
                        bitmapImage = originalImage;

                    if (comboBox.SelectedIndex == 0)
                    {

                        double t;
                        if (!Double.TryParse(textBox1.Text, out t))
                            return;
                        selectedImage.Source = twoStepMethod(bitmapImage, t, gridLog);
                    }
                    else
                        selectedImage.Source = EqualizeImage(bitmapImage, gridLog);

                    
                }
            );
            GeneralTools.AddElementsToStackPanel(toolsPanel, comboBox, grid, checkBox, button, gridLog);
        }
        static void comboBoxSelectionChanged(object sender, Grid grid, Grid gridLog)
        {
            if (sender is ComboBox comboBox)
            {

                for (int i = gridLog.Children.Count - 1; i >= 0; i--)
                {
                    UIElement child = gridLog.Children[i];
                    gridLog.Children.Remove(child);
                }
                int selectedIndex = comboBox.SelectedIndex;
                if (selectedIndex == 1)
                    grid.Visibility = Visibility.Collapsed;
                else
                    grid.Visibility = Visibility.Visible;
            }
        }
        static int findMin(double[] p, double t)
        {
            double sum = 0;
            for(int i=0; i<256; i++)
            {
                sum += p[i];
                if (sum >= t)
                    return i;
            }
            return 255;
        }

        static int findMax(double[] p, double t)
        {
            double sum = 0;
            for (int i = 255; i >=0; i--)
            {
                sum += p[i];
                if (sum >= t)
                    return i;
            }
            return 0;
        }
        static BitmapSource EqualizeImage(BitmapImage bitmapImage, Grid gridLog)
        {

            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * (bitmapImage.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[height * stride];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            int gridRow = 0;
            gridLog.Children.Add(GeneralTools.textBlockProperties("Гістограми початкового зображення: ", gridRow++, 0, 2));
            for (int i = 2; i >= 0; i--)
                addHistogramLog(histogram(pixelData, i, width, height), gridLog, ref gridRow);

            for (int i = 0; i < 3; i++) 
                EqualizeHistogramComponent(ref pixelData, width, height, stride, i);

            gridLog.Children.Add(GeneralTools.textBlockProperties("Гістограми після вирівнювання: ", gridRow++, 0, 2));
            for (int i = 2; i >= 0; i--)
                addHistogramLog(histogram(pixelData, i, width, height), gridLog, ref gridRow);

            return BitmapSource.Create(width, height, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, null, pixelData, stride);

        }

        static void EqualizeHistogramComponent(ref byte[] pixelData, int width, int height, int stride, int componentIndex)
        {
            int[] histogram = CalculateHistogram(pixelData, width, height, stride, componentIndex);
            int[] cumulativeHistogram = CalculateCumulativeHistogram(histogram);

            int totalPixels = width * height;
            double scaleFactor = 255.0 / totalPixels;

            for (int i = 0; i < pixelData.Length; i += 4) // Assuming 32 bits per pixel (8 bits per channel * 4 channels)
            {
                int pixelValue = pixelData[i + componentIndex];
                int newValue = (int)(cumulativeHistogram[pixelValue] * scaleFactor);

                pixelData[i + componentIndex] = toByte(newValue);
            }
        }

        static int[] CalculateHistogram(byte[] pixelData, int width, int height, int stride, int componentIndex)
        {
            int[] histogram = new int[256];

            for (int i = 0; i < pixelData.Length; i += 4) // Assuming 32 bits per pixel (8 bits per channel * 4 channels)
            {
                int pixelValue = pixelData[i + componentIndex];
                histogram[pixelValue]++;
            }

            return histogram;
        }

        static int[] CalculateCumulativeHistogram(int[] histogram)
        {
            int[] cumulativeHistogram = new int[256];
            cumulativeHistogram[0] = histogram[0];

            for (int i = 1; i < 256; i++)
            {
                cumulativeHistogram[i] = cumulativeHistogram[i - 1] + histogram[i];
            }

            return cumulativeHistogram;
        }
        static void addHistogramLog(System.Windows.Controls.Image image, string prop1, object prop1Value, string prop2, object prop2Value, Grid grid, ref int gridRow)
        {
            Grid.SetRow(image, gridRow++);
            Grid.SetColumnSpan(image, 2);
            TextBlock label1 = GeneralTools.textBlockProperties(prop1 + prop1Value.ToString(), gridRow);
            TextBlock label2 = GeneralTools.textBlockProperties(prop2 + prop2Value.ToString(), gridRow++, 1);

            grid.Children.Add(image);
            grid.Children.Add(label1);
            grid.Children.Add(label2);
        }
        static void addHistogramLog(System.Windows.Controls.Image image, Grid grid, ref int gridRow)
        {
            Grid.SetRow(image, gridRow++);
            Grid.SetColumnSpan(image, 2);
            grid.Children.Add(image);
        }

        static BitmapSource twoStepMethod(BitmapImage bitmapImage, double t, Grid gridLog)
        {
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * (bitmapImage.Format.BitsPerPixel / 8);
            byte[] pixelData = new byte[height * stride];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            List<double[]> p = new List<double[]>();
            for (int i = 0; i < 3; i++)
                p.Add(new double[256]);

            int bytesPerPixel = pixelData.Length / (width * height);
            double inv = 1.0 / (width * height);

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                for (int j = 0; j < 3; j++)
                    p[j][pixelData[i + j]] += inv;

            int[] min = new int[3];
            int[] max = new int[3];

            for(int i=0; i<3; i++)
            {
                min[i] = findMin(p[i], t);
                max[i] = findMax(p[i], t);
            }

            int gridRow = 0;
            gridLog.Children.Add(GeneralTools.textBlockProperties("Гістограми початкового зображення: ", gridRow++, 0, 2));
            for(int i=2; i>=0; i--)
                addHistogramLog(histogram(pixelData, i, width, height),"min = ", min[i], "max = ", max[i], gridLog, ref gridRow);

            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                for(int j=0; j<3; j++)
                    pixelData[i+j] = getColor(pixelData[i+j], min[j], max[j]);

            //гамма перетворення

            List<byte>[] components = new List<byte>[3];

            for (int i = 0; i < 3; i++)
                components[i]=new List<byte>();
            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                for (int j = 0; j < 3; j++)
                    components[j].Add(pixelData[i + j]);

            double[] median = new double[3];
            double[] gamma = new double[3];
            for (int i = 0; i < 3; i++)
                gamma[i] = findGamma(components[i], ref median[i]);

            gridLog.Children.Add(GeneralTools.textBlockProperties("Гістограми після приведення рівнів: ", gridRow++, 0, 2));
            for (int i = 2; i >= 0; i--)
                addHistogramLog(histogram(pixelData, i, width, height), "Медіана: ", median[i], "Гамма: ", Math.Round(gamma[i], 3), gridLog, ref gridRow);


            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
                for(int j= 0; j<3; j++)
                    pixelData[i+j] = toByte(255 * Math.Pow(pixelData[i+j] / 255.0, gamma[j]));


            gridLog.Children.Add(GeneralTools.textBlockProperties("Гістограми після гамма корекції: ", gridRow++, 0, 2));
            for (int i = 2; i >= 0; i--)
                addHistogramLog(histogram(pixelData, i, width, height), gridLog, ref gridRow);

            return BitmapSource.Create(width, height, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, null, pixelData, stride);
        }

        public static System.Windows.Controls.Image histogram(byte[] pixelData, int option, int width, int height)
        {

            const int size = 256;
            double[] xData = new double[size];
            for (int i = 0; i < size; i++)
                xData[i] = i;
            double[] yData = new double[size];

            int bytesPerPixel = pixelData.Length / (width * height);
            for (int i = 0; i < pixelData.Length; i += bytesPerPixel)
            {
                yData[pixelData[i + option]] += 1;
            }

            for (int i = 0; i < size; i++)
                yData[i] /= width * height;
            var plt = new ScottPlot.Plot(800, 800);
            System.Drawing.Color color;
            switch (option)
            {
                case 0:
                    color = System.Drawing.Color.Blue;
                    break;
                case 1:
                    color = System.Drawing.Color.Green;
                    break;
                default:
                    color = System.Drawing.Color.Red;
                    break;
            }
            plt.AddBar(yData, xData, color);

            plt.SetAxisLimits(yMin: 0);

            System.Drawing.Bitmap bitmap = plt.GetBitmap();
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();

            image.Margin = new Thickness(20, 10, 20, 0);
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                BitmapImage sourceImage = new BitmapImage();
                sourceImage.BeginInit();
                sourceImage.StreamSource = stream;
                sourceImage.CacheOption = BitmapCacheOption.OnLoad;
                sourceImage.EndInit();
                image.Source = sourceImage;
            }

            return image;
        }

        public static double findGamma(List<byte> values, ref double median)
        {
            values.Sort();
            int middle = values.Count / 2;
            if (values.Count % 2 == 0)            
                median = (values[middle - 1] + values[middle]) / 2.0;
            else            
                median = values[middle];

            if (median > 127)
                return (median - 127) / 127.0 + 1;
            else if (median < 127)
                return 1 - (127 - median) / 127.0;
            else
                return 1.0;
        }


        static byte getColor(byte i, int min, int max)
        {
            if (i <= min)
                return 0;
            if (i >= max)
                return 255;
            return toByte(255 * (i - min) / (max - min));
        }

        static byte toByte(double a)
        {
            if (a > 255) a = 255;
            else if (a < 0) a = 0;
            return (byte)a;
        }
    }
}
