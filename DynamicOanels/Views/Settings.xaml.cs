using DynamicOanels;
using PixeLINK;
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

namespace Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public enum Features
        {
             FEATURE_BRIGHTNESS = 0,
            FEATURE_PIXELINK_RESERVED_1   =   1,
            FEATURE_SHARPNESS =  2,
            FEATURE_COLOR_TEMP       =       3,
            FEATURE_HUE             =        4,
            FEATURE_SATURATION      =        5,
            FEATURE_GAMMA          =         6,
            FEATURE_SHUTTER       =          7,
            FEATURE_GAIN          =          8,
            FEATURE_IRIS           =         9,
            FEATURE_FOCUS          =         10,
            FEATURE_SENSOR_TEMPERATURE  =    11,
            FEATURE_TRIGGER         =        12,
            FEATURE_ZOOM           =         13,
            FEATURE_PAN           =          14,
            FEATURE_TILT         =           15,
            FEATURE_OPT_FILTER   =           16,
            FEATURE_GPIO        =            17,
            FEATURE_FRAME_RATE  =            18,
            FEATURE_ROI         =            19,
            FEATURE_FLIP         =           20,
            FEATURE_PIXEL_ADDRESSING  =      21,
            FEATURE_PIXEL_FORMAT    =        22,
            FEATURE_EXTENDED_SHUTTER =       23,
            FEATURE_AUTO_ROI       =         24,
            FEATURE_LOOKUP_TABLE    =        25,
            FEATURE_MEMORY_CHANNEL   =       26,
            FEATURE_WHITE_SHADING   =        27 ,         /* Seen in Capture OEM as White Balance */
             FEATURE_ROTATE         =         28,
            FEATURE_IMAGER_CLK_DIVISOR =     29 ,         /* DEPRECATED - New applications should not use. */
            FEATURE_TRIGGER_WITH_CONTROLLED_LIGHT=   30,  /* Allows trigger to be used more deterministically where */
                                                    /* lighting cannot be controlled.                         */
            FEATURE_MAX_PIXEL_SIZE    =      31  ,        /* The number of bits used to represent 16-bit data (10 or 12) */
            FEATURE_BODY_TEMPERATURE	=	32,	
            FEATURE_MAX_PACKET_SIZE  	=	33,	
            FEATURE_BANDWIDTH_LIMIT    =     34,
            FEATURE_ACTUAL_FRAME_RATE   =    35,
            FEATURE_SHARPNESS_SCORE     =    36,
            FEATURE_SPECIAL_CAMERA_MODE =    37,
            FEATURES_TOTAL              =    38
        
        }
        CameraContainer cam = null;
        public Settings(CameraContainer cam)
        {
            InitializeComponent();
            this.cam = cam;
            
            Loaded += Settings_Loaded;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            CameraFeature feature = cam.GetFeature(Feature.Exposure);
            Exposure.Minimum = feature.parameters[0].MinimumValue;
            Exposure.Maximum = feature.parameters[0].MaximumValue;
            feature = cam.GetFeature(Feature.FrameRate);
            FrameRateValue.Minimum = feature.parameters[0].MinimumValue;
            FrameRateValue.Maximum = feature.parameters[0].MaximumValue;
            
        }

        //this thread will pull work of of myQueue and update the UI using Dispatcher.Invoke
        public void Work()
        {
            //Application.Current.Dispatcher.Invoke(
            //   DispatcherPriority.Background,
            //       new Action(() =>
            //       {
            //           MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            //           byte[] hash = md5.ComputeHash(dstBuf);
            //           ((App)Application.Current).logger.MyLogFile("Preview hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));

            //           image = new BitmapImage();
            //           using (MemoryStream stream = new MemoryStream(dstBuf))
            //           {
            //               image.BeginInit();
            //               image.DecodePixelHeight = (int)myCanvas.ActualHeight;
            //               image.DecodePixelWidth = (int)myCanvas.ActualWidth;
            //               image.StreamSource = stream;
            //               image.CacheOption = BitmapCacheOption.OnLoad;
            //               image.EndInit();
            //           }

            //           still.Height = myCanvas.ActualHeight;
            //           still.Width = myCanvas.ActualWidth;
            //           still.Source = image;
            //           dstBuf = null;
            //           ((App)Application.Current).logger.MyLogFile("Preview Camera ", this.Name);

            //       }));
        }

        private void FrameRateValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {


        }

        private void FrameRateValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            string value = FrameRateSet.Text;
            float test = float.Parse(value);
            cam.SetFeature(float.Parse(value));
        }
    }
}
