using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Assist.Controls.Store;
using Assist.Services;
using Assist.Views.Game.Live.Pages;
using Assist.Views.Store.Pages;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using ValNet.Objects.Exceptions;
using ValNet.Objects.Store;

namespace Assist.ViewModels.Store;

public partial class StoreViewModel : ViewModelBase
{
    static Dictionary<string, StoreStorageModel> PlayerStoreDictionary = new Dictionary<string, StoreStorageModel>();
    static Dictionary<string, ValWallet> PlayerWalletDictionary = new Dictionary<string, ValWallet>();
    private static string lastProfileLoaded = string.Empty;
    private static ValOffers _offers;
    [ObservableProperty]private bool _loadingStore = true;
    [ObservableProperty]private bool _nightMarketActive = false;
    [ObservableProperty]private bool _accessoriesActive = false;
    [ObservableProperty] private string _offerTimerText;
    [ObservableProperty] private string _vpText;
    [ObservableProperty] private string _rpText;
    [ObservableProperty] private string _kcText;
    [ObservableProperty] private List<string> _bundleTimerTexts = new List<string>();
    [ObservableProperty] private ObservableCollection<SkinOfferControl> _offerControls = new ObservableCollection<SkinOfferControl>();
    [ObservableProperty] private ObservableCollection<BundleOfferControl> _bundleControls = new ObservableCollection<BundleOfferControl>();

    [ObservableProperty] private Control _currentContent;
    
    public async Task Initialize()
    {
        CurrentContent = new MainStorePageView()
        {
            BundleControls = this.BundleControls,
            OfferControls = OfferControls
        };
    }
    
    public async Task SetupStoreView()
    {
        OfferControls.Clear();
        BundleControls.Clear();
        
        var store = await GetCurrentUserStore();
        if (store == null || store.Store == null)
        {
            Log.Error("StoreViewModel: store is null; cannot populate store view.");
            LoadingStore = false;
            return;
        }

        CreateBundleControl(store);
        CreateSkinControls(store);
        if (store.Store.BonusStore is not null)
        {
            Log.Information("Night market is Active, Loading Night Market Tab");
            NightMarketActive = true;
        }
        
        var wallet = await GetCurrentUserWallet();
        HandleUserWallet(wallet);
        
        LoadingStore = false;
    }

    private async Task<ValWallet> GetCurrentUserWallet()
    {
        if (PlayerWalletDictionary.ContainsKey(AssistApplication.ActiveUser.UserData.sub))
            return PlayerWalletDictionary[AssistApplication.ActiveUser.UserData.sub];
        
        
        Log.Information("Wallet does not exist. Getting Wallet Data");
        try
        {
            var wallet = await AssistApplication.ActiveUser.Store.GetPlayerWallet();
            if (PlayerWalletDictionary.ContainsKey(AssistApplication.ActiveUser.UserData.sub)) PlayerWalletDictionary[AssistApplication.ActiveUser.UserData.sub] = wallet;
            else PlayerWalletDictionary.TryAdd(AssistApplication.ActiveUser.UserData.sub, wallet);

            return wallet;

        }
        catch (Exception e)
        {
            Log.Error("StorePageView -----");
            Log.Error(e.Message);
            Log.Error(e.StackTrace);

            if (e is RequestException)
            {
                var exception = (RequestException)e;
                Log.Error(exception.Content);
            }
            // Show failed to get store control.
            return null;
        }
    }
    
    private async void HandleUserWallet(ValWallet _wallet)
    {
        if (_wallet is null)
            return;

        VpText = $"{_wallet.Balances.ValorantPoints:n0}";
        RpText = $"{_wallet.Balances.RadianitePoints:n0}";
        KcText = $"{_wallet.Balances.KingdomCredits:n0}";
    }

    public async Task<StoreStorageModel> GetCurrentUserStore()
    {
        if (PlayerStoreDictionary.ContainsKey(AssistApplication.ActiveUser.UserData.sub))
        {
            if (PlayerStoreDictionary[AssistApplication.ActiveUser.UserData.sub].ResetTime > DateTime.Now)
                return PlayerStoreDictionary[AssistApplication.ActiveUser.UserData.sub];
        }
        
        Log.Information("Store does not exist or is expired. Getting Store Data");

        StoreStorageModel store = null;
        try
        {
            var storeResp = await AssistApplication.ActiveUser.Store.GetPlayerStore();
            store = new()
            {
                Store = storeResp,
                ResetTime = new DateTime().AddSeconds(storeResp.SkinsPanelLayout.SingleItemOffersRemainingDurationInSeconds)
            };

            _offers = await AssistApplication.ActiveUser.Store.GetStoreOffers();
        }
        catch (Exception e)
        {
            Log.Error("StorePageView -----");
            Log.Error(e.Message);
            Log.Error(e.StackTrace);

            if (e is RequestException)
            {
                var exception = (RequestException)e;
                Log.Error(exception.Content);
            }
            // Show failed to get store control.
            return null;
        }
        
        if (PlayerStoreDictionary.ContainsKey(AssistApplication.ActiveUser.UserData.sub)) PlayerStoreDictionary[AssistApplication.ActiveUser.UserData.sub] = store;
        else PlayerStoreDictionary.TryAdd(AssistApplication.ActiveUser.UserData.sub, store);

        lastProfileLoaded = AssistApplication.ActiveUser.UserData.sub;
        Log.Information("Store data is here.");

        return store;
    }

    private void CreateSkinControls(StoreStorageModel? value)
    {
        OfferControls.Clear();
        try
        {
            if (value?.Store?.SkinsPanelLayout?.SingleItemOffers == null)
                return;

            foreach (var itemOffer in value.Store.SkinsPanelLayout.SingleItemOffers)
            {
                CreateSkinControl(itemOffer);
            }
        }
        catch (Exception ex)
        {
            Log.Error("CreateSkinControls failed: {Message}", ex.Message);
            Log.Error(ex.StackTrace);
        }
    }

    private async void CreateSkinControl(string itemOffer)
    {
        
        Log.Information("Creating Item Offer");

        var sData = await AssistApplication.AssistApiService.GetWeaponSkinAsync(itemOffer);

        if (sData is null)
        {
            OfferControls.Add(new SkinOfferControl()
            {
                SkinName = "Not Found",
                SkinPrice = "0"
            });
            return;
        }

        var price = GetOfferPrice(itemOffer);
        var skinOfferControl = new SkinOfferControl()
        {
            SkinId = itemOffer,
            SkinImage = sData.Levels[0].DisplayIcon,
            SkinName = sData.DisplayName,
            SkinPrice = price
        };

        OfferControls.Add(skinOfferControl);
    }
    
    private async void CreateBundleControl(StoreStorageModel? value)
    {
        try
        {
            var bundles = value?.Store?.FeaturedBundle?.Bundles;
            if (bundles == null)
                return;

            foreach (var bundleData in bundles)
            {
                if (string.IsNullOrEmpty(bundleData.DataAssetID))
                    continue;

                if (BundleControls.Where(x => x.BundleId == bundleData.DataAssetID).FirstOrDefault() is not null)
                    continue;

                var bData = await AssistApplication.AssistApiService.GetBundleAsync(bundleData.DataAssetID);

                var price = bundleData.Items.Sum(x => x.DiscountedPrice);
                BundleControls.Add(new BundleOfferControl()
                {
                    BundleName = (bData?.Name ?? "Bundle").ToUpper(),
                    BundleImage = bData?.DisplayIcon ?? string.Empty,
                    BundlePrice = $"{price:n0}",
                    BundleId = bundleData.DataAssetID,
                    Margin = bundles.Count > 1 ? new Thickness(0,5) : new Thickness(0,0,0,5)
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error("CreateBundleControl failed: {Message}", ex.Message);
            Log.Error(ex.StackTrace);
        }
    }

    private string GetOfferPrice(string itemOffer)
    {
        if (_offers is null) return "";

        var offer = _offers.Offers.Find(x => x.OfferID == itemOffer);
        if (offer is null)return "";
        
        return $"{offer.Cost.VP:n0}";
    }
    public record StoreStorageModel
    {
        public ValUserStore Store;
        public DateTime ResetTime;
    }

    
}
