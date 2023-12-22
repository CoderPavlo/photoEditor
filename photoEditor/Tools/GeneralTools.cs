using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace photoEditor.Tools
{
    static public class GeneralTools
    {
        static public BitmapImage ImageToBitmapImage(Image selectedImage)
        {

            var sourceImage = new BitmapImage();
            if (selectedImage.Source is BitmapSource bitmapSource)
            {
                using (MemoryStream stream = new MemoryStream())
                {

                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);

                    sourceImage.BeginInit();
                    sourceImage.StreamSource = stream;
                    sourceImage.CacheOption = BitmapCacheOption.OnLoad;
                    sourceImage.EndInit();
                }
            }
            return sourceImage;
        }

        static public void AddElementsToStackPanel(StackPanel stackPanel, params UIElement[] elements)
        {
            foreach (var element in elements)
            {
                stackPanel.Children.Add(element);
            }
        }
        static public TextBlock textBlockProperties(string text, int row=0, int column=0, int span = 1)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.FontSize = 25;
            textBlock.Text = text;
            textBlock.Margin = new Thickness(5);
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(textBlock, column);
            Grid.SetRow(textBlock, row);
            Grid.SetColumnSpan(textBlock, span);
            return textBlock;
        }

        static public Button buttonProperties(string text, RoutedEventHandler eventHandler)
        {
            Button button = new Button();
            button.Content = text;
            button.FontSize = 18;
            button.Margin = new Thickness(5);
            button.Click += eventHandler;
            return button;
        }

        static public CheckBox checkBoxProperties(string text)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Content = text;
            checkBox.FontSize = 18;
            checkBox.Margin = new Thickness(5);
            checkBox.VerticalContentAlignment = VerticalAlignment.Center;
            checkBox.IsChecked = true;
            return checkBox;
        }

        static public Grid gridProperties(double colW1, double colW2, int rows, params UIElement[] elements)
        {
            Grid grid = new Grid();
            ColumnDefinition gridCol1 = new ColumnDefinition();
            gridCol1.Width = new GridLength(colW1, GridUnitType.Star);
            ColumnDefinition gridCol2 = new ColumnDefinition();
            gridCol2.Width = new GridLength(colW2, GridUnitType.Star);

            grid.ColumnDefinitions.Add(gridCol1);
            grid.ColumnDefinitions.Add(gridCol2);
            for (int _ = 0; _ < rows; _++)
            {
                RowDefinition gridRow1 = new RowDefinition();
                grid.RowDefinitions.Add(gridRow1);
            }
            foreach (var element in elements)
            {
                grid.Children.Add(element);
            }
            return grid;
        }

        static public TextBox textBoxProperties(int row =0, int column = 1)
        {
            TextBox textBox = new TextBox();
            textBox.FontSize = 25;
            textBox.Margin = new Thickness(5);
            textBox.PreviewTextInput += (sender, e) =>
            {
                Regex regex = new Regex("[^0-9]+,");
                e.Handled = regex.IsMatch(e.Text);
            };
            Grid.SetRow(textBox, row);
            Grid.SetColumn(textBox, column);
            return textBox;
        }

        static public ComboBox comboBoxProperties(string[] option, SelectionChangedEventHandler eventHandler)
        {
            ComboBox comboBox = new ComboBox();
            comboBox.FontSize = 25;
            comboBox.Margin = new Thickness(5);
            for (int i = 0; i < option.Length; i++)
                comboBox.Items.Add(option[i]);
            if(eventHandler!=null)
                comboBox.SelectionChanged += eventHandler;
            comboBox.SelectedIndex = 0;
            return comboBox;
        }

        static public Image imageProperties()
        {
            Image image= new Image();

            image.Margin = new Thickness(10);
            return image;
        }
    }
}
