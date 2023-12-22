using Microsoft.Win32;
using OpenCvSharp.XPhoto;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace photoEditor.Tools
{
    static class Watermark
    {
        public static void setWatermark(BitmapImage originalImage, System.Windows.Controls.Image selectedImage, StackPanel toolsPanel)
        {

            CheckBox checkBox = GeneralTools.checkBoxProperties("Оригінальне зображення");
            Image image = GeneralTools.imageProperties();

            string[] option = new string[] { "Накладання", "Зчитування" };
            ComboBox comboBox = GeneralTools.comboBoxProperties(option, (sender, e) => image.Source=null);

            Button button = GeneralTools.buttonProperties("Виконати",
                (sender, e) =>
                {
                    BitmapImage bitmapImage;
                    if (!checkBox.IsChecked.Value)
                        bitmapImage = GeneralTools.ImageToBitmapImage(selectedImage);
                    else
                        bitmapImage = originalImage;

                    if (comboBox.SelectedIndex == 0)
                    {
                        BitmapImage watermark = null;
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = "Зображення|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|Всі файли|*.*";

                        if (openFileDialog.ShowDialog() == true)
                        {
                            try
                            {
                                watermark = new BitmapImage(new Uri(openFileDialog.FileName));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Помилка: " + ex.Message);
                                return;
                            }
                        }
                        if (watermark == null) return;
                        image.Source = watermark;
                        WriteableBitmap writableSecret = ResizeBitmapSource(ConvertToGrayScale(watermark), bitmapImage.PixelWidth, bitmapImage.PixelHeight);
                        WriteableBitmap writableSimple = new WriteableBitmap(bitmapImage);

                        int width = writableSimple.PixelWidth;
                        int height = writableSimple.PixelHeight;

                        int stride = width * ((writableSimple.Format.BitsPerPixel + 7) / 8);
                        int arraySize = height * stride;

                        byte[] pixelsSimple = new byte[arraySize];
                        byte[] pixelsSecret = new byte[arraySize];

                        writableSimple.CopyPixels(pixelsSimple, stride, 0);
                        writableSecret.CopyPixels(pixelsSecret, stride, 0);

                        int offset = 0;

                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                byte[] MsgBits = GetBits(pixelsSecret[offset + 2]); // Assuming R channel for simplicity
                                byte[] AlphaBits = GetBits(pixelsSimple[offset + 3]); // Alpha channel
                                byte[] RedBits = GetBits(pixelsSimple[offset + 2]);
                                byte[] GreenBits = GetBits(pixelsSimple[offset + 1]);
                                byte[] BlueBits = GetBits(pixelsSimple[offset]);

                                AlphaBits[6] = MsgBits[0];
                                AlphaBits[7] = MsgBits[1];
                                RedBits[6] = MsgBits[2];
                                RedBits[7] = MsgBits[3];
                                GreenBits[6] = MsgBits[4];
                                GreenBits[7] = MsgBits[5];
                                BlueBits[6] = MsgBits[6];
                                BlueBits[7] = MsgBits[7];

                                pixelsSimple[offset + 3] = GetByte(AlphaBits); // Update Alpha channel
                                pixelsSimple[offset + 2] = GetByte(RedBits);
                                pixelsSimple[offset + 1] = GetByte(GreenBits);
                                pixelsSimple[offset] = GetByte(BlueBits);

                                offset += 4;
                            }
                        }

                        WriteableBitmap resultBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                        resultBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelsSimple, stride, 0);

                        selectedImage.Source = resultBitmap;
                    }
                    else
                    {

                        int width = bitmapImage.PixelWidth;
                        int height = bitmapImage.PixelHeight;

                        WriteableBitmap writableEncrypted = new WriteableBitmap(bitmapImage);

                        int stride = width * ((writableEncrypted.Format.BitsPerPixel + 7) / 8);
                        int arraySize = height * stride;

                        byte[] pixelsEncrypted = new byte[arraySize];

                        writableEncrypted.CopyPixels(pixelsEncrypted, stride, 0);

                        int offset = 0;

                        for (int i = 0; i < height; i++)
                        {
                            for (int j = 0; j < width; j++)
                            {
                                byte[] AlphaBits = GetBits(pixelsEncrypted[offset + 3]);
                                byte[] RedBits = GetBits(pixelsEncrypted[offset + 2]);
                                byte[] GreenBits = GetBits(pixelsEncrypted[offset + 1]);
                                byte[] BlueBits = GetBits(pixelsEncrypted[offset]);

                                byte[] BitsToDecrypt = new byte[8];
                                BitsToDecrypt[0] = AlphaBits[6];
                                BitsToDecrypt[1] = AlphaBits[7];
                                BitsToDecrypt[2] = RedBits[6];
                                BitsToDecrypt[3] = RedBits[7];
                                BitsToDecrypt[4] = GreenBits[6];
                                BitsToDecrypt[5] = GreenBits[7];
                                BitsToDecrypt[6] = BlueBits[6];
                                BitsToDecrypt[7] = BlueBits[7];

                                byte newGrey = GetByte(BitsToDecrypt);
                                pixelsEncrypted[offset + 3] = newGrey;
                                pixelsEncrypted[offset + 2] = newGrey;
                                pixelsEncrypted[offset + 1] = newGrey;
                                pixelsEncrypted[offset] = newGrey;

                                offset += 4;
                            }
                        }

                        WriteableBitmap hiddenImage = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                        hiddenImage.WritePixels(new Int32Rect(0, 0, width, height), pixelsEncrypted, stride, 0);

                        image.Source = hiddenImage;
                    }
                }
            );

            GeneralTools.AddElementsToStackPanel(toolsPanel, comboBox, checkBox, button, image);
        }
        static WriteableBitmap ConvertToGrayScale(BitmapImage originalBitmap)
        {
            WriteableBitmap grayBitmap = new WriteableBitmap(originalBitmap);

            int width = grayBitmap.PixelWidth;
            int height = grayBitmap.PixelHeight;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    System.Windows.Media.Color pixelColor = grayBitmap.GetPixel(x, y);
                    byte Y = (byte)(0.3 * pixelColor.R + 0.6 * pixelColor.G + 0.1 * pixelColor.B);

                    pixelColor = System.Windows.Media.Color.FromRgb(Y, Y, Y);

                    grayBitmap.SetPixel(x, y, pixelColor);
                }
            }
            return grayBitmap;
        }

        static WriteableBitmap ResizeBitmapSource(WriteableBitmap source, int newWidth, int newHeight)
        {
            return source.Resize(newWidth, newHeight, WriteableBitmapExtensions.Interpolation.Bilinear);
        }

        static byte GetByte(byte[] bits)
        {
            string bitString = "";
            for (int i = 0; i < 8; i++)
                bitString += bits[i];

            byte newpix = Convert.ToByte(bitString, 2);
            int dePix = (int)newpix ^ 2;
            return (byte)dePix;
        }

        static byte[] GetBits(byte simplepixel)
        {
            int pixel = 0;
            pixel = (int)simplepixel ^ 2;

            BitArray bits = new BitArray(new byte[] { (byte)pixel });
            bool[] boolarray = new bool[bits.Count];
            bits.CopyTo(boolarray, 0);
            byte[] bitsArray = boolarray.Select(bit => (byte)(bit ? 1 : 0)).ToArray();
            Array.Reverse(bitsArray);
            return bitsArray;
        }


    }
}
