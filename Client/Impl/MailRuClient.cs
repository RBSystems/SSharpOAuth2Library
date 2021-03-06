#region Copyright and License
// -----------------------------------------------------------------------------------------------------------------
// 
// MailRuClient.cs
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
using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
	{
	/// <summary>
	/// Mail.Ru authentication client.
	/// </summary>
	public class MailRuClient : OAuth2Client
		{
		private readonly IClientConfiguration _configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailRuClient"/> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="configuration">The configuration.</param>
		public MailRuClient (IRequestFactory factory, IClientConfiguration configuration)
			: base (factory, configuration)
			{
			_configuration = configuration;
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
					BaseUri = "https://connect.mail.ru",
					Resource = "/oauth/authorize"
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
					BaseUri = "https://connect.mail.ru",
					Resource = "/oauth/token"
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
					BaseUri = "http://www.appsmail.ru",
					Resource = "/platform/api"
					};
				}
			}

		/// <summary>
		/// Called just before issuing request to third-party service when everything is ready.
		/// Allows to add extra parameters to request or do any other needed preparations.
		/// </summary>
		protected override void BeforeGetUserInfo (BeforeAfterRequestArgs args)
			{
			// Source documents
			// http://api.mail.ru/docs/guides/restapi/
			// http://api.mail.ru/docs/reference/rest/users.getInfo/

			args.Request.AddParameter ("app_id", _configuration.ClientId);
			args.Request.AddParameter ("method", "users.getInfo");
			args.Request.AddParameter ("secure", "1");
			args.Request.AddParameter ("session_key", AccessToken);

			// workaround for current design, oauth_token is always present in URL, so we need emulate it for correct request signing 
			var fakeParam = new Parameter("oauth_token", AccessToken, ParameterType.GetOrPost);
			args.Request.AddParameter (fakeParam);

			//sign=hex_md5('app_id={client_id}method=users.getInfosecure=1session_key={access_token}{secret_key}')
			string signature = string.Concat (args.Request.Parameters.OrderBy (x => x.Name).Select (x => string.Format ("{0}={1}", x.Name, x.Value)).ToList ());
			signature = (signature + _configuration.ClientSecret).GetMd5Hash ();

			args.Request.Parameters.Remove (fakeParam);

			args.Request.AddParameter ("sig", signature);
			}

		/// <summary>
		/// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
		/// </summary>
		/// <param name="content">The content which is received from third-party service.</param>
		protected override UserInfo ParseUserInfo (string content)
			{
			var response = JArray.Parse (content);
			var avatarUri = response[0]["pic"].Value<string> ();
			return new UserInfo
				{
				Id = response[0]["uid"].Value<string> (),
				FirstName = response[0]["first_name"].Value<string> (),
				LastName = response[0]["last_name"].Value<string> (),
				Email = response[0]["email"].SafeGet (x => x.Value<string> ()),
				AvatarUri =
					{
					Small = null,
					Normal = avatarUri,
					Large = null
					}
				};
			}

		/// <summary>
		/// Friendly name of provider (OAuth2 service).
		/// </summary>
		public override string Name
			{
			get { return "MailRu"; }
			}
		}
	}