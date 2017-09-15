﻿using System;
using System.Net;
using System.Threading.Tasks;

namespace HSReplay.OAuth
{
	internal class CallbackListener : IDisposable
	{
		public string RedirectUrl { get; }
		private static HttpListener _listener;

		private CallbackListener(HttpListener listener, string redirectUrl)
		{
			RedirectUrl = redirectUrl;
			_listener = listener;
		}

		public void Dispose()
		{
			_listener.Abort();
			GC.SuppressFinalize(this);
		}

		~CallbackListener()
		{
			_listener.Abort();
		}

		public async Task<string> Listen(string expectedState, string successUrl, string errorUrl)
		{
			using(_listener)
			{
				while(_listener.IsListening)
				{
					var context = await _listener.GetContextAsync();
					var state = context.Request.QueryString.Get("state");
					var code = context.Request.QueryString.Get("code");
					var valid = state == expectedState && !string.IsNullOrEmpty(code);
					context.Response.Redirect(valid ? successUrl : errorUrl);
					context.Response.Close();
					_listener.Stop();
					_listener.Close();
					return valid ? code : null;
				}
				return null;
			}
		}

		public static CallbackListener Create(int[] ports)
		{
			foreach(var port in ports)
			{
				var url = $"http://127.0.0.1:{port}/";
				var listener = TryGetListener(url);
				if(listener != null)
					return new CallbackListener(listener, url);
			}
			return null;
		}

		private static HttpListener TryGetListener(string url)
		{
			var listener = new HttpListener();
			listener.Prefixes.Add(url);
			try
			{
				listener.Start();
				return listener;
			}
			catch(Exception)
			{
				return null;
			}
		}
	}
}
