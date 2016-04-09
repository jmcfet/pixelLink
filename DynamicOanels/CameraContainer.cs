using DynamicOanels.Views;
using PixeLINK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Views;

namespace DynamicOanels
{
    public class CameraContainer
    {
        struct FEATURE_PARAM
        {
            float fMinValue;
            float fMaxValue;
        };
       

        struct CAMERA_FEATURE
        {
            uint uFeatureId;
            uint uFlags;
            uint uNumberOfParameters;
            FEATURE_PARAM[] pParams;
        }
        
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
       
       
        //      public Preview preview { get; set; }
        public settingsContainer settings = null;
        public Preview preview = null;
        public Histogram hist { get; set; }
        public ImageEntity trayImage { get; set; }
        public string Name { get; set; }
        public ProcessCameraDelegate del { get; set; }
        public bool bActive { get; set; }
        public bool bFeature { get; set; }
        public string ImagePath { get; set; }
        public List<TransferBits> fake = null;
        static Api.Callback[] s_callbackDelegate = new Api.Callback[4];
        private int m_hCamera = 0;
        private static Object thisLock = new Object();
        long m_startframetime = (0x7FFFFFFFL);
        int m_startframe = 0;
        double m_rate = 0;
        byte[] rawbits = null;
        ListBox LsImageGallery = null;
        public static int[] serialNums = new int[10];
        int camNum = 0;
        private List<CameraFeature> camfeatures = new List<CameraFeature>();

        public CameraContainer(int cameraNum, ImageEntity Tray, ListBox gallery)
        {
            preview = new Preview(this);
            camNum = cameraNum;
            hist = new Histogram();
            settings = new settingsContainer(this);
            LsImageGallery = gallery;
            trayImage = Tray;

            ReturnCode rc = Api.Initialize(serialNums[cameraNum], ref m_hCamera);
            if (cameraNum == 0)
                bActive = true;

            CameraInformation info = new CameraInformation();

            rc = Api.GetCameraInformation(m_hCamera, ref info);
            this.Name = Tray.CameraName = "a" + info.SerialNumber;
            LoadCameraFeatures();
            s_callbackDelegate[cameraNum] = new Api.Callback(MyCallbackFunction);
            Api.SetCallback(m_hCamera, Overlays.Frame, 0xD00D, s_callbackDelegate[cameraNum]);
            Api.SetStreamState(m_hCamera, StreamState.Start);

        }
        public void LoadCameraFeatures()
        {
            //   ClearFeatures();

            // Determine how much memory to allocate for the CAMERA_FEATURES struct.
            // API Note: By passing NULL in the second parameter, we are telling the
            //   API that we don't want it to populate our CAMERA_FEATURES structure
            //   yet - instead we just want it to tell us how much memory it will
            //   need when it does populate it.
            CameraFeature featureInfo = new CameraFeature();
            foreach (Features feature in Enum.GetValues(typeof(Features)))
            {
                switch (feature)
                {
                    case Features.FEATURE_SHUTTER:

                        ReturnCode rc = Api.GetCameraFeatures(m_hCamera, Feature.Shutter, ref featureInfo);
                        break;
                    case Features.FEATURE_BRIGHTNESS:
                    
                        rc = Api.GetCameraFeatures(m_hCamera, Feature.Brightness, ref featureInfo);
                        break;
                    case Features.FEATURE_FRAME_RATE:

                        Api.GetCameraFeatures(m_hCamera, Feature.FrameRate, ref featureInfo);
                        break;
                }

                               
                camfeatures.Add(featureInfo);
            }
 
        }
        /**
* Function: FeatureSupported
* Purpose:  Return true if the feature exists on the current camera.
*/
        public bool FeatureSupported(int featureId)
        {
            return FeatureSupportsFlag(featureId, FEATURE_FLAG_PRESENCE);
        }

    /**
    * Function: FeatureSupportsFlag
    * Purpose:  Return true if the feature supports the given FEATURE_FLAG.
    */
        public bool FeatureSupportsFlag(int featureId, UInt32 flag)
        {
            CameraFeature camFeature = camfeatures[featureId];
            return ((int)camFeature.flags & flag) != 0;
           
        }
        /**
        * Function: FeatureSupportsManual
        * Purpose:  Return true if the feature supports Manual setting.
        */
        bool FeatureSupportsManual(int featureId)
        {
	        return FeatureSupported(featureId) && FeatureSupportsFlag(featureId, FEATURE_FLAG_MANUAL);
        }


    /**
    * Function: FeatureSupportsAuto
    * Purpose:  Return true if the feature supports Auto mode.
    *           In Auto mode, the camera continuously updates the value of the
    *           feature (until it is set back into Manual mode).
*/
    //bool
    //CPxLCamera::FeatureSupportsAuto(const U32 featureId)
    //{
    //    return FeatureSupported(featureId) && FeatureSupportsFlag(featureId, FEATURE_FLAG_AUTO);
    //}


    /**
    * Function: FeatureSupportsOnePush
    * Purpose:  Return true if the feature supports One Push setting.
    *           One Push setting is when the camera auto-sets the value of the
    *           feature, then immediately goes back into manual mode.
*/
    //bool
    //CPxLCamera::FeatureSupportsOnePush(const U32 featureId)
    //{
    //    return FeatureSupported(featureId) && FeatureSupportsFlag(featureId, FEATURE_FLAG_ONEPUSH);
    //}


    /**
    * Function: FeatureSupportsOnOff
    * Purpose:  
*/
    //bool
    //CPxLCamera::FeatureSupportsOnOff(const U32 featureId)
    //{
    //    return FeatureSupported(featureId) && FeatureSupportsFlag(featureId, FEATURE_FLAG_OFF);
    //}

    private bool IsDynamicFeature(uint featureId)
        {
	        switch (featureId)
	        {
	        case (int)Features.FEATURE_FRAME_RATE: // Depends on ROI, decimation, etc.
	        case (int)Features.FEATURE_SHUTTER: // As of ~May 2004, can depend on frame rate (for DCAM 1.31 compliance)
	        case (int)Features.FEATURE_EXTENDED_SHUTTER: // Bugzilla.178 -- reread new limits when a kneepoint is set
	        case (int)Features.FEATURE_SHARPNESS_SCORE:
	        // others?
		        return true;
	        default:
		        return false;
	        }
        }
        public ReturnCode GetFeature(PixeLINK.Feature feature, ref CameraFeature features)
        {

            ReturnCode rc = Api.GetCameraFeatures(m_hCamera, feature, ref features);

            return rc;
        }
        public ReturnCode GetFeatureByParms(PixeLINK.Feature feature, ref FeatureFlags flags, ref float[] parms)
        {

            int numParms = 4;
            return Api.GetFeature(m_hCamera, feature, ref flags, ref numParms, parms);

        }
        public ReturnCode SetFeature(PixeLINK.Feature feature, float[] parms, FeatureFlags flag = FeatureFlags.Manual)
        {
            
            if (!FeatureSupportsManual((int)feature))
                return ReturnCode.NotSupportedError;
           
            ReturnCode rc = Api.SetFeature(m_hCamera, feature, flag, parms.Count(), parms);
            if (rc != ReturnCode.Success)
            {
                MessageBox.Show("bad parm");
            }
            ((App)Application.Current).logger.MyLogFile("SetFeature ", string.Format("Feature : {0} ",feature));
         

            return ReturnCode.Success;
        }
        public CameraFeature SetFeatureandWait(PixeLINK.Feature feature, float[] parms, FeatureFlags flag = FeatureFlags.Manual)
        {
            CameraFeature features = new CameraFeature();
            ReturnCode rc = Api.SetFeature(m_hCamera, feature, flag, parms.Count(), parms);
            if (rc != ReturnCode.Success)
            {
                MessageBox.Show("bad parm");
            }
        ((App)Application.Current).logger.MyLogFile("SetFeature ", "Stop /start camera ");
            //     rc = Api.SetStreamState(m_hCamera, StreamState.Start);

            //          Api.SetStreamState(m_hCamera, StreamState.Start);
            return features;
        }
        public void StopCamera()
        {
            ReturnCode rc = Api.SetStreamState(m_hCamera, StreamState.Stop);
        }

        public void StartCamera()
        {
            ReturnCode rc = Api.SetStreamState(m_hCamera, StreamState.Start);
        }

