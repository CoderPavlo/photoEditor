using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using OpenCvSharp;
using OpenCvSharp.Flann;

namespace photoEditor.Tools
{
    static public class Noise
    {
        static void comboBoxSelectionChanged(object sender, TextBlock textBlock1, TextBlock textBlock2)
        {
            if (sender is ComboBox comboBox)
            {
                int selectedIndex = comboBox.SelectedIndex;

                if (selectedIndex == 2)
                {
                    textBlock1.Text = "Ps =";
                    textBlock2.Text = "Pp =";
                }
                else
                {
                    textBlock1.Text = "mean =";
                    textBlock2.Text = "std =";
                }
            }
        }
        static byte toByte(double a)
        {
            if (a > 255) a = 255;
            else if (a < 0) a = 0;
            return (byte)a;
        }
        static void applyNoise_Click(CheckBox checkBox, Image selectedImage, BitmapImage originalImage, TextBox textBox1, TextBox textBox2, ComboBox comboBox, Image image)
        {
            WriteableBitmap writableBitmap;
            if (!checkBox.IsChecked.Value)
                writableBitmap = new WriteableBitmap(GeneralTools.ImageToBitmapImage(selectedImage));
            else
                writableBitmap = new WriteableBitmap(originalImage);

            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            double mean, std;
            if (!Double.TryParse(textBox1.Text, out mean) || !Double.TryParse(textBox2.Text, out std))
                return;
            Random random = new Random();

            double b=0, a=0;
            if (comboBox.SelectedIndex == 1)
            {
                mean /= 255;
                std /= 255;
                b = 4 * std * std / (4 - Math.PI);
                a = mean - Math.Sqrt(Math.PI * b) / 2;
            }
          
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    Color originalColor = writableBitmap.GetPixel(x, y);
                    Color newColor;
                    double P = 0;
                    if (comboBox.SelectedIndex == 0)
                    {
                        P = random.NextDouble() * 2 - 1 + random.NextDouble() * 2 - 1 + random.NextDouble() * 2 - 1;
                        P /= 3; 
                        P = P * mean + std; 
                        newColor = Color.FromRgb(toByte(originalColor.R + P), toByte(originalColor.G + P), toByte(originalColor.B + P));

                    }
                    else if (comboBox.SelectedIndex == 1)
                    {
                        P= a + Math.Sqrt(-b * Math.Log(1 - random.NextDouble()));
                        P *= 255;
                        newColor = Color.FromRgb(toByte(originalColor.R + P), toByte(originalColor.G + P), toByte(originalColor.B + P));
                    }
                    else
                    {
                        double rand = random.NextDouble();
                        if (rand < mean)
                            newColor = Colors.White;
                        else if (rand > 1 - std)
                            newColor = Colors.Black;
                        else
                            newColor = originalColor;
                    }


                    writableBitmap.SetPixel(x, y, newColor);
                }
            }

            selectedImage.Source = writableBitmap;
            image.Source = Hist.histogram(GeneralTools.ImageToBitmapImage(selectedImage));

        }

        public static void setNoise(BitmapImage originalImage, Image selectedImage, StackPanel toolsPanel, bool hist = true)
        {
            
            TextBox textBox1 = GeneralTools.textBoxProperties();
            TextBox textBox2 = GeneralTools.textBoxProperties(1);
            TextBlock textBlock1 = GeneralTools.textBlockProperties("a = ");
            TextBlock textBlock2 = GeneralTools.textBlockProperties("b = ", 1);
            Grid grid = GeneralTools.gridProperties(1, 2, 2, textBlock1, textBox1, textBlock2, textBox2);
            Image image = GeneralTools.imageProperties();
            CheckBox checkBox = GeneralTools.checkBoxProperties("На оригінальне зображення");

            string[] option = new string[3] { "Гаусівський шум", "Шум Релея", "Імпульсний шум" };
            ComboBox comboBox = GeneralTools.comboBoxProperties(option, (sender, e) => comboBoxSelectionChanged(sender, textBlock1, textBlock2));

            Button button = GeneralTools.buttonProperties("Накласти шум", 
                (sender, e)=> applyNoise_Click(checkBox, selectedImage, originalImage, textBox1, textBox2, comboBox, image)
            );
            GeneralTools.AddElementsToStackPanel(toolsPanel, comboBox, grid, checkBox, button);
            if (hist)
                toolsPanel.Children.Add(image);
        }

    }
}
