namespace RESTfulFoundation
{
	// ReSharper disable once InconsistentNaming
	public class RESTObject
	{
		public RESTConnection? Connection { get; set; } = null;
		protected RESTObject()
		{
		}
		public RESTObject(RESTConnection connection)
		{
			Connection = connection;
		}
	}
}

