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
        public enum rectSides
        {
            left = 0,
            top = 1,
            width = 2,
            height = 3,
        }


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
        float[] startRoiSizes = new float[4];
        float[] RoiSizes = new float [4];
        Rect stillBB = new Rect();
        FeatureFlags flags = 0;
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
            this.KeyDown += new KeyEventHandler(roi_KeyDown);
            //         roi.PreviewKeyUp += Roi_PreviewKeyUp;
            roi.Focusable = true;
            ReturnCode rc = cam.GetFeatureByParms(Feature.Roi,ref flags,ref RoiSizes);
            startRoiSizes = RoiSizes;
            top.Text = startRoiSizes[(int)rectSides.top].ToString();
            left.Text = startRoiSizes[(int)rectSides.left].ToString();
            width.Text = startRoiSizes[(int)rectSides.width].ToString();
            height.Text = startRoiSizes[(int)rectSides.height].ToString();
            selected = true;
            
            e.Handled = true;
        }
        


        public void grab_Click(object sender, RoutedEventArgs e)
        {


        }

        private void bROI_ItemClick(object sender, RoutedEventArgs e)
        {
            //position the ROI rectangle over the camera image
            Canvas.SetTop(roi, still.Height*.40 );
            Canvas.SetLeft(roi, still.Width*.20);
           
            roi.Width = myCanvas.ActualWidth * .60;
            roi.Visibility = Visibility.Visible;

            scaleX = RoiSizes[(int)rectSides.width] / (float)still.Width;
            scaleY = RoiSizes[(int)rectSides.height] / (float)still.Height;
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
            double test = Canvas.GetLeft(still);
            double test1 = Canvas.GetLeft(still);
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
              
            }

            //scaleX = RoiSizes[(int)rectSides.width] / (float)still.Width;
            //scaleY = RoiSizes[(int)rectSides.height] / (float)still.Height;
            //RoiSizes[(int)rectSides.left] = (float)Math.Round(((Canvas.GetLeft(roi) - Canvas.GetLeft(still))) * scaleX);
            //RoiSizes[(int)rectSides.top] = (float)Math.Round((float)((Canvas.GetTop(roi) - Canvas.GetTop(still))) * scaleY);

            //RoiSizes[(int)rectSides.width] = (float)Math.Round(roi.Width * scaleX);
            //RoiSizes[(int)rectSides.height] = (float)Math.Round(roi.Height * scaleY);
            //cam.SetFeature(Feature.Roi, RoiSizes);

            //Adorner[] toRemoveArray = aLayer.GetAdorners(roi);
            //if (toRemoveArray != null)
            //{
            //    aLayer.Remove(toRemoveArray[0]);
            //}
            //roi.Visibility = Visibility.Hidden;
            top.Text = RoiSizes[(int)rectSides.top].ToString();
            left.Text = RoiSizes[(int)rectSides.left].ToString();
            width.Text = RoiSizes[(int)rectSides.width].ToString();
            height.Text = RoiSizes[(int)rectSides.height].ToString();
            //cam.StartCamera();
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
        // Handler for providing drag operation with selected element as the mouse moves
        //the roi rectangle is moved within the image 
        void Window1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source != roi)
           
                return;
           
            Point position = Mouse.GetPosition(myCanvas);
            down.Text = _isDown.ToString();
            if (_isDown)
            {

                //adjust the roi box relative to how far we moved relative to starting position when the mouse
                //was clicked (this is just relative to Still (image showing camera image)
                double newPosX = position.X - (_startPoint.X - _originalLeft);
                double newPosY = (position.Y - (_startPoint.Y - _originalTop));
                //check to see if user trys moving outside the image
                if (newPosY <= 0 || newPosX <= 0)
                    return;
                if (newPosY + roi.Height > still.Height)
                    return;
                Canvas.SetTop(roi, newPosY);
                Canvas.SetLeft(roi, newPosX);
                //now we need to position new roi in the camera image (original ROI)
                left.Text = ((float)Math.Round(((Canvas.GetLeft(roi) )) * scaleX)).ToString();
                top.Text = ((float)Math.Round(((Canvas.GetTop(roi))) * scaleY)).ToString();
                width.Text = ((float)Math.Round(roi.Width * scaleX)).ToString();
                height.Text =   ((float)Math.Round(roi.Height * scaleY)).ToString();
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
 
            RoiSizes[0] = 0;    //left
            RoiSizes[1] = 0;    //top
            RoiSizes[2] = 2048;  //width
            RoiSizes[3] = 2048;   //height
            cam.StopCamera();
            cam.SetFeature(Feature.Roi, RoiSizes);
            cam.StartCamera();
            ReturnCode rc = cam.GetFeatureByParms(Feature.Roi, ref flags, ref RoiSizes);
            top.Text = startRoiSizes[(int)rectSides.top].ToString();
            left.Text = startRoiSizes[(int)rectSides.left].ToString();
            width.Text = startRoiSizes[(int)rectSides.width].ToString();
            height.Text = startRoiSizes[(int)rectSides.height].ToString();
            RoiValues.Visibility = Visibility.Visible;
        }
        //this thread will pull work of of myQueue and update the UI using Dispatcher.Invoke
        public void Work(TransferBits trans)
        {
            if (Application.Current == null)
                return;
            
            Application.Current.Dispatcher.Invoke(
               DispatcherPriority.Background,
                   new Action(() =>
                   {
                       //MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                       //byte[] hash = md5.ComputeHash(trans.FormattedBuf);
                       //((App)Application.Current).logger.MyLogFile("Preview hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));
                       FRate.Content = string.Format("Frame Rate  {0:F0}", trans.frameDesc.ActualFrameRate);
                       Exp.Content = string.Format("Exposure  {0:F0}", trans.frameDesc.Shutter);
                       
                       image = new BitmapImage();
                       using (MemoryStream stream = new MemoryStream(trans.FormattedBuf))
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
                       trans.FormattedBuf = null;
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

        private void roi_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isDown)
            {
                Console.WriteLine("DragFinishedMouseHandler _isDown");
                _isDown = false;
                _isDragging = false;
                //              return;
            }
            //scaleX = RoiSizes[(int)rectSides.width] / (float)still.Width;
            //scaleY = RoiSizes[(int)rectSides.height] / (float)still.Height;
            RoiSizes[(int)rectSides.left] = (float)Math.Round(((Canvas.GetLeft(roi) - Canvas.GetLeft(still))) * scaleX);
            RoiSizes[(int)rectSides.top] = (float)Math.Round((float)((Canvas.GetTop(roi) - Canvas.GetTop(still))) * scaleY);

            RoiSizes[(int)rectSides.width] = (float)Math.Round(roi.Width * scaleX);
            RoiSizes[(int)rectSides.height] = (float)Math.Round(roi.Height * scaleY);
            Mouse.OverrideCursor = Cursors.Wait;
            
            cam.SetFeature(Feature.Roi, RoiSizes);
            
           
            Adorner[] toRemoveArray = aLayer.GetAdorners(roi);
            if (toRemoveArray != null)
            {
                aLayer.Remove(toRemoveArray[0]);
            }
            roi.Visibility = Visibility.Hidden;
            top.Text = RoiSizes[(int)rectSides.top].ToString();
            left.Text = RoiSizes[(int)rectSides.left].ToString();
            width.Text = RoiSizes[(int)rectSides.width].ToString();
            height.Text = RoiSizes[(int)rectSides.height].ToString();
            cam.StartCamera();
            Mouse.OverrideCursor = null;
            e.Handled = true;
        }

       
    }


    }
