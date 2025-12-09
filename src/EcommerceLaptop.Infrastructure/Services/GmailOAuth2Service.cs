using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Apis.Util;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services
{
    public interface IGmailOAuth2Service
    {
        /// <summary>
        /// Initialize OAuth2 credentials for Gmail
        /// </summary>
        Task<UserCredential> InitializeCredentialsAsync();

        /// <summary>
        /// Send email using OAuth2 authentication
        /// </summary>
        Task<bool> SendEmailWithOAuth2Async(MimeMessage message);

        /// <summary>
        /// Refresh OAuth2 token if expired
        /// </summary>
        Task<bool> RefreshTokenAsync();

        /// <summary>
        /// Validate OAuth2 credentials
        /// </summary>
        Task<bool> ValidateCredentialsAsync();

        /// <summary>
        /// Get authorization URL for OAuth2 setup
        /// </summary>
        Task<string> GetAuthorizationUrlAsync();

        /// <summary>
        /// Exchange authorization code for tokens
        /// </summary>
        Task<bool> ExchangeCodeForTokensAsync(string authorizationCode);
    }

    public class GmailOAuth2Service : IGmailOAuth2Service
    {
        private readonly ILogger<GmailOAuth2Service> _logger;
        private readonly IConfiguration _configuration;
        private readonly string[] _scopes = { "https://mail.google.com/" };
        private UserCredential? _userCredential;
        private readonly string _credentialsPath;

        public GmailOAuth2Service(ILogger<GmailOAuth2Service> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "credentials", "gmail_oauth2.json");
        }

        public async Task<UserCredential> InitializeCredentialsAsync()
        {
            try
            {
                var clientId = _configuration["EmailSettings:Gmail:OAuth2:ClientId"];
                var clientSecret = _configuration["EmailSettings:Gmail:OAuth2:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Gmail OAuth2 credentials not configured");
                }

                var clientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                };

                var credentialsDirectory = Path.GetDirectoryName(_credentialsPath);
                if (!Directory.Exists(credentialsDirectory))
                {
                    Directory.CreateDirectory(credentialsDirectory!);
                }

                _userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credentialsDirectory, true));

                _logger.LogInformation("Gmail OAuth2 credentials initialized successfully");
                return _userCredential;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Gmail OAuth2 credentials");
                throw new InvalidOperationException("OAuth2 initialization failed", ex);
            }
        }

        public async Task<bool> SendEmailWithOAuth2Async(MimeMessage message)
        {
            try
            {
                if (_userCredential == null)
                {
                    await InitializeCredentialsAsync();
                }

                if (await RefreshTokenAsync())
                {
                    using var client = new SmtpClient();

                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                    // Use OAuth2 for authentication
                    var oauth2 = new SaslMechanismOAuth2(_userCredential!.UserId, _userCredential.Token.AccessToken);
                    await client.AuthenticateAsync(oauth2);

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    _logger.LogInformation("Email sent successfully using OAuth2 to: {To}", string.Join(", ", message.To));
                    return true;
                }

                _logger.LogError("Failed to refresh OAuth2 token");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email using OAuth2");
                return false;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                if (_userCredential?.Token == null)
                {
                    _logger.LogWarning("No OAuth2 token available for refresh");
                    return false;
                }

                if (_userCredential.Token.IsStale)
                {
                    var refreshed = await _userCredential.RefreshTokenAsync(CancellationToken.None);
                    if (refreshed)
                    {
                        _logger.LogInformation("OAuth2 token refreshed successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogError("Failed to refresh OAuth2 token");
                        return false;
                    }
                }

                return true; // Token is still valid
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth2 token refresh");
                return false;
            }
        }

        public async Task<bool> ValidateCredentialsAsync()
        {
            try
            {
                if (_userCredential == null)
                {
                    await InitializeCredentialsAsync();
                }

                return _userCredential?.Token != null && !_userCredential.Token.IsStale;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate OAuth2 credentials");
                return false;
            }
        }

        public async Task<string> GetAuthorizationUrlAsync()
        {
            try
            {
                var clientId = _configuration["EmailSettings:Gmail:OAuth2:ClientId"];
                var clientSecret = _configuration["EmailSettings:Gmail:OAuth2:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Gmail OAuth2 credentials not configured");
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = _scopes
                });

                var redirectUri = _configuration["EmailSettings:Gmail:OAuth2:RedirectUri"] ?? "http://localhost:8080/";
                var authUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build().ToString();

                _logger.LogInformation("Generated OAuth2 authorization URL");
                return authUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate authorization URL");
                throw new InvalidOperationException("Authorization URL generation failed", ex);
            }
        }

        public async Task<bool> ExchangeCodeForTokensAsync(string authorizationCode)
        {
            try
            {
                if (string.IsNullOrEmpty(authorizationCode))
                {
                    throw new ArgumentException("Authorization code cannot be null or empty", nameof(authorizationCode));
                }

                var clientId = _configuration["EmailSettings:Gmail:OAuth2:ClientId"];
                var clientSecret = _configuration["EmailSettings:Gmail:OAuth2:ClientSecret"];
                var redirectUri = _configuration["EmailSettings:Gmail:OAuth2:RedirectUri"] ?? "http://localhost:8080/";

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId!,
                        ClientSecret = clientSecret!
                    },
                    Scopes = _scopes
                });

                var token = await flow.ExchangeCodeForTokenAsync("user", authorizationCode, redirectUri, CancellationToken.None);

                if (token != null)
                {
                    _userCredential = new UserCredential(flow, "user", token);
                    _logger.LogInformation("Successfully exchanged authorization code for tokens");
                    return true;
                }

                _logger.LogError("Failed to exchange authorization code for tokens");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authorization code exchange");
                return false;
            }
        }
    }
}