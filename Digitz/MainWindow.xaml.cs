﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TensorFlow;

namespace Digitz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();
            numberLabel.Text = "";
        }

        private string Stringify(float[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                if (i == 0) sb.Append("{\r\n\t");
                else if (i % 28 == 0)
                    sb.Append("\r\n\t");
                sb.Append($"{data[i],3:##0}, ");

            }
            sb.Append("\r\n}\r\n");
            return sb.ToString();
        }

        private TFTensor GetWrittenDigit(int size)
        {
            RenderTargetBitmap b = new RenderTargetBitmap(
                (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight,
                96d, 96d, PixelFormats.Default
            );

            b.Render(inkCanvas);
            var bitmap = new WriteableBitmap(b)
                            .Resize(size, size, WriteableBitmapExtensions.Interpolation.Bilinear);

            float[] data = new float[size * size];
            for (int x = 0; x < bitmap.PixelWidth; x++)
            {
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    var color = bitmap.GetPixel(x, y);
                    data[y * bitmap.PixelWidth + x] = 255 - ((color.R + color.G + color.B) / 3);
                }
            }
            
            // sanity check
            Console.Write(Stringify(data));

            // normalize
            for(int i = 0; i < data.Length; i++) data[i] /= 255;

            return TFTensor.FromBuffer(new TFShape(1, data.Length), data, 0, data.Length);
        }

        private void recognizeButton_Click(object sender, RoutedEventArgs e)
        {
            var tensor = GetWrittenDigit(28);
            const string input_node = "x";
            const string output_node = "Model/prediction";
            using (var graph = new TFGraph())
            {
                graph.Import(File.ReadAllBytes("digits.pb"));
                var session = new TFSession(graph);
                var runner = session.GetRunner();
                runner.AddInput(graph[input_node][0], tensor);
                runner.Fetch(graph[output_node][0]);
                var output = runner.Run();
                TFTensor result = output[0];
                float[] p = ((float[][])result.GetValue(true))[0];
                int guess = Array.IndexOf(p, p.Max());
                numberLabel.Text = guess.ToString();
            }
        }
    }
}
