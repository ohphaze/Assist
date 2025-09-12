using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Assist.Models.Enums;
using Assist.Shared.Controls;
using Assist.Shared.Services.Utils;
using Assist.Shared.Settings;
using Assist.Shared.Settings.Accounts;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Serilog;
using ValNet;
using ValNet.Enums;
using ValNet.Objects;
using ValNet.Objects.Authentication;

namespace Assist.ViewModels.RAccount;


/// <summary>
/// Integration of Custom Webview2 Class.
/// </summary>
public partial class RAccountCloudViewModel : ViewModelBase
{
    [ObservableProperty] private WebView _currentContent = new WebView();
    [ObservableProperty] private bool _webViewVisible = true;
    [ObservableProperty] private ICommand? _loginCompletedCommand;
    // Fallback userinfo when ValNet user model cannot be set via reflection
    private string? _fbSub;
    private string? _fbGameName;
    private string? _fbTagLine;

    private const string authUrl = "https://account.riotgames.com/";
    private string cacheLoc = System.IO.Path.Combine(AssistSettings.FolderPath, "Cache", "web");
    private const string socialGuide = "https://github.com/HeyM1ke/Assist/wiki/Social-Login-Guide";

    public async Task Setup()
    {
        CurrentContent.View = new WebView2();
        var webView2Environment = await CoreWebView2Environment.CreateAsync(null, cacheLoc);
        await CurrentContent.View.EnsureCoreWebView2Async(webView2Environment);
        
        CurrentContent.View.NavigationStarting += EnsureHttps;
        CurrentContent.View.SourceChanged += SourceChanged;
        CurrentContent.View.Source = new Uri(authUrl);
        
        ChangeViewResolution();
    }

    private void ChangeViewResolution()
    {
        switch (AssistSettings.Default.SelectedResolution)
        {
            case EResolution.R360:
                CurrentContent.View.Width = 500;
                CurrentContent.View.Height = 325;
                CurrentContent.Width = 500;
                CurrentContent.Height = 325;
                CurrentContent.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentContent.VerticalAlignment = VerticalAlignment.Center;
                break;
            case EResolution.R540:
                CurrentContent.View.Width = 750;
                CurrentContent.View.Height = 488;
                CurrentContent.Width = 750;
                CurrentContent.Height = 488;
                CurrentContent.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentContent.VerticalAlignment = VerticalAlignment.Center;
                break;
            case EResolution.R900:
                CurrentContent.View.Width = 1250;
                CurrentContent.View.Height = 813;
                CurrentContent.Width = 1250;
                CurrentContent.Height = 813;
                CurrentContent.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentContent.VerticalAlignment = VerticalAlignment.Center;
                break;
            case EResolution.R1080:
                CurrentContent.View.Width = 1500;
                CurrentContent.View.Height = 975;
                CurrentContent.Width = 1500;
                CurrentContent.Height = 975;
                CurrentContent.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentContent.VerticalAlignment = VerticalAlignment.Center;
                break;
            case EResolution.R1260:
                CurrentContent.View.Width = 1750;
                CurrentContent.View.Height = 1300;
                CurrentContent.Width = 1750;
                CurrentContent.Height = 1300;
                CurrentContent.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentContent.VerticalAlignment = VerticalAlignment.Center;
                break;
            default:
                CurrentContent.View.Width = 1000;
                CurrentContent.View.Height = 650;
                CurrentContent.Width = 1000;
                CurrentContent.Height = 650;
                CurrentContent.HorizontalAlignment = HorizontalAlignment.Center;
                CurrentContent.VerticalAlignment = VerticalAlignment.Center;
                break;
        }
    }

