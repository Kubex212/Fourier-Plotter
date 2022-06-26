using System;
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
using System.Threading;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;

namespace WpfLab2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool progress = false;
        bool refresh = false;
        Point prevMarker = new Point();
        List<Ellipse> circles = new List<Ellipse>();
        List<Line> lines = new List<Line>();
        Ellipse theDot = new Ellipse();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new List<Info>
            {
                new Info(){Frequency=1, Radius=150.0},
                new Info(){Frequency=-2, Radius=150.0},
                new Info(){Frequency=7, Radius=10.0},
            };
        }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes) this.Close();
            else return;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            progress = true;
        }

        //https://www.wpf-tutorial.com/misc-controls/the-progressbar-control/
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();
            refresh = true;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; ; i++)
            {
                Thread.Sleep(10);
                if (!progress && !refresh) continue;
                (sender as BackgroundWorker).ReportProgress(i);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if(refresh && !progress)
            {
                refresh = false;
                prevMarker = new Point();
                Draw(0);
                return;
            }
            if(progress) pBar.Value += 0.1;
            canv.UpdateLayout();
            if(progress) Draw(pBar.Value);
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            progress = false;
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            progress = false;
            pBar.Value = 0;
            prevMarker = new Point();
            canv.Children.Clear();
            Draw(0);
        }
        void Draw(double d)
        {
            foreach (var c in circles) canv.Children.Remove(c);
            foreach (var l in lines)   canv.Children.Remove(l);
            canv.Children.Remove(theDot);
            int i = (int)(d * 10);
            circles = new List<Ellipse>();
            lines = new List<Line>();
            Point currentCenter = new Point()
            {
                X = (double)(canv.ActualWidth / 2),
                Y = (double)(canv.ActualHeight / 2)
            };
            if(DG.Items[0] as Info is null)
            {
                //MessageBox.Show("No item");
                return;
            }
            Point marker = new Point()
            {
                X = currentCenter.X,
                Y = currentCenter.Y - (DG.Items[0] as Info).Radius
            };
            
            var rows = GetDataGridRows(DG);
            int k = 0;
            foreach (DataGridRow row in rows)
            {
                k++;
                Info info = (Info)row.Item;
                double angle = i / 1000.0 * 360 * info.Frequency + 90;
                Point target = new Point(currentCenter.X, currentCenter.Y - info.Radius);
                Point rotated = Rotate(currentCenter, target, angle);
                Ellipse circle = new Ellipse()
                {
                    Width = info.Radius*2,
                    Height = info.Radius*2,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                circles.Add(circle);
                circle.SetValue(Canvas.LeftProperty, currentCenter.X - info.Radius);
                circle.SetValue(Canvas.TopProperty, currentCenter.Y - info.Radius);
                Line line = new Line()
                {
                    X1 = currentCenter.X,
                    Y1 = currentCenter.Y,
                    X2 = rotated.X,
                    Y2 = rotated.Y,
                    StrokeThickness = 1,
                    Stroke = Brushes.Black
                };
                lines.Add(line);
                if (k == rows.Count(z => 0==0))
                {
                    marker = rotated;
                    if (prevMarker.X == 0 && prevMarker.Y == 0) prevMarker = marker;
                    Line mark = new Line()
                    {
                        X1 = marker.X,
                        Y1 = marker.Y,
                        X2 = prevMarker.X,
                        Y2 = prevMarker.Y,
                        StrokeThickness = 2,
                        Stroke = Brushes.Blue
                    };
                    prevMarker = marker;
                    canv.Children.Add(mark);
                    theDot = new Ellipse()
                    {
                        Width = 6,
                        Height = 6,
                        Stroke = Brushes.Red,
                        StrokeThickness = 4
                    };
                    theDot.SetValue(Canvas.LeftProperty, rotated.X - 3);
                    theDot.SetValue(Canvas.TopProperty, rotated.Y - 3);
                    canv.Children.Add(theDot);
                }
                if(drawcircles.IsChecked) canv.Children.Add(circle);
                if(  drawlines.IsChecked) canv.Children.Add(line);
                currentCenter = rotated;
            }
            canv.UpdateLayout();
        }
        Point Rotate(Point center, Point target, double angle)
        {
            angle = angle * (Math.PI / 180);
            double cosTheta = Math.Cos(angle);
            double sinTheta = Math.Sin(angle);
            return new Point
            {
                X = (cosTheta * (target.X - center.X) - sinTheta * (target.Y - center.Y) + center.X),
                Y = (sinTheta * (target.X - center.X) + cosTheta * (target.Y - center.Y) + center.Y)
            };
        }
        public IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable<Info>;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            progress = false;
            this.DataContext = new List<Info>();
            pBar.Value = 0;
            prevMarker = new Point();
            canv.Children.Clear();
        }

        private void saveItem_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = "c:\\";
            saveFileDialog.Filter = "XML files (*.xml)|*.xml";
            saveFileDialog.Title = "Save an XML file";
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName != "")
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<Info>));
                TextWriter writer = new StreamWriter(saveFileDialog.FileName);
                ser.Serialize(writer, DataContext as List<Info>);
                writer.Close();
            }
            //else MessageBox.Show("You must enter the name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "XML files (*.xml)|*.xml";
            openFileDialog.Title = "Select an XML file";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                List<Info> newList;
                XmlSerializer serializer = new XmlSerializer(typeof(List<Info>));
                Stream reader;
                try 
                {
                    reader = new FileStream(openFileDialog.FileName, FileMode.Open);
                    try { newList = (List<Info>)serializer.Deserialize(reader); }
                    catch (InvalidOperationException)
                    {
                        MessageBox.Show("Bad XML file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        newList = null;
                        return;
                    }
                    DataContext = newList;
                }
                catch(IOException)
                {
                    MessageBox.Show("Please choose a proper file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
            canv.Children.Clear();
            refresh = true;
        }

        private void drawcircles_Click(object sender, RoutedEventArgs e)
        {
            if (pBar.Value >= 0) Draw(pBar.Value);
        }

        private void drawlines_Click(object sender, RoutedEventArgs e)
        {
            if (pBar.Value >= 0) Draw(pBar.Value);
        }

        private void DG_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if(pBar.Value <= 0)
            {
                refresh = true;
            }
        }

        private void DG_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && pBar.Value <= 0)
            {
                if(pBar.Value <= 0) prevMarker = new Point();
                //canv.Children.Clear();
                refresh = true;
            }
        }
    }
    public class Info
    {
        public double Frequency { get; set; }
        public double Radius { get; set; }
    }
}