        [HandleProcessCorruptedStateExceptions]
        public int MyCallbackFunction(int hCamera, System.IntPtr pBuf, PixeLINK.PixelFormat dataFormat, ref FrameDescriptor frameDesc, int userData)
        {
            lock (thisLock)
            {
                if (Application.Current == null)
                    return 0;
                ((App)Application.Current).logger.MyLogFile("MyCallbackFunction ", string.Format("camera {0} threadid {1} ", hCamera, Thread.CurrentThread.ManagedThreadId));
                // Calculate actual framerate.
                long curtime = (long)(frameDesc.FrameTime * 1000);
                long elapsedtime = curtime - m_startframetime;

                int elapsedframes = frameDesc.FrameNumber - m_startframe;
                Console.WriteLine("calc frame rate {0}  {1}", elapsedtime, elapsedframes);
                if (elapsedtime >= 50 && elapsedframes >= 5)
                {
                    // enough time and enough frames have elapsed to calculate a reasonably
                    // accurate frame rate.
                    m_rate = (double)(1000 * elapsedframes / elapsedtime);
                    m_startframetime = curtime;
                    m_startframe = frameDesc.FrameNumber;

                }
                else if (elapsedframes < 0 || elapsedtime < 0)
                {
                    // Stream has been restarted. Reset our start values.
                    m_startframetime = curtime;
                    m_startframe = frameDesc.FrameNumber;
                }

                if ((elapsedtime < 50) || (elapsedframes < 0))
                {
                    // The rest of this function calculates the histogram data, and then
                    // sends a message to update the GUI. Do not do this more than 20 times
                    // per second - that would be a waste of processor power, since users
                    // can't tell the difference between a GUI that is updating 20 times
                    // per second and one that is updating 1000 times per second.
                    //
                    // The frame should also be ignored if the frame is older than 
                    // the most recent frame we've seen. (i.e. elapsedframes < 0)

                    Console.WriteLine("skip frame");
                    return 0;
                }
                long numPixels = frameDesc.NumberOfPixels();
                if (rawbits == null)
                    rawbits = new byte[numPixels];
                TransferBits transfer = new TransferBits();

                transfer.bits = rawbits;
    //            byte[] bits = new byte[40];

                //copy the image bits from API to managed buffer
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(pBuf, transfer.bits, 0, (int)numPixels);
                }
                catch
                {
                    ((App)Application.Current).logger.MyLogFile("MyCallbackFunction ", "Exception in Marshal.Copy ");
                    return 1;
                }
                //Buffer.BlockCopy(transfer.bits, 0, bits, 0, 40);
                //((App)Application.Current).logger.MyLogFile("pBuf ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(bits)));
                transfer.dataFormat = dataFormat;
                transfer.frameDesc = frameDesc;
                transfer.hCamera = hCamera;

                //really should not need the worker thread but put this in to keep the camera red lite off
                ThreadPool.QueueUserWorkItem(Work1, transfer);


                return 1;
            }

        }
        public ReturnCode WaitForAutoToComplete(Feature apiFeature, ref float[] parms)
        {
            bool wait = true;
            FeatureFlags flags = 0;
            
            while (wait == true)
            {
                System.Threading.Thread.Sleep(1000);
                ReturnCode rc = this.GetFeatureByParms(Feature.Shutter, ref flags, ref parms);
                if (rc != ReturnCode.Success)
                {
                    Mouse.OverrideCursor =  null;
                    return rc;
                }
                if (0 == (flags & FeatureFlags.OnePush))
                {
                    Mouse.OverrideCursor = null;
                    return ReturnCode.Success;
                }
            }
            return ReturnCode.Success;


        }
        
        void showBuffer(byte[] FormattedBuf)
        {

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(FormattedBuf);
            ((App)Application.Current).logger.MyLogFile("showbuffer hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));

            trayImage.ImagePath = FormattedBuf;
           ((App)Application.Current).logger.MyLogFile("rendered ", trayImage.CameraName);

        }
        void Work1(object state)
        {
            if (Application.Current == null)
                return;
            int destBufferSize = 0;
            TransferBits transfer = state as TransferBits; ;

            Api.FormatImage(transfer.bits, ref transfer.frameDesc, ImageFormat.Bmp, null, ref destBufferSize);
            //                if (FormattedBuf == null)
            transfer.FormattedBuf = new byte[destBufferSize];
            Api.FormatImage(transfer.bits, ref transfer.frameDesc, ImageFormat.Bmp, transfer.FormattedBuf, ref destBufferSize);
            //MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            //byte[] hash = md5.ComputeHash(FormattedBuf);
            //((App)Application.Current).logger.MyLogFile("hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));
            if (this.bActive == true)
            {
                preview.Work(transfer);
                hist.Work(transfer);
                return ;
            }
            //this code will paint the camera in the tray
            Application.Current.Dispatcher.Invoke(
                  DispatcherPriority.Render,
                      new Action(() => showBuffer(transfer.FormattedBuf)));


            ((App)Application.Current).logger.MyLogFile("WorkThread ", String.Format(" Memory: {0:N0} bytes cam {1}  frame {2}", GC.GetTotalMemory(false), transfer.hCamera, transfer.frameDesc.FrameNumber));




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
