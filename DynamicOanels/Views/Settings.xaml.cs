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
        CameraContainer cam = null;
        public Settings(CameraContainer cam)
        {
            InitializeComponent();
            this.cam = cam;

            Loaded += Settings_Loaded;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
           
            CameraFeature feature = cam.GetFeature(Feature.Shutter);
            Exposure.Minimum = feature.parameters[0].MinimumValue * 1000;
            Exposure.Maximum = feature.parameters[0].MaximumValue * 1000;
            float [] parms = cam.GetFeatureByParms(Feature.Shutter);
            ExposureSet.Text = (parms[0] * 1000).ToString();

            setFeature(Feature.FrameRate, FrameRateValue, FrameRateSet);
            //FrameRateValue.Minimum = feature.parameters[0].MinimumValue;
            //FrameRateValue.Maximum = feature.parameters[0].MaximumValue;
            //parms = cam.GetFeatureByParms(Feature.FrameRate);
            //FrameRateSet.Text = parms[0].ToString();
            setFeature(Feature.Gain, GainValue, gainActual);
            setFeature(Feature.Saturation, SaturationValue, SaturationActual);
            setFeature(Feature.Gamma, GammaValue, GammaActual);
            setFeature(Feature.WhiteShading, slColorR, red);
            setFeature(Feature.WhiteShading, slColorG, green);
            setFeature(Feature.WhiteShading, slColorB, blue);


        }

        void setFeature(PixeLINK.Feature feature,Slider slider,TextBox box)
        {
            CameraFeature camfeature = cam.GetFeature(feature);
            slider.Minimum = camfeature.parameters[0].MinimumValue;
            slider.Maximum = camfeature.parameters[0].MaximumValue;
            float[] parms = cam.GetFeatureByParms(feature);
            box.Text = parms[0].ToString();
        }

        private void FrameRateValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {


        }

        private void FrameRateValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            float[] parms = new float[2];
            parms[0] = float.Parse(FrameRateSet.Text);
            cam.SetFeature(Feature.FrameRate, parms);
        }

        private void Exposure_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            float[] parms = new float[4];
            parms[0] = float.Parse(ExposureSet.Text) / 1000;
            cam.SetFeature(Feature.Shutter, parms);
            parms = cam.GetFeatureByParms(Feature.FrameRate);
            FrameRateSet.Text = parms[0].ToString();
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
