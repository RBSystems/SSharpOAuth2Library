﻿#region Copyright and License
// -----------------------------------------------------------------------------------------------------------------
// 
// UberClient.cs
// 
// Copyright (c) 2012-2013 Constantin Titarenko, Andrew Semack and others
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
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
	{
	/// <summary>
	/// Uber authentication client
	/// </summary>
	public class UberClient : OAuth2Client
		{
		/// <summary>
		/// Initializes a new instance of the <see cref="UberClient"/> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="configuration">The configuration.</param>
		public UberClient (IRequestFactory factory, IClientConfiguration configuration)
			: base (factory, configuration)
			{
			}

		/// <summary>
		/// The provider name
		/// </summary>
		public override string Name
			{
			get { return "Uber"; }
			}

		/// <summary>
		/// Defines URI of service which issues access code.
		/// </summary>
		protected override Endpoint AccessCodeServiceEndpoint
			{
			get
				{
				return new Endpoint
					{
					BaseUri = "https://login.uber.com",
					Resource = "/oauth/v2/authorize"
					};
				}
			}

		/// <summary>
		/// Defines URI of service which issues access token.
		/// </summary>
		protected override Endpoint AccessTokenServiceEndpoint
			{
			get
				{
				return new Endpoint
					{
					BaseUri = "https://login.uber.com",
					Resource = "/oauth/v2/token"
					};
				}
			}

		/// <summary>
		/// Called just before issuing request to third-party service when everything is ready.
		/// Allows to add extra parameters to request or do any other needed preparations.
		/// </summary>
		protected override void BeforeGetUserInfo (BeforeAfterRequestArgs args)
			{
			args.Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator (AccessToken, "Bearer");
			}

		/// <summary>
		/// Defines URI of service which allows to obtain information about user which is currently logged in.
		/// </summary>
		protected override Endpoint UserInfoServiceEndpoint
			{
			get
				{
				return new Endpoint
					{
					BaseUri = "https://api.uber.com",
					Resource = "/v1/me"
					};
				}
			}

		/// <summary>
		/// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
		/// </summary>
		/// <param name="content">The content which is received from third-party service.</param>
		protected override UserInfo ParseUserInfo (string content)
			{
			var response = JObject.Parse (content);
			var userInfo = new UserInfo ();
			JToken firstName;
			if (response.TryGetValue ("first_name", StringComparison.OrdinalIgnoreCase, out firstName))
				{
				userInfo.FirstName = firstName.ToString ();
				}

			JToken lastName;
			if (response.TryGetValue ("last_name", StringComparison.OrdinalIgnoreCase, out lastName))
				{
				userInfo.LastName = lastName.ToString ();
				}

			JToken email;
			if (response.TryGetValue ("email", StringComparison.OrdinalIgnoreCase, out email))
				{
				userInfo.Email = email.ToString ();
				}

			JToken picture;
			if (response.TryGetValue ("picture", StringComparison.OrdinalIgnoreCase, out picture))
				{
				var pictureUri = picture.ToString ();
				userInfo.AvatarUri.Small = pictureUri;
				userInfo.AvatarUri.Normal = pictureUri;
				userInfo.AvatarUri.Large = pictureUri;
				}

			return userInfo;
			}
		}
	}