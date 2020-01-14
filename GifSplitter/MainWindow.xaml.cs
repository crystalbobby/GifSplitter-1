using Microsoft.Win32;
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

namespace GifSplitter
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 存储Gif分解后的所有帧
        /// </summary>
        private readonly List<GifFrame> allFrame = new List<GifFrame>();
        /// <summary>
        /// Gif图片的文件名(不包含扩展名)
        /// </summary>
        private string GifFileShortName = "";
        /// <summary>
        /// Gif图片的文件名(包含扩展名)
        /// </summary>
        private string GifFileName = "";
        /// <summary>
        /// Gif图片的绝对路径(包含文件名和扩展名)
        /// </summary>
        private string GifFilePath = "";


        public MainWindow()
        {
            InitializeComponent();
            AllFrame_ListBox.AddHandler(ListBox.MouseWheelEvent, new MouseWheelEventHandler(AllFrame_MouseWheel), true);
        }
        private void OpenGif(string path,bool isLocalGif)
        {
            try
            {
                Stream ms = isLocalGif ? new MemoryStream(File.ReadAllBytes(path)) : System.Net.WebRequest.Create(GifFilePath).GetResponse().GetResponseStream();
                GifBitmapDecoder decoder = new GifBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                allFrame.Clear();
                for (int i = 0; i < decoder.Frames.Count; i++)
                {
                    Title = (i + 1) + "/" + decoder.Frames.Count + " 正在分解中...";
                    GifFrame gifFrame = new GifFrame()
                    {
                        Frame = decoder.Frames[i],
                        Index = i + 1,
                        ToolTip = "尺寸:" + decoder.Frames[i].PixelWidth + "×" + decoder.Frames[i].PixelHeight,
                        DpiX = decoder.Frames[i].DpiX,
                        DpiY = decoder.Frames[i].DpiY
                    };
                    allFrame.Add(gifFrame);
                }
                AllFrame_ListBox.ItemsSource = null;
                AllFrame_ListBox.ItemsSource = allFrame;
                AllFrame_ListBox.SelectedIndex = 0;
                Title = path;
            }
            catch (FileFormatException)
            {
                MessageBox.Show("打开的图片不是Gif图片!");
            }
        }
        private void OpenGif(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Gif图片|*.gif|所有文件|*.*",
                Multiselect = false
            };
            if ((bool)ofd.ShowDialog())
            {
                GifFilePath = ofd.FileName;
                GifFileName = ofd.SafeFileName;
                GifFileShortName = ofd.SafeFileName.Substring(0, ofd.SafeFileName.LastIndexOf('.'));
                OpenGif(ofd.FileName, true);
            }
        }
        private void OpenGifByDrop(object sender, DragEventArgs e)
        {
            ///从文件管理器拖拽的图片
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                GifFilePath = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0) as string;
                GifFileName = GifFilePath.Substring(GifFilePath.LastIndexOf('\\') + 1, GifFilePath.Length - GifFilePath.LastIndexOf('\\') - 1);
                GifFileShortName = GifFileName.Substring(0, GifFileName.LastIndexOf('.'));
                OpenGif(GifFilePath,true);
            }
            ///从浏览器拖拽的图片
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                GifFilePath = e.Data.GetData(DataFormats.StringFormat) as string;
                GifFileName = GifFilePath.Substring(GifFilePath.LastIndexOf('/') + 1, GifFilePath.Length - GifFilePath.LastIndexOf('/') - 1);
                try
                {
                    GifFileShortName = GifFileName.Substring(0, GifFileName.LastIndexOf('.'));
                }
                catch (ArgumentOutOfRangeException)
                {
                    GifFileShortName = GifFileName;
                }
                if (GifFileName.Contains(".gif"))
                    OpenGif(GifFilePath, false);
                else
                    MessageBox.Show("打开的内容不是Gif图片!");
            }
        }

        private void FrameSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AllFrame_ListBox.SelectedIndex < 0 || AllFrame_ListBox.SelectedIndex >= allFrame.Count)
                return;
            Frame_Image.DataContext = allFrame[AllFrame_ListBox.SelectedIndex];
        }
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }
                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }
        /// <summary>
        /// 用于在下边ListBox中用鼠标滚轮滚动所有图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllFrame_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ItemsControl items = (ItemsControl)sender;
            ScrollViewer scroll = FindVisualChild<ScrollViewer>(items);
            if (scroll != null)
            {
                int d = e.Delta;
                if (d < 0)
                {
                    scroll.LineRight();
                }
                else if (d > 0)
                {
                    scroll.LineLeft();
                }
                scroll.ScrollToTop();
            }
        }

        private void SaveAllFrame(object sender, RoutedEventArgs e)
        {
            if (allFrame.Count <= 0)
            {
                MessageBox.Show("请先打开一个Gif图片!");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog
            {
                FileName = GifFileShortName,
                AddExtension = false,
                RestoreDirectory = true,
                Filter = "Png图片|*.*"
            };
            if (sfd.ShowDialog() == true)
            {
                for (int i = 0; i < allFrame.Count; i++)
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(allFrame[i].Frame);
                    using (FileStream fileStream = new FileStream(sfd.FileName + "-" + (i + 1) + ".png", FileMode.Create, FileAccess.Write))
                    {
                        encoder.Save(fileStream);
                    }
                    Title = (i + 1) + "/" + allFrame.Count+"正在保存中...";
                }
                Title = GifFilePath;
                MessageBox.Show("导出完成!");
            }
        }
        private void SaveFrame(object sender, RoutedEventArgs e)
        {
            if (allFrame.Count <= 0)
            {
                MessageBox.Show("请先打开一个Gif图片!");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog
            {
                FileName = GifFileShortName,
                AddExtension = false,
                RestoreDirectory = true,
                Filter = "Png图片|*.*"
            };
            if (sfd.ShowDialog() == true)
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(allFrame[AllFrame_ListBox.SelectedIndex].Frame);
                using (FileStream fileStream = new FileStream(sfd.FileName + "-" + (AllFrame_ListBox.SelectedIndex + 1) + ".png", FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fileStream);
                }
                MessageBox.Show("保存完成!");
            }
        }
        private void GifDetail(object sender, RoutedEventArgs e)
        {
            string detail =
                "名称 : " + GifFileName +
                "\r\n位置 : " + GifFilePath +
                "\r\n共包含 : " + allFrame.Count + " 张图片";
            MessageBox.Show(detail, "Gif详细信息");
        }
        private void FrameDetail(object sender, RoutedEventArgs e)
        {
            string detail =
              "尺寸 : " + allFrame[AllFrame_ListBox.SelectedIndex].Frame.PixelWidth + "×" + allFrame[AllFrame_ListBox.SelectedIndex].Frame.PixelHeight +
              "\r\nDpiX : " + allFrame[AllFrame_ListBox.SelectedIndex].DpiX +
              "\r\nDpiY : " + allFrame[AllFrame_ListBox.SelectedIndex].DpiY;
            MessageBox.Show(detail, "详细信息");
        }
        private void AppClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyFrame(object sender, RoutedEventArgs e)
        {
            if (allFrame.Count <= 0)
                return;
            Clipboard.SetImage(allFrame[AllFrame_ListBox.SelectedIndex].Frame);
        }

        private void Help(object sender, RoutedEventArgs e)
        {
            string help =
              "1.文件->Gif详细信息,可查看Gif分解后图片数量\r\n" +
              "2.导出所有图片时将自动在用户输入的文件名后面加入对应的序号\r\n" +
              "3.导出所有图片时如果存在相同文件名图片，则默认替换原有图片";
            MessageBox.Show(help, "帮助");
        }

        private void About(object sender, RoutedEventArgs e)
        {
            Version v = Application.ResourceAssembly.GetName().Version;
            string about =
               "GifSplitter" +
               "\r\n版本 : " + v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision +
               "\r\n邮箱 : zzvr@outlook.com" +
               "\r\nQQ : 1575375168" +
               "\r\n微信 : Guodcx"+
               "\r\nGitHub : https://github.com/zou-z/GifSplitter";
            MessageBox.Show(about, "关于");
        }
    }

    /// <summary>
    /// 存储Gif其中一帧图片的数据
    /// </summary>
    public class GifFrame
    {
        /// <summary>
        /// 绑定数据，存储图片
        /// </summary>
        public BitmapFrame Frame { get; set; }
        /// <summary>
        /// 绑定数据，保存图片是Gif的第几帧，范围为[1,Gif帧数]
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 绑定数据，保存鼠标悬浮在Image控件上时显示图片的尺寸信息
        /// </summary>
        public string ToolTip { get; set; }
        /// <summary>
        /// 绑定数据，保存图片的DpiX信息
        /// </summary>
        public double DpiX { get; set; } 
        /// <summary>
        /// 绑定数据，保存图片的DpiY信息
        /// </summary>
        public double DpiY { get; set; }
    }
}
