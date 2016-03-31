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
using PixeLINK;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Threading;
using System.IO;
using System.Diagnostics;
using DynamicOanels;
using System.Security.Cryptography;
using DevExpress.Xpf.Docking.Base;
using DevExpress.Xpf.Docking;

namespace Views
{
    /// <summary>
    /// Interaction logic for Camera.xaml
    /// </summary>
    ///
   
    public partial class Preview : UserControl
    {
        
        AdornerLayer aLayer;

        bool _isDown = false;
        bool _isDragging;
        bool selected = false;
        UIElement selectedElement = null;
        Point _startPoint;
        private double _originalLeft;
        private double _originalTop;
        CancellationTokenSource cts;
        float scaleX = 0f;
        float scaleY = 0f;
        public string Name = "";
        private object logger;
        BitmapImage image = null;
        CameraContainer cam = null;
        float[] RoiSizes = null;

        //   TextBoxOutputter outputter;
        public Preview(CameraContainer cam)
        {
            InitializeComponent();
            this.cam = cam;
            //          outputter = new TextBoxOutputter(TestBox);
            //          Console.SetOut(outputter);
            Loaded += Camera_Loaded;
       
        }
       
        private void DockLayoutManager_ShowingMenu(object sender, DevExpress.Xpf.Docking.Base.ShowingMenuEventArgs e)
        {
            DockLayoutManager dlm = sender as DockLayoutManager;
            DocumentPanel dp = dlm.ActiveDockItem as DocumentPanel;
        }

        private void Preview_DockItemClosing(object sender, ItemCancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Preview_DockItemClosed(object sender, DockItemClosedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Camera_Loaded(object sender, RoutedEventArgs e)
        {
            
            selectedElement = roi;
            _originalLeft = Canvas.GetLeft(selectedElement);
            _originalTop = Canvas.GetTop(selectedElement);
            //add adoner to the ROI rectangle so the user can grow/shrink area
            aLayer = AdornerLayer.GetAdornerLayer(selectedElement);
            aLayer.Add(new ResizingAdorner(selectedElement));
            roi.MouseMove += new MouseEventHandler(Window1_MouseMove);
            roi.MouseLeftButtonDown += new MouseButtonEventHandler(Window1_MouseLeftButtonDown);
            roi.MouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
            RoiSizes = cam.GetFeatureByParms(Feature.Roi);
            top.Text = RoiSizes[0].ToString();
            left.Text = RoiSizes[1].ToString();
            width.Text = RoiSizes[2].ToString();
            height.Text = RoiSizes[3].ToString();
            selected = true;
           
           
            e.Handled = true;
        }
        


        public void grab_Click(object sender, RoutedEventArgs e)
        {


        }

        private void bROI_ItemClick(object sender, RoutedEventArgs e)
        {
            
            Canvas.SetTop(roi, still.Height*.40 );
            Canvas.SetLeft(roi, still.Width*.20);
           
            roi.Width = myCanvas.ActualWidth * .60;
            roi.Visibility = Visibility.Visible;
            cam.StopCamera();

        }
        void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Window1_MouseLeftButtonDown");
            if (e.Source != roi)
            {
  //              _startPoint = e.GetPosition(myCanvas);
                return;
            }

            _startPoint = e.GetPosition(myCanvas);
            _originalLeft = Canvas.GetLeft(roi);
            _originalTop = Canvas.GetTop(roi);
            Vector offset = VisualTreeHelper.GetOffset(myCanvas);
            Console.WriteLine("original position {0} ,{1} ", _originalLeft, _originalTop);
            Console.WriteLine("Window1_MouseLeftButtonDown starting point  {0} ,{1} ", _startPoint.X, _startPoint.Y);

            _isDown = true;

        }

        // Handler for drag stopping on user choise
        void DragFinishedMouseHandler(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("DragFinishedMouseHandler");
            if (_isDown)
            {
                Console.WriteLine("DragFinishedMouseHandler _isDown");
                _isDown = false;
                _isDragging = false;
  //              return;
            }
           
            scaleX = RoiSizes[2] / (float)still.Width;
            scaleY = RoiSizes[3] / (float)still.Height;
            RoiSizes[0] = (float)Math.Round((float)((Canvas.GetTop(roi) - Canvas.GetTop(still))) * scaleY);
            RoiSizes[1] = (float)Math.Round(((Canvas.GetLeft(roi) - Canvas.GetLeft(still))) * scaleX);
            RoiSizes[2] = (float)Math.Round(roi.Width * scaleX);
            RoiSizes[3] = (float)Math.Round(roi.Height * scaleY);
            cam.SetFeature(Feature.Roi, RoiSizes);
           
            Adorner[] toRemoveArray = aLayer.GetAdorners(roi);
            if (toRemoveArray != null)
            {
               aLayer.Remove(toRemoveArray[0]);
            }
            roi.Visibility = Visibility.Hidden;
            top.Text = RoiSizes[0].ToString();
            left.Text = RoiSizes[1].ToString();
            width.Text = RoiSizes[2].ToString();
            height.Text = RoiSizes[3].ToString();
            cam.StartCamera();
            e.Handled = true;
        }

        private void Roi_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDown)
            {
                _isDown = false;
                _isDragging = false;
            }
        }
        private void StopDragging()
        {
            
        }
        // Hanler for providing drag operation with selected element
        void Window1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source != roi)
            {
              
                return;
            }
            Point position = Mouse.GetPosition(myCanvas);
            down.Text = _isDown.ToString();
            if (_isDown)
            {
                double test = e.GetPosition(myCanvas).X;
 //               if (_isDragging)
  //              {
                    double newY = (_startPoint.Y - _originalTop);
                    double newX = (_startPoint.X - _originalLeft);
                    Console.WriteLine("newx newy {0} ,{1} ", position.X - (_startPoint.X - _originalLeft), position.Y - (_startPoint.Y - _originalTop));
                    left.Text = (position.X - (_startPoint.X - _originalLeft)).ToString();
                    top.Text = (position.Y - (_startPoint.Y - _originalTop)).ToString();
                Canvas.SetTop(roi, position.Y - (_startPoint.Y - _originalTop));
                    Canvas.SetLeft(roi, position.X - (_startPoint.X - _originalLeft));
   //             }
            }
        }

      

        private void bSelectAll_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }

        private void Clear_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            roi.Visibility = Visibility.Collapsed;
        }

        private void Reset_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
 
            RoiSizes[0] = 0;
            RoiSizes[1] = 0;
            RoiSizes[2] = 2048;
            RoiSizes[3] = 2048;
            cam.SetFeature(Feature.Roi, RoiSizes);
        }
        //this thread will pull work of of myQueue and update the UI using Dispatcher.Invoke
        public void Work(byte[] dstBuf)
        {
            Application.Current.Dispatcher.Invoke(
               DispatcherPriority.Background,
                   new Action(() =>
                   {
                       MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                       byte[] hash = md5.ComputeHash(dstBuf);
                       ((App)Application.Current).logger.MyLogFile("Preview hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));

                       image = new BitmapImage();
                       using (MemoryStream stream = new MemoryStream(dstBuf))
                       {
                           image.BeginInit();
                           image.DecodePixelHeight = (int)myCanvas.ActualHeight;
                           image.DecodePixelWidth = (int)myCanvas.ActualWidth;
                           image.StreamSource = stream;
                           image.CacheOption = BitmapCacheOption.OnLoad;
                           image.EndInit();
                       }
                      
                       still.Height = myCanvas.ActualHeight;
                       still.Width = myCanvas.ActualWidth;
                       still.Source = image;
                       dstBuf = null;
                        ((App)Application.Current).logger.MyLogFile("Preview Camera ", this.Name);

                   }));
        }
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        
    }


    }
