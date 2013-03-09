// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;
using ContosoCookbook.Common;
using ContosoCookbook.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Item Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace ContosoCookbook
{
    /// <summary>
    /// A page that displays details for a single item within a group while allowing gestures to
    /// flip through other items belonging to the same group.
    /// </summary>
    public sealed partial class ItemDetailPage : ContosoCookbook.Common.LayoutAwarePage
    {
        private StorageFile _photo;

        public ItemDetailPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            DataTransferManager.GetForCurrentView().DataRequested += OnDataRequested;
            // Allow saved page state to override the initial item to display
            if (pageState != null && pageState.ContainsKey("SelectedItem"))
            {
                navigationParameter = pageState["SelectedItem"];
            }

            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var item = RecipeDataSource.GetItem((String) navigationParameter);
            this.DefaultViewModel["Group"] = item.Group;
            this.DefaultViewModel["Items"] = item.Group.Items;
            this.flipView.SelectedItem = item;
        }

        void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            var item = (RecipeDataItem)this.flipView.SelectedItem;
            request.Data.Properties.Title = item.Title;

            if (_photo != null)
            {
                request.Data.Properties.Description = "Recipe photo";
                var reference = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(_photo);
                request.Data.Properties.Thumbnail = reference;
                request.Data.SetBitmap(reference);
                _photo = null;
            }
            else
            {
                request.Data.Properties.Description = "Recipe ingredients and directions";

                // Share recipe text
                var recipe = "\r\nINGREDIENTS\r\n";
                recipe += String.Join("\r\n", item.Ingredients);
                recipe += ("\r\n\r\nDIRECTIONS\r\n" + item.Directions);
                request.Data.SetText(recipe);

                // Share recipe image
                var reference = RandomAccessStreamReference.CreateFromUri(new Uri(item.ImagePath.AbsoluteUri));
                request.Data.Properties.Thumbnail = reference;
                request.Data.SetBitmap(reference);
            }
        }


        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            DataTransferManager.GetForCurrentView().DataRequested -= OnDataRequested;
            var selectedItem = (RecipeDataItem) this.flipView.SelectedItem;
            pageState["SelectedItem"] = selectedItem.UniqueId;
        }

        private async void OnPinRecipeButtonClicked(object sender, RoutedEventArgs e)
        {
            var item = (RecipeDataItem) this.flipView.SelectedItem;
            var uri = new Uri(item.TileImagePath.AbsoluteUri);

            var tile = new SecondaryTile(
                item.UniqueId, // Tile ID
                item.ShortTitle, // Tile short name
                item.Title, // Tile display name
                item.UniqueId, // Activation argument
                TileOptions.ShowNameOnLogo, // Tile options
                uri // Tile logo URI
                );

            await tile.RequestCreateAsync();

            var notifier = ToastNotificationManager.CreateToastNotifier();

            // Make sure notifications are enabled
            if (notifier.Setting != NotificationSetting.Enabled)
            {
                var dialog = new MessageDialog("Notifications are currently disabled");
                await dialog.ShowAsync();
                return;
            }

            // Get a toast template and insert a text node containing a message
            var template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            var element = template.GetElementsByTagName("text")[0];
            element.AppendChild(template.CreateTextNode("Reminder!"));

            // Schedule the toast to appear 30 seconds from now
            var date = DateTimeOffset.Now.AddSeconds(6);
            var stn = new ScheduledToastNotification(template, date);
            notifier.AddToSchedule(stn);
        }

        private async void OnBragClicked(object sender, RoutedEventArgs e)
        {
            var camera = new CameraCaptureUI();
            var file = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (file != null)
            {
                _photo = file;
                //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                //StorageFile savedFile = await localFolder.CreateFileAsync("test.jpg", CreationCollisionOption.ReplaceExisting);
                //await FileIO.WriteBytesAsync(savedFile, await GetPhotoBytesAsync(_photo));
                //// Load
                //StorageFile loadedFile = await localFolder.GetFileAsync("test.jpg");
                //var buffer = await FileIO.ReadBufferAsync(loadedFile);

                FileSavePicker savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("jpeg image", new List<string>() { ".jpeg" });
                savePicker.SuggestedFileName = "New picture";

                StorageFile ff = await savePicker.PickSaveFileAsync();
                if (ff != null)
                {
                    await _photo.MoveAndReplaceAsync(ff);
                }

                DataTransferManager.ShowShareUI();
            }

        }

        public async Task<byte[]> GetPhotoBytesAsync(StorageFile file)
        {
            IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
            var reader = new DataReader(fileStream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)fileStream.Size);

            byte[] pixels = new byte[fileStream.Size];
            reader.ReadBytes(pixels);
            return pixels;
        }
    }
}
