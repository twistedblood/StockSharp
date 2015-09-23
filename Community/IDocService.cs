namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;
	using System.ServiceModel;

	/// <summary>
	/// Products.
	/// </summary>
	[DataContract]
	public enum Products
	{
		/// <summary>
		/// S#.API.
		/// </summary>
		[EnumMember]
		Api,

		/// <summary>
		/// S#.Data.
		/// </summary>
		[EnumMember]
		Hydra,

		/// <summary>
		/// S#.Studio.
		/// </summary>
		[EnumMember]
		Studio,

		/// <summary>
		/// S#.Server.
		/// </summary>
		[EnumMember]
		Server,

		/// <summary>
		/// S#.StrategyRunner.
		/// </summary>
		[EnumMember]
		StrategyRunner
	}

	/// <summary>
	/// The interface describing the documentation service.
	/// </summary>
	[ServiceContract]
	public interface IDocService
	{
		/// <summary>
		/// To get child pages.
		/// </summary>
		/// <param name="parentUrl">The query string of the parent page.</param>
		/// <returns>Child pages. If no pages the <see langword="null" /> will be returned.</returns>
		[OperationContract]
		DocPage[] GetChildPages(string parentUrl);

		/// <summary>
		/// To get the body of the page.
		/// </summary>
		/// <param name="url">The page query string.</param>
		/// <returns>The body of the page.</returns>
		[OperationContract]
		string GetContentBody(string url);

		/// <summary>
		/// To download new documentation.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="pages">New pages of documentation.</param>
		[OperationContract]
		void Upload(Guid sessionId, DocPage[] pages);

		/// <summary>
		/// To download the new version description.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="version">New version.</param>
		/// <param name="product">Product type.</param>
		/// <param name="description">New version description.</param>
		[OperationContract]
		void PostNewVersion(Guid sessionId, Products product, string version, string description);
	}
}