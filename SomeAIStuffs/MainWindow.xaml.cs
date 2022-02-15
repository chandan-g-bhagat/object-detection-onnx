using System;
using io = System.IO;
using System.Collections.Generic;
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
using draw = System.Drawing;
using System.Drawing.Drawing2D;
using SomeAIStuffs.YoloParser;
using SomeAIStuffs.DataStructures;
using Microsoft.ML;

using System.IO;

namespace SomeAIStuffs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string assetsPath;
        private string modelFilePath;
        private string imagesFolder;

        public MainWindow()
        {
            InitializeComponent();

            assetsPath = GetAbsolutePath();
            modelFilePath = io.Path.Combine(assetsPath, "Model", "TinyYolo2_model.onnx");
            imagesFolder = io.Path.Combine(assetsPath, "images");
        }

        private void process()
        {
            MLContext mlContext = new MLContext();
            try
            {
                IDataView imageDataView = mlContext.Data.LoadFromEnumerable(imageNetDatas);
                // Create instance of model scorer
                var modelScorer = new OnnxModelScorer(imagesFolder, modelFilePath, mlContext);

                // Use model to score data
                IEnumerable<float[]> probabilities = modelScorer.Score(imageDataView);

                // Post-process model output
                YoloOutputParser parser = new YoloOutputParser();

                var boundingBoxes =
                    probabilities
                    .Select(probability => parser.ParseOutputs(probability))
                    .Select(boxes => parser.FilterBoundingBoxes(boxes, 5, .5F));

                // Draw bounding boxes for detected objects in each of the images
                string imageFileName = imageNetDatas.FirstOrDefault().ImagePath;
                IList<YoloBoundingBox> detectedObjects = boundingBoxes.ElementAt(0);

                DrawBoundingBox(imageFileName, detectedObjects);

                LogDetectedObjects(imageFileName, detectedObjects);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private string GetAbsolutePath()
        {
            var _dataRoot = AppDomain.CurrentDomain.BaseDirectory;//new FileInfo(typeof(OnnxModelScorer).Assembly.Location);
            //string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = io.Path.GetFullPath(io.Path.Combine(_dataRoot, @"..\..\..\"));
            fullPath = io.Path.Combine(fullPath, "assets");

            return fullPath;
        }

        private void DrawBoundingBox(string imageName, IList<YoloBoundingBox> filteredBoundingBoxes)
        {
            draw.Image image = draw.Image.FromFile(imageName);

            var originalImageHeight = image.Height;
            var originalImageWidth = image.Width;

            foreach (var box in filteredBoundingBoxes)
            {
                // Get Bounding Box Dimensions
                var x = (uint)Math.Max(box.Dimensions.X, 0);
                var y = (uint)Math.Max(box.Dimensions.Y, 0);
                var width = (uint)Math.Min(originalImageWidth - x, box.Dimensions.Width);
                var height = (uint)Math.Min(originalImageHeight - y, box.Dimensions.Height);

                // Resize To Image
                x = (uint)originalImageWidth * x / OnnxModelScorer.ImageNetSettings.imageWidth;
                y = (uint)originalImageHeight * y / OnnxModelScorer.ImageNetSettings.imageHeight;
                width = (uint)originalImageWidth * width / OnnxModelScorer.ImageNetSettings.imageWidth;
                height = (uint)originalImageHeight * height / OnnxModelScorer.ImageNetSettings.imageHeight;

                // Bounding Box Text
                string text = $"{box.Label} ({(box.Confidence * 100).ToString("0")}%)";

                using (draw.Graphics thumbnailGraphic = draw.Graphics.FromImage(image))
                {
                    thumbnailGraphic.CompositingQuality = CompositingQuality.HighQuality;
                    thumbnailGraphic.SmoothingMode = SmoothingMode.HighQuality;
                    thumbnailGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    // Define Text Options
                    draw.Font drawFont = new draw.Font("Arial", 40, draw.FontStyle.Bold);
                    draw.SizeF size = thumbnailGraphic.MeasureString(text, drawFont);
                    draw.SolidBrush fontBrush = new draw.SolidBrush(draw.Color.Black);
                    draw.Point atPoint = new draw.Point((int)x, (int)y - (int)size.Height - 1);

                    // Define BoundingBox options
                    draw.Pen pen = new draw.Pen(box.BoxColor, 3.2f);
                    draw.SolidBrush colorBrush = new draw.SolidBrush(box.BoxColor);

                    // Draw text on image
                    thumbnailGraphic.FillRectangle(colorBrush, (int)x, (int)(y - size.Height - 1), (int)size.Width, (int)size.Height);
                    thumbnailGraphic.DrawString(text, drawFont, fontBrush, atPoint);

                    // Draw bounding box on image
                    thumbnailGraphic.DrawRectangle(pen, x, y, width, height);
                }
            }

            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
                FinalImg.Source = bi;
            }

            // image.Save(io.Path.Combine(outputImageLocation, imageName));
        }

        private void LogDetectedObjects(string imageName, IList<YoloBoundingBox> boundingBoxes)
        {
            Console.WriteLine($".....The objects in the image {imageName} are detected as below....");

            foreach (var box in boundingBoxes)
            {
                Console.WriteLine($"{box.Label} and its Confidence score: {box.Confidence}");
            }

            Console.WriteLine("");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Filter = "Image files (*.jpg) | *.jpg";
                dialog.FilterIndex = 1;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var url = dialog.FileName;
                    var image = new ImageNetData() { ImagePath = dialog.FileName, Label = dialog.SafeFileName };
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(url, UriKind.Absolute);
                    bmp.EndInit();
                    Img.Source = bmp;

                    imageNetDatas = new List<ImageNetData>() { image };
                }
            }
        }

        private IEnumerable<ImageNetData> imageNetDatas;

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (imageNetDatas != null)
            {
                process();
            }
        }
    }
}