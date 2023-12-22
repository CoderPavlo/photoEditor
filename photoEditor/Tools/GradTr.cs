using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace photoEditor.Tools
{
    static public class GradTr
    {
        static void comboBoxSelectionChanged(object sender, Grid grid)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.SelectedIndex == 2)
                    grid.Visibility = Visibility.Visible;
                else
                    grid.Visibility = Visibility.Collapsed;
            }
        }
        static public void setGradTr(BitmapImage originalIimage, Image selectedImage, StackPanel stackPanel)
        {

            CheckBox checkBox = GeneralTools.checkBoxProperties("На оригінальне зображення");
            TextBox textBox = GeneralTools.textBoxProperties();
            TextBlock textBlock = GeneralTools.textBlockProperties("γ = ");
            Grid grid = GeneralTools.gridProperties(1, 3, 1, textBlock, textBox);

            string[] option = new string[3] { "Негатив", "Логарифмічне", "Степеневе" };
            ComboBox comboBox = GeneralTools.comboBoxProperties(option, (sender, e) => comboBoxSelectionChanged(sender, grid));
            Button button = GeneralTools.buttonProperties("Перетворити",
               (sender, e) => transformClick(checkBox, originalIimage, selectedImage, comboBox, textBox)
           );
            GeneralTools.AddElementsToStackPanel(stackPanel, comboBox, grid, checkBox, button);
        }

        static void transformClick(CheckBox checkBox, BitmapImage originalImage, Image selectedImage, ComboBox comboBox, TextBox textBox)
        {
            BitmapImage sourceImage = null;
            if (checkBox.IsChecked ?? false)
                sourceImage = originalImage;
            else
                sourceImage = GeneralTools.ImageToBitmapImage(selectedImage);

            BitmapSource processedImage;
            switch (comboBox.SelectedIndex)
            {
                case 0:
                    processedImage = ApplyNegativeEffect(sourceImage);
                    break;
                case 1:
                    processedImage = ApplyLogarithmicEffect(sourceImage);
                    break;
                default:
                    processedImage = ApplyPowerEffect(sourceImage, textBox);
                    break;
            }
            if (processedImage != null)
                selectedImage.Source = processedImage;
        }
        //Функція перетворення зображення в негатив
        static BitmapSource ApplyNegativeEffect(BitmapImage sourceImage)
        {

            WriteableBitmap writableBitmap = new WriteableBitmap(sourceImage);

            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    System.Windows.Media.Color pixelColor = writableBitmap.GetPixel(x, y);


                    pixelColor = System.Windows.Media.Color.FromRgb((byte)(255 - pixelColor.R), (byte)(255 - pixelColor.G), (byte)(255 - pixelColor.B));


                    writableBitmap.SetPixel(x, y, pixelColor);
                }
            }

            return writableBitmap;
        }

        //Функція логарифмічного перетворення
        static BitmapSource ApplyLogarithmicEffect(BitmapImage sourceImage)
        {

            WriteableBitmap writableBitmap = new WriteableBitmap(sourceImage);

            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            double c = 255.0 / Math.Log(256);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    System.Windows.Media.Color pixelColor = writableBitmap.GetPixel(x, y);
                    pixelColor = System.Windows.Media.Color.FromRgb((byte)(c*Math.Log(pixelColor.R+1)), (byte)(c * Math.Log(pixelColor.G + 1)), (byte)(c * Math.Log(pixelColor.B + 1)));

                    writableBitmap.SetPixel(x, y, pixelColor);
                }
            }

            return writableBitmap;
        }

        //Функція степеневого перетворення
        static BitmapSource ApplyPowerEffect(BitmapImage sourceImage, TextBox textBox)
        {
            double lambda;
            if (double.TryParse(textBox.Text, out lambda))
            {
                WriteableBitmap writableBitmap = new WriteableBitmap(sourceImage);


                int width = writableBitmap.PixelWidth;
                int height = writableBitmap.PixelHeight;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {

                        System.Windows.Media.Color pixelColor = writableBitmap.GetPixel(x, y);

                        pixelColor = System.Windows.Media.Color.FromRgb((byte)(255.0*Math.Pow(pixelColor.R/255.0, lambda)), (byte)(255.0 * Math.Pow(pixelColor.G / 255.0, lambda)), (byte)(255.0 * Math.Pow(pixelColor.B / 255.0, lambda)));


                        writableBitmap.SetPixel(x, y, pixelColor);
                    }
                }

                return writableBitmap;
            }
            textBox.Focus();
            return null;
        }

    }
}
