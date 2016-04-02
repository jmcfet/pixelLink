using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Layout.Core;
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
using Logging;
using Views;
using System.Threading;
using System.Timers;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using System.ComponentModel;
//using DevExpress.Xpf.Core;
//using DevExpress.Xpf.LayoutControl;

namespace DynamicOanels
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public delegate int ProcessCameraDelegate(int hCamera, System.IntPtr pBuf, PixeLINK.PixelFormat dataFormat, ref FrameDescriptor frameDesc, int userData);

    public partial class MainWindow : Window
    {

        
        public Views.Target target = null;
        List<Preview> camlist = new List<Preview>();
        List<Histogram> hists = new List<Histogram>();
        private Object thisLock = new Object();
        private Object camera1Lock = new Object();
        private List<CameraContainer> cams = new List<CameraContainer>();
               
        List<LayoutPanel> panels = new List<LayoutPanel>();
        List<LayoutGroup> allgroups = new List<LayoutGroup>();

       
        LayoutGroup Groups = null;
        List<TransferBits> fake = new List<TransferBits>();
        ObservableCollection<ImageEntity> ListImageObj = new ObservableCollection<ImageEntity>();
        
        int nAttachedCams = 0;



        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //nCams = GetNumberCams();
            ((App)Application.Current).logger.MyLogFile("app start", "");

            //     < dxdo:LayoutGroup Orientation = "Horizontal" >
            docMan.DockItemClosed += DocMan_DockItemClosed;

            nAttachedCams = getNumberAttachedCams();
            if (nAttachedCams == 0)
            {
                MessageBox.Show("no cameras");
                return;
            }

           LsImageGallery.ItemsSource = ListImageObj;

            if (nAttachedCams > 0)
            {
                CreateSingleView();
            }
            //make the first camera active so that it shows in preview and histogram
            //           cams.First().bActive = true;
            //if there are more than 2 cameras place the 2 .. N in tray as the first is in the primary view
            for (int k = 0; k < nAttachedCams; k++)
            {
                ImageEntity ent = new ImageEntity();
                ent.ImagePath = File.ReadAllBytes("preview.png");
                ListImageObj.Add(ent);
            }
            SetupAttachedCams(ListImageObj);
            PopulateView();


            //        layoutGroup2.Add(layoutPanel4);

            //_timer = new System.Timers.Timer(70);
            //_timer.Elapsed += new ElapsedEventHandler(FakeCamera1);
            //_timer.Enabled = true; // Enable it


            // Store Data in List Object



            //         camtray.ItemsSource = listAvailable;

            //         ReturnCode rc = Api.Initialize(0, ref m_hCamera);
            //if (rc == ReturnCode.NoCameraError)
            //{
            //    MessageBoxResult result = MessageBox.Show(" Do you want to close this window?", "NO CAMERA", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //    if (result == MessageBoxResult.Yes)
            //    {
            //        Application.Current.Shutdown();
            //    }
            //}

        }

        private void DocMan_DockItemClosed(object sender, DevExpress.Xpf.Docking.Base.DockItemClosedEventArgs e)
        {

            SetTrayImageToNotinPreview(e.Item as LayoutPanel);
        }

        private void Tile_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            DevExpress.Xpf.Bars.BarSubItem subitem = (sender as DevExpress.Xpf.Bars.BarSubItem);
            if ((string)subitem.Content == "2Vert")
            {
                Create2View(Orientation.Horizontal, true);

            }
            if ((string)subitem.Content == "2Horz")
            {
                Create2View(Orientation.Vertical, true);

            }
            if ((string)subitem.Content == "1x2")
            {
                Create12CamView();

            }
            if ((string)subitem.Content == "4cams")
            {
                Create4CamView();

            }

            PopulateView();
        }
        void CreateSingleView()
        {
            Groups = new LayoutGroup() { Orientation = Orientation.Horizontal, Name = "Groups" };
            docMan.LayoutRoot = Groups;
            LayoutGroup Group1 = new LayoutGroup() { Orientation = Orientation.Horizontal, Name = "Groups"   };
            Groups.Add(Group1);
            LayoutPanel pan1 = new LayoutPanel();
            Group1.Add(pan1);
            //Group1.AllowDrop = true;
            //Group1.Drop += TargetPanel_Drop;
        }

        void Create2View(Orientation or, bool DropTarget)
        {
            Groups.Clear();
            Groups.Orientation = or;
            LayoutGroup Group1 = new LayoutGroup() { Orientation = Orientation.Horizontal, Name = "Groups" };
            Groups.Add(Group1);
            LayoutPanel pan1 = new LayoutPanel();
            Group1.Add(pan1);

            LayoutGroup Group2 = new LayoutGroup() { Orientation = Orientation.Horizontal};
            Groups.Add(Group2);
            LayoutPanel pan2 = new LayoutPanel();
            
            Group1.Add(pan2);

        }
        void Create12CamView()
        {
            Groups.Clear();
            Groups.Orientation = Orientation.Horizontal;
            LayoutGroup top = new LayoutGroup() { ShowCaption = true, Caption = "camera1" };
            LayoutPanel pan1 = new LayoutPanel();
            top.Add(pan1);
            Groups.Add(top);
            List<CameraContainer> activecams = cams.Where(c => c.bActive == true).ToList();
            LayoutGroup side = new LayoutGroup() { Orientation = Orientation.Vertical };
            
            Groups.Add(side);
            LayoutGroup side2 = new LayoutGroup() { Orientation = Orientation.Horizontal, ShowCaption = true, Caption = "DropTarget1" };
            side2.Add(new LayoutPanel());
            side.Add(side2);
            LayoutGroup side3 = new LayoutGroup() { Orientation = Orientation.Horizontal, ShowCaption = true, Caption = "DropTarget2" };
            side3.Add(new LayoutPanel());
            side.Add(side3);
        }
        void Create4CamView()
        {
            Groups.Clear();
            Groups.Orientation = Orientation.Vertical;
            LayoutGroup top = new LayoutGroup() { };
            Groups.Add(top);
            LayoutGroup topleft = new LayoutGroup() { ShowCaption = true, Caption = "camera1" };
            topleft.Add(new LayoutPanel());
            top.Add(topleft);
            LayoutGroup topRight = new LayoutGroup() { ShowCaption = true, Caption = "camera2" };
            topRight.Add(new LayoutPanel());
            top.Add(topRight);


            LayoutGroup bottom = new LayoutGroup() { Orientation = Orientation.Horizontal };
           
            Groups.Add(bottom);
            LayoutGroup side2 = new LayoutGroup() { Orientation = Orientation.Horizontal, ShowCaption = true};
            side2.Add(new LayoutPanel());
            bottom.Add(side2);
            LayoutGroup side3 = new LayoutGroup() { Orientation = Orientation.Horizontal, ShowCaption = true, Caption = "DropTarget2" };
            side3.Add(new LayoutPanel());
            bottom.Add(side3);
            //         setDropstargets(activecams,4);


        }
        private int getNumberAttachedCams()
        {

            int numSerials = 5;

            Api.GetNumberCameras(CameraContainer.serialNums, ref numSerials);
            numSerials = 0;
            for (int ii = 0; ii < CameraContainer.serialNums.Length; ii++)
            {
                if (CameraContainer.serialNums[ii] > 0)
                    numSerials++;
            }
            return numSerials;
        }
        private bool SetupAttachedCams(ObservableCollection<ImageEntity> ListImageObj)
        {

            for (int i = 0; i < nAttachedCams; i++)
            {

                CameraContainer cam = new CameraContainer(i, ListImageObj[i], LsImageGallery);
                cams.Add(cam);
            }
            return true;
        }



        void PopulateView()
        {
            panels.Clear();
            FindPanels(((LayoutGroup)Groups) as DependencyObject);
            List<CameraContainer> activecams = cams.Where(c => c.bActive == true).ToList();
            int j = 0;
            for (int i = 0; i < panels.Count; i++)
            {
                //if (allgroups[i].Caption != null)
                //{
                    if (j == activecams.Count)
                    {
                        panels[i].AllowDrop = true;
                        panels[i].Drop += TargetPanel_Drop;
                        panels[i].Caption = string.Format("DropTarget{0}", i);
                        continue;
                    }
                    addViews(panels[i], activecams[j++]);
                    //allgroups[i].Caption = activecams[j++].Name;
                    //freepanels[0].Content = cam.preview;
                    //freepanels[0].Caption = cam.Name;
                    //freepanels[1].Content = cam.hist;
                    //freepanels[1].Name = "hist" + cam.Name;

               // }
            }
        }

        private void addViews(LayoutPanel pan1, CameraContainer activecam)
        {
            
            DockLayoutManager doc = new DockLayoutManager();
            pan1.Content = doc ;
            LayoutGroup gr2 = new LayoutGroup() { Orientation = Orientation.Horizontal };
            doc.LayoutRoot = gr2;
            LayoutPanel settings = new LayoutPanel();
            settings.Content = activecam.settings;
            gr2.Add(settings);
            LayoutPanel prev = new LayoutPanel();
            prev.Content = activecam.preview;
            gr2.Add(prev);
            LayoutPanel hist = new LayoutPanel();
            hist.Content = activecam.hist;
            gr2.Add(hist);
            pan1.Caption = activecam.Name;   //panel contains preview
        }
        // find the first free panel and assign to the camera
        private void Addframe(CameraContainer cam, List<LayoutPanel> panels)
        {

            List<LayoutPanel> freepanels = panels.Where(p => p.Content == null).ToList();
            if (freepanels.Count == 0)
                return;
            //Preview cam = new Preview();
            //camlist.Add(cam);
            cam.bActive = true;
            freepanels[0].Content = cam.preview;
            freepanels[0].Caption = cam.Name;
            freepanels[1].Content = cam.hist;
            freepanels[1].Name = "hist" + cam.Name;

        }
        private void FindPanels(DependencyObject d)
        {

            System.Collections.IEnumerable logicalChildren = LogicalTreeHelper.GetChildren(d);
            foreach (object obj in logicalChildren)
            {
                if (obj is LayoutPanel)
                    panels.Add(obj as LayoutPanel);
                if (obj is DependencyObject)
                    FindPanels(obj as DependencyObject);
            }


        }
        private void FindGroups(DependencyObject d)
        {

            System.Collections.IEnumerable logicalChildren = LogicalTreeHelper.GetChildren(d);
            foreach (object obj in logicalChildren)
            {
                if (obj is LayoutGroup)
                    allgroups.Add(obj as LayoutGroup);
                if (obj is DependencyObject)
                    FindGroups(obj as DependencyObject);
            }


        }

        void setDropstargets(List<CameraContainer> activecams, int nTotalPanels)
        {
            panels.Clear();
            FindPanels(((LayoutGroup)Groups) as DependencyObject);
            int inActive = nTotalPanels - activecams.Count;
            //set the panels that are not in preview state to drop targets         
            for (int i = activecams.Count; i < nTotalPanels; i++)
            {
                if (allgroups[i].Caption != null)
                {
                    allgroups[i].AllowDrop = true;
                    allgroups[i].Drop += TargetPanel_Drop;
                    //              panels[i * 2 + 1].Visibility = Visibility.Collapsed;
                    //              allgroups[i ].Caption = string.Format("DropTarget{0}", i);
                }

            }
        }




        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {


                Image img = e.OriginalSource as Image;
                ImageEntity item = img.DataContext as ImageEntity;
                if (item == null)
                {
                    MessageBox.Show("bad");
                    return;
                }
                /*single
                cams.ForEach(c => c.bActive = false);
                CameraContainer selected = cams.Where(c => c.Name == item.CameraName).SingleOrDefault();
                selected.bActive = true;
                panels.Clear();
                WalkDownLogicalTree(((LayoutGroup)Groups) as DependencyObject);
                panels[0].Content = selected.preview;
                panels[1].Content = selected.hist;
        */
                SetTrayImageToinUse(item);
            
                List<CameraContainer> activecams = cams.Where(c => c.bActive == true).ToList();
                if (activecams.Count == 2)
                    Create2View(Orientation.Horizontal, false);
                if (activecams.Count == 3)
                    Create12CamView();
                if (activecams.Count == 4)
                    Create4CamView();
                PopulateView();
            }

        }
        //implement the drag of a tray image and it will be dropped onto an open window
        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            Image image = (sender as Image);
            ImageEntity item = (ImageEntity)(sender as Image).DataContext;
            var dragData = new DataObject(typeof(ImageEntity), item);
            if (image != null && e.LeftButton == MouseButtonState.Pressed)
            {

                DragDrop.DoDragDrop(image,
                                     dragData,
                                     DragDropEffects.Copy);
            }
        }
        private void TargetPanel_Drop(object sender, DragEventArgs e)
        {

            var dataObj = e.Data as DataObject;
            ImageEntity dragged = dataObj.GetData(typeof(ImageEntity)) as ImageEntity;
            CameraContainer selectedCam = cams.Where(c => c.Name == dragged.CameraName).SingleOrDefault();
            selectedCam.bActive = true;
            LayoutPanel droppedOn = e.Source as LayoutPanel;
            panels.Clear();
            FindPanels(((LayoutGroup)Groups) as DependencyObject);
            for (int y = 0; y < panels.Count; y++)
            {
                if (panels[y].Caption == droppedOn.Caption)
                {
                    addViews(panels[y], selectedCam);

                    //panels[y + 1].Visibility = Visibility.Visible;
                    //panels[y + 1].Content = selectedCam.hist;
                    //panels[y + 1].Name = "hist" + selectedCam.Name;

                }
            }
            //           droppedOn.Content = selectedCam.preview;
            SetTrayImageToinUse(dragged);
        }

        void SetTrayImageToinUse(ImageEntity item)
        {
            CameraContainer selected = cams.Where(c => c.Name == item.CameraName).SingleOrDefault();
            selected.bActive = true;
            //show the tray image as camera in preview mode
            selected.trayImage.ImagePath = File.ReadAllBytes("preview.png");
           
            
        }
        //reset the previewed camera to not active and close the associated histogram window
        void SetTrayImageToNotinPreview(LayoutPanel item)
        {
            CameraContainer selected = cams.Where(c => c.Name == item.Caption).SingleOrDefault();
            if (selected == null)
                return;
            selected.bActive = false;
            panels.Clear();
            FindPanels(((LayoutGroup)Groups) as DependencyObject);
            //close the associated Histogram as we close the preview
            string associatedHist = "hist" + selected.Name;
            for (int y = 0; y < panels.Count; y++)
            {
                if (panels[y].Name == associatedHist)
                {
                    docMan.DockController.RemovePanel(panels[y]);

                }
            }

        }



    



    }



    public class ImageEntity :  INotifyPropertyChanged
    {
        #region Property

        private byte[] imagePath;
        public byte[] ImagePath
        {
            get { return this.imagePath; }
            set
            {
              
                    this.imagePath = value;
                    this.NotifyPropertyChanged("ImagePath");
             
            }
        }
        public string CameraName
        {
            get;
            set;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion
    }
    //we need to bind the buffer (byte[]) received from the camera to an image, this convertor will stream the bytes into a memory stream
    //assocaited with an image and return the image.

    internal class ByteImageConverter : IValueConverter
    {
        #region IValueConverter Members
        private Object camera1Lock = new Object();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapImage image = new BitmapImage();
            //lock (camera1Lock)
            //{
            //    ((App)Application.Current).logger.MyLogFile("ByteImageConverter ", string.Format("called"));
            if (targetType != typeof(ImageSource))
                throw new InvalidOperationException("The target must be ImageSource or derived types");

            MemoryStream stream = new MemoryStream((byte[])value);

            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            //          }
            return image;


        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
