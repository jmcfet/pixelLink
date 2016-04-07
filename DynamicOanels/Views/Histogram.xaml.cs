using DevExpress.Xpf.Charts;
using DynamicOanels;
using PixeLINK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Views
{
    /// <summary>
    /// Interaction logic for Histogram.xaml
    /// </summary>
    public partial class Histogram : UserControl
    {
        const int Interval = 40;
        private int m_hCamera = 0;
        static Api.Callback s_callbackDelegate;

        enum formats
        {
            ALL_PIXELS,
            EVEN_COLUMN_PIXELS,
            ODD_COLUMN_PIXELS,
            BAYER_GREEN1_PIXELS, BAYER_FIRST_CHANNEL = BAYER_GREEN1_PIXELS,
            BAYER_RED_PIXELS,
            BAYER_BLUE_PIXELS,
            BAYER_GREEN2_PIXELS, BAYER_LAST_CHANNEL = BAYER_GREEN2_PIXELS,
            RGB_RED_PIXELS, RGB_FIRST_CHANNEL = RGB_RED_PIXELS,
            RGB_GREEN_PIXELS,
            RGB_BLUE_PIXELS, RGB_LAST_CHANNEL = RGB_BLUE_PIXELS,
            YUV_Y,
            YUV_U,
            YUV_V,
            PIXEL_TYPES_TOTAL
        };


        enum BayerChannel
        {
            GREEN1,
            RED,
            BLUE,
            GREEN2,
        };
        class countHolder
        {
            public double[] bytecounts;
            public countHolder()
            {
                bytecounts = new double[256];
            }
        }
        public class HistElt
        {
            public bool bShow;
            public int eltNo;
            Color color;
            int lineWidth;
            public List<double> data;
            double mean;
            double stddev;

            public HistElt()
            {
                bShow = false;
                eltNo = -1;
                color = Color.FromArgb(0, 0, 0, 0);
                lineWidth = 0;
                mean = 0;
                data = new List<double>(new double[256]);
                stddev = 0;
            }
        };
        //     HistElt[] m_elts = new HistElt[(int)formats.PIXEL_TYPES_TOTAL];


        public Histogram()
        {
            InitializeComponent();
   //         new Thread(Work).Start();
            Loaded += Histogram_Loaded;
        }

        private void Histogram_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private HistElt[] Processdata(TransferBits transfer)
        {
            FrameDescriptor frameDesc = transfer.frameDesc;
            byte[] bits = transfer.bits;
            //       Api.SetStreamState(m_hCamera, StreamState.Stop);
            //        logger.MyLogFile("Callback ", "MyCallbackFunction");

            HistElt[] m_elts = Enumerable.Range(0, (int)formats.PIXEL_TYPES_TOTAL).Select(i => new HistElt()).ToArray();
            List<countHolder> counts = new List<countHolder> {
                new countHolder(),
                new countHolder(),
                new countHolder(),
                new countHolder()
            };
            List<countHolder> countsGRBG = new List<countHolder> {
                new countHolder(),
                new countHolder(),
                new countHolder(),
                new countHolder()
            };
            ////      Array.Clear(m_elts, 0, (int)formats.PIXEL_TYPES_TOTAL);
            //for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
            //{
            //    for (int j = 0; j < 256; j++)
            //    {
            //        m_elts[i].data[j] = 0;
            //    }

            //}

            long curtime = (long)frameDesc.FrameTime * 1000;
            int elapsedframes = frameDesc.FrameNumber;
            int decX = frameDesc.PixelAddressingValueHorizontal;
            int decY = frameDesc.PixelAddressingValueVertical;
            //int decY = static_cast<int>(pFrameDesc->PixelAddressingValue.fVertical);
            //          int frameCols = DEC_SIZE(static_cast<int>(pFrameDesc->Roi.fWidth), decX);
            //   DEC_SIZE(len,dec) (((len) + (dec) - 1) / (dec))
            int frameCols = (frameDesc.RoiWidth + decX - 1) / decX;
            int frameRows = (frameDesc.RoiWidth + decY - 1) / decY;
            int nPixels = frameRows * frameCols;
            setDataFormat(transfer.dataFormat, m_elts);
            // BAYER(or MONO) format

            // Set up an array of four pointers to doubles, ordered according to the
            // data format. That is, we set up the pointers such that the first one
            // points to the block of histogram data for RED if we are in RGGB format,
            // BLUE if we are in BGGR format, etc.
            double[] pcounts = new double[4];
            for (int i = (int)formats.BAYER_FIRST_CHANNEL; i <= (int)formats.BAYER_LAST_CHANNEL; i++)
            {
                //    std::fill(pDlg->m_elts[i].data.begin(), pDlg->m_elts[i].data.end(), 0.0);
                //    std::pair<int, int> offsetYX = GetChannelOffset(i - BAYER_FIRST_CHANNEL, uDataFormat);
                Tuple<int, int> offsetYX = GetChannelOffset(i - (int)formats.BAYER_FIRST_CHANNEL, transfer.dataFormat);
                //          pcounts[2 * offsetYX.first + offsetYX.second] = &pDlg->m_elts[i].data[0];
                pcounts[2 * offsetYX.Item1 + offsetYX.Item2] = m_elts[i].data[0];
                //}
            }

            GetCountsfromFrame(bits, frameCols, frameRows, counts);
            UpdateBayersTotals(m_elts, counts);
            CreateGRBGarray(transfer.dataFormat, counts, countsGRBG);
            UpdateBayerRGBTotals(m_elts, countsGRBG);
 //           Console.WriteLine("Elments calc");


            return m_elts;

        }

        Tuple<int, int> GetChannelOffset(int /*BayerChannel*/ channel, PixeLINK.PixelFormat dataFormat)
        {

            //// Return the row# and col# of the given channel, when the data is in the
            //// given format.
            //       std::pair<int, int> channelCoords(channel / 2, channel % 2);
            Tuple<int, int> channelCoords = new Tuple<int, int>(channel / 2, channel % 2);
            //std::pair<int, int> offset = Green1Index(dataFormat);
            Tuple<int, int> offset = Green1Index(dataFormat);
            //return std::make_pair((channelCoords.first + offset.first) % 2, (channelCoords.second + offset.second) % 2);
            return new Tuple<int, int>((channelCoords.Item1 + offset.Item1) % 2, (channelCoords.Item2 + offset.Item2) % 2);
        }

        Tuple<int, int> Green1Index(PixeLINK.PixelFormat dataFormat)
        {
            // Return (row#, col#) of Green1 (that's the green on the red row, and the blue column)
            switch (dataFormat)
            {
                //             case PIXEL_FORMAT_BAYER8_GRBG:
                case PixeLINK.PixelFormat.Bayer8GRBG:
                //               case PIXEL_FORMAT_BAYER16_GRBG:
                case PixeLINK.PixelFormat.Bayer16GRBG:
                    //              case PIXEL_FORMAT_BAYER12_GRBG_PACKED:
                    //             case PIXEL_FORMAT_BAYER12_GRBG_PACKED_MSFIRST:
                    //                return std::make_pair(0, 0);
                    return new Tuple<int, int>(0, 0);
                //              case PIXEL_FORMAT_BAYER8_RGGB:
                case PixeLINK.PixelFormat.Bayer8RGGB:
                //             case PIXEL_FORMAT_BAYER16_RGGB:
                case PixeLINK.PixelFormat.Bayer16RGGB:
                //             case PIXEL_FORMAT_BAYER12_RGGB_PACKED:
                case PixeLINK.PixelFormat.Bayer12RGGBPacked:
                //             case PIXEL_FORMAT_BAYER12_RGGB_PACKED_MSFIRST:
                case PixeLINK.PixelFormat.Bayer12RGGBPackedMsfirst:
                    //                 return std::make_pair(0, 1);
                    return new Tuple<int, int>(0, 1);
                //            case PIXEL_FORMAT_BAYER8_BGGR:
                case PixeLINK.PixelFormat.Bayer8BGGR:
                //              case PIXEL_FORMAT_BAYER12_BGGR_PACKED:
                case PixeLINK.PixelFormat.Bayer12BGGRPacked:
                //            case PIXEL_FORMAT_BAYER12_BGGR_PACKED_MSFIRST:
                case PixeLINK.PixelFormat.Bayer12BGGRPackedMsfirst:
                    //                  return std::make_pair(1, 0);
                    return new Tuple<int, int>(1, 0);
                //               case PIXEL_FORMAT_BAYER8_GBRG:
                case PixeLINK.PixelFormat.Bayer8GBRG:
                    //            case PIXEL_FORMAT_BAYER16_GBRG:
                    //           case PIXEL_FORMAT_BAYER12_GBRG_PACKED:
                    //          case PIXEL_FORMAT_BAYER12_GBRG_PACKED_MSFIRST:
                    //                return std::make_pair(1, 1);
                    return new Tuple<int, int>(1, 1);
            }
            return new Tuple<int, int>(0, 0); // Treat MONO as GRBG (the default for the PL-A682).
        }

        void GetCountsfromFrame(byte[] pData, int frameRows, int frameCols, List<countHolder> counts)
        {


            // Get histogram data for the bayer channels
            for (int y = 0; y < frameRows - 1; y += 2)
            {
                for (int x = 0; x < frameCols - 1; x += 2)
                {
                    int tt = y * frameCols + x;
                    if (tt >= pData.Length)
                        break;
                    counts[0].bytecounts[pData[y * frameCols + x]]++;
                    //++pcounts[1][pData[y * frameCols + x + 1]];
                    counts[1].bytecounts[pData[y * frameCols + x + 1]]++;
                    //++pcounts[2][pData[(y + 1) * frameCols + x]];
                    counts[2].bytecounts[pData[(y + 1) * frameCols + x]]++;
                    //++pcounts[3][pData[(y + 1) * frameCols + x + 1]];
                    counts[3].bytecounts[pData[(y + 1) * frameCols + x + 1]]++;
                }
            }

            return;
        }

        void UpdateBayersTotals(HistElt[] m_elts, List<countHolder> counts)
        {

            int nValues = 256;

            // Use the four arrays of Bayer totals to update the EvenCol, OddCol, and All totals.

            for (int i = 0; i < nValues; i++)
            {

                m_elts[(int)formats.EVEN_COLUMN_PIXELS].data[i] = counts[0].bytecounts[i] + counts[2].bytecounts[i];
                m_elts[(int)formats.ODD_COLUMN_PIXELS].data[i] = counts[1].bytecounts[i] + counts[3].bytecounts[i];
                m_elts[(int)formats.ALL_PIXELS].data[i] = counts[0].bytecounts[i] + counts[1].bytecounts[i] + counts[2].bytecounts[i] + counts[3].bytecounts[i];
            }
        }
        // Create another array of four double-pointers that point to the 4 bayer
        // data blocks, but this time put them in the order GRBG.
        void CreateGRBGarray(PixeLINK.PixelFormat dataFormat, List<countHolder> counts, List<countHolder> countsGRBG)
        {

            for (int i = (int)BayerChannel.GREEN1; i <= (int)BayerChannel.GREEN2; i++)
            {
                //            std::pair<int, int> offsetYX = GetChannelOffset(i, uDataFormat);
                Tuple<int, int> offsetYX = GetChannelOffset(i, dataFormat);
                countsGRBG[i] = counts[2 * offsetYX.Item1 + offsetYX.Item2];
            }
        }
        void UpdateBayerRGBTotals(HistElt[] m_elts, List<countHolder> countsGRBG)
        {
            int nValues = 256;

            // Use the Bayer totals to update the RGB totals.
            for (int i = 0; i < nValues; i++)
            {
                m_elts[(int)formats.RGB_BLUE_PIXELS].data[i] = 4 * countsGRBG[(int)BayerChannel.BLUE].bytecounts[i];
                //pDlg->m_elts[RGB_GREEN_PIXELS].data[i] = (2 * pcountsGRBG[GREEN1][i]) + (2 * pcountsGRBG[GREEN2][i]);
                m_elts[(int)formats.RGB_GREEN_PIXELS].data[i] = (2 * countsGRBG[(int)BayerChannel.GREEN1].bytecounts[i]) + (2 * countsGRBG[(int)BayerChannel.GREEN2].bytecounts[i]);
                m_elts[(int)formats.RGB_RED_PIXELS].data[i] = 4 * countsGRBG[(int)BayerChannel.RED].bytecounts[i];
            }
        }
        void DataUpdated(HistElt[] m_elts)
        {
          
  //          Console.WriteLine(String.Format("  DataUpdated Memory: {0:N0} bytes (starting memory)", GC.GetTotalMemory(false)));
            int iSeries = 0;
            LineSeries2D currentSeries;

            List<double> allpoints = new List<double>();
            // Transfer the updated histogram data into the Dev Express UI.
            for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
            {
                if (m_elts[i].bShow)
                {

                    // This element is to be displayed.
                    // Check whether it has already been added to the GraphCtrl object.
                    if (m_elts[i].eltNo == -1)
                    {
                        //m_elts[i].eltNo = m_Graph.AddElement();
                        //m_Graph.SetLineColor(m_elts[i].eltNo, m_elts[i].color);
                        //m_Graph.SetLineWidth(m_elts[i].eltNo, m_elts[i].lineWidth);
                    }

                    if (iSeries == 0)
                    {
                        currentSeries = series1;
                        currentSeries.Brush = new SolidColorBrush(Colors.Red);
                    }
                    else if (iSeries == 1)
                    {
                        currentSeries = series2;
                        currentSeries.Brush = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        currentSeries = series3;
                        currentSeries.Brush = new SolidColorBrush(Colors.Blue);
                    }
                    iSeries++;

                    currentSeries.Points.BeginInit();
                    currentSeries.Points.Clear();
                    allpoints.AddRange(m_elts[i].data);

                    for (int j = 0; j < 256; j++)
                    {

                        if (m_elts[i].data[j] > 0)
                        {

                           
                            currentSeries.Points.Add(new SeriesPoint((double)j, m_elts[i].data[j]));
                        }

                    }
                    currentSeries.Points.EndInit();

                    //m_Graph.SetYData(m_elts[i].eltNo, &m_elts[i].data[0], m_elts[i].data.size());

                    //// Calculate Mean and StdDev for this element.
                    //std::pair<double, double> msd = GetMeanAndSD(&m_elts[i].data[0], m_elts[i].data.size());
                    //m_elts[i].mean = msd.first;
                    //m_elts[i].stddev = msd.second;

                }
                if (allpoints.Count > 0)
                {
                    double max = allpoints.OrderByDescending(x => x).FirstOrDefault();
                    axisY.WholeRange.SetMinMaxValues(0, max);
                }
                //             axisY.WholeRange.SetMinMaxValues(0, allpoints.OrderBy(x => x).FirstOrDefault());

                // Don't show this element.
                // If the element has only just had its bShow member set to false
                // (by unchecking its checkbox), then we need to remove its
                // corresponding element from the GraphCtrl object.
                //            if (m_elts[i].eltNo != -1)
                //           {
                //               m_Graph.RemoveElement(m_elts[i].eltNo);
                //               m_elts[i].eltNo = -1;
                //            }
                //             m_elts[i].mean = m_elts[i].stddev = 0;
                //        }
            }
 //           Console.WriteLine(String.Format("  end DataUpdated Memory: {0:N0} bytes (starting memory)", GC.GetTotalMemory(false)));
        }
        void setDataFormat(PixeLINK.PixelFormat dataFormat, HistElt[] m_elts)
        {
            bool m_bSimpleMode = true;

            if (dataFormat == PixeLINK.PixelFormat.Yuv422)
            {
                // In YUV format, we do not allow the user to select different plot lines
                // to display, because that would involve lots of format converstions.
                if (m_bSimpleMode)
                {
                    // Show RGB
                    for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
                    {
                        m_elts[i].bShow = (i >= (int)formats.RGB_FIRST_CHANNEL && i <= (int)formats.RGB_LAST_CHANNEL);
                    }
                }
                else
                {
                    // Show YUV
                    for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
                    {
                        m_elts[i].bShow = (i >= (int)formats.YUV_Y && i <= (int)formats.YUV_V);
                    }
                }
                return;
            }

            //if (!m_bAutoFormatMode)
            //    return;

            if (dataFormat == PixeLINK.PixelFormat.Mono8
                || dataFormat == PixeLINK.PixelFormat.Mono16
                || dataFormat == PixeLINK.PixelFormat.Mono12Packed
                || dataFormat == PixeLINK.PixelFormat.Mono12PackedMsfirst)
            {
                for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
                {
                    m_elts[i].bShow = (i == (int)formats.ALL_PIXELS);
                }
            }
            else if (IsBayerFormat(dataFormat)) // Bayer format
            {
                if (m_bSimpleMode)
                {
                    // Show RGB
                    for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
                    {
                        m_elts[i].bShow = (i >= (int)formats.RGB_FIRST_CHANNEL && i <= (int)formats.RGB_LAST_CHANNEL);
                    }
                }
                else
                {
                    // Show 4 bayer channels
                    for (int i = 0; i < (int)formats.PIXEL_TYPES_TOTAL; i++)
                    {
                        m_elts[i].bShow = (i >= (int)formats.BAYER_FIRST_CHANNEL && i <= (int)formats.BAYER_LAST_CHANNEL);
                    }
                }
            }

        }
        bool IsBayerFormat(PixeLINK.PixelFormat fmt)
        {
            return (fmt == PixeLINK.PixelFormat.Bayer8BGGR
                || fmt == PixeLINK.PixelFormat.Bayer16GBRG
                || fmt == PixeLINK.PixelFormat.Bayer16GRBG
                || fmt == PixeLINK.PixelFormat.Bayer16RGGB
                || fmt == PixeLINK.PixelFormat.Bayer8BGGR
                || fmt == PixeLINK.PixelFormat.Bayer8GBRG
                || fmt == PixeLINK.PixelFormat.Bayer8GRBG
                || fmt == PixeLINK.PixelFormat.Bayer8RGGB
                || fmt == PixeLINK.PixelFormat.Bayer12RGGBPacked
                || fmt == PixeLINK.PixelFormat.Bayer12GBRGPacked
                || fmt == PixeLINK.PixelFormat.Bayer12GRBGPacked
                || fmt == PixeLINK.PixelFormat.Bayer12RGGBPacked
                || fmt == PixeLINK.PixelFormat.Bayer12BGGRPackedMsfirst
                || fmt == PixeLINK.PixelFormat.Bayer12GBRGPackedMsfirst
                || fmt == PixeLINK.PixelFormat.Bayer12GRBGPackedMsfirst
                || fmt == PixeLINK.PixelFormat.Bayer12RGGBPackedMsfirst
                );

        }

        public void Work(TransferBits trans)
        {
            //do not execute processData on the UI thread
            if (Application.Current == null)
                return;
            HistElt[] elts = Processdata(trans);
  //          trans.bits = null;
            trans = null;
            Application.Current.Dispatcher.BeginInvoke(
               DispatcherPriority.Background,
                  new Action(() =>
                  {
                      DataUpdated(elts);
                      ((App)Application.Current).logger.MyLogFile("Histogram ", String.Format("     GC Memory: {0:N0} bytes (starting memory)", GC.GetTotalMemory(false)));

                  }

            ));

         
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
