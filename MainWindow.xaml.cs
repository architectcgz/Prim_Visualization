using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Prim_visiualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<int> vertices;
        public double[,] arcs;
        List<Tuple<int, int, double>> edges;
        public float minDistance = 0.0f;
        private bool isPaused = false;
        private ManualResetEvent pauseEvent = new ManualResetEvent(true);

        public MainWindow()
        {
            InitializeComponent();
            //构造8个顶点的顶点表
            vertices = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
            arcs = new double[,]
            {
                { 0.0, 1.3, 2.1, 0.9, 0.7, 1.8, 2.0, 1.8 },
                { 1.3, 0.0, 0.9, 1.8, 1.2, 2.8, 2.3, 1.1 },
                { 2.1, 0.9, 0.0, 2.6, 1.7, 2.5, 1.9, 1.0 },
                { 0.9, 1.8, 2.6, 0.0, 0.7, 1.6, 1.5, 0.9 },
                { 0.7, 1.2, 1.7, 0.7, 0.0, 0.9, 1.1, 0.8 },
                { 1.8, 2.8, 2.5, 1.6, 0.9, 0.0, 0.6, 1.0 },
                { 2.0, 2.3, 1.9, 1.5, 1.1, 0.6, 0.0, 0.5 },
                { 1.8, 1.1, 1.0, 0.9, 0.8, 1.0, 0.5, 0.0 }
            };
            //创建图的边
            edges = new List<Tuple<int, int, double>>();
            for (int i = 0; i < vertices.Count; i++)
            {
                for (int j = i + 1; j < vertices.Count; j++)
                {
                    edges.Add(new Tuple<int, int, double>(vertices[i], vertices[j], arcs[i, j]));
                }
            }
        }
        public async Task<double> Prim()
        {
            double dist = 0.0;//所需油管最长长度
            await Task.Run(async () =>
            {
                //定义访问过的顶点以及未访问过的顶点
                List<int> visited = new List<int> { 1 };
                List<int> unvisited = new List<int> { 2, 3, 4, 5, 6, 7, 8 };
                List<Tuple<int, int, double>> mst = new List<Tuple<int, int, double>>();
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await DrawLineBetweenCirclesAsync("海岸", "Oil_Well_1");
                    textBox.Text += $"选择的边为:海岸 - 一号油井: 5.0\n";
                });
                
                //循环结束条件是访问了所有的结点
                while (unvisited.Count > 0)
                {
                    pauseEvent.WaitOne();

                    double minDistance = double.MaxValue;
                    int visitedVertex = -1;
                    int unvisitedVertex = -1;

                    foreach (int i in visited)
                    {
                        foreach (int j in unvisited)
                        {
                            double distance = arcs[i - 1, j - 1];
                            
                            if (distance < minDistance)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(async() =>
                                {
                                    infoTextBox.Text += $"正在比较边{i} - {j} 该边权值为{distance},小于当前最小权值{minDistance}(边{visitedVertex}-{unvisitedVertex}的权值),记录该边顶点及权值\n";
                                });
                                minDistance = distance;
                                visitedVertex = i;
                                unvisitedVertex = j;
                            }
                            else
                            {
                                await Application.Current.Dispatcher.InvokeAsync(async() =>
                                {
                                    infoTextBox.Text += $"正在比较边{i} - {j} 该边权值为{distance},大于当前最小权值{minDistance}(边{visitedVertex}-{unvisitedVertex}的权值),不记录该边顶点及权值\n";
                                });
                            }
                            
                        }
                    }
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        infoTextBox.Text += $"本次循环选最小生成树边完成\n";
                    });
                    visited.Add(unvisitedVertex);
                    unvisited.Remove(unvisitedVertex);
                    mst.Add(new Tuple<int, int, double>(visitedVertex, unvisitedVertex, minDistance));
                    //在UI线程上执行
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await DrawLineBetweenCirclesAsync("Oil_Well_" + visitedVertex, "Oil_Well_" + unvisitedVertex);
                        textBox.Text += $"选择的边为:{visitedVertex}号油井 - {unvisitedVertex}号油井,距离为:{minDistance}\n";
                    });
                    dist += minDistance;
                    await Task.Delay(1000);
                }
              
            });
            return dist;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //点击先清除上次的所有线
            var linesToRemove = grid.Children.OfType<Line>().ToList();

            foreach (var line in linesToRemove)
            {
                grid.Children.Remove(line);
            }
            //将点击键设置为不可用 防止多次点击使得异步方法多次执行
            btn_prim.IsEnabled = false;
            // 清空textBox和infoTextBox
            textBox.Text = string.Empty;
            infoTextBox.Text = string.Empty;
            var result = await Prim();
            
            textBox.Text += "所需油管总长度为: "+ (result+5.0).ToString("0.00");
            //执行完重新设置为可执行
            btn_prim.IsEnabled = true;
        }
        // 获取EllipseGeometry的圆心坐标
        private async Task<Point> GetEllipseCenter(string CirclePathName)
        {
            //找名为Circle的Path元素
             Path myPath =(Path)FindName(CirclePathName);
            if (myPath != null)
            {
                EllipseGeometry ellipseGeometry = (EllipseGeometry)myPath.Data;

                if (ellipseGeometry != null)
                {
                    return ellipseGeometry.Center;
                }
            }
            // 如果无法获取圆心坐标，返回一个默认值
            return new Point(0, 0);
        }
        private async Task DrawLineBetweenCirclesAsync(string circle1, string circle2)
        {
            // 创建一个新的线条元素
            Line line = new Line();
            // 获取两个圆的中心点
            Point p1 = await GetEllipseCenter(circle1);
            Point p2 = await GetEllipseCenter(circle2);

            // 设置线条的起点和终点
            line.X1 = p1.X;
            line.Y1 = p1.Y;
            line.X2 = p2.X;
            line.Y2 = p2.Y;

            // 设置线条的颜色和宽度
            line.Stroke = Brushes.Black;
            line.StrokeThickness = 2;

            // 将线条添加到Grid中
            grid.Children.Add(line);
        }

        private void btn_togglePauseContinue_Click(object sender, RoutedEventArgs e)
        {
            
            if (isPaused)
            {
                // 继续操作
                pauseEvent.Set();
                string colorCode = "#FFFD9F9F";//点击后按钮变成红色
                Color color = (Color)ColorConverter.ConvertFromString(colorCode);
                btn_togglePauseContinue.Background = new SolidColorBrush(color);
                btn_togglePauseContinue.Content = "暂停";
            }
            else
            {
                // 暂停操作
                pauseEvent.Reset();
                string colorCode = "#FFBBECA6";//点击后按钮变成绿色
                Color color = (Color)ColorConverter.ConvertFromString(colorCode);
                btn_togglePauseContinue.Background = new SolidColorBrush(color);
                btn_togglePauseContinue.Content = "继续";
            }
            isPaused = !isPaused;
        }

    }

}
