using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace photoEditor.Tools
{
    static public class Recovery
    {
        static public void setRecovery(BitmapImage originalImage, Image selectedImage, StackPanel toolsPanel)
        {
            Noise.setNoise(originalImage, selectedImage, toolsPanel, false);
            string[] option = new string[5] { "Середньоарифметичний фільтр", "Середньогеометричний фільтр", "Середньогармонійний фільтр", "Середньоконтргармонійний фільтр", "Медіанний фільтр" };
            TextBox textBox_n = GeneralTools.textBoxProperties();
            TextBlock textBlock_n = GeneralTools.textBlockProperties("n = ");
            TextBox textBox = GeneralTools.textBoxProperties(1);
            TextBlock textBlock = GeneralTools.textBlockProperties("Q = ", 1);
            Grid grid = GeneralTools.gridProperties(1, 2, 2, textBlock_n, textBox_n, textBlock, textBox);
            ComboBox comboBox = GeneralTools.comboBoxProperties(option, (sender, e) => comboBoxSelectionChanged(sender, grid));
            Button button = GeneralTools.buttonProperties("Накласти",
                (sender, e) => filterClick(originalImage, selectedImage, comboBox, textBox, textBox_n)
            );
            GeneralTools.AddElementsToStackPanel(toolsPanel, comboBox, grid, button);
        }
        static void comboBoxSelectionChanged(object sender, Grid grid)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.SelectedIndex == 3)
                    grid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                else
                    grid.RowDefinitions[1].Height = new GridLength(0);
            }
        }
        static byte toByte(double a)
        {
            if (a > 255) a = 255;
            else if (a < 0) a = 0;
            return (byte)a;
        }
        static void filterClick(BitmapImage originalImage, Image selectedImage, ComboBox comboBox, TextBox textBox, TextBox textBox_n)
        {
            BitmapImage bitmapImage = GeneralTools.ImageToBitmapImage(selectedImage);
            int n;
            if (!Int32.TryParse(textBox_n.Text, out n) || (n!=3 && n!=5 && n!=7))
                return;
            if (comboBox.SelectedIndex == 0)
            {
                var kernel = new double[n,n];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        kernel[i, j] = 1.0 / (n * n);

                selectedImage.Source = Convolution.applyConvolution(bitmapImage, kernel);
            }
            else 
            {
                int width = (int)bitmapImage.PixelWidth;
                int height = (int)bitmapImage.PixelHeight;

                int stride = width * (bitmapImage.Format.BitsPerPixel / 8);
                byte[] pixelData = new byte[height * stride];
                bitmapImage.CopyPixels(pixelData, stride, 0);

                int halfn = n / 2;

                for (int y = halfn; y < height - halfn; y++)
                {
                    for (int x = halfn; x < width - halfn; x++)
                    {
                        int resultGrayValue = 0;
                        if(comboBox.SelectedIndex == 1)
                        {
                            double product = 1.0;

                            for (int dy = -halfn; dy <= halfn; dy++)
                            {
                                for (int dx = -halfn; dx <= halfn; dx++)
                                {
                                    int pixelX = x + dx;
                                    int pixelY = y + dy;
                                    int index = pixelY * stride + pixelX * 4;
                                    byte grayValue = (byte)((pixelData[index] + pixelData[index + 1] + pixelData[index + 2]) / 3);
                                    product *= grayValue;
                                }
                            }

                            double geometricMean = Math.Pow(product, 1.0 / (n * n));
                            resultGrayValue = (int)geometricMean;
                        }
                        else if(comboBox.SelectedIndex == 2)
                        {
                            double sum = 0;

                            for (int dy = -halfn; dy <= halfn; dy++)
                            {
                                for (int dx = -halfn; dx <= halfn; dx++)
                                {
                                    int pixelX = x + dx;
                                    int pixelY = y + dy;
                                    int index = pixelY * stride + pixelX * 4;
                                    byte grayValue = (byte)((pixelData[index] + pixelData[index + 1] + pixelData[index + 2]) / 3);
                                    if(grayValue!=0)
                                        sum += 1.0/grayValue;
                                }
                            }
                            if (sum == 0) sum = 1;
                            double garmonicMean = (n * n)/sum;
                            resultGrayValue = (int)garmonicMean;
                        }
                        else if (comboBox.SelectedIndex == 3)
                        {
                            double sum1 = 0;
                            double sum2 = 0;
                            double Q;
                            if (!Double.TryParse(textBox.Text, out Q))
                                return;
                            for (int dy = -halfn; dy <= halfn; dy++)
                            {
                                for (int dx = -halfn; dx <= halfn; dx++)
                                {
                                    int pixelX = x + dx;
                                    int pixelY = y + dy;
                                    int index = pixelY * stride + pixelX * 4;
                                    double grayValue = (pixelData[index] + pixelData[index + 1] + pixelData[index + 2]) / 3;
                                    if (grayValue == 0) continue;
                                    sum1 += Math.Pow(grayValue, Q + 1);
                                    sum2 += Math.Pow(grayValue, Q);
                                }
                            }
                            if (sum2 == 0) sum2 = 1;
                            double contrGarmonicMean = sum1/sum2;
                            resultGrayValue = (int)contrGarmonicMean;
                        }
                        else
                        {
                            List<byte> values = new List<byte>();
                            for (int dy = -halfn; dy <= halfn; dy++)
                            {
                                for (int dx = -halfn; dx <= halfn; dx++)
                                {
                                    int pixelX = x + dx;
                                    int pixelY = y + dy;
                                    int index = pixelY * stride + pixelX * 4;
                                    byte grayValue = (byte)((pixelData[index] + pixelData[index + 1] + pixelData[index + 2]) / 3);
                                    values.Add(grayValue);
                                }
                            }
                            
                            values.Sort();
                            resultGrayValue = (int)values[values.Count / 2];

                        }

                        for (int i = 0; i < 3; i++)
                        {
                            int index = y * stride + x * 4 + i;
                            pixelData[index] = toByte(resultGrayValue);
                        }
                    }
                }
                BitmapSource result = BitmapSource.Create(width, height, bitmapImage.DpiX, bitmapImage.DpiY, bitmapImage.Format, null, pixelData, stride);
                selectedImage.Source = result;
            }
        }
    }
}
