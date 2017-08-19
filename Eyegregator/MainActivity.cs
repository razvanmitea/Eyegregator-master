using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Environment = System.Environment;
using SQLite;
using System.Collections.Generic;
using Eyegregator.Controller;
using System;
using System.Collections;
using System.Xml;



namespace Eyegregator
{
	[Activity (Label = "Eyegregator", MainLauncher = true)]
	public class RSSActivity : Activity
	{
		RSSUtil rssUtil = new RSSUtil ();
		internal static SQLiteAsyncConnection DB;
		private readonly TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext ();
		List<RSSSources> rssSourcesList = new List<RSSSources> ();
		ListView listViewRSS;

		enum OpType
		{
			Sources = 1,
			Feeds = 2
		}

		OpType opType;
		EditText nameInput;
		EditText urlInput;
		EditText descriptionInput;
		Button btnDeleteSelectedSources;
		bool isLoaded = false;

		protected override void OnCreate (Bundle bundle)
		{
			rssUtil.bgndWorker.RunWorkerCompleted += (sender, e) => {

				//todo clear list
				//listViewRSS.Adapter = new ArrayAdapter<string> (this, Android.Resource.Layout.ActivityListItem, 
				//                                              rssUtil.RssFeeds.Select(x=>x.Title).ToArray());
				try {
					listViewRSS.Adapter = new CustomAdapterRSS (this, rssUtil.RssFeeds);
				} catch (Exception ex) {
					Logger.LogThis (ex.Message, "Background Workker - Completed", "MainActivity");
				}
				SetProgressBarIndeterminateVisibility (false);
				opType = OpType.Feeds;
			};

			if (isLoaded)
				return;

			base.OnCreate (bundle);
			isLoaded = true;

			RequestWindowFeature (WindowFeatures.ActionBar);
			RequestWindowFeature (WindowFeatures.Progress);
			RequestWindowFeature (WindowFeatures.IndeterminateProgress);

			SetProgressBarIndeterminate (true);
			SetProgressBarIndeterminateVisibility (true);

			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
			ActionBar.Title = "Eyegregator";
			ActionBar.SetDisplayShowTitleEnabled (true);

			CreateDB ();
			//ReadDefaultListOfFeeds ();
			SetContentView (Resource.Layout.Main);
			listViewRSS = (ListView)FindViewById (Resource.Id.listView);
			listViewRSS.Clickable = true;
		}

		private void ReadDefaultListOfFeeds ()
		{
			string content;
			try {
				using (StreamReader sr = new StreamReader (Assets.Open ("rssFeedsList.xml"))) {
					content = sr.ReadToEnd ();
				}
				if (!String.IsNullOrEmpty (content)) {
					XmlDocument doc = new XmlDocument ();
					doc.LoadXml (content);
					ProcessXmlFile (doc);
				}
			} catch (Exception ex) {
				Logger.LogThis (ex.Message, "ReadDefaultListOfFeeds", "MainActivity");
			}
		}

		private void ProcessXmlFile (XmlDocument doc)
		{
			foreach (var item in doc.GetElementsByTagName("feed").Cast<XmlNode>()) {
				var title = item ["title"].InnerText;
				var link = item ["link"].InnerText;
				var description = item ["description"].InnerText;

				//Logger.LogThis ("Entry from file read: " + title + ";" + link + ";" + description, "ProcessXmlFile", "MainActivity");
				var entryFound = FindEntryRSSSources (title);
				if (!entryFound) {
					Logger.LogThis ("Entry not found for Title:" + title, "After Find RSS Entry", "MainActivity");
					InsertNewEntryRSSSources (title, link, description);
				} else {
					Logger.LogThis ("Entry found:" + title, "After Find RSS Entry", "MainActivity");
				}
			}
		}

		private void CreateDB ()
		{
			DB = new SQLiteAsyncConnection (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "eyegregator.db"));
			DB.CreateTableAsync<RSSSources> ().ContinueWith (t => LoadRSSSources (), uiScheduler);
		}

		private string GetUrl (RSSSources rss)
		{
			return rss.Url;
		}

