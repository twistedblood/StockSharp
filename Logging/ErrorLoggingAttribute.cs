namespace StockSharp.Logging
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.ServiceModel;
	using System.ServiceModel.Channels;
	using System.ServiceModel.Description;
	using System.ServiceModel.Dispatcher;

	/// <summary>
	/// The attribute for the WCF server that automatically records all errors to <see cref="LoggingHelper.LogError"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ErrorLoggingAttribute : Attribute, IServiceBehavior
	{
		private sealed class ErrorHandler : IErrorHandler
		{
			private ErrorHandler()
			{
			}

			private static readonly Lazy<ErrorHandler> _instance = new Lazy<ErrorHandler>(() => new ErrorHandler());

			public static ErrorHandler Instance
			{
				get { return _instance.Value; }
			}

			public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
			{
			}

			public bool HandleError(Exception error)
			{
				error.LogError();
				return true;
			}
		}

		void IServiceBehavior.Validate(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
		}

		void IServiceBehavior.AddBindingParameters(ServiceDescription description, ServiceHostBase serviceHostBase,
												   Collection<ServiceEndpoint> endpoints,
												   BindingParameterCollection parameters)
		{
		}

		void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
			foreach (var channelDispatcher in serviceHostBase.ChannelDispatchers.Cast<ChannelDispatcher>())
				channelDispatcher.ErrorHandlers.Add(ErrorHandler.Instance);
		}
	}
}