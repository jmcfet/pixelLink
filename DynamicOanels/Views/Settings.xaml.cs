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
using System.Globalization;

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
            FEATURE_PIXELINK_RESERVED_1 = 1,
            FEATURE_SHARPNESS = 2,
            FEATURE_COLOR_TEMP = 3,
            FEATURE_HUE = 4,
            FEATURE_SATURATION = 5,
            FEATURE_GAMMA = 6,
            FEATURE_SHUTTER = 7,
            FEATURE_GAIN = 8,
            FEATURE_IRIS = 9,
            FEATURE_FOCUS = 10,
            FEATURE_SENSOR_TEMPERATURE = 11,
            FEATURE_TRIGGER = 12,
            FEATURE_ZOOM = 13,
            FEATURE_PAN = 14,
            FEATURE_TILT = 15,
            FEATURE_OPT_FILTER = 16,
            FEATURE_GPIO = 17,
            FEATURE_FRAME_RATE = 18,
            FEATURE_ROI = 19,
            FEATURE_FLIP = 20,
            FEATURE_PIXEL_ADDRESSING = 21,
            FEATURE_PIXEL_FORMAT = 22,
            FEATURE_EXTENDED_SHUTTER = 23,
            FEATURE_AUTO_ROI = 24,
            FEATURE_LOOKUP_TABLE = 25,
            FEATURE_MEMORY_CHANNEL = 26,
            FEATURE_WHITE_SHADING = 27,         /* Seen in Capture OEM as White Balance */
            FEATURE_ROTATE = 28,
            FEATURE_IMAGER_CLK_DIVISOR = 29,         /* DEPRECATED - New applications should not use. */
            FEATURE_TRIGGER_WITH_CONTROLLED_LIGHT = 30,  /* Allows trigger to be used more deterministically where */
                                                         /* lighting cannot be controlled.                         */
            FEATURE_MAX_PIXEL_SIZE = 31,        /* The number of bits used to represent 16-bit data (10 or 12) */
            FEATURE_BODY_TEMPERATURE = 32,
            FEATURE_MAX_PACKET_SIZE = 33,
            FEATURE_BANDWIDTH_LIMIT = 34,
            FEATURE_ACTUAL_FRAME_RATE = 35,
            FEATURE_SHARPNESS_SCORE = 36,
            FEATURE_SPECIAL_CAMERA_MODE = 37,
            FEATURES_TOTAL = 38

        }
        uint FEATURE_FLAG_PRESENCE = 0x00000001;  /* The feature is supported on this camera. */
        uint FEATURE_FLAG_MANUAL = 0x00000002;
        uint FEATURE_FLAG_AUTO = 0x00000004;
        uint FEATURE_FLAG_ONEPUSH = 0x00000008;
        uint FEATURE_FLAG_OFF = 0x00000010;
        uint FEATURE_FLAG_DESC_SUPPORTED = 0x00000020;
        uint FEATURE_FLAG_READ_ONLY = 0x00000040;
        uint FEATURE_FLAG_SETTABLE_WHILE_STREAMING = 0x00000080;
        uint FEATURE_FLAG_PERSISTABLE = 0x00000100; /* The feature will be saved with PxLSaveSettings */
        uint FEATURE_FLAG_EMULATION = 0x00000200; /* The feature is implemented in the API, not the camera */
        uint FEATURE_FLAG_VOLATILE = 0x00000400; /* The features (settable) value or limits, may change as the result of*/

        CameraContainer cam = null;
        FeatureFlags flags = 0;
        float[] parms = new float[4];
        public Settings(CameraContainer cam)
        {
            InitializeComponent();
            this.cam = cam;

            Loaded += Settings_Loaded;
           
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            if (cam.FeatureSupported((int)Feature.Shutter))
            {
                SetSlidersandInitialValue(Feature.Shutter, Exposure, ExposureSet, 1000);
                if (!cam.FeatureSupportsFlag((int)Feature.Shutter, FEATURE_FLAG_ONEPUSH))
                    AutoExpButton.Visibility = Visibility.Hidden;
            }
            else
                ExposureStuff.Visibility = Visibility.Collapsed;

            if (cam.FeatureSupported((int)Feature.FrameRate))
            {
                SetSlidersandInitialValue(Feature.FrameRate, FrameRateValue, FrameRateSet, 1);
                if (!cam.FeatureSupportsFlag((int)Feature.FrameRate, FEATURE_FLAG_ONEPUSH))
                    AutoFrameButton.IsEnabled = false;
            }
            else
                FrameStuff.Visibility = Visibility.Collapsed;
            if (cam.FeatureSupported((int)Feature.Gain))
                SetSlidersandInitialValue(Feature.Gain, GainValue, gainActual,1);
            else
                GainStuff.Visibility = Visibility.Collapsed;
            if (cam.FeatureSupported((int)Feature.Saturation))
                SetSlidersandInitialValue(Feature.Saturation, SaturationValue, SaturationActual, 1);
            else
                Satstuff.Visibility = Visibility.Collapsed;
            if (cam.FeatureSupported((int)Feature.Gamma))
                SetSlidersandInitialValue(Feature.Gamma, GammaValue, GammaActual,1);
            SetSlidersandInitialValue(Feature.WhiteShading, slColorR, red,1);
            SetSlidersandInitialValue(Feature.WhiteShading, slColorG, green,1);
            SetSlidersandInitialValue(Feature.WhiteShading, slColorB, blue,1);


        }
        void SetSlidersandInitialValue(PixeLINK.Feature feature, Slider slider, TextBox box, float multiplier)
        {
            CameraFeature features = new CameraFeature();
            ReturnCode rc = cam.GetFeature(feature, ref features);
            if (rc == ReturnCode.NotSupportedError)
            {
                slider.IsEnabled = false;
                return;
            }
            slider.Minimum = features.parameters[0].MinimumValue * multiplier;
            slider.Maximum = features.parameters[0].MaximumValue * multiplier;
            rc = cam.GetFeatureByParms(feature,ref flags, ref parms);
            box.Text = (parms[0] * multiplier).ToString();
        }
        void setFeature(PixeLINK.Feature feature,Slider slider,TextBox box)
        {
            CameraFeature features = new CameraFeature();
            ReturnCode rc =  cam.GetFeature(feature,ref features);
            if (rc == ReturnCode.NotSupportedError)
                return;
            slider.Minimum = features.parameters[0].MinimumValue;
            slider.Maximum = features.parameters[0].MaximumValue;
            rc = cam.GetFeatureByParms(feature,ref flags,ref parms);
            box.Text = parms[0].ToString();
        }

        private void Exposure_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double val = Math.Round((double)e.NewValue,5);
            if (val <= Math.Round(Exposure.Minimum,5))
                val = Exposure.Minimum;
            else
                val = Math.Round(val);
            ExposureSet.Text = val.ToString();

        }

        private void FrameRateValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            bool bStopped = false;
            if (!cam.FeatureSupportsFlag((int)Feature.FrameRate, FEATURE_FLAG_SETTABLE_WHILE_STREAMING))
            {
                cam.StopCamera();
                bStopped = true;
            }
            float[] parms = new float[1];
            parms[0] = (float)Math.Round(Double.Parse(FrameRateSet.Text));
            cam.SetFeature(Feature.FrameRate, parms);
            if (bStopped)
            {
                cam.StartCamera();
               
            }
           
        }

        private void Exposure_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            float[] parms = new float[4];
            parms[0] = float.Parse(ExposureSet.Text) / 1000;
            if (parms[0] < Exposure.Minimum)
            {
                parms[0] = (float)Exposure.Minimum;
                ExposureSet.Text = parms[0].ToString();
            }
            Mouse.OverrideCursor = Cursors.Wait;
            cam.SetFeature(Feature.Shutter, parms);
            Mouse.OverrideCursor = null;
            ReturnCode rc = cam.GetFeatureByParms(Feature.ActualFrameRate, ref flags, ref parms);
            actualframerate.Content = string.Format("Actual frame rate {0}", parms[0].ToString());
        }

        private void GainValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            float[] parms = new float[2];
            parms[0] = float.Parse(gainActual.Text);
            cam.SetFeature(Feature.Gain, parms);

        }

        private void SaturationValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            float[] parms = new float[2];
            parms[0] = float.Parse(SaturationActual.Text);
            cam.SetFeature(Feature.Saturation, parms);


        }

        private void GammaValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            
            float[] parms = new float[2];
            parms[0] = float.Parse(GammaActual.Text);
            cam.SetFeature(Feature.Gamma, parms);

        }

        private void AutoExpose_Click(object sender, RoutedEventArgs e)
        {
            bool bStopped = false;
            float[] parameters1 = new float[1];
            float[] parameters2 = new float[4];
            if (!cam.FeatureSupportsFlag((int)Feature.Shutter, FEATURE_FLAG_SETTABLE_WHILE_STREAMING))
            {
                cam.StopCamera();
                bStopped = true;
            }
            parameters1[0] = 0;
            cam.SetFeature(Feature.Shutter, parameters1,FeatureFlags.OnePush);
            //Auto exposure call
            cam.SetFeature( Feature.Shutter, parameters1, FeatureFlags.OnePush);
            // Get current Exposure after auto exposure  
            //           ReturnCode rc = cam.GetFeatureByParms(Feature.Shutter, ref flags, ref parms);
            Mouse.OverrideCursor = Cursors.Wait;
            cam.WaitForAutoToComplete(Feature.Shutter, ref parms);
            Mouse.OverrideCursor = null;
            float expTime = parms[0] * 1000; //convert to ms
            Exposure.Value = (int)expTime;
            ExposureSet.Text = expTime.ToString();
            ReturnCode rc = cam.GetFeatureByParms(Feature.ActualFrameRate, ref flags, ref parms);
            actualframerate.Content = string.Format("Actual frame rate {0}", parms[0].ToString());

            if (bStopped == true)
                cam.StartCamera();
          }

        private void AutoFrame_Click(object sender, RoutedEventArgs e)
        {
            float[] parameters1 = new float[1];
            float[] parameters2 = new float[4];
            bool bStopped = false;
            if (!cam.FeatureSupportsFlag((int)Feature.FrameRate, FEATURE_FLAG_SETTABLE_WHILE_STREAMING))
            {
                cam.StopCamera();
                bStopped = true;
            }
            //Auto exposure call
            cam.SetFeature(Feature.FrameRate, parameters1, FeatureFlags.OnePush);
            // Get current Exposure after auto exposure  
            //           ReturnCode rc = cam.GetFeatureByParms(Feature.Shutter, ref flags, ref parms);
            Mouse.OverrideCursor = Cursors.Wait;
            cam.WaitForAutoToComplete(Feature.FrameRate, ref parms);
            Mouse.OverrideCursor = null;
            float expTime = parms[0] ; 
            FrameRateValue.Value = (int)expTime;
            FrameRateSet.Text = expTime.ToString();
        }
    }

    public class RoundConvertor : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double tt = Math.Round((double)value,2);
            return Math.Round((double)value);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
  
}