    private async Task LoginWithWebCookies(Dictionary<string, Cookie> cookieContainer, string? regionHint = null)
    {
        Log.Information("Attempting to login with Riot Account with Cloud");
        string curlPath = Path.Exists(Path.Combine(DependencyUtils.CurlDependencyFolder, "curl.exe")) ? Path.Combine(DependencyUtils.CurlDependencyFolder, "curl.exe") : "curl";
        var builder = new RiotUserBuilder().WithCustomCurl(curlPath).WithSettings(new RiotUserSettings()
        {
            AuthenticationMethod = AuthenticationMethod.CURL
        });
        if (!string.IsNullOrWhiteSpace(regionHint))
        {
            // Try to map string to RiotRegion enum dynamically
            try
            {
                if (Enum.TryParse<RiotRegion>(regionHint, true, out var rr))
                {
                    Log.Information($"Using region hint: {rr}");
                    builder = builder.WithRegion(rr);
                }
            }
            catch { }
        }
        RiotUser usr = builder.Build();

        try
        {
            usr.GetAuthClient().SetCookies(cookieContainer);
            var result = await usr.Authentication.ReAuthWithCookies();
        }
        catch (Exception e)
        {
            Log.Error("Failed to Authenticate with Cookies");
            Log.Error("Message: " + e.Message);
            Log.Error("Source: " + e.Source);
            Log.Error("Stack: " + e.StackTrace);

            // Fallback: perform JSON token cookie auth locally and hydrate RiotUser
            try
            {
                var success = await TryCookieAuthFallback(usr, cookieContainer);
                if (!success)
                {
                    Log.Error("Cookie auth fallback failed");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Cookie auth fallback threw");
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
                return;
            }
        }
        
        
        await HandleSuccessfulLogin(usr);
    }
    
    private async Task HandleSuccessfulLogin(RiotUser usr)
    {
        Log.Information("Successful login with Riot Account with CloudWebLogin");
        AccountProfile profile = new AccountProfile();
        var subId = usr.UserData?.sub ?? _fbSub ?? string.Empty;
        if (string.IsNullOrEmpty(subId))
        {
            Log.Error("HandleSuccessfulLogin: Missing user sub");
            return;
        }
        if (AccountSettings.Default.Accounts.Exists(x => x.Id == subId))
            profile = AccountSettings.Default.Accounts.Find(x => x.Id == subId);
        
        try
        {
            profile.Id = subId;
            try { profile.Region = usr.GetRegion(); } catch { }
            profile.LastLoginTime = DateTime.UtcNow;
            profile.Personalization = new AccountProfile.AccountProfilePersonalization()
            {
                GameName = usr.UserData?.acct?.game_name ?? _fbGameName ?? string.Empty,
                TagLine = usr.UserData?.acct?.tag_line ?? _fbTagLine ?? string.Empty
            };

            try
            {
                var inventory = await usr.Inventory.GetPlayerInventory();
                profile.Personalization.PlayerCardId = inventory.PlayerData.PlayerCardID;
                profile.Personalization.PlayerLevel = inventory.PlayerData.AccountLevel;
            }
            catch (Exception e)
            {
                Log.Error("Failed to Get Inventory Data when setting up profile");
            }
            
            try
            {
                var pMmr = await usr.Player.GetPlayerMmr();
                profile.Personalization.ValRankTier = pMmr.LatestCompetitiveUpdate.TierAfterUpdate;
            }
            catch (Exception e)
            {
                Log.Error("Failed to Get MMR Data when setting up profile");
            }
            
            profile.CanAssistBoot = true;
            profile.CanLauncherBoot = false;
            profile.IsExpired = false;

            profile.ConvertCookiesTo64(usr.GetAuthClient().ClientCookies);
            
        }
        catch (Exception e)
        {
            Log.Error("Failed to Setup Account");
            Log.Error(e.Message);
            Log.Error(e.Source);
            Log.Error(e.StackTrace);
            
            return;
        }

        AssistApplication.ActiveUser = usr;
        AssistApplication.ActiveAccountProfile = profile;

        AccountSettings.Default.DefaultAccount = string.IsNullOrEmpty(AccountSettings.Default.DefaultAccount) ? profile.Id : AccountSettings.Default.DefaultAccount;
        
        await AccountSettings.Default.UpdateAccount(profile);
        AccountSettings.Save();
        
        Log.Information("Finished Setting up Riot Account as the Main User & To the settings.");
        LoginCompletedCommand?.Execute("");
    }

    private static void TryAddDefaultHeader(object? restClient, string name, string value)
    {
        if (restClient == null) return;
        try
        {
            var m = restClient.GetType().GetMethod("AddDefaultHeader", new[] { typeof(string), typeof(string) });
            m?.Invoke(restClient, new object[] { name, value });
        }
        catch { }
        try
        {
            var prop = restClient.GetType().GetProperty("Client");
            var httpClient = prop?.GetValue(restClient);
            var headersProp = httpClient?.GetType().GetProperty("DefaultRequestHeaders");
            var headers = headersProp?.GetValue(httpClient);
            var addMethod = headers?.GetType().GetMethod("Add", new[] { typeof(string), typeof(string) });
            addMethod?.Invoke(headers, new object[] { name, value });
        }
        catch { }
    }

    private static string BuildCookieHeader(Dictionary<string, Cookie> cookies)
    {
        var list = new List<string>();
        foreach (var kv in cookies)
        {
            if (string.IsNullOrWhiteSpace(kv.Key)) continue;
            list.Add($"{kv.Key}={kv.Value?.Value}");
        }
        return string.Join("; ", list);
    }

    private async Task<bool> TryCookieAuthFallback(RiotUser usr, Dictionary<string, Cookie> cookies)
    {
        Log.Information("Attempting cookie-auth fallback (JSON tokens)");
        using var handler = new System.Net.Http.HttpClientHandler
        {
            UseCookies = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };
        using var http = new System.Net.Http.HttpClient(handler);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var cookieHeader = BuildCookieHeader(cookies);
        if (!string.IsNullOrWhiteSpace(cookieHeader))
            http.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        var authUrl = "https://auth.riotgames.com/api/v1/authorization";
        var entitleUrl = "https://entitlements.auth.riotgames.com/api/token/v1";
        var userInfoUrl = "https://auth.riotgames.com/userinfo";

        var payload = new
        {
            client_id = "play-valorant-web-prod",
            nonce = 1,
            redirect_uri = "https://playvalorant.com/opt_in",
            response_type = "token id_token",
            scope = "account openid"
        };

        string accessToken = string.Empty;
        string idToken = string.Empty;
        string entitlements = string.Empty;

        var authResp = await http.PostAsync(authUrl, new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"));
        var authBody = await authResp.Content.ReadAsStringAsync();
        if (!authResp.IsSuccessStatusCode)
        {
            Log.Error($"Fallback auth failed: {(int)authResp.StatusCode} {authResp.ReasonPhrase}");
            Log.Debug(authBody);
            return false;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(authBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("response", out var response) &&
                response.TryGetProperty("parameters", out var parameters) &&
                parameters.TryGetProperty("uri", out var uriEl))
            {
                var uri = uriEl.GetString() ?? string.Empty;
                var fragIndex = uri.IndexOf('#');
                var query = fragIndex >= 0 ? uri.Substring(fragIndex + 1) : uri;
                foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    var key = kv[0];
                    var val = Uri.UnescapeDataString(kv[1]);
                    if (key == "access_token") accessToken = val;
                    else if (key == "id_token") idToken = val;
                }
            }
            if (string.IsNullOrEmpty(accessToken))
            {
                if (root.TryGetProperty("access_token", out var at)) accessToken = at.GetString() ?? string.Empty;
                if (root.TryGetProperty("id_token", out var it)) idToken = it.GetString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed parsing fallback auth response");
            Log.Error(ex.Message);
            return false;
        }

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(idToken))
        {
            Log.Error("Fallback auth missing tokens");
            return false;
        }

        // Entitlements
        var entReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, entitleUrl)
        {
            Content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        entReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var entResp = await http.SendAsync(entReq);
        var entBody = await entResp.Content.ReadAsStringAsync();
        if (!entResp.IsSuccessStatusCode)
        {
            Log.Error("Entitlements request failed");
            Log.Debug(entBody);
            return false;
        }
        try
        {
            using var edoc = System.Text.Json.JsonDocument.Parse(entBody);
            entitlements = edoc.RootElement.TryGetProperty("entitlements_token", out var et)
                ? et.GetString() ?? string.Empty
                : string.Empty;
        }
        catch { }

        if (string.IsNullOrEmpty(entitlements))
        {
            Log.Error("Entitlements token missing");
            return false;
        }

        // User info
        var uiReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, userInfoUrl);
        uiReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var uiResp = await http.SendAsync(uiReq);
        var uiBody = await uiResp.Content.ReadAsStringAsync();
        if (!uiResp.IsSuccessStatusCode)
        {
            Log.Error("Userinfo request failed");
            Log.Debug(uiBody);
            return false;
        }

        object? userData = null;
        string? sub = null; string? gameName = null; string? tagLine = null;
        try
        {
            using var udoc = System.Text.Json.JsonDocument.Parse(uiBody);
            var root = udoc.RootElement;
            sub = root.TryGetProperty("sub", out var sEl) ? sEl.GetString() : null;
            if (root.TryGetProperty("acct", out var acct))
            {
                gameName = acct.TryGetProperty("game_name", out var gn) ? gn.GetString() : null;
                tagLine = acct.TryGetProperty("tag_line", out var tl) ? tl.GetString() : null;
            }

            // Attempt to construct ValNet user types; continue if not found
            var asm = usr.GetType().Assembly;
            var udType = asm.GetType("ValNet.Objects.Authentication.RiotUserData");
            var acctType = asm.GetType("ValNet.Objects.Authentication.AccountInfo");
            if (udType != null && acctType != null)
            {
                var acctObj = Activator.CreateInstance(acctType);
                acctType.GetProperty("game_name")?.SetValue(acctObj, gameName ?? string.Empty);
                acctType.GetProperty("tag_line")?.SetValue(acctObj, tagLine ?? string.Empty);

                userData = Activator.CreateInstance(udType);
                udType.GetProperty("sub")?.SetValue(userData, sub ?? string.Empty);
                udType.GetProperty("acct")?.SetValue(userData, acctObj);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to parse/construct user info via reflection (continuing)");
            Log.Error(ex.Message);
        }

        if (string.IsNullOrEmpty(sub))
        {
            Log.Error("UserData invalid after fallback");
            return false;
        }

        // Apply tokens/headers to ValNet clients
        try
        {
            // Try set on UserClient (RestSharp) via reflection
            var userClientObj = usr.GetType().GetProperty("UserClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(usr);
            TryAddDefaultHeader(userClientObj, "Authorization", $"Bearer {accessToken}");
            TryAddDefaultHeader(userClientObj, "X-Riot-Entitlements-JWT", entitlements);

            // AuthClient (HttpClient wrapper) via reflection
            var authClientObj = usr.GetType().GetProperty("AuthClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(usr);
            TryAddDefaultHeader(authClientObj, "Authorization", $"Bearer {accessToken}");
            TryAddDefaultHeader(authClientObj, "X-Riot-Entitlements-JWT", entitlements);
        }
        catch { }
        // Assign user data if possible; otherwise cache for HandleSuccessfulLogin
        if (userData != null)
        {
            usr.GetType().GetProperty("UserData")?.SetValue(usr, userData);
        }
        else
        {
            _fbSub = sub;
            _fbGameName = gameName;
            _fbTagLine = tagLine;
        }
        Log.Information("Cookie-auth fallback succeeded");
        return true;
    }

    private async void SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        var redirectUrl = CurrentContent.View.Source.ToString();
        Log.Information(redirectUrl);
        CurrentContent.View.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        CurrentContent.View.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        
        if (redirectUrl.Contains("https://login.riotgames.com/oauth2-callback?"))
        {
            var cookies = await GetCookies(this.CurrentContent.View);
            var valid = cookies.Find(_c => _c.Name == "ssid") != null;
            if (valid)
            {
                CurrentContent.IsVisible = false;

                var cc = new Dictionary<string, Cookie>();
                cookies.ForEach(x =>
                {
                    // Exclude analytics/noise cookies, include only Riot/Valorant domains
                    var isNoise = x.Name.Contains("osano", StringComparison.OrdinalIgnoreCase)
                                  || x.Name.Contains("PVPNET", StringComparison.OrdinalIgnoreCase)
                                  || x.Name.Contains("_ga_", StringComparison.OrdinalIgnoreCase)
                                  || x.Name.Equals("_ga", StringComparison.OrdinalIgnoreCase);

                    // Keep only core Riot auth hosts; avoid playvalorant/valorant/lolesports/account.* noise
                    var host = (x.Domain ?? string.Empty).Trim('.');
                    var isRelevantDomain = host.Equals("auth.riotgames.com", StringComparison.OrdinalIgnoreCase)
                                           || host.Equals("login.riotgames.com", StringComparison.OrdinalIgnoreCase);

                    if (!isNoise && isRelevantDomain)
                    {
                        var fwd = new Cookie
                        {
                            Name = x.Name,
                            Value = x.Value,
                            Path = x.Path,
                            Domain = x.Domain,
                            Secure = x.IsSecure,
                            HttpOnly = x.IsHttpOnly,
                        };
                        if (x.Expires.ToString().Contains("1/1/0001"))
                            fwd.Expires = DateTime.Now.AddMonths(1);
                        else
                            fwd.Expires = x.Expires;

                        cc[x.Name] = fwd;
                        Log.Debug($"Forwarding cookie: {x.Name}; Domain={x.Domain}; Path={x.Path}; Secure={x.IsSecure}; HttpOnly={x.IsHttpOnly}");
                    }
                });

                // Attempt to derive region from cookies if available
                string? regionHint = null;
                try
                {
                    var regionCookie = cookies.Find(c => c.Name.Equals("PVPNET_REGION", StringComparison.OrdinalIgnoreCase));
                    if (regionCookie != null)
                    {
                        regionHint = MapRegionFromCookie(regionCookie.Value);
                    }
                }
                catch { }

                await LoginWithWebCookies(cc, regionHint);
            }
        }
    }
    
    
    
    private async Task<List<CoreWebView2Cookie>?> GetCookies(WebView2 webView)
    {           
        var result = await webView.CoreWebView2.CookieManager.GetCookiesAsync(null);

        result.ForEach(x => Log.Information(x.Name));
        foreach (var ck in result)
        {
            Log.Debug($"Cookie: {ck.Name}; Domain={ck.Domain}; Path={ck.Path}; Expires={ck.Expires}");
        }
        var c = result.Find(_c => _c.Name == "ssid");

        if (c != null)
        {
            Log.Debug("Found SSID Cookie from Authentication");
            var cook = new Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Path = c.Path,
                Secure = c.IsSecure,
                HttpOnly = c.IsHttpOnly,
                Domain = c.Domain,
            };
            if (c.Expires.ToString().Contains("1/1/0001"))
                cook.Expires = DateTime.Now.AddMonths(1);
            else
                cook.Expires = c.Expires;

            webView.CoreWebView2.CookieManager.DeleteAllCookies();

        }

        return result;
    }
    private static string? MapRegionFromCookie(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ToUpperInvariant();
        if (v.Contains("EU")) return "EU";
        if (v.Contains("NA")) return "NA";
        if (v.Contains("AP")) return "AP";
        if (v.Contains("KR")) return "KR";
        if (v.Contains("BR")) return "BR";
        if (v.Contains("LATAM")) return "LATAM";
        if (v.Contains("OCE") || v.Contains("OC1")) return "AP";
        return null;
    }
    private void EnsureHttps(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!e.Uri.StartsWith("https://"))
        {
            e.Cancel = true;
        }
    }
}
