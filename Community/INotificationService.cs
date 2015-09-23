namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The interface to the notification sending service to the phone or e-mail.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/notificationservice.svc")]
	public interface INotificationService
	{
		/// <summary>
		/// To get the available number of SMS messages.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The available number of SMS-messages.</returns>
		[OperationContract]
		int GetSmsCount(Guid sessionId);

		/// <summary>
		/// To get the available number of email messages.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The available number of email messages.</returns>
		[OperationContract]
		int GetEmailCount(Guid sessionId);

		/// <summary>
		/// To send a SMS message.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="message">Message body.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendSms(Guid sessionId, string message);

		/// <summary>
		/// To send an email message.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="caption">The message title.</param>
		/// <param name="message">Message body.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendEmail(Guid sessionId, string caption, string message);

		/// <summary>
		/// To get the latest news.
		/// </summary>
		/// <param name="sessionId">Session ID. It can be empty if the request is anonymous.</param>
		/// <param name="fromId">The identifier from which you need to receive the news.</param>
		/// <returns>Last news.</returns>
		[OperationContract]
		CommunityNews[] GetNews(Guid sessionId, long fromId);
	}
}