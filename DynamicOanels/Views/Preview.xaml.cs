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
        //public event DockItemClosedEventHandler DockItemClosed;
        //public event DockItemCancelEventHandler DockItemClosing;

        //   TextBoxOutputter outputter;
        public Preview()
        {
            InitializeComponent();
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
       //     aLayer.Add(new ResizingAdorner(selectedElement));
            roi.MouseMove += new MouseEventHandler(Window1_MouseMove);
            roi.MouseLeftButtonDown += new MouseButtonEventHandler(Window1_MouseLeftButtonDown);
            roi.MouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
            
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
                _isDown = false;
                _isDragging = false;
                return;
            }
  //          FeatureFlags flags = 0;
            int numParms = 4;
            float[] parms = new float[numParms];
 //           Api.GetFeature(m_hCamera, Feature.Roi, ref flags, ref numParms, parms);
            scaleX = parms[2] / (float)still.Width;
            scaleY = parms[3] / (float)still.Height;
           
 //           Api.SetStreamState(m_hCamera, StreamState.Stop);

            parms[0] = (float)(Canvas.GetTop(roi) - Canvas.GetTop(still)) * scaleY;
            parms[1] = (float)(Canvas.GetLeft(roi) - Canvas.GetLeft(still)) * scaleX;
            parms[2] = (float)roi.Width * scaleX;
            parms[3] = (float)roi.Height * scaleY;

  //          Api.SetFeature(m_hCamera, Feature.Roi, FeatureFlags.Manual, 4, parms);
  //          Api.SetStreamState(m_hCamera, StreamState.Start);
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
                return;
            Point position = Mouse.GetPosition(myCanvas);
            if (_isDown)
            {
                double test = e.GetPosition(myCanvas).X;
 //               if (_isDragging)
  //              {
                    double newY = (_startPoint.Y - _originalTop);
                    double newX = (_startPoint.X - _originalLeft);
                    Console.WriteLine("newx newy {0} ,{1} ", position.X - (_startPoint.X - _originalLeft), position.Y - (_startPoint.Y - _originalTop));
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
           
            int numParms = 4;
            float[] parms = new float[numParms];
 //           Api.SetStreamState(m_hCamera, StreamState.Stop);

            parms[0] = 0;
            parms[1] = 0;
            parms[2] = 700;
            parms[3] = 900;

    //        Api.SetFeature(m_hCamera, Feature.Roi, FeatureFlags.Manual, 4, parms);
  //          Api.SetStreamState(m_hCamera, StreamState.Start);
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
