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

namespace Eyegregator.Controller
{
    public class RSSUtil
    {
		public BackgroundWorker bgndWorker = new BackgroundWorker();
		public XmlDocument doc = new XmlDocument();
		String streamResult;
		public List<string> Urls
		{ get; set ;}


		public List<rssFeeds> RssFeeds = new List<rssFeeds>();
		//public IDictionary<string,string> RssFeeds;


        public RSSUtil()
        {
			//if (RssFeeds == null)
			//	RssFeeds = new Dictionary<string, string> ();

			bgndWorker.DoWork += (sender, e) => 
			{
				if (RssFeeds.Any ())
					RssFeeds.Clear ();
				foreach (var _url in Urls) {		
					try {
						var httpReq = (HttpWebRequest)HttpWebRequest.Create (new Uri(_url));
						httpReq.ContentType = "application/xml";
						httpReq.Method = "GET";

						using (WebResponse response = httpReq.GetResponse()) {
							using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
								streamResult = reader.ReadToEnd ();	
								doc.LoadXml (streamResult);
								foreach (var item in doc.GetElementsByTagName("title").Cast<XmlNode>()) {
									RssFeeds.Add(new rssFeeds()
									             {
											Link = item.ParentNode["link"].InnerText == String.Empty ? item.ParentNode["link"].NextSibling.InnerText : item.ParentNode["link"].InnerText,
											Title = item.InnerText,
											ImgIcon = item.ParentNode["icon"].InnerText ?? String.Empty});
								}
							}
						}	
					} catch (Exception ex)
					{
					}
				}
			};
        }


    }
	public class rssFeeds
	{
		string link;
		public string Link{ get {return link;}  set{ this.link = value;}}

		string title;
		public string Title{ get {return title;}  set{ this.title = value;}}

		string imgIcon;

		public string ImgIcon {
			get {
				return imgIcon;
			}
			set {
				imgIcon = value;
			}
		}
	}

}