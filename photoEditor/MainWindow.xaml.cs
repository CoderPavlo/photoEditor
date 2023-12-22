using Microsoft.Win32;
using photoEditor.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace photoEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage bitmapImage;
        int USED_TOOLS = -1;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Зображення|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|Всі файли|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    selectedImage.Source = bitmapImage;
                    menuTools.IsEnabled = true;
                    menuSave.IsEnabled = true;
                    closeStackPanel();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка: " + ex.Message);
                }
            }
        }
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (selectedImage.Source is BitmapSource bitmapSource)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Image Files (*.png;*.jpeg;*.jpg;*.bmp;*.tif;*.tiff)|*.png;*.jpeg;*.jpg;*.bmp;*.tif;*.tiff|All Files (*.*)|*.*";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    string fileExtension = System.IO.Path.GetExtension(filePath);
                    BitmapEncoder encoder = null;
                    if (string.Equals(fileExtension, ".png", StringComparison.OrdinalIgnoreCase))
                        encoder = new PngBitmapEncoder();
                    else if (string.Equals(fileExtension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(fileExtension, ".jpg", StringComparison.OrdinalIgnoreCase))
                        encoder = new JpegBitmapEncoder();
                    else if (string.Equals(fileExtension, ".bmp", StringComparison.OrdinalIgnoreCase))
                        encoder = new BmpBitmapEncoder();
                    else if (string.Equals(fileExtension, ".tif", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(fileExtension, ".tiff", StringComparison.OrdinalIgnoreCase))
                        encoder = new TiffBitmapEncoder();

                    if (encoder != null)
                    {
                        BitmapFrame frame = BitmapFrame.Create(bitmapSource);

                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            encoder.Frames.Add(frame);
                            encoder.Save(fs);
                        }

                        MessageBox.Show("Зображення було успішно збережено.");
                    }
                    else
                    {
                        MessageBox.Show("Непідтримуваний формат файлу.");
                    }
                }
            }
            else
            {
                MessageBox.Show("Не відкрито жодного зображення");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void toolClick(int usedTool, double width, string title, Action action)
        {
            if (USED_TOOLS == usedTool)
                closeStackPanel();
            else
            {
                toolsColumn.Width = new GridLength(width, GridUnitType.Star);
                stackPanelDesc.Text = title;
                toolsStackPanel.Children.Clear();
                action();
                USED_TOOLS = usedTool;
            }
        }
        private void Convolution_Click(object sender, RoutedEventArgs e)
        {
            toolClick(0, 2, "Фільтр", () => Convolution.setConvolution(bitmapImage, selectedImage, toolsStackPanel));
        }

        private void StackPanelClose_Click(object sender, RoutedEventArgs e)
        {
            closeStackPanel();
        }

        void closeStackPanel(){
            toolsColumn.Width = new GridLength(0);
            USED_TOOLS = -1;
        }

        //функція перетворення зображення в сіре
        private void ConvertToGray(object sender, RoutedEventArgs e)
        {
            WriteableBitmap writableBitmap = new WriteableBitmap(bitmapImage);

            int width = writableBitmap.PixelWidth;
            int height = writableBitmap.PixelHeight;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    System.Windows.Media.Color pixelColor = writableBitmap.GetPixel(x, y);
                    byte Y = (byte)(0.3 * pixelColor.R + 0.6 * pixelColor.G + 0.1 * pixelColor.B);
                    
                    pixelColor = System.Windows.Media.Color.FromRgb(Y,Y,Y);

                    writableBitmap.SetPixel(x, y, pixelColor);
                }
            }
            selectedImage.Source = writableBitmap;
        }

        private void GradTr_Click(object sender, RoutedEventArgs e)
        {
            toolClick(1, 2, "Перетворення", () => GradTr.setGradTr(bitmapImage, selectedImage, toolsStackPanel));
        }

        private void Histogram_Click(object sender, RoutedEventArgs e)
        {
            toolClick(2, 4, "Гістограма", () => Hist.setHistogram(selectedImage, toolsStackPanel));
        }

        private void EqHistogram_Click(object sender, RoutedEventArgs e)
        {
            toolClick(3, 4, "Еквалізована гістограма", () => Hist.setEqualizeHistogram(selectedImage, toolsStackPanel));
        }

        private void FourierSpectrum_Click(object sender, RoutedEventArgs e)
        {
            toolClick(4, 4, "Фур'є спектр", () => FourierTransform.setFourierSpectrum(selectedImage, toolsStackPanel));
        }
        private void FiltrFourierSpectrum_Click(object sender, RoutedEventArgs e)
        {
            toolClick(5, 4, "Фільтр фур'є спектру", () => FourierTransform.setFourierTransform(bitmapImage, selectedImage, toolsStackPanel));
        }

        private void Noise_Click(object sender, RoutedEventArgs e)
        {
            toolClick(6, 3, "Накладання шумів", () => Noise.setNoise(bitmapImage, selectedImage, toolsStackPanel));
        }
        private void Recovery_Click(object sender, RoutedEventArgs e)
        {
            toolClick(7, 4, "Відновлення зображення", () => Recovery.setRecovery(bitmapImage, selectedImage, toolsStackPanel));
        }
        
        private void ColorProcessing_Click(object sender, RoutedEventArgs e)
        {
            toolClick(8, 3, "Обробка кольорових зображень", () => ColorProcessing.setColorProcessing(bitmapImage, selectedImage, toolsStackPanel));
        }
        
        private void Watermark_Click(object sender, RoutedEventArgs e)
        {
            toolClick(9, 4, "Водяний знак", () => Watermark.setWatermark(bitmapImage, selectedImage, toolsStackPanel));
        }

    }
}
