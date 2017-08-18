using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using nanovaTest.Utils;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Syncfusion.Pdf.Parsing;
using Windows.UI.Xaml.Media.Imaging;
using Syncfusion.UI.Xaml.TreeGrid;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Resources;
using System.Collections.ObjectModel;
using Syncfusion.Pdf;
using System.Reflection;

namespace nanovaTest.MethodHistory
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MethodHistoryPage : Page , IDisposable
    {
        private int ClickStatus = 0;
        TreeViewModel viewModel;
        private string searchText = "";
        private string pdfFolderName, pdfFileName;
        private ResourceLoader loader;

        public MethodHistoryPage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
 
            loader = new ResourceLoader();
            initPage();
        }

        private async void initPage()
        {
            try
            {
                LoadingIndicator.IsActive = true;
                PdfGrid.Visibility = Visibility.Collapsed;
                this.treeGrid.SelectionController = new TreeGridSelectionControllerExt(this.treeGrid);
                ViewPdfWithFile("Test_default.pdf");
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            finally
            {
                this.viewModel = (TreeViewModel)this.Resources["treeViewModel"];
                this.treeGrid.Loaded += TreeGrid_Loaded;
                this.treeGrid.Unloaded += TreeGrid_Unloaded;
                PdfGrid.Visibility = Visibility.Visible;
                LoadingGrid.Visibility = Visibility.Collapsed;
                LoadingIndicator.IsActive = false;
            }
        }

        public void ViewPdfWithFile(string fileName)
        {
            Assembly assembly = typeof(MethodHistoryPage).GetTypeInfo().Assembly;
            Stream fileStream = assembly.GetManifestResourceStream(string.Format("nanovaTest.Assets.{0}", fileName));
            byte[] buffer = new byte[fileStream.Length];
            fileStream.Read(buffer, 0, buffer.Length);
            PdfLoadedDocument ldoc = new PdfLoadedDocument(buffer);
            pdfViewer.LoadDocument(ldoc);
            pdfViewer.PdfProgressRing.Visibility = Visibility.Collapsed;
        }

        private void TreeGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            this.treeGrid.RequestTreeItems -= treeGrid_RequestChildSource;
            this.treeGrid.RepopulateTree();
        }

        private void TreeGrid_Loaded(object sender, RoutedEventArgs e)
        {
            this.treeGrid.RequestTreeItems += treeGrid_RequestChildSource;
            this.treeGrid.RepopulateTree();
            this.treeGrid.ExpandNode(1);
        }

        private void treeGrid_RequestChildSource(object sender, TreeGridRequestTreeItemsEventArgs args)
        {
            if (args.ParentItem == null)
            {
                //args.ChildItems = viewModel.ItemsCollection;
                args.ChildItems = GetItems();
            }
            else
            {
                if (ClickStatus == 0)
                {
                    args.ChildItems = viewModel.GetSubItems((args.ParentItem as TreeModel).Name, "runTest", searchText);
                }
                else
                {
                    args.ChildItems = viewModel.GetSubItems((args.ParentItem as TreeModel).Name, "calibrate", searchText);
                }
            }
        }

        private ObservableCollection<TreeModel> GetItems()
        {
            if (ClickStatus == 0)
            {
                ObservableCollection<TreeModel> items = new ObservableCollection<TreeModel>();
                items.Add(new TreeModel() { Name = "Advance Test" });
                items.Add(new TreeModel() { Name = "TVOC" });
                items.Add(new TreeModel() { Name = "BTEX" });
                items.Add(new TreeModel() { Name = "MTBE" });
                items.Add(new TreeModel() { Name = "TCE&PCE" });
                items.Add(new TreeModel() { Name = "Malodorous Gas" });
                items.Add(new TreeModel() { Name = "Vehicle" });
                items.Add(new TreeModel() { Name = "Air Quality" });
                items.Add(new TreeModel() { Name = "Pollution Source" });
                items.Add(new TreeModel() { Name = "Water Quality" });
                return items;
            }
            else
            {
                ObservableCollection<TreeModel> items = new ObservableCollection<TreeModel>();
                items.Add(new TreeModel() { Name = "TVOC" });
                items.Add(new TreeModel() { Name = "BTEX" });
                items.Add(new TreeModel() { Name = "MTBE" });
                items.Add(new TreeModel() { Name = "TCE&PCE" });
                items.Add(new TreeModel() { Name = "Malodorous Gas" });
                items.Add(new TreeModel() { Name = "Vehicle" });
                items.Add(new TreeModel() { Name = "Air Quality" });
                items.Add(new TreeModel() { Name = "Pollution Source" });
                items.Add(new TreeModel() { Name = "Water Quality" });
                return items;
            }
            
        }

        public void Dispose()
        {
            this.Resources.Clear();
            this.treeGrid.RequestTreeItems -= treeGrid_RequestChildSource;
            this.treeGrid.Loaded -= TreeGrid_Loaded;
            this.treeGrid.Unloaded -= TreeGrid_Unloaded;
            this.treeGrid.Dispose();
            (this.treeGrid.DataContext as IDisposable).Dispose();
            this.treeGrid.DataContext = null;
        }

        //搜索框筛选列表
        private void SearchWordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = SearchWordBox.Text;
            TreeGrid_Unloaded(new object(), new RoutedEventArgs());
            TreeGrid_Loaded(new object(), new RoutedEventArgs());
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;
            rootFrame.Navigate(typeof(MainPage), null);
            //if (rootFrame.CanGoBack && e.Handled == false)
            //{
            //    e.Handled = true;
            //    rootFrame.GoBack();
            //}
        }

        //获取/Pic/目录下面所有文件
        private async Task<List<string>> GetPdfList(string folderName)
        {
            List<string> list = new List<string>();
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder pdfFolder = await applicationFolder.CreateFolderAsync(folderName,
                CreationCollisionOption.OpenIfExists);
            var items = await pdfFolder.GetItemsAsync();

            foreach (IStorageItem item in items)
            {
                if (item.IsOfType(StorageItemTypes.File))
                {
                    list.Add(item.Name);
                }
            }

            return list;
        }
        
        //加载Pdf到View
        public async void ViewPdf(string folderName, string pdfName)
        {
            pdfFolderName = folderName;
            pdfFileName = pdfName;
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder methodFolder;
            if(ClickStatus == 0)
            {
                methodFolder = await applicationFolder.CreateFolderAsync("runTest",
                CreationCollisionOption.OpenIfExists);
            }
            else
            {
                methodFolder = await applicationFolder.CreateFolderAsync("calibrate",
                CreationCollisionOption.OpenIfExists);
            }
            StorageFolder pdfFolder = await methodFolder.CreateFolderAsync(folderName,
                CreationCollisionOption.OpenIfExists);
            StorageFile pdfFile = await pdfFolder.GetFileAsync(pdfName);

            using (var stream = await pdfFile.OpenReadAsync())
            {
                Stream fileStream = stream.AsStreamForRead();
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                //Loads the PDF document.
                PdfLoadedDocument ldoc = new PdfLoadedDocument(buffer);
                pdfViewer.LoadDocument(ldoc);
                pdfViewer.PdfProgressRing.Visibility = Visibility.Collapsed;

            }
        }

        private void RunTest_Tab_Click(object sender, RoutedEventArgs e)
        {
            if (ClickStatus == 1)
            {
                ConfigImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/config-tab-t.png"));
                ConfigText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#007DC4"));
                StatusImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/status-tab-f.png"));
                StatusText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#808080"));
                ClickStatus = 0;
            }
            TreeGrid_Unloaded(new object(), new RoutedEventArgs());
            TreeGrid_Loaded(new object(), new RoutedEventArgs());
            
        }

        private void Calibrate_Tab_Click(object sender, RoutedEventArgs e)
        {
            if (ClickStatus == 0)
            {
                ConfigImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/config-tab-f.png"));
                ConfigText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#808080"));
                StatusImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/status-tab-t.png"));
                StatusText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#007DC4"));
                ClickStatus = 1;
            }
            TreeGrid_Unloaded(new object(), new RoutedEventArgs());
            TreeGrid_Loaded(new object(), new RoutedEventArgs());
            
        }

        private void treeGrid_SelectionChanged(object sender, Syncfusion.UI.Xaml.Grid.GridSelectionChangedEventArgs args)
        {
            SfTreeGrid sfTreeGrid = (SfTreeGrid)sender;
            if(null != sfTreeGrid && null != sfTreeGrid.View.CurrentItem)
            {
                TreeNode treeNode = (TreeNode)sfTreeGrid.View.CurrentItem;
                if(null != treeNode && null != treeNode.ParentNode)
                {
                    var parentNode = (TreeModel)treeNode.ParentNode.Item;

                    TreeModel childNode = (TreeModel)sfTreeGrid.SelectedItem;
                    ViewPdf(parentNode.Name, childNode.Name);
                    PdfViewerDefault.Visibility = Visibility.Collapsed;
                    PdfViewerGrid.Visibility = Visibility.Visible;
                    ExportPdf.Visibility = Visibility.Visible;
                    PrintPdf.Visibility = Visibility.Visible;
                }
            }
        }

        private void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            pdfViewer.Print();
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            ExportPdfFile();
        }

        private async void ExportPdfFile()
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.DefaultFileExtension = ".pdf";
            savePicker.SuggestedFileName = pdfFileName;
            savePicker.FileTypeChoices.Add("Adobe PDF Document", new List<string>() { ".pdf" });
            StorageFile stFile = await savePicker.PickSaveFileAsync();

            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder methodFolder;
            if (ClickStatus == 0)
            {
                methodFolder = await applicationFolder.CreateFolderAsync("runTest",
                CreationCollisionOption.OpenIfExists);
            }
            else
            {
                methodFolder = await applicationFolder.CreateFolderAsync("calibrate",
                CreationCollisionOption.OpenIfExists);
            }
            StorageFolder pdfFolder = await methodFolder.CreateFolderAsync(pdfFolderName,
                CreationCollisionOption.OpenIfExists);
            StorageFile pdfFile = await pdfFolder.GetFileAsync(pdfFileName);

            byte[] buffer;
            Stream stream = await pdfFile.OpenStreamForReadAsync();
            buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, (int)stream.Length);

            if (stFile != null)
            {
                CachedFileManager.DeferUpdates(stFile);
                await FileIO.WriteBytesAsync(stFile, buffer);
                Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(stFile);
                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("SaveSuccess"));
                    notifyPopup.Show();
                }
            }
        }
    }
}
