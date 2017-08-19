using System;
using SQLite;

namespace Eyegregator
{

	public class RSSSources
	{
		[PrimaryKey,AutoIncrement]
		public int ID { get; set; }
		public string Name{ get; set;}
		public string Url { get; set; }
		public DateTime Date{ get; set; }
		public string Description { get; set; }

		public RSSSources ()
		{
		}
	}
}

