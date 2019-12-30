#region Copyright and License
// -----------------------------------------------------------------------------------------------------------------
// 
// FacebookClient.cs
// 
// Copyright (c) 2012-2013 Constantin Titarenko, Andrew Semack and others
// 
// Copyright � 2019 Nivloc Enterprises Ltd.  All rights reserved.
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

namespace OAuth2.Client.Impl
	{
	/// <summary>
	/// Facebook authentication client.
	/// </summary>
	public class FacebookClient : OAuth2Client
		{
		/// <summary>
		/// Initializes a new instance of the <see cref="FacebookClient"/> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="configuration">The configuration.</param>
		public FacebookClient (IRequestFactory factory, IClientConfiguration configuration)
			: base (factory, configuration)
			{
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
					BaseUri = "https://www.facebook.com",
					Resource = "/dialog/oauth"
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
					BaseUri = "https://graph.facebook.com",
					Resource = "/oauth/access_token"
					};
				}
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
					BaseUri = "https://graph.facebook.com",
					Resource = "/me"
					};
				}
			}

		/// <summary>
		/// Called just before issuing request to third-party service when everything is ready.
	
		/// Allows to add extra parameters to request or do any other needed preparations.
		/// </summary>
		protected override void BeforeGetUserInfo (BeforeAfterRequestArgs args)
			{
			args.Request.AddParameter ("fields", "id,first_name,last_name,email,picture");
			}

		/// <summary>
		/// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
		/// </summary>
		/// <param name="content">The content which is received from third-party service.</param>
		protected override UserInfo ParseUserInfo (string content)
			{
			var response = JObject.Parse (content);
			const string avatarUriTemplate = "{0}?type={1}";
			var avatarUri = response["picture"]["data"]["url"].Value<string> ();
			return new UserInfo
				{
				Id = response["id"].Value<string> (),
				FirstName = response["first_name"].Value<string> (),
				LastName = response["last_name"].Value<string> (),
				Email = response["email"].SafeGet (x => x.Value<string> ()),
				AvatarUri =
					{
					Small = !StringEx.IsNullOrWhiteSpace (avatarUri) ? string.Format (avatarUriTemplate, avatarUri, "small") : string.Empty,
					Normal = !StringEx.IsNullOrWhiteSpace (avatarUri) ? string.Format (avatarUriTemplate, avatarUri, "normal") : string.Empty,
					Large = !StringEx.IsNullOrWhiteSpace (avatarUri) ? string.Format (avatarUriTemplate, avatarUri, "large") : string.Empty
					}
				};
			}

		/// <summary>
		/// Friendly name of provider (OAuth2 service).
		/// </summary>
		public override string Name
			{
			get { return "Facebook"; }
			}
		}
	}