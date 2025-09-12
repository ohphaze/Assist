using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Assist.Controls.Assist.Account;
using Assist.Controls.Assist.Authentication;
using Assist.Controls.Infobars;
using Assist.Controls.Navigation;
using Assist.Controls.Startup;
using Assist.Core.Helpers;
using Assist.Core.Settings.Options;
using Assist.Services.Navigation;
using Assist.Shared.Services.Utils;
using Assist.Shared.Settings;
using Assist.Shared.Settings.Accounts;
using Assist.ViewModels.Infobars;
using Assist.Views.Assist;
using Assist.Views.Dashboard;
using Assist.Views.Extras;
using Assist.Views.Game;
using Assist.Views.RAccount;
using Assist.Views.Setup;
using Assist.Views.Startup;
using AssistUser.Lib.V2.Models;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using SkiaSharp;
using Svg.Skia;
using ValNet;
using ValNet.Enums;
using ValNet.Objects;
using ValNet.Objects.Authentication;
using Velopack;

namespace Assist.ViewModels.Startup;

public partial class StartupViewModel : ViewModelBase
{
    [ObservableProperty]
    private Control _currentContent = new BasicStartupControl();

    [ObservableProperty] private string _attemptProfileId = String.Empty;

    public async Task Startup()
    {  
        NavigationContainer.ViewModel.HideAllButtons();
        // Check for Dependency
        await DependencyUtils.CheckDepends();
        var newVersion = await AssistApplication.CheckForUpdates();
        
        if(newVersion)
        {
            Log.Information("New Version Available. Redirecting to Update Page");
            AssistApplication.OpenUpdateWindow();
            return;
        }
        // Check if setting have completed the tutorial.
        
        if (!AssistSettings.Default.CompletedSetup)
        {
            // Start Setup Guide
            Log.Information("Settings is Reading that the setup has not been completed. Starting Setup");
            AssistApplication.ChangeMainWindowView(new SetupView());
            return;
        }

        Log.Information("Checking for Assist Account Login");
        if (!string.IsNullOrEmpty(AssistSettings.Default.AssistUserCode) && string.IsNullOrEmpty(AssistApplication.AssistUser.userTokens.AccessToken))
        {
            Log.Information("Settings is Reading that an Assist Account exists. Attempting to login");
            try
            {
                var t = await AssistApplication.AssistUser.Authentication.AuthenticateWithRefreshToken(AssistSettings
                    .Default
                    .GetAssistUserCode());
#if DEBUG
                Log.Information($"Assist Access: {t.AccessToken}");
#endif
                
                Log.Information("Assist Account Login Successful");
                Log.Information("Saving Account Code...");
                AssistSettings.Default.SaveAssistUserCode(AssistApplication.AssistUser.userTokens.RefreshToken);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                Log.Information("Assist Account Login Unsuccessful");
                AssistSettings.Default.AssistUserCode = ""; // Doing this since this will make it only show once.
                Dispatcher.UIThread.Invoke(() =>
                {
                    AssistApplication.ChangeMainWindowPopupView(new AssistAuthenticationView());
                });
                
                
            }
        }

        if (!string.IsNullOrEmpty(AssistApplication.AssistUser.userTokens.AccessToken))
        {
            Log.Information("Checking Assist Account Information");
            try
            {
                var accountInfo = await AssistApplication.AssistUser.Account.GetAccountInfo();

                if (accountInfo.Code == 200)
                {
                    var _accountInfo = JsonSerializer.Deserialize<AAccount>(accountInfo.Data.ToString());
                    if (string.IsNullOrEmpty(_accountInfo.Personalization.DisplayName))
                    {
                        AssistApplication.ChangeMainWindowPopupView(new CustomizeAssistDisplayNameControl(DisplayNameCompletedCommand));
                        return; 
                    }
                }
                
                
            }
            catch (Exception e)
            {
                
            }
            
        }

        if (!string.IsNullOrEmpty(AssistApplication.AssistUser.userTokens.AccessToken))
        {
            try
            {
                Log.Information("Attempting to connect to the Assist Server.");
                await AssistApplication.Server.Connect();
            }
            catch (Exception e)
            {
                Log.Error("Failed to connect to Assist Server");
            }
        }
        
        switch (AssistSettings.Default.AppType)
        {
            case AssistApplicationType.GAME_ONLY:
                Log.Information("Assist is in Game only mode. Switching to Game Mode");
                AssistApplication.ChangeMainWindowView(new GameInitialStartupView());
                return;
            default:
                Log.Information("Assist is in Launcher Only/Complete . Continuing Basic Procedure.");
                break;
        }

        if (IsValorantRunning())
        {
            Log.Information("Valorant is running, swapping to game mode.");
            AssistApplication.ChangeMainWindowView(new GameInitialStartupView());
            return;
        }
        
        Log.Information("Launcher Setup Starting...");

        await LauncherSetup();
    }

    private async Task LauncherSetup()
    {
        Log.Information("Checking for any Accounts Stored");

        if (AccountSettings.Default.Accounts.Count == 0)
        {
            Log.Information("No Accounts are stored, Redirecting to Riot Authentication Page");
            AssistApplication.ChangeMainWindowView(new RAccountAddPage());
            return;
        }
        
        if (IsValorantRunning())
        {
            Log.Information("Valorant is Running. Switching to Game Mode");
            AssistApplication.ChangeMainWindowView(new GameInitialStartupView());
            return;
        }
        

        await AttemptAuthentication();
    }

    private bool IsValorantRunning()
    {
        return Process.GetProcessesByName("VALORANT-Win64-Shipping").Any(process => process.Id != Process.GetCurrentProcess().Id);
    }

    private async Task AttemptAuthentication()
    {

        if (!string.IsNullOrEmpty(AttemptProfileId))
        {
            Log.Information("Attempt Profile ID Passed In, Attempting to Login");
            var passedProfile = AccountSettings.Default.Accounts.Find(x => x.Id == AttemptProfileId);
            Log.Information("Create UI Preview");

            if (passedProfile is not null)
            {
                await CreateUiAccountPreview(passedProfile);
                try
                {
                    await AuthenticateProfile(passedProfile); // This method handles the navigation to the Dashboard View.
                    return;
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }
            }
           
        }
        
        
        Log.Information("Checking for Default Account");
        var defaultAccount = AccountSettings.Default.Accounts.Find(x => x.Id == AccountSettings.Default.DefaultAccount);

        if (defaultAccount is not null && !defaultAccount.IsExpired && defaultAccount.CanAssistBoot)
        {
            Log.Information("Default Account Attempting to Login");
            Log.Information("Create UI Preview");
            
            await CreateUiAccountPreview(defaultAccount);
            try
            {
                await AuthenticateProfile(defaultAccount); // This method handles the navigation to the Dashboard View.
                return;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }

        Log.Information("Starting to Loop through other accounts.");
        for (int i = 0; i < AccountSettings.Default.Accounts.Count; i++)
        {
            var acc = AccountSettings.Default.Accounts[i];
            if (acc.Id.Equals(AccountSettings.Default.DefaultAccount) || acc.IsExpired || !acc.CanAssistBoot) // This has already been attempted. No Reason for it to be twice.
                continue;
            
            Log.Information("New Account Attempting to Login");
            Log.Information("Create UI Preview");
            
            await CreateUiAccountPreview(acc);
            try
            {
                await AuthenticateProfile(acc); // This method handles the navigation to the Dashboard View.
                return;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }
        
        Log.Information("This has been hit, meaning all accounts are not valid. Moving to Adding Page");

        
        AssistApplication.ChangeMainWindowView(new RAccountAddPage());
        
    }

    private async Task CreateUiAccountPreview(AccountProfile defaultAccount)
    {
        
        CurrentContent = new AccountPreviewStartupControl()
        {
            Icon = $"https://cdn.assistval.com/playercards/{defaultAccount.Personalization.PlayerCardId}_DisplayIcon.png",
            AccountName = !string.IsNullOrEmpty(defaultAccount.Personalization.AccountNickName)
                ? defaultAccount.Personalization.AccountNickName
                : defaultAccount.Personalization.RiotId,
            AccountRegion = $"Region: {defaultAccount.Region.ToString()}"
        };
    }


    private async Task AuthenticateProfile(AccountProfile profile)
    {
        Log.Information($"Attempting to login to AccountProfile Riot Account with Code | ID: {profile.Personalization.GameName}//{profile.Id}");
        string curlPath = Path.Exists(Path.Combine(DependencyUtils.CurlDependencyFolder, "curl.exe")) ? Path.Combine(DependencyUtils.CurlDependencyFolder, "curl.exe") : "curl";
        RiotUser usr = new RiotUserBuilder().WithCustomCurl(curlPath).WithRegion(profile.Region).WithSettings(new RiotUserSettings() { AuthenticationMethod = AuthenticationMethod.CURL }).Build();

        try
        {
            var cookies = profile.Convert64ToCookies();
            usr.GetAuthClient().SaveCookies(cookies);
            await usr.Authentication.ReAuthWithCookies();
        }
        catch (Exception e)
        {
            Log.Error("Failed to Authenticate with Cookies");
            Log.Error("Message: " + e.Message);
            Log.Error("Source: " + e.Source);
            Log.Error("Stack: " + e.StackTrace);

            // Fallback to JSON-token cookie auth (same as cloud flow)
            try
            {
                var cookies = profile.Convert64ToCookies();
                var ok = await TryCookieAuthFallback(usr, cookies);
                if (!ok)
                {
                    profile.CanAssistBoot = false;
                    profile.IsExpired = true;
                    await AccountSettings.Default.UpdateAccount(profile);
                    throw new Exception("Failed to Authenticate");
                }
            }
            catch
            {
                profile.CanAssistBoot = false;
                profile.IsExpired = true;
                await AccountSettings.Default.UpdateAccount(profile);
                throw new Exception("Failed to Authenticate");
            }
        }
        
        Log.Information("Account Successfully Logged in!");
        try
        {
            await HandleSuccessfulLogin(usr: usr);
        }
        catch (Exception ex)
        {
            Log.Error("HandleSuccessfulLogin failed");
            Log.Error(ex.Message);
            // Don't invalidate account here; continue to launcher
            await AssistApplication.SetupComplete_Launcher();
            return;
        }
        
        Log.Information("Going to Dashboard.");
        await AssistApplication.SetupComplete_Launcher();
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

    private async Task<bool> TryCookieAuthFallback(RiotUser usr, Dictionary<string, Cookie> cookies)
    {
        Log.Information("Startup fallback: cookie-auth with JSON tokens");
        using var handler = new System.Net.Http.HttpClientHandler { UseCookies = false, AutomaticDecompression = System.Net.DecompressionMethods.All };
        using var http = new System.Net.Http.HttpClient(handler);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        var cookieHeader = BuildCookieHeader(cookies);
        if (!string.IsNullOrWhiteSpace(cookieHeader)) http.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        var authUrl = "https://auth.riotgames.com/api/v1/authorization";
        var entitleUrl = "https://entitlements.auth.riotgames.com/api/token/v1";
        var userInfoUrl = "https://auth.riotgames.com/userinfo";

        var payload = new { client_id = "play-valorant-web-prod", nonce = 1, redirect_uri = "https://playvalorant.com/opt_in", response_type = "token id_token", scope = "account openid" };

        var authResp = await http.PostAsync(authUrl, new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        var authBody = await authResp.Content.ReadAsStringAsync();
        if (!authResp.IsSuccessStatusCode) { Log.Error($"Startup fallback auth failed: {(int)authResp.StatusCode}"); Log.Debug(authBody); return false; }

        string accessToken = string.Empty, idToken = string.Empty;
        using (var doc = System.Text.Json.JsonDocument.Parse(authBody))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("response", out var resp) && resp.TryGetProperty("parameters", out var par) && par.TryGetProperty("uri", out var uriEl))
            {
                var uri = uriEl.GetString() ?? string.Empty;
                var frag = uri.IndexOf('#');
                var query = frag >= 0 ? uri[(frag + 1)..] : uri;
                foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    var key = kv[0]; var val = Uri.UnescapeDataString(kv[1]);
                    if (key == "access_token") accessToken = val; else if (key == "id_token") idToken = val;
                }
            }
            if (string.IsNullOrEmpty(accessToken))
            {
                if (root.TryGetProperty("access_token", out var at)) accessToken = at.GetString() ?? string.Empty;
                if (root.TryGetProperty("id_token", out var it)) idToken = it.GetString() ?? string.Empty;
            }
        }
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(idToken)) { Log.Error("Startup fallback missing tokens"); return false; }

        var entReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, entitleUrl) { Content = new System.Net.Http.StringContent("{}", Encoding.UTF8, "application/json") };
        entReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var entResp = await http.SendAsync(entReq);
        var entBody = await entResp.Content.ReadAsStringAsync();
        if (!entResp.IsSuccessStatusCode) { Log.Error("Startup fallback entitlements failed"); Log.Debug(entBody); return false; }
        string entitlements = string.Empty;
        using (var ed = System.Text.Json.JsonDocument.Parse(entBody))
        {
            if (ed.RootElement.TryGetProperty("entitlements_token", out var et)) entitlements = et.GetString() ?? string.Empty;
        }
        if (string.IsNullOrEmpty(entitlements)) { Log.Error("Startup fallback entitlements missing"); return false; }

        var uiReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, userInfoUrl);
        uiReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var uiResp = await http.SendAsync(uiReq);
        var uiBody = await uiResp.Content.ReadAsStringAsync();
        if (!uiResp.IsSuccessStatusCode) { Log.Error("Startup fallback userinfo failed"); Log.Debug(uiBody); return false; }

        // Apply headers to ValNet clients via reflection
        try
        {
            var userClientObj = usr.GetType().GetProperty("UserClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(usr);
            TryAddDefaultHeader(userClientObj, "Authorization", $"Bearer {accessToken}");
            TryAddDefaultHeader(userClientObj, "X-Riot-Entitlements-JWT", entitlements);
            var authClientObj = usr.GetType().GetProperty("AuthClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(usr);
            TryAddDefaultHeader(authClientObj, "Authorization", $"Bearer {accessToken}");
            TryAddDefaultHeader(authClientObj, "X-Riot-Entitlements-JWT", entitlements);
        }
        catch { }

        // Best effort to set UserData from userinfo json via reflection; if it fails downstream code may still populate later
        try
        {
            string? sub = null, game = null, tag = null;
            using var udoc = System.Text.Json.JsonDocument.Parse(uiBody);
            var root = udoc.RootElement;
            sub = root.TryGetProperty("sub", out var sEl) ? sEl.GetString() : null;
            if (root.TryGetProperty("acct", out var acct))
            {
                game = acct.TryGetProperty("game_name", out var gn) ? gn.GetString() : null;
                tag = acct.TryGetProperty("tag_line", out var tl) ? tl.GetString() : null;
            }
            var asm = usr.GetType().Assembly;
            var udType = asm.GetType("ValNet.Objects.Authentication.RiotUserData");
            var acctType = asm.GetType("ValNet.Objects.Authentication.AccountInfo");
            if (udType != null && acctType != null)
            {
                var acctObj = Activator.CreateInstance(acctType);
                acctType.GetProperty("game_name")?.SetValue(acctObj, game ?? string.Empty);
                acctType.GetProperty("tag_line")?.SetValue(acctObj, tag ?? string.Empty);
                var userData = Activator.CreateInstance(udType);
                udType.GetProperty("sub")?.SetValue(userData, sub ?? string.Empty);
                udType.GetProperty("acct")?.SetValue(userData, acctObj);
                usr.GetType().GetProperty("UserData")?.SetValue(usr, userData);
            }
        }
        catch { }

        Log.Information("Startup cookie-auth fallback succeeded");
        return true;
    }

    private async Task HandleSuccessfulLogin(RiotUser usr)
    {
        Log.Information("Successful login with Riot Account with Username/Password");
        AccountProfile profile = new AccountProfile();
        var subId = usr.UserData?.sub ?? string.Empty;
        if (AccountSettings.Default.Accounts.Exists(x => x.Id == subId))
            profile = AccountSettings.Default.Accounts.Find(x => x.Id == subId);
        
        try
        {
            profile.Id = subId;
            try { profile.Region = usr.GetRegion(); } catch { }
            profile.LastLoginTime = DateTime.UtcNow;
            profile.Personalization = new AccountProfile.AccountProfilePersonalization()
            {
                GameName = usr.UserData?.acct?.game_name ?? string.Empty,
                TagLine = usr.UserData?.acct?.tag_line ?? string.Empty
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
            
            try
            {
                var clientCookiesProp = usr.GetAuthClient().GetType().GetProperty("ClientCookies");
                var cc = clientCookiesProp?.GetValue(usr.GetAuthClient()) as System.Collections.Generic.Dictionary<string, Cookie>;
                if (cc != null) profile.ConvertCookiesTo64(cc);
            }
            catch { }
        }
        catch (Exception e)
        {
            Log.Error("Failed to Setup Account");
            Log.Error(e.Message);
            Log.Error(e.Source);
            Log.Error(e.StackTrace);
        }

        AssistApplication.ActiveUser = usr;
        AssistApplication.ActiveAccountProfile = profile;
        await AccountSettings.Default.UpdateAccount(profile);
        
        Log.Information("Finished Setting up Riot Account as the Main User & To the settings.");
    }

    [RelayCommand]
    private async void DisplayNameCompleted()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            AssistApplication.ChangeMainWindowPopupView(null);
            AssistApplication.ChangeMainWindowView(new StartupView());
        });
    }
}
