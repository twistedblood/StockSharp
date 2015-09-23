namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;
	using System.ServiceModel;

	/// <summary>
	/// The profile Information.
	/// </summary>
	[DataContract]
	public class Profile
	{
		/// <summary>
		/// Login.
		/// </summary>
		[DataMember]
		public string Login { get; set; }

		/// <summary>
		/// Password (not filled in when obtaining from the server).
		/// </summary>
		[DataMember]
		public string Password { get; set; }

		/// <summary>
		/// E-mail address.
		/// </summary>
		[DataMember]
		public string Email { get; set; }

		/// <summary>
		/// Phone.
		/// </summary>
		[DataMember]
		public string Phone { get; set; }

		/// <summary>
		/// Web site.
		/// </summary>
		[DataMember]
		public string Homepage { get; set; }

		/// <summary>
		/// Skype.
		/// </summary>
		[DataMember]
		public string Skype { get; set; }

		/// <summary>
		/// City.
		/// </summary>
		[DataMember]
		public string City { get; set; }

		/// <summary>
		/// Gender.
		/// </summary>
		[DataMember]
		public bool? Gender { get; set; }

		/// <summary>
		/// Is the mailout enabled.
		/// </summary>
		[DataMember]
		public bool IsSubscription { get; set; }
	}

	/// <summary>
	/// The interface describing the registration service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/registrationservice.svc")]
	public interface IProfileService
	{
		/// <summary>
		/// To start the registration.
		/// </summary>
		/// <param name="profile">The profile Information.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte CreateProfile(Profile profile);

		/// <summary>
		/// To send an e-mail message.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendEmail(string email, string login);

		/// <summary>
		/// To confirm the e-mail address.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <param name="emailCode">The e-mail confirmation code.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte ValidateEmail(string email, string login, string emailCode);

		/// <summary>
		/// To send SMS.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <param name="phone">Phone.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte SendSms(string email, string login, string phone);

		/// <summary>
		/// To confirm the phone number.
		/// </summary>
		/// <param name="email">E-mail address.</param>
		/// <param name="login">Login.</param>
		/// <param name="smsCode">SMS verification code.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte ValidatePhone(string email, string login, string smsCode);

		/// <summary>
		/// To update profile information.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="profile">The profile Information.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UpdateProfile(Guid sessionId, Profile profile);

		/// <summary>
		/// To get profile information.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The profile Information.</returns>
		[OperationContract]
		Profile GetProfile(Guid sessionId);

		/// <summary>
		/// To update the profile photo.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="fileName">The file name.</param>
		/// <param name="body">The contents of the image file.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte UpdateAvatar(Guid sessionId, string fileName, byte[] body);

		/// <summary>
		/// To get a profile photo.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>The contents of the image file.</returns>
		[OperationContract]
		byte[] GetAvatar(Guid sessionId);
	}
}