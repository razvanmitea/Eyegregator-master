using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Eyegregator.Controller;
using System.Net;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Eyegregator.Controller
{
	public class RSSUtil
	{
		public BackgroundWorker bgndWorker = new BackgroundWorker ();
		public XmlDocument doc = new XmlDocument ();
		String streamResult;

		public List<string> Urls
		{ get; set ; }
        
		public List<rssFeeds> RssFeeds = new List<rssFeeds> ();

		public RSSUtil ()
		{

			bgndWorker.DoWork += (sender, e) => {
				if (RssFeeds.Any ())
					RssFeeds.Clear ();
				foreach (var _url in Urls) {		
					try {
						var httpReq = (HttpWebRequest)HttpWebRequest.Create (new Uri (_url));
						httpReq.ContentType = "application/xml";
						httpReq.Method = "GET";

						using (WebResponse response = httpReq.GetResponse ()) {
							using (StreamReader reader = new StreamReader (response.GetResponseStream ())) {
								streamResult = reader.ReadToEnd ();	
								doc.LoadXml (streamResult);
								foreach (var item in doc.GetElementsByTagName("title").Cast<XmlNode>()) {
									var newFeed = GetNewFeed (item);
									if (newFeed != null)
										RssFeeds.Add (newFeed);
								}
							}
						}	
					} catch (Exception ex) {
						Logger.LogThis (ex.Message, "Background Worker", "GetRSS");
					}
				}
			};
		}

		private rssFeeds GetNewFeed (XmlNode item)
		{
			try {
				rssFeeds newFeed = new rssFeeds ();
				var date = GetDate (item);
				var description = GetDescription (item);
				if (date == new DateTime ().ToString ())
					date = String.Empty;
				newFeed.Link = item.ParentNode ["link"].InnerText == String.Empty ? item.ParentNode ["link"].NextSibling.InnerText : item.ParentNode ["link"].InnerText;
				newFeed.Title = item.InnerText;
				newFeed.Description = "@" + date + System.Environment.NewLine + description;
				//newFeed.UpdateDate = GetDate (item);
				newFeed.ImgIcon = GetImgLink (item);
				//Logger.LogThis("Image link:" + newFeed.ImgIcon, "GetNewFeed", "GetRSS");

				// if are equal (assume they are empty) then we got the Header
				newFeed.IsHeader = String.IsNullOrEmpty(description) && String.IsNullOrEmpty(newFeed.ImgIcon);

				return newFeed;
			} catch (Exception ex) {
				Logger.LogThis (ex.Message + System.Environment.NewLine + item.InnerText, "GetNewFeed", "GetRSS");
			}
			return null;
		}

		private string GetDescription (XmlNode item)
		{
			var description = String.Empty;
			try {
				if (item.ParentNode ["description"] != null) {
					description = item.ParentNode ["description"].InnerText;
				} else {
					var content = item.ParentNode ["content"].InnerText;
					if (!String.IsNullOrEmpty (content)) {
						var paragraphs = Regex.Split (content, "<p>");
						if (paragraphs.Any ()) {
							if (paragraphs.Length > 1) {
								description = paragraphs.Skip (1).First ();
								//Logger.LogThis ("Description (with skip 1): " + description, "GetDescription", "GetRSS");
							} else {
								description = paragraphs.First ();
								//Logger.LogThis ("Description (first): " + description, "GetDescription", "GetRSS");
							}
						}
					}
				}
			} catch (Exception ex) {
				Logger.LogThis (ex.Message, "GetDescription", "GetRSS");
			}
			
			return description.Replace ("</p>", String.Empty);
		}

		private string GetDate (XmlNode item)
		{
			DateTime date = new DateTime ();
			string dateAsString = String.Empty;
			try {
				foreach (XmlNode node in item.ParentNode.ChildNodes) {
					if (node.Name == "updated") {
						if (DateTime.TryParse (node.InnerText, out date))
							dateAsString = date.ToString ();
					} else if (node.Name == "published") {
						if (DateTime.TryParse (node.InnerText, out date))
							dateAsString = date.ToString ();
					} else if (node.Name == "pubDate") {
						if (DateTime.TryParse (node.InnerText, out date))
							dateAsString = date.ToString ();
					}
				}
			} catch (Exception ex) {
				Logger.LogThis (ex.Message, "GetDate", "GetRSS");
			}
			return dateAsString;
		}

		private string GetImgLink (XmlNode item)
		{
			var imgLink = String.Empty;
			var content = String.Empty;
			try {
				if (item.ParentNode ["content"] != null) {
					content = item.ParentNode ["content"].InnerText;
					if (!String.IsNullOrEmpty (content)) {
						imgLink = Regex.Match (content, "<img.+?src=[\"'](.+?)[\"'].*?>", RegexOptions.IgnoreCase).Groups [1].Value;
					}
				}
			} catch (Exception ex) {
				Logger.LogThis (ex.Message + System.Environment.NewLine + "Content is:" + content, "GetImgLink", "GetRSS");
			}
			return imgLink;
		}
	}

	public class rssFeeds
	{
		string link;

		public string Link{ get { return link; } set { this.link = value; } }

		string title;

		public string Title{ get { return title; } set { this.title = value; } }

		string imgIcon;

		public string ImgIcon {
			get {
				return imgIcon;
			}
			set {
				imgIcon = value;
			}
		}

		string description;

		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}


		string updateDate;

		public string UpdateDate {
			get {
				return updateDate;
			}
			set {
				updateDate = value;
			}
		}

		bool isHeader;

		public bool IsHeader {
			get {
				return isHeader;
			}
			set {
				isHeader = value;
			}
		}

		public rssFeeds ()
		{
			
		}
	}

}