using System;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using IO = System.IO;
using System.Text.Json;

namespace MSGraph1
{

    public class ParameterJson
    {
        public string authority { get; set; }
        public string client_id { get; set; }
        public string[] scope { get; set; }
        public string username { get; set; }
        public string endpoint { get; set; }
    }

    /// <summary>
    /// Class which mantains the user session token and gives access to Microsoft Graph API
    /// </summary>
    public class AuthenticationService
    {


        /// <summary>
        /// Keeps the identity of the current user
        /// </summary>
        private readonly IPublicClientApplication _identityClientApp;

        private ParameterJson _identificationInfo;

        private ParameterJson LoadParameter(string parameterFilePath)
        {
            if (string.IsNullOrWhiteSpace(parameterFilePath))
            {
                parameterFilePath = "parameters.json";
            }

            if (!IO.File.Exists(parameterFilePath))
            {
                throw new Exception("We need parameters.json at least");
            }
            var jsonString = IO.File.ReadAllText(parameterFilePath);

            return JsonSerializer.Deserialize<ParameterJson>(jsonString);
        }

        public AuthenticationService(string parameterFilePath = null)
        {

            var idInfo = LoadParameter(parameterFilePath);
            if (idInfo == null)
            {
                throw new Exception("parameters.json is wrong format");
            }

            _identificationInfo = idInfo;

            _identityClientApp = PublicClientApplicationBuilder.Create(_identificationInfo.client_id).
                WithAuthority(new Uri(_identificationInfo.authority)).WithRedirectUri("http://localhost").Build();
            GraphClient = GetAuthenticatedClient();
        }


        /// <summary>
        /// Gets the token of the current user
        /// </summary>
        public string TokenForUser { get; private set; }

        /// <summary>
        /// Gets when current user's token expires
        /// </summary>
        public DateTimeOffset Expiration { get; private set; }

        /// <summary>
        /// Gets the client which allows to interact with the current user
        /// </summary>
        public GraphServiceClient GraphClient { get; private set; }

        /// <summary>
        /// Gets whether the user is logged in or not
        /// </summary>
        public bool IsConnected => TokenForUser != null && Expiration > DateTimeOffset.UtcNow.AddMinutes(1);

        /// <summary>
        /// Gets a new istance of <see cref="GraphServiceClient"/>
        /// </summary>
        /// <returns></returns>
        private GraphServiceClient GetAuthenticatedClient()
        {
            // Create Microsoft Graph client.
            return new GraphServiceClient(
                _identificationInfo.endpoint,
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        await AcquireTokenForUserAsync();
                        // Set bearer authentication on header
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", TokenForUser);
                    }));
        }


        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        private async Task AcquireTokenForUserAsync()
        {
            // Get an access token for the given context and resourceId. An attempt is first made to 
            // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
            var accounts = await _identityClientApp.GetAccountsAsync(_identificationInfo.username);
            if (accounts.Any())
            {
                try
                {
                    var authResult = await _identityClientApp.AcquireTokenSilent(_identificationInfo.scope, accounts.First()).ExecuteAsync(); 
                    
                    TokenForUser = authResult.AccessToken;
                    Expiration = authResult.ExpiresOn;
                    return;
                }
                catch (Exception)
                {
                    TokenForUser = null;
                    Expiration = DateTimeOffset.MinValue;
                }
            }

            // Cannot get the token silently. Ask user to log in
            if (!IsConnected)
            {
                var authResult = await _identityClientApp.AcquireTokenInteractive(_identificationInfo.scope).WithLoginHint(_identificationInfo.username).ExecuteAsync();

                // Set access token and expiration
                TokenForUser = authResult.AccessToken;
                Expiration = authResult.ExpiresOn;
            }
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public async Task SignOutAsync()
        {
            foreach (IAccount account in await _identityClientApp.GetAccountsAsync())
            {
                await _identityClientApp.RemoveAsync(account);
            }
            GraphClient = GetAuthenticatedClient();

            // Reset token and expiration
            Expiration = DateTimeOffset.MinValue;
            TokenForUser = null;
        }

    }
}
