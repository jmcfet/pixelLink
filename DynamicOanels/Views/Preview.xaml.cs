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
        Point _MouseStartPoint;
        private double _ROIoriginalLeft;
        private double _ROIoriginalTop;
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
            Loaded += Preview_Loaded;
       
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

        private void Preview_Loaded(object sender, RoutedEventArgs e)
        {
            
           
            _ROIoriginalLeft = Canvas.GetLeft(roi);
            _ROIoriginalTop = Canvas.GetTop(roi);
           
            roi.MouseMove += new MouseEventHandler(Window1_MouseMove);
            roi.MouseLeftButtonDown += new MouseButtonEventHandler(Window1_MouseLeftButtonDown);
            roi.MouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
            this.KeyDown += new KeyEventHandler(roi_KeyDown);
            roi.Focusable = true;
            //get the max roi for camera
            CameraFeature features = cam.GetFeature(PixeLINK.Feature.Roi);
            if (!features.IsSupported)
                Reset.IsEnabled = false;
            else
            {
                string temp = string.Format("{0}",features.parameters[2].MaximumValue);
                temp += " X ";
                temp += string.Format("{0}", features.parameters[3].MaximumValue);
                Reset.Content = temp;
            }
            //get the current roi 
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
            //add adoner to the ROI rectangle so the user can grow/shrink area
            aLayer = AdornerLayer.GetAdornerLayer(roi);
            aLayer.Add(new ResizingAdorner(roi));

            scaleX = RoiSizes[(int)rectSides.width] / (float)still.Width;
            scaleY = RoiSizes[(int)rectSides.height] / (float)still.Height;
            cam.StopCamera();

        }

     //mouse button down in the ROI rectangle is signalling the start of  ROI definition change using the mouse and drag
        void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
          
            if (e.Source != roi)
               return;
            _MouseStartPoint = e.GetPosition(myCanvas);
            _ROIoriginalLeft = Canvas.GetLeft(roi);
            _ROIoriginalTop = Canvas.GetTop(roi);
            ((App)Application.Current).logger.MyLogFile("Window1_MouseLeftButtonDown ", string.Format("Mouse start {0} {1} ROI {2} {3} ", _MouseStartPoint.X, _MouseStartPoint.Y, _ROIoriginalLeft, _ROIoriginalTop));
            _isDown = true;

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
           
            Point newMouseposition = Mouse.GetPosition(myCanvas);
            ((App)Application.Current).logger.MyLogFile("MouseMove  ", string.Format("Mouse new pos {0} {1}  ", newMouseposition.X, newMouseposition.Y ));

            down.Text = _isDown.ToString();
            if (_isDown)
            {

                //get the amount the mouse moved in X and Y directions
                double mouseXdiff = newMouseposition.X - _MouseStartPoint.X ;
                double mouseYdiff = newMouseposition.Y - _MouseStartPoint.Y ;
                ((App)Application.Current).logger.MyLogFile("MouseMove  ", string.Format("mouseXdiff {0} {1}  ", mouseXdiff, mouseYdiff));
              
                double newROIPosY = mouseYdiff > 0 ? _ROIoriginalTop + mouseYdiff : _ROIoriginalTop - Math.Abs(mouseYdiff);
                double newROIPosX = mouseXdiff > 0 ? _ROIoriginalLeft + mouseXdiff : _ROIoriginalLeft - Math.Abs(mouseXdiff);
               
              
                //check to see if user trys moving outside the image
                if (newROIPosY <= 0 || newROIPosX <= 0)
                    return;
                if (newROIPosY + roi.Height > still.Height)
                    return;
                if (newROIPosX + roi.Width > still.Width)
                    return;
                ((App)Application.Current).logger.MyLogFile("new ROI top left  ", string.Format(" {0} {1}  ", newROIPosX, newROIPosY));
                Canvas.SetTop(roi, newROIPosY);
                Canvas.SetLeft(roi, newROIPosX);
                //now we need to position new roi in the camera image (original ROI)
                left.Text = ((float)Math.Round(((Canvas.GetLeft(roi) )) * scaleX)).ToString();
                top.Text = ((float)Math.Round(((Canvas.GetTop(roi))) * scaleY)).ToString();
                width.Text = ((float)Math.Round(roi.Width * scaleX)).ToString();
                height.Text =   ((float)Math.Round(roi.Height * scaleY)).ToString();
    //            _MouseStartPoint = newMouseposition;
            }
        }
        //ROI definition is complete so tell the camera. we first scale the ROI units to the cameras current ROI 
        private void roi_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isDown)
            {
                Console.WriteLine("DragFinishedMouseHandler _isDown");
                _isDown = false;
                _isDragging = false;
                //              return;
            }

            RoiSizes[(int)rectSides.left] = (float)Math.Round(((Canvas.GetLeft(roi))) * scaleX);
            RoiSizes[(int)rectSides.top] = (float)Math.Round((float)((Canvas.GetTop(roi))) * scaleY);
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


        private void bSelectAll_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {

        }

        private void Clear_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            roi.Visibility = Visibility.Collapsed;
        }

        private void Reset_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            CameraFeature features = cam.GetFeature(PixeLINK.Feature.Roi);
            RoiSizes[0] = features.parameters[0].MinimumValue;    //left
            RoiSizes[1] = features.parameters[1].MinimumValue;     //top
            RoiSizes[2] = features.parameters[2].MaximumValue; //width
            RoiSizes[3] = features.parameters[3].MaximumValue;    //height
            cam.StopCamera();
            cam.SetFeature(Feature.Roi, RoiSizes);
            cam.StartCamera();
            ((App)Application.Current).logger.MyLogFile("Reset ", string.Format("ROI {0} {1} {2} {3} ", RoiSizes[0], RoiSizes[1], RoiSizes[2], RoiSizes[3]));

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


    }


    }
