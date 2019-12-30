﻿#region Copyright and License
// -----------------------------------------------------------------------------------------------------------------
// 
// OAuthClient.cs
// 
// Copyright © 2019 Nivloc Enterprises Ltd.  All rights reserved.
// 
// -----------------------------------------------------------------------------------------------------------------
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// 
// 
// 
#endregion
using System;
using System.Collections.Specialized;
#if ASYNC
using System.Threading;
using System.Threading.Tasks;
#endif
using SSMono.Web;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace OAuth2.Client
	{
	/// <summary>
	/// Base class for OAuth (version 1) client implementation.
	/// </summary>
	public abstract class OAuthClient : IClient
		{
		private const string OAuthTokenKey = "oauth_token";
		private const string OAuthTokenSecretKey = "oauth_token_secret";

		private readonly IRequestFactory _factory;

		/// <summary>
		/// Client configuration object.
		/// </summary>
		public IClientConfiguration Configuration { get; private set; }

		/// <summary>
		/// Friendly name of provider (OAuth service).
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// State which was posted as additional parameter
		/// to service and then received along with main answer.
		/// </summary>
		public string State
			{
			get { return null; }
			}

		/// <summary>
		/// Access token received from service. Can be used for further service API calls.
		/// </summary>
		public string AccessToken { get; private set; }

		/// <summary>
		/// Access token secret received from service. Can be used for further service API calls.
		/// </summary>
		public string AccessTokenSecret { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthClient" /> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="configuration">The configuration.</param>
		protected OAuthClient (IRequestFactory factory, IClientConfiguration configuration)
			{
			_factory = factory;
			Configuration = configuration;
			}

#if NETCF
		/// <summary>
		/// Returns URI of service which should be called in order to start authentication process.
		/// You should use this URI when rendering login link.
		/// </summary>
		/// <returns>Login link URI.</returns>
		public string GetLoginLinkUri ()
			{
			return GetLoginLinkUri(null);
			}
#endif

		/// <summary>
		/// Returns URI of service which should be called in order to start authentication process.
		/// You should use this URI when rendering login link.
		/// </summary>
		/// <param name="state">Any additional information needed by application.</param>
		/// <returns>Login link URI.</returns>
		public string GetLoginLinkUri (string state)
			{
			if (!state.IsEmpty ())
				{
				throw new NotSupportedException ("State transmission is not supported by current implementation.");
				}
			QueryRequestToken ();
			return GetLoginRequestUri (state);
			}

		/// <summary>
		/// Obtains user information using third-party authentication service
		/// using data provided via callback request.
		/// </summary>
		/// <param name="parameters">Callback request payload (parameters).
		/// <example>Request.QueryString</example></param>
		/// <returns></returns>
		public UserInfo GetUserInfo (NameValueCollection parameters)
			{
			AccessToken = parameters.GetOrThrowUnexpectedResponse (OAuthTokenKey);
			QueryAccessToken (parameters.GetOrThrowUnexpectedResponse ("oauth_verifier"));

			var result = ParseUserInfo (QueryUserInfo ());
			result.ProviderName = Name;

			return result;
			}

		/// <summary>
		/// Defines URI of service which is called for obtaining request token.
		/// </summary>
		protected abstract Endpoint RequestTokenServiceEndpoint { get; }

		/// <summary>
		/// Defines URI of service which should be called to initiate authentication process.
		/// </summary>
		protected abstract Endpoint LoginServiceEndpoint { get; }

		/// <summary>
		/// Defines URI of service which issues access token.
		/// </summary>
		protected abstract Endpoint AccessTokenServiceEndpoint { get; }

		/// <summary>
		/// Defines URI of service which is called to obtain user information.
		/// </summary>
		protected abstract Endpoint UserInfoServiceEndpoint { get; }

		/// <summary>
		/// Should return parsed <see cref="UserInfo"/> using content of callback issued by service.
		/// </summary>
		protected abstract UserInfo ParseUserInfo (string content);

		/// <summary>
		/// Issues request for request token and returns result.
		/// </summary>
		private void QueryRequestToken ()
			{
			var client = _factory.CreateClient (RequestTokenServiceEndpoint);
			client.Authenticator = OAuth1Authenticator.ForRequestToken (
				Configuration.ClientId, Configuration.ClientSecret, Configuration.RedirectUri);

			var request = _factory.CreateRequest (RequestTokenServiceEndpoint, Method.POST);

			BeforeGetAccessToken (new BeforeAfterRequestArgs
				{
				Client = client,
				Request = request,
				Configuration = Configuration
				});

			var response = client.ExecuteAndVerify (request);

			AfterGetAccessToken (new BeforeAfterRequestArgs
				{
				Response = response,
				});

			var collection = HttpUtility.ParseQueryString (response.Content);

			AccessToken = collection.GetOrThrowUnexpectedResponse (OAuthTokenKey);
			AccessTokenSecret = collection.GetOrThrowUnexpectedResponse (OAuthTokenSecretKey);
			}

#if NETCF
		/// <summary>
		/// Composes login link URI.
		/// </summary>
		private string GetLoginRequestUri ()
			{
			return GetLoginLinkUri(null);
			}
#endif

		/// <summary>
		/// Composes login link URI.
		/// </summary>
		/// <param name="state">Any additional information needed by application.</param>
		private string GetLoginRequestUri (string state)
			{
			var client = _factory.CreateClient (LoginServiceEndpoint);
			var request = _factory.CreateRequest (LoginServiceEndpoint);

			request.AddParameter (OAuthTokenKey, AccessToken);
			if (!state.IsEmpty ())
				{
				request.AddParameter ("state", state);
				}

			return client.BuildUri (request).ToString ();
			}

		/// <summary>
		/// Obtains access token by calling corresponding service.
		/// </summary>
		/// <param name="verifier">Verifier posted with callback issued by provider.</param>
		/// <returns>Access token and other extra info.</returns>
		private void QueryAccessToken (string verifier)
			{
			var client = _factory.CreateClient (AccessTokenServiceEndpoint);
			client.Authenticator = OAuth1Authenticator.ForAccessToken (
				Configuration.ClientId, Configuration.ClientSecret, AccessToken, AccessTokenSecret, verifier);

			var request = _factory.CreateRequest (AccessTokenServiceEndpoint, Method.POST);

			var response = client.ExecuteAndVerify (request);
			HandleTokenResponse (response);
			}

#if ASYNC
		/// <summary>
		/// Obtains access token by calling corresponding service.
		/// </summary>
		/// <param name="verifier">Verifier posted with callback issued by provider.</param>
		/// <param name="cancellationToken">Optional cancellation token</param>
		/// <returns>Access token and other extra info.</returns>
		private async Task QueryAccessTokenAsync (string verifier, CancellationToken cancellationToken = default)
			{
			var client = _factory.CreateClient (AccessTokenServiceEndpoint);
			client.Authenticator = OAuth1Authenticator.ForAccessToken (
				Configuration.ClientId, Configuration.ClientSecret, AccessToken, AccessTokenSecret, verifier);

			var request = _factory.CreateRequest (AccessTokenServiceEndpoint, Method.POST);

			var response = await client.ExecuteAndVerifyAsync (request, cancellationToken).ConfigureAwait (false);
			HandleTokenResponse (response);
			}
#endif

		private void HandleTokenResponse (IRestResponse response)
			{
			var content = response.Content;
			var collection = HttpUtility.ParseQueryString (content);

			AccessToken = collection.GetOrThrowUnexpectedResponse (OAuthTokenKey);
			AccessTokenSecret = collection.GetOrThrowUnexpectedResponse (OAuthTokenSecretKey);
			}

		protected virtual void BeforeGetAccessToken (BeforeAfterRequestArgs args)
			{
			}

		/// <summary>
		/// Called just after obtaining response with access token from service.
		/// Allows to read extra data returned along with access token.
		/// </summary>
		protected virtual void AfterGetAccessToken (BeforeAfterRequestArgs args)
			{
			}

		/// <summary>
		/// Called just before issuing request to service when everything is ready.
		/// Allows to add extra parameters to request or do any other needed preparations.
		/// </summary>
		protected virtual void BeforeGetUserInfo (BeforeAfterRequestArgs args)
			{
			}

		/// <summary>
		/// Queries user info using corresponding service and data received by access token request.
		/// </summary>
		private string QueryUserInfo ()
			{
			var client = _factory.CreateClient (UserInfoServiceEndpoint);
			client.Authenticator = OAuth1Authenticator.ForProtectedResource (
				Configuration.ClientId, Configuration.ClientSecret, AccessToken, AccessTokenSecret);

			var request = _factory.CreateRequest (UserInfoServiceEndpoint);

			BeforeGetUserInfo (new BeforeAfterRequestArgs
				{
				Client = client,
				Request = request,
				Configuration = Configuration
				});

			return client.ExecuteAndVerify (request).Content;
			}

#if ASYNC
		/// <inheritdoc />
		public async Task<UserInfo> GetUserInfoAsync (NameValueCollection parameters, CancellationToken cancellationToken = default)
			{
			AccessToken = parameters.GetOrThrowUnexpectedResponse (OAuthTokenKey);
			await QueryAccessTokenAsync (parameters.GetOrThrowUnexpectedResponse ("oauth_verifier"), cancellationToken).ConfigureAwait (false);

			var result = ParseUserInfo (QueryUserInfo ());
			result.ProviderName = Name;

			return result;
			}
#endif
		}
	}