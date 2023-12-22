using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace photoEditor.Tools
{
    static public class Convolution
    {
        class ConvolutionMatrix
        {
            public string Name;
            public int[,] Matrix;
            public ConvolutionMatrix(string Name, int[,] Matrix)
            {
                this.Name = Name;
                this.Matrix = Matrix;
            }
        }

        static public void setConvolution(BitmapImage originalIimage, Image selectedImage, StackPanel stackPanel)
        {

            List<int[,]> matrix = new List<int[,]>();
            List<string> option = new List<string>();
            setMatrices(matrix, option);

            DataGrid dataGrid = new DataGrid();
            dataGrid.FontSize = 25;
            dataGrid.Margin = new Thickness(5);
            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.CanUserResizeColumns = false;
            dataGrid.CanUserSortColumns = false;
            dataGrid.CanUserResizeRows = false;
            dataGrid.IsReadOnly = false;
            dataGrid.SelectionMode = DataGridSelectionMode.Single;
            dataGrid.SelectionUnit = DataGridSelectionUnit.Cell;
            dataGrid.HeadersVisibility = DataGridHeadersVisibility.None;
            dataGrid.ColumnWidth = DataGridLength.SizeToCells;

            CheckBox checkBox = GeneralTools.checkBoxProperties("На оригінальне зображення");
            ComboBox comboBox = GeneralTools.comboBoxProperties(option.ToArray(), (sender, e) => comboBoxSelectionChanged(sender, dataGrid, matrix));

            Button button = GeneralTools.buttonProperties("Фільтрувати",
                (sender, e) => filtrClick(dataGrid, checkBox, originalIimage, selectedImage)
            );
            GeneralTools.AddElementsToStackPanel(stackPanel, comboBox, dataGrid, checkBox, button);
        }

        //функція згортки зображення
        public static BitmapImage applyConvolution(BitmapImage inputImage, double[,] kernel)
        {
            int width = inputImage.PixelWidth;
            int height = inputImage.PixelHeight;

            int kernelSize = kernel.GetLength(0);
            int kernelRadius = kernelSize / 2;

            BitmapSource bitmapSource = new FormatConvertedBitmap(inputImage, PixelFormats.Gray32Float, null, 0);
            float[] pixels = new float[width * height];
            bitmapSource.CopyPixels(pixels, width * 4, 0);

            float[] resultPixels = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float convolution = 0;

                    float sumKernel = 0; 

                    for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                    {
                        for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                        {
                            int pixelX = x + kx;
                            int pixelY = y + ky;

                            if (pixelX >= 0 && pixelX < width && pixelY >= 0 && pixelY < height)
                            {
                                int pixelIndex = pixelY * width + pixelX;
                                convolution += pixels[pixelIndex] * (float)kernel[ky + kernelRadius, kx + kernelRadius];
                                sumKernel += (float)kernel[ky + kernelRadius, kx + kernelRadius];
                            }
                        }
                    }

                    if (sumKernel != 0)
                    {
                        convolution /= sumKernel; 
                    }

                    int resultIndex = y * width + x;
                    resultPixels[resultIndex] = Math.Min(Math.Max(convolution, 0), 1); 
                }
            }

            FormatConvertedBitmap resultBitmap = new FormatConvertedBitmap();
            resultBitmap.BeginInit();
            resultBitmap.Source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray32Float, null, resultPixels, width * 4);
            resultBitmap.EndInit();

            BitmapImage resultImage = new BitmapImage();
            resultImage.BeginInit();
            resultImage.StreamSource = new System.IO.MemoryStream();
            resultImage.StreamSource.Position = 0;
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(resultBitmap));
            encoder.Save(resultImage.StreamSource);
            resultImage.EndInit();
            resultImage.CacheOption = BitmapCacheOption.OnLoad;
            resultImage.StreamSource.Seek(0, System.IO.SeekOrigin.Begin);

            return resultImage;
        }

        //функція натискання на кнопку
        static void filtrClick(DataGrid dataGrid, CheckBox checkBox, BitmapImage originalIimage, Image selectedImage)
        {

            int rows = dataGrid.Items.Count;
            int columns = dataGrid.Columns.Count;

            double[,] data = new double[rows, columns];

            for (int i = 0; i < rows; i++)
            {

                var item = dataGrid.Items[i];

                for (int j = 0; j < columns; j++)
                {

                    var cell = dataGrid.Columns[j].GetCellContent(item);

                    if (cell is TextBlock)
                    {

                        string text = (cell as TextBlock).Text;

                        if (Double.TryParse(text, out double value))
                        {
                            data[i, j] = value;
                        }
                        else data[i, j] = 0;
                    }
                }
            }


            if (checkBox.IsChecked ?? false)
            {

                BitmapImage outputImage = applyConvolution(originalIimage, data);
                selectedImage.Source = outputImage;
            }
            else if (selectedImage.Source is BitmapSource bitmapSource)
            {
                BitmapImage bitmapImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {

                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
                BitmapImage outputImage = applyConvolution(bitmapImage, data);
                selectedImage.Source = outputImage;
            }
        }

        //функція зміна вибраного фільтру
        static void comboBoxSelectionChanged(object sender, DataGrid dataGrid, List<int[,]> matrices)
        {
            if (sender is ComboBox comboBox)
            {
                int selectedItem = comboBox.SelectedIndex;
                if (selectedItem == 0)
                {
                    int[,] data = new int[3, 3]
                    {
                        {0, 0, 0},
                        {0, 0, 0},
                        {0, 0, 0}
                    };
                    setDataToDataGrid(dataGrid, data);
                }
                else if (selectedItem == 1)
                {
                    int[,] data = new int[5, 5]
                    {
                        {0, 0, 0, 0, 0},
                        {0, 0, 0, 0, 0},
                        {0, 0, 0, 0, 0},
                        {0, 0, 0, 0, 0},
                        {0, 0, 0, 0, 0}
                    };
                    setDataToDataGrid(dataGrid, data);
                }
                else setDataToDataGrid(dataGrid, matrices[selectedItem - 2]);
            }
        }
        // Виведення матриці в DataGrid
        static void setDataToDataGrid(DataGrid dataGrid, int[,] data)
        {
            DataTable dataTable = new DataTable();

            for (int i = 0; i < data.GetLength(1); i++)
            {
                dataTable.Columns.Add("Column" + i.ToString(), typeof(int));
            }

            for (int i = 0; i < data.GetLength(0); i++)
            {
                DataRow row = dataTable.NewRow();

                for (int j = 0; j < data.GetLength(1); j++)
                {
                    row[j] = data[i, j];
                }

                dataTable.Rows.Add(row);
            }

            // Встановлення джерела даних для DataGrid
            dataGrid.ItemsSource = dataTable.DefaultView;

        }
        //Функція для додавання фільтрів
        static void setMatrices(List<int[,]> matrix, List<string> option)
        {

            option.Add("3*3");
            option.Add("5*5");
            // Гауса (НЧ)
            int[,] gaussianLowPass = new int[3, 3]
            {
                {1, 2, 1},
                {2, 4, 2},
                {1, 2, 1}
            };
            option.Add("Гауса (НЧ)");
            matrix.Add(gaussianLowPass);
            // Гауса (ВЧ)
            int[,] gaussianHighPass = new int[3, 3]
            {
                {0, -1, 0},
                {-1, 4, -1},
                {0, -1, 0}
            };
            option.Add("Гауса (ВЧ)");
            matrix.Add(gaussianHighPass);
            // Лапласа (НЧ)
            int[,] laplaceLowPass = new int[3, 3]
            {
                {0, 1, 0},
                {1, 4, 1},
                {0, 1, 0}
            };
            option.Add("Лапласа (НЧ)");
            matrix.Add(laplaceLowPass);
            // Лапласа (ВЧ)
            int[,] laplaceHighPass = new int[3, 3]
            {
                {-1, -1, -1},
                {-1,  8, -1},
                {-1, -1, -1}
            };
            option.Add("Лапласа (ВЧ)");
            matrix.Add(laplaceHighPass);
            // Прюіта (Prewitt)
            int[,] prewitt = new int[3, 3]
            {
                {-1, 0, 1},
                {-1, 0, 1},
                {-1, 0, 1}
            };
            option.Add("Прюіта (Prewitt)");
            matrix.Add(prewitt);
            // Собеля (Sobel)
            int[,] sobel = new int[3, 3]
            {
                {-1, 0, 1},
                {-2, 0, 2},
                {-1, 0, 1}
            };
            option.Add("Собеля (Sobel)");
            matrix.Add(sobel);
            // Підвищення чіткості (Hipass)
            int[,] hipass = new int[3, 3]
            {
                {-1, -1, -1},
                {-1,  9, -1},
                {-1, -1, -1}
            };
            option.Add("Підвищення чіткості (Hipass)");
            matrix.Add(hipass);
            // Підвищення чіткості (Sharpen)
            int[,] sharpen = new int[3, 3]
            {
                {-1, -1, -1},
                {-1,  16, -1},
                {-1, -1, -1}
            };
            option.Add("Підвищення чіткості (Sharpen)");
            matrix.Add(sharpen);
            // Пом'якшення зображення
            int[,] blur = new int[3, 3]
            {
                {2, 2, 2},
                {2, 0, 2},
                {2, 2, 2}
            };
            option.Add("Пом'якшення зображення");
            matrix.Add(blur);

            // Фільтр для виявлення границь (Edge detection)
            int[,] edgeDetection = new int[3, 3]
            {
                {1, 1, 1},
                {1,  -2, 1},
                {-1, -1, -1}
            };
            option.Add("Edge detection");
            matrix.Add(edgeDetection);
        }

    }
}
