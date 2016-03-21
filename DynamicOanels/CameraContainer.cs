using PixeLINK;
using System;
using System.Collections.Generic;
using System.Linq;
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
        //      public Preview preview { get; set; }
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

        public CameraContainer(int cameraNum, ImageEntity Tray, ListBox gallery)
        {
            preview = new Preview();
           
            hist = new Histogram();
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
                System.Runtime.InteropServices.Marshal.Copy(pBuf, transfer.bits, 0, (int)numPixels);
                Buffer.BlockCopy(transfer.bits, 0, bits, 0, 40);
                ((App)Application.Current).logger.MyLogFile("pBuf ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(bits)));
                transfer.dataFormat = dataFormat;
                transfer.frameDesc = frameDesc;
                transfer.hCamera = hCamera;
                int destBufferSize = 0;
              
                //                  ((App)Application.Current).logger.MyLogFile("PixelThread ", string.Format("Thread 1 {0:N0} bytes {1}", GC.GetTotalMemory(false), numPixels));
                //   really should not need the worker thread but put this in to keep the camera red lite off

                //        ThreadPool.QueueUserWorkItem(Work1, transfer);
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
                    return 1;
                }
                Application.Current.Dispatcher.Invoke(
                      DispatcherPriority.Background,
                          new Action(() => showBuffer(FormattedBuf)));


                return 1;
            }

        }

        void showBuffer(byte[] FormattedBuf)
        {

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(FormattedBuf);
            ((App)Application.Current).logger.MyLogFile("showbuffer hash ", string.Format(" thread {0} Bytes  {1}", Thread.CurrentThread.ManagedThreadId, ByteArrayToString(hash)));

            trayImage.ImagePath = FormattedBuf;
                //force a re-bind to update the gallery
                var itemsView = CollectionViewSource.GetDefaultView(LsImageGallery.ItemsSource);
                itemsView.Refresh();
                ((App)Application.Current).logger.MyLogFile("rendered ", trayImage.CameraName);

        }
        void Work1(object state)
        {

            TransferBits transfer = state as TransferBits; ;

            //((App)Application.Current)._wh.WaitOne();
            //Console.WriteLine("WorkerQueue signalled!");

            ((App)Application.Current).logger.MyLogFile("WorkThread ", String.Format(" Memory: {0:N0} bytes cam {1}  frame {2}", GC.GetTotalMemory(false), transfer.hCamera, transfer.frameDesc.FrameNumber));


            //display the image by streaming the bits into our image control but using the
            //the dispatcher to access the UI thread

            //preview.Work(FormattedBuf);
            //hist.Work(trans);




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
