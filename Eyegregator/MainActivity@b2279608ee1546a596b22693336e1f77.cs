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



namespace Eyegregator
{
	[Activity (Label = "Eyegregator", MainLauncher = true)]
	public class RSSActivity : Activity
	{
		RSSUtil rssUtil = new RSSUtil();
		internal static SQLiteAsyncConnection DB;
		private readonly TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		List<RSSSources> rssSourcesList = new List<RSSSources>();
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

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			rssUtil.bgndWorker.RunWorkerCompleted += (sender, e) => 
			{

				//todo clear list
				listViewRSS.Adapter = new ArrayAdapter<string> (this, Android.Resource.Layout.ActivityListItem, 
				                                                rssUtil.RssFeeds.Select(x=>x.Title).ToArray());


				SetProgressBarIndeterminateVisibility (false);
				opType = OpType.Feeds;
			};

			RequestWindowFeature (WindowFeatures.ActionBar);
			RequestWindowFeature (WindowFeatures.Progress);
			RequestWindowFeature (WindowFeatures.IndeterminateProgress);

			SetProgressBarIndeterminate (true);
			SetProgressBarIndeterminateVisibility (true);

			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
			ActionBar.Title = "Eyegregator";
			ActionBar.SetDisplayShowTitleEnabled (true);

			CreateDB ();

			SetContentView (Resource.Layout.Main);
			listViewRSS = (ListView)FindViewById (Resource.Id.listView);
			listViewRSS.Clickable = true;
		}

		private void CreateDB()
		{
			DB = new SQLiteAsyncConnection (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "eyegregator.db"));
			DB.CreateTableAsync<RSSSources> ().ContinueWith (t=>LoadRSSSources(), uiScheduler);
		}
		private string GetUrl(RSSSources rss)
		{
			return rss.Url;
		}

		private object FindEntryRSSSources(string url)
		{
			SetProgressBarIndeterminateVisibility (true);
			var entry = new RSSSources();
			DB.QueryAsync<RSSSources> ("SELECT * FROM RSSSources where URL like '"+url+"'").ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
				entry = t.Result.Select(x=>x).First();
			}, uiScheduler);
			return entry;

		}
		#region DB CRUD

		private void LoadRSSSources()
		{
			SetProgressBarIndeterminateVisibility (true);

			DB.QueryAsync<RSSSources> ("SELECT * FROM RSSSources").ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
				rssSourcesList.Clear();
				rssSourcesList = t.Result.Select(x=>x).ToList();
			}, uiScheduler);
		}

		private void InsertNewEntryRSSSources(string name, string url, string description)
		{
			SetProgressBarIndeterminateVisibility (true);
			url = url.Contains("http://") ? url : String.Format ("{0}" + url, "http://");
			var newRSS = new RSSSources { Name = name, Url = url, Description = description, Date = DateTime.Now };
			DB.InsertAsync (newRSS).ContinueWith (t => { 
				SetProgressBarIndeterminateVisibility(false);
				LoadRSSSources();
			}, uiScheduler);
		}

		private void DeleteEntryRSSSources(int idEntry)
		{
			SetProgressBarIndeterminateVisibility (true);
			DB.QueryAsync<RSSSources> ("Delete FROM RSSSources where ID = " + idEntry).ContinueWith (t => {
				SetProgressBarIndeterminateVisibility (false);
			}, uiScheduler);
		}

		private void UpdateEntryRSSSources(RSSSources _entry)
		{
			DB.UpdateAsync (_entry).ContinueWith (t => { 
				SetProgressBarIndeterminateVisibility(false);
				LoadRSSSources();
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
			builder.SetPositiveButton ("Create", (sender, args) => InsertNewEntryRSSSources (nameInput.Text, urlInput.Text,descriptionInput.Text));
			builder.SetNegativeButton ("Cancel", (IDialogInterfaceOnClickListener)null);

			AlertDialog dialog = builder.Create();
			dialog.Show();

			return true;
		}

		private bool OnViewSourcesClicked (IMenuItem menuItem)
		{
			SetContentView(Resource.Layout.Main);
			listViewRSS = (ListView)FindViewById(Resource.Id.listView);
			listViewRSS.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, 
			                                               rssSourcesList.Select(x=>x.Name).ToArray());
			opType = OpType.Sources;
			
			listViewRSS.ItemClick += ListViewItemClick;
			return true;
		}

		private bool OnGetFeedClicked (IMenuItem menuItem)
		{
			SetProgressBarIndeterminateVisibility (true);
			rssUtil.Urls = rssSourcesList.Select(x=>x.Url).ToList();
			rssUtil.bgndWorker.RunWorkerAsync ();

			opType = OpType.Feeds;

			listViewRSS.ItemClick += ListViewItemClick;
			return true;
		}

		private void ListViewItemClick(object sender,  AdapterView.ItemClickEventArgs ea)
		{

			switch (opType) {
			case OpType.Feeds:
				var clickedItem = listViewRSS.Adapter.GetItem (ea.Position).ToString ();
				//var link = rssUtil.RssFeeds.FirstOrDefault (x=>x.Value == clickedItem).Key;
				var link = rssUtil.RssFeeds.FirstOrDefault (x=>x.Title == clickedItem).Link;
				if(link != String.Empty)
				{
					var uri = Android.Net.Uri.Parse (link);
					var intent = new Intent (Intent.ActionView, uri); 
					StartActivity (intent); 
				}
				//todo open webpage
				break;
			case OpType.Sources:
				//var entry = (RSSSources)FindEntryRSSSources (rssSourcesList[ea.Position]);
				var entry = rssSourcesList [ea.Position];
				AlertDialog.Builder builder = CreateDialog ("Edit source", entry);
				builder.SetPositiveButton ("Delete", (snd, args) => {
					DeleteEntryRSSSources (entry.ID);
					LoadRSSSources ();
					OnViewSourcesClicked(null);
				});

				builder.SetNegativeButton ("Save", (snd, args) => {
					//todo verifica date
					entry.Description = descriptionInput.Text;
					entry.Name = nameInput.Text;
					entry.Url = urlInput.Text;

					UpdateEntryRSSSources (entry);
					LoadRSSSources ();
					OnViewSourcesClicked(null);
				});

				AlertDialog dialog = builder.Create ();
				dialog.Show ();
				break;
			default:
				break;
			}


		}

		private AlertDialog.Builder CreateDialog(string title, RSSSources _source)
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


