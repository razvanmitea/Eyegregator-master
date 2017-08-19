using System;
using Android.Widget;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Eyegregator.Controller;
using Android.Graphics;
using System.Net;
using UrlImageViewHelper;
using Android.Text;

namespace Eyegregator
{
	public class CustomAdapterRSS : BaseAdapter<rssFeeds> {
		List<rssFeeds> items;
		Activity context;
		public CustomAdapterRSS(Activity context, List<rssFeeds> items)
			: base()
		{
			this.context = context;
			this.items = items;
		}
		public override long GetItemId(int position)
		{
			return position;
		}
		public override rssFeeds this[int position]
		{
			get { return items[position]; }
		}
		public override int Count
		{
			get { return items.Count; }
		}
		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var item = items[position];
			View view = convertView;
			if (view == null) // no view to re-use, create new
				view = context.LayoutInflater.Inflate(Resource.Layout.ListItemRow, null);


			view.FindViewById<TextView>(Resource.Id.Title).Text = item.Title;
			if (!item.IsHeader) {
				view.FindViewById<TextView>(Resource.Id.Description).TextFormatted = Html.FromHtml(item.Description);
				view.FindViewById<ImageView> (Resource.Id.Thumbnail).SetUrlDrawable (item.ImgIcon, Resource.Drawable.Icon);
			} 
			else {
				view.FindViewById<TextView> (Resource.Id.Title).SetTextSize (Android.Util.ComplexUnitType.Dip, 20);
				view.FindViewById<ImageView> (Resource.Id.Thumbnail).SetUrlDrawable (item.ImgIcon);
				view.SetBackgroundColor (Color.SteelBlue);
			}

			return view;
		}

	}
}

