using nanovaTest.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;

namespace nanovaTest.MethodHistory
{
    public class TreeViewModel : IDisposable
    {
        public TreeViewModel()
        {
            this.ItemsCollection = this.GetItems();
        }

        private ObservableCollection<TreeModel> _itemsCollection;
        /// <summary>
        /// Gets or sets the employee info.
        /// </summary>
        /// <value>The employee info.</value>
        public ObservableCollection<TreeModel> ItemsCollection
        {
            get
            {
                return _itemsCollection;
            }
            set
            {
                _itemsCollection = value;
            }
        }

        public ObservableCollection<TreeModel> GetItems()
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

        public ObservableCollection<TreeModel> GetSubItems(string name, string type, string search)
        {
            ObservableCollection<TreeModel> items = new ObservableCollection<TreeModel>();
            if (name == "TVOC")
            {
                initList("TVOC", items, type);
            }
            else if (name == "BTEX")
            {
                initList("BTEX", items, type);
            }
            else if (name == "MTBE")
            {
                initList("MTBE", items, type);
            }
            else if (name == "TCE&PCE")
            {
                initList("TCE&PCE", items, type);
            }
            else if (name == "Malodorous Gas")
            {
                initList("Malodorous Gas", items, type);
            }
            else if (name == "Vehicle")
            {
                initList("Vehicle", items, type);
            }
            else if (name == "Air Quality")
            {
                initList("Air Quality", items, type);
            }
            else if (name == "Pollution Source")
            {
                initList("Pollution Source", items, type);
            }
            else if (name == "Water Quality")
            {
                initList("Water Quality", items, type);
            }
            else if(name == "Advance Test")
            {
                initList("Advance Test", items, type);
            }
            ObservableCollection<TreeModel> list = new ObservableCollection<TreeModel>();
            foreach (var model in items)
            {
                if (search.Equals("") || model.Name.Contains(search))
                {
                    list.Add(model);
                }
            }
                return list;
        }

        private void initList(string folderName, ObservableCollection<TreeModel> items, string type)
        {
            var result = AsyncHelpers.RunSync<List<string>>(() => GetPdfList(folderName, type));
            foreach (string s in result)
            {
                items.Add(new TreeModel() { Name = s });
            }
        }

        private async Task<List<string>> GetPdfList(string folderName, string type)
        {
            List<string> list = new List<string>();
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder typeFolder = await applicationFolder.CreateFolderAsync(type,
                CreationCollisionOption.OpenIfExists);
            StorageFolder pdfFolder = await typeFolder.CreateFolderAsync(folderName,
                CreationCollisionOption.OpenIfExists);
            var items = await pdfFolder.GetItemsAsync();

            foreach (IStorageItem item in items)
            {
                if (item.IsOfType(StorageItemTypes.File))
                {
                    list.Add(item.Name);
                }
            }
            list.Reverse();
            return list;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isdisposable)
        {
            if (this.ItemsCollection != null)
            {
                this.ItemsCollection.Clear();
            }
        }
    }
}