		private bool FindEntryRSSSources (string name)
		{
			SetProgressBarIndeterminateVisibility (true);
			bool entryFound = false;
			DB.QueryAsync<RSSSources> ("SELECT * FROM RSSSources where Name = '" + name + "'").ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
				if (t.Result.Any ())
					entryFound = true;
			}, uiScheduler);
			return entryFound;

		}

		#region DB CRUD

		private void LoadRSSSources ()
		{
			SetProgressBarIndeterminateVisibility (true);

			DB.QueryAsync<RSSSources> ("SELECT * FROM RSSSources").ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
				rssSourcesList.Clear ();
				rssSourcesList = t.Result.Select (x => x).ToList ();
			}, uiScheduler);
		}

		private void InsertNewEntryRSSSources (string name, string url, string description)
		{
			SetProgressBarIndeterminateVisibility (true);
			url = url.Contains ("http://") ? url : String.Format ("{0}" + url, "http://");
			var newRSS = new RSSSources { Name = name, Url = url, Description = description, Date = DateTime.Now };
			DB.InsertAsync (newRSS).ContinueWith (t => { 
				SetProgressBarIndeterminateVisibility (false);
				LoadRSSSources ();
			}, uiScheduler);
		}

		private void DeleteEntryRSSSources (int idEntry)
		{
			SetProgressBarIndeterminateVisibility (true);
			DB.QueryAsync<RSSSources> ("Delete FROM RSSSources where ID = " + idEntry).ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
			}, uiScheduler);
		}

		private void DeleteAllEntriesRSSSources ()
		{
			SetProgressBarIndeterminateVisibility (true);
			DB.QueryAsync<RSSSources> ("Delete FROM RSSSources").ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
			}, uiScheduler);
		}

		private void UpdateEntryRSSSources (RSSSources _entry)
		{
			DB.UpdateAsync (_entry).ContinueWith (t => { 
				SetProgressBarIndeterminateVisibility (false);
				LoadRSSSources ();
			}, uiScheduler);
		}

		#endregion

		public override bool OnCreateOptionsMenu (IMenu menu)
		{
			IMenuItem getFeedItem = menu.Add ("Get news feed");
			getFeedItem.SetShowAsAction (ShowAsAction.IfRoom);
			getFeedItem.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnGetFeedClicked));

			IMenuItem createItem = menu.Add ("Add new source");
			createItem.SetShowAsAction (ShowAsAction.IfRoom);
			createItem.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnCreateClicked));

			IMenuItem viewSources = menu.Add ("View sources");
			viewSources.SetShowAsAction (ShowAsAction.IfRoom);
			viewSources.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnViewSourcesClicked));

			IMenuItem importSources = menu.Add ("Import sources");
			importSources.SetShowAsAction (ShowAsAction.IfRoom);
			importSources.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnImportSourcesClicked));

			IMenuItem deleteAllSources = menu.Add ("Delete all sources");
			deleteAllSources.SetShowAsAction (ShowAsAction.IfRoom);
			deleteAllSources.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnDeleteAllSourcesCliked));

			return true;
		}

		private bool OnCreateClicked (IMenuItem menuItem)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder (this);
			builder.SetTitle ("Add a new source");
			LinearLayout inputLayout = (LinearLayout)LayoutInflater.Inflate (Resource.Layout.NewRSSSourceEntry, null);
			nameInput = (EditText)inputLayout.FindViewById (Resource.Id.editTextName);
			urlInput = (EditText)inputLayout.FindViewById (Resource.Id.editTextURL);
			descriptionInput = (EditText)inputLayout.FindViewById (Resource.Id.editTextDescription);
			builder.SetView (inputLayout);
			builder.SetPositiveButton ("Create", (sender, args) => InsertNewEntryRSSSources (nameInput.Text, urlInput.Text, descriptionInput.Text));
			builder.SetNegativeButton ("Cancel", (IDialogInterfaceOnClickListener)null);

			AlertDialog dialog = builder.Create ();
			dialog.Show ();

			return true;
		}

		private bool OnViewSourcesClicked (IMenuItem menuItem)
		{
			SetContentView (Resource.Layout.ViewSources);
			listViewRSS = (ListView)FindViewById (Resource.Id.listView);
			listViewRSS.Adapter = new ArrayAdapter<string> (this, Android.Resource.Layout.SimpleListItemMultipleChoice, 
				rssSourcesList.Select (x => x.Name).ToArray ());
			opType = OpType.Sources;

			try{
				btnDeleteSelectedSources = (Button)FindViewById (Resource.Id.btnDeleteSelected);
				btnDeleteSelectedSources.Click += (object sender, EventArgs e) => 
				{
					AlertDialog.Builder builder = new AlertDialog.Builder (this);
					builder.SetTitle ("Delete selected sources?");
					builder.SetPositiveButton ("Yes", (s, args) => DeleteSelectedSources ());
					builder.SetNegativeButton ("No", (IDialogInterfaceOnClickListener)null);

					AlertDialog dialog = builder.Create ();
					dialog.Show ();
				};
			}
			catch(Exception ex) 
			{
				Logger.LogThis (ex.Message, "OnViewSourcesClicked", "MainActivity");
			}

			listViewRSS.ItemClick += ListViewItemClick;
			return true;
		}

		private bool OnDeleteAllSourcesCliked (IMenuItem menuItem)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder (this);
			builder.SetTitle ("Delete all sources...Are you sure?");
			builder.SetPositiveButton ("Yes", (sender, args) => DeleteAllEntriesRSSSources ());
			builder.SetNegativeButton ("No", (IDialogInterfaceOnClickListener)null);

			AlertDialog dialog = builder.Create ();
			dialog.Show ();
			return true;
		}

		private bool OnImportSourcesClicked (IMenuItem menuItem)
		{
			ReadDefaultListOfFeeds ();
			Toast.MakeText (this, "Feeds imported", ToastLength.Short);
			SetContentView (Resource.Layout.Main);
			return true;
		}

		private bool OnGetFeedClicked (IMenuItem menuItem)
		{
			Logger.LogThis ("Trying to get feeds", "OnGetFeedClicked", "MainActivity");

			SetProgressBarIndeterminateVisibility (true);
			rssUtil.Urls = rssSourcesList.Select (x => x.Url).ToList ();
			rssUtil.bgndWorker.RunWorkerAsync ();

			opType = OpType.Feeds;

			listViewRSS.ItemClick += ListViewItemClick;
			return true;
		}

		private void DeleteSelectedSources()
		{
			var sparseArray = listViewRSS.CheckedItemPositions;
			for (var i = 0; i < sparseArray.Size(); i++ )
			{
				DeleteEntryRSSSources ( rssSourcesList [i].ID);
			}
			LoadRSSSources ();
			OnViewSourcesClicked (null);
		}

		private void ListViewItemClick (object sender, AdapterView.ItemClickEventArgs ea)
		{

			switch (opType) {
			case OpType.Feeds:
				var link = String.Empty;
				try {
					rssFeeds clickedItem = rssUtil.RssFeeds [ea.Position]; 
					link = rssUtil.RssFeeds.FirstOrDefault (x => x.Title == clickedItem.Title).Link;

					if (link != String.Empty) {
						var uri = Android.Net.Uri.Parse (link);
						var intent = new Intent (Intent.ActionView, uri); 
						StartActivity (intent); 
					}
				} catch (Exception ex) {
					Logger.LogThis (ex.Message + Environment.NewLine + link, "OpenFeed", "MainActivity");
				}
				//todo open webpage
				break;
			case OpType.Sources:
				//var entry = (RSSSources)FindEntryRSSSources (rssSourcesList[ea.Position]);
				var entry = rssSourcesList [ea.Position];
				//rssUtil.rss
				AlertDialog.Builder builder = CreateDialog ("Edit source", entry);
				builder.SetPositiveButton ("Delete", (snd, args) => {
					DeleteEntryRSSSources (entry.ID);
					LoadRSSSources ();
					OnViewSourcesClicked (null);
				});

				builder.SetNegativeButton ("Save", (snd, args) => {
					//todo verifica date
					entry.Description = descriptionInput.Text;
					entry.Name = nameInput.Text;
					entry.Url = urlInput.Text;

					UpdateEntryRSSSources (entry);
					LoadRSSSources ();
					OnViewSourcesClicked (null);
				});

				AlertDialog dialog = builder.Create ();
				dialog.Show ();
				break;
			default:
				break;
			}


		}

		private AlertDialog.Builder CreateDialog (string title, RSSSources _source)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder (this);
			builder.SetTitle (title);
			LinearLayout inputLayout = (LinearLayout)LayoutInflater.Inflate (Resource.Layout.NewRSSSourceEntry, null);
			nameInput = (EditText)inputLayout.FindViewById (Resource.Id.editTextName);
			nameInput.Text = _source.Name;
			urlInput = (EditText)inputLayout.FindViewById (Resource.Id.editTextURL);
			urlInput.Text = _source.Url;
			descriptionInput = (EditText)inputLayout.FindViewById (Resource.Id.editTextDescription);
			descriptionInput.Text = _source.Description;
			builder.SetView (inputLayout);

			return builder;
		}

	}
}


