using DynamicOanels.Views;
using PixeLINK;
using System;
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
using System.Windows.Threading;
using Views;

namespace DynamicOanels
{
    public class CameraContainer
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
        byte[] FormattedBuf = null;
        ListBox LsImageGallery = null;
        public static int[] serialNums = new int[10];
        int camNum = 0;

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
            this.Name = Tray.CameraName ="a" + info.SerialNumber;
            
            s_callbackDelegate[cameraNum] = new Api.Callback(MyCallbackFunction);
            Api.SetCallback(m_hCamera, Overlays.Frame, 0xD00D, s_callbackDelegate[cameraNum]);
            Api.SetStreamState(m_hCamera, StreamState.Start);
            
        }
        public CameraFeature GetFeature(PixeLINK.Feature feature)
        {
            CameraFeature features = new CameraFeature();
            Api.GetCameraFeatures(m_hCamera, feature, ref features);
            return features;
        }
        public float[] GetFeatureByParms(PixeLINK.Feature feature)
        {
            FeatureFlags flags = 0;
            int numParms = 4;
            float[] parms = new float[numParms];
            Api.GetFeature(m_hCamera, feature, ref flags, ref numParms, parms);
            return parms;
        }
        public CameraFeature SetFeature(PixeLINK.Feature feature, float[] parms)
        {
            CameraFeature features = new CameraFeature();
           
            ReturnCode rc = Api.SetStreamState(m_hCamera, StreamState.Stop);
            rc = Api.SetFeature(m_hCamera, feature, FeatureFlags.Manual, parms.Count(), parms);
            if (rc != ReturnCode.Success)
            {
                MessageBox.Show("bad parm");
            }
            ((App)Application.Current).logger.MyLogFile("SetFeature ", "Stop /start camera ");
            //     rc = Api.SetStreamState(m_hCamera, StreamState.Start);
            //rc = Api.Uninitialize(m_hCamera);
            //rc = Api.Initialize(serialNums[camNum], ref m_hCamera);
            //s_callbackDelegate[camNum] = new Api.Callback(MyCallbackFunction);
            //Api.SetCallback(m_hCamera, Overlays.Frame, 0xD00D, s_callbackDelegate[camNum]);
            Api.SetStreamState(m_hCamera, StreamState.Start);
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
                ((App)Application.Current).logger.MyLogFile("MyCallbackFunction ", string.Format("camera {0} threadid {1} ", hCamera, Thread.CurrentThread.ManagedThreadId));
                // Calculate actual framerate.
                long curtime = (long)(frameDesc.FrameTime * 1000);
                long elapsedtime = curtime - m_startframetime;
               
                //int elapsedframes = frameDesc->uFrameNumber - pDlg->m_startframe;
                //int elapsedframes = frameDesc.FrameNumber - m_startframe;
                //Console.WriteLine("calc frame rate {0}  {1}", elapsedtime, elapsedframes);
                //if (elapsedtime >= 50 && elapsedframes >= 5)
                //{
                //    // enough time and enough frames have elapsed to calculate a reasonably
                //    // accurate frame rate.
                //    m_rate = (double)(1000 * elapsedframes / elapsedtime);
                //    m_startframetime = curtime;
                //    m_startframe = frameDesc.FrameNumber;

                //}
                //else if (elapsedframes < 0 || elapsedtime < 0)
                //{
                //    // Stream has been restarted. Reset our start values.
                //    m_startframetime = curtime;
                //    m_startframe = frameDesc.FrameNumber;
                //}

                //if ((elapsedtime < 50) || (elapsedframes < 0))
                //{
                //    // The rest of this function calculates the histogram data, and then
                //    // sends a message to update the GUI. Do not do this more than 20 times
                //    // per second - that would be a waste of processor power, since users
                //    // can't tell the difference between a GUI that is updating 20 times
                //    // per second and one that is updating 1000 times per second.
                //    //
                //    // The frame should also be ignored if the frame is older than 
                //    // the most recent frame we've seen. (i.e. elapsedframes < 0)

                //    Console.WriteLine("skip frame");
                //    return 0;
                //}
                long numPixels = frameDesc.NumberOfPixels();
                if (rawbits == null)
                    rawbits = new byte[numPixels];
                TransferBits transfer = new TransferBits();

                transfer.bits = rawbits;
                byte[] bits = new byte[40];

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
                Buffer.BlockCopy(transfer.bits, 0, bits, 0, 40);
                ((App)Application.Current).logger.MyLogFile("pBuf ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(bits)));
                transfer.dataFormat = dataFormat;
                transfer.frameDesc = frameDesc;
                transfer.hCamera = hCamera;

               //really should not need the worker thread but put this in to keep the camera red lite off
                ThreadPool.QueueUserWorkItem(Work1, transfer);
 

                return 1;
            }

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
            int destBufferSize = 0;
            TransferBits transfer = state as TransferBits; ;

            Api.FormatImage(transfer.bits, ref transfer.frameDesc, ImageFormat.Bmp, null, ref destBufferSize);
            //                if (FormattedBuf == null)
            FormattedBuf = new byte[destBufferSize];
            Api.FormatImage(transfer.bits, ref transfer.frameDesc, ImageFormat.Bmp, FormattedBuf, ref destBufferSize);
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(FormattedBuf);
            ((App)Application.Current).logger.MyLogFile("hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));
            if (this.bActive == true)
            {
                preview.Work(FormattedBuf);
                hist.Work(transfer);
                return ;
            }
            Application.Current.Dispatcher.Invoke(
                  DispatcherPriority.Render,
                      new Action(() => showBuffer(FormattedBuf)));


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
