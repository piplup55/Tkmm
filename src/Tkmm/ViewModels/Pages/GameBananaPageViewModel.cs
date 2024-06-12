﻿using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using System.Text.Json;
using Tkmm.Core;
using Tkmm.Core.Helpers;
using Tkmm.Core.Models.GameBanana;
using Tkmm.Helpers;

namespace Tkmm.ViewModels.Pages;

public partial class GameBananaPageViewModel : ObservableObject
{
    private static readonly HttpClient _client = new();

    private const string GAME_ID = "7617";
    private const string FEED_ENDPOINT = $"/Game/{GAME_ID}/Subfeed?_nPage={{0}}&_csvModelInclusions=Mod";
    private const string FEED_ENDPOINT_SEARCH = $"/Game/{GAME_ID}/Subfeed?_nPage={{0}}&_sName={{1}}&_csvModelInclusions=Mod";

    private static GameBananaFeed? _sugesstedModsFeed = GetSuggestedFeed();

    [ObservableProperty]
    private string _searchArgument = string.Empty;

    [ObservableProperty]
    private int _page = 0;

    [ObservableProperty]
    private bool _isShowingSuggested = false;

    [ObservableProperty]
    private GameBananaFeed _feed = new();

    public GameBananaPageViewModel()
    {
        InitLoad();
    }

    [RelayCommand]
    public async Task Search(ScrollViewer modsViewer)
    {
        Page = 0;
        await UpdatePage();
        modsViewer.ScrollToHome();
    }

    [RelayCommand]
    public async Task ResetSearch(ScrollViewer modsViewer)
    {
        Page = 0;
        SearchArgument = string.Empty;
        await UpdatePage();
        modsViewer.ScrollToHome();
    }

    [RelayCommand]
    public async Task NextPage(ScrollViewer modsViewer)
    {
        Page++;
        await UpdatePage();
        modsViewer.ScrollToHome();
    }

    [RelayCommand]
    public async Task PrevPage(ScrollViewer modsViewer)
    {
        Page--;
        await UpdatePage();
        modsViewer.ScrollToHome();
    }

    [RelayCommand]
    public async Task ShowSuggested(ScrollViewer modsViewer)
    {
        _sugesstedModsFeed ??= GetSuggestedFeed();

        if (IsShowingSuggested == false) {
            await UpdatePage();
            modsViewer.ScrollToHome();
            return;
        }

        await UpdatePage(_sugesstedModsFeed);
    }

    [RelayCommand]
    public static async Task InstallMod(GameBananaModInfo mod)
    {
        StackPanel panel = new() {
            Spacing = 5
        };

        panel.Children.Add(new TextBlock {
            Text = mod.Name,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        panel.Children.Add(new TextBlock {
            Text = "Choose a file to install:",
            FontSize = 11,
            Margin = new(15, 10, 0, 0),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        ArgumentNullException.ThrowIfNull(mod.Full);

        bool first = true;
        foreach (var file in mod.Full.Files) {
            panel.Children.Add(new RadioButton {
                GroupName = "@",
                Content = file.Name,
                IsChecked = first,
                Tag = file
            });

            first = false;
        }

        ContentDialog dialog = new() {
            Title = $"Install {mod.Name}?",
            Content = panel,
            SecondaryButtonText = "No",
            PrimaryButtonText = "Yes"
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
            if (panel.Children.FirstOrDefault(x => x is RadioButton radioButton && radioButton.IsChecked == true)?.Tag is GameBananaFile file) {
                AppStatus.Set($"Downloading '{file.Name}'", "fa-solid fa-download", isWorkingStatus: true);
                await ModHelper.Import(file, mod.Full.FromFile);
            }
        }
    }

    private async void InitLoad()
    {
        await UpdatePage();
    }

    private async Task UpdatePage(GameBananaFeed? customFeed = null)
    {
        if (customFeed is null) {
            IsShowingSuggested = false;
        }

        Feed = await Fetch(Page + 1, SearchArgument, customFeed);
    }

    private static async Task<GameBananaFeed> Fetch(int page, string search, GameBananaFeed? customFeed = null)
    {
        string endpoint = !string.IsNullOrEmpty(search) && search.Length > 2
            ? string.Format(FEED_ENDPOINT_SEARCH, page, search)
            : string.Format(FEED_ENDPOINT, page);

        using Stream stream = await GameBananaHelper.Get(endpoint);
        GameBananaFeed feed = customFeed ?? JsonSerializer.Deserialize<GameBananaFeed>(stream)
            ?? throw new InvalidOperationException($"Could not parse feed from '{FEED_ENDPOINT}'");

        await Task.WhenAll(feed.Records.Select(x => x.FetchMetadata()));
        feed.Records = [.. feed.Records.Where(x =>
            x.Full?.IsTrashed == false &&
            x.Full?.IsFlagged == false &&
            x.IsObsolete == false &&
            x.IsContentRated == false &&
            x.Full?.IsPrivate == false
        )];

        _ = Task.Run(() => DownloadThumbnails(feed));
        return feed;
    }

    private static async Task DownloadThumbnails(GameBananaFeed feed)
    {
        foreach (var mod in feed.Records) {
            if (mod.Media.Images.FirstOrDefault() is GameBananaImage img) {
                byte[] image = await _client
                    .GetByteArrayAsync($"{img.BaseUrl}/{img.SmallFile}");
                using MemoryStream ms = new(image);
                mod.Thumbnail = new Bitmap(ms);
            }
        }
    }

    private static GameBananaFeed? GetSuggestedFeed()
    {
        string path = Path.Combine(Config.Shared.StaticStorageFolder, "suggested.json");
        if (File.Exists(path)) {
            using FileStream fs = File.OpenRead(path);
            return JsonSerializer.Deserialize<GameBananaFeed>(fs);
        }

        return null;
    }
}
