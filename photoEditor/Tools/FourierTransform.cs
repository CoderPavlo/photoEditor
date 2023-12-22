using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Numerics;

using OpenCvSharp;
using OpenCvSharp.XFeatures2D;
using OpenCvSharp.WpfExtensions;
using System.Windows;
using System.Windows.Media.Media3D;
using System.IO;
using ScottPlot.Drawing.Colormaps;
using System.Text.RegularExpressions;

namespace photoEditor.Tools
{
    static public class FourierTransform
    {
        static BitmapSource CalculateFourierTransform(BitmapImage bitmapImage)
        {
            FormatConvertedBitmap grayImage = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, null, 0);
            Mat matImage = grayImage.ToMat();

            Mat floatImage = CenterImage(matImage);

            Mat fftImage = new Mat();
            Cv2.Dft(floatImage, fftImage, DftFlags.ComplexOutput);

            Mat[] channels = Cv2.Split(fftImage);
            Mat magnitude = new Mat();
            Cv2.Magnitude(channels[0], channels[1], magnitude);

            Cv2.Add(magnitude, new Scalar(1.0), magnitude);
            Cv2.Log(magnitude, magnitude);

            Cv2.Normalize(magnitude, magnitude, 0, 255, NormTypes.MinMax, MatType.CV_8U);

            return magnitude.ToBitmapSource();
        }

        static Mat CenterImage(Mat matImage)
        {

            Mat floatImage = new Mat();
            matImage.ConvertTo(floatImage, MatType.CV_32FC1);
            for (int y = 0; y < floatImage.Rows; y++)
                for (int x = 0; x < floatImage.Cols; x++)
                    floatImage.At<float>(y, x) *= (float)Math.Pow(-1, x + y);
            return floatImage;
        }

        static public void setFourierSpectrum(Image selectedImage, StackPanel toolsPanel)
        {
            Image image = GeneralTools.imageProperties();
            image.Source = CalculateFourierTransform(GeneralTools.ImageToBitmapImage(selectedImage));
            toolsPanel.Children.Add(image);
        }

        static void comboBoxSelectionChanged(object sender, Grid grid)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.SelectedIndex == 1 || comboBox.SelectedIndex == 4)
                    grid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                else
                    grid.RowDefinitions[1].Height = new GridLength(0);
            }
        }

        static void filterClick(BitmapImage originalIimage, CheckBox checkBox, Image selectedImage, TextBox textBoxD0, TextBox textBox_n, ComboBox comboBox, Image image)
        {
            BitmapImage bitmapImage = originalIimage;
            if (!checkBox.IsChecked.Value)
            {
                bitmapImage = GeneralTools.ImageToBitmapImage(selectedImage);
            }
            //Перетворення Фур'є
            FormatConvertedBitmap grayImage = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, null, 0);
            Mat matImage = grayImage.ToMat();
            Mat floatImage = CenterImage(matImage);
            Mat fftImage = new Mat();
            Cv2.Dft(floatImage, fftImage, DftFlags.ComplexOutput);


            // Обчислюємо розміри зображення
            int rows = fftImage.Rows;
            int cols = fftImage.Cols;
            List<double> distances = new List<double>();
            List<double> values = new List<double>();
            double d0;
            if (!Double.TryParse(textBoxD0.Text, out d0)) return;
            // Цикл для обчислення фільтру
            for (int u = 0; u < rows; u++)
            {
                for (int v = 0; v < cols; v++)
                {
                    // Обчислюємо відстань до центра Фур'є-спектру
                    double distance = Math.Sqrt(Math.Pow(u - rows / 2, 2) + Math.Pow(v - cols / 2, 2));

                    // Задаємо параметри фільтру в залежності від типу фільтру
                    double filterValue = 0.0;

                    if (comboBox.SelectedIndex == 0) // Ідеальний фільтр
                    {
                        if (distance <= d0)
                            filterValue = 1.0;
                    }
                    else if (comboBox.SelectedIndex == 1) // Фільтр Баттерворта
                    {
                        double n;
                        if (!Double.TryParse(textBox_n.Text, out n)) return;
                        filterValue = 1.0 / (1 + Math.Pow(distance / d0, 2 * n));
                    }
                    else if (comboBox.SelectedIndex == 2) // Гаусівський фільтр
                    {
                        filterValue = Math.Exp(-(Math.Pow(distance, 2) / (2 * Math.Pow(d0, 2))));
                    }
                    else if (comboBox.SelectedIndex == 3) // Ідеальний фільтр
                    {
                        filterValue = 1.0;
                        if (distance <= d0)
                            filterValue = 0.0; 
                    }
                    else if (comboBox.SelectedIndex == 4) // Фільтр Баттерворта
                    {
                        double n;
                        if (!Double.TryParse(textBox_n.Text, out n)) return;
                        filterValue = 1.0 - 1.0 / (1 + Math.Pow(distance / d0, 2 * n)); 
                    }
                    else if (comboBox.SelectedIndex == 5) // Гаусівський фільтр
                    {
                        filterValue = 1.0 - Math.Exp(-(Math.Pow(distance, 2) / (2 * Math.Pow(d0, 2))));
                    }

                    // Застосовуємо фільтр до Фур'є-спектру
                    fftImage.At<Vec2f>(u, v)[0] *= (float)filterValue;
                    fftImage.At<Vec2f>(u, v)[1] *= (float)filterValue;
                    distances.Add(distance);
                    values.Add(filterValue);
                }
            }

            //Зворотнє перетворення фурє
            Mat inverseImage = new Mat();
            Cv2.Dft(fftImage, inverseImage, DftFlags.Inverse | DftFlags.RealOutput);

            floatImage = CenterImage(inverseImage);

            Cv2.Normalize(floatImage, floatImage, 0, 255, NormTypes.MinMax, MatType.CV_8U);

            selectedImage.Source = floatImage.ToBitmapSource();



            var plt = new ScottPlot.Plot(600, 400);
            plt.PlotScatter(distances.ToArray(), values.ToArray());
            plt.XLabel("D(u, v)");
            plt.YLabel("H(u, v)");

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

                image.Source = sourceImage;
            }
        }
        static public void setFourierTransform(BitmapImage originalImage, Image selectedImage, StackPanel toolsPanel)
        {

            CheckBox checkBox = GeneralTools.checkBoxProperties("На оригінальне зображення");
            TextBlock textBlock = GeneralTools.textBlockProperties("D₀ = ");
            TextBlock textBlock_n = GeneralTools.textBlockProperties("n = ", 1);
            TextBox textBoxD0 = GeneralTools.textBoxProperties(0,1);
            TextBox textBox_n = GeneralTools.textBoxProperties(1,1); ;
            Grid grid = GeneralTools.gridProperties(1, 3, 2, textBlock, textBlock_n, textBoxD0, textBox_n);

            string[] option = new string[6] { "Ідеальний фільтр НЧ", "Фільтр Баттерворта НЧ", "Гаусівський фільтр НЧ", "Ідеальний фільтр ВЧ", "Фільтр Баттерворта ВЧ", "Гаусівський фільтр ВЧ" };
            ComboBox comboBox = GeneralTools.comboBoxProperties(option, (sender, e) => comboBoxSelectionChanged(sender, grid));
            Image image = GeneralTools.imageProperties();


            Button button = GeneralTools.buttonProperties("Перетворити",
                (sender, e) => filterClick(originalImage, checkBox, selectedImage, textBoxD0, textBox_n, comboBox, image)
            );

            GeneralTools.AddElementsToStackPanel(toolsPanel, comboBox, grid, checkBox, button, image);
        }
    }
}
