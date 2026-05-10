using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace RedVoltWebBrowser2
{
    public partial class MainWindow : Window
    {
        private bool isGhostMode = true;
        private bool isSleepingTabs = true;
        public static List<string> HistoryList = new List<string>();
        public static List<string> DownloadList = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            CreateNewTab("https://www.google.com");
        }

        private async void CreateNewTab(string url)
        {
            TabItem newTab = new TabItem { Header = "Loading..." };
            WebView2 wv = new WebView2();
            newTab.Content = wv;
            BrowserTabs.Items.Add(newTab);
            BrowserTabs.SelectedItem = newTab;

            try 
            {
                await wv.EnsureCoreWebView2Async();
                
                string ghostScript = "const kill = () => { document.querySelectorAll('[id*=cookie],[class*=cookie],[id*=consent]').forEach(el => el.remove()); }; setInterval(kill, 1500);";
                await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(ghostScript);

                wv.CoreWebView2.NavigationStarting += (s, e) => { SiteLoadingBar.Visibility = Visibility.Visible; SiteLoadingBar.IsIndeterminate = true; };
                wv.CoreWebView2.NavigationCompleted += (s, e) => { SiteLoadingBar.Visibility = Visibility.Hidden; SiteLoadingBar.IsIndeterminate = false; };
                
                wv.CoreWebView2.SourceChanged += (s, e) => {
                    if (BrowserTabs.SelectedItem == newTab) AddressBar.Text = wv.Source.ToString();
                    if (!HistoryList.Contains(wv.Source.ToString())) HistoryList.Add(wv.Source.ToString());
                };

                wv.CoreWebView2.DocumentTitleChanged += (s, e) => { newTab.Header = wv.CoreWebView2.DocumentTitle; };
                wv.CoreWebView2.Navigate(url);
            } 
            catch (Exception) { }
        }

        private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserTabs?.SelectedItem is TabItem t && t.Content is WebView2 wv)
            {
                if (wv.CoreWebView2 != null) AddressBar.Text = wv.Source?.ToString();
                
                if (isSleepingTabs)
                {
                    foreach (TabItem item in BrowserTabs.Items)
                    {
                        if (item != t && item.Content is WebView2 inactiveWv && inactiveWv.CoreWebView2 != null)
                        {
                            // FIXED: Use property assignment instead of Set... method
                            inactiveWv.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                        }
                    }
                    if (wv.CoreWebView2 != null)
                        wv.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
                }
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && GetWv() != null)
            {
                string q = AddressBar.Text;
                if (!q.Contains(".") || q.Contains(" ")) q = "https://www.google.com/search?q=" + q;
                else if (!q.StartsWith("http")) q = "https://" + q;
                GetWv().CoreWebView2.Navigate(q);
            }
        }

        private WebView2 GetWv() => (BrowserTabs?.SelectedItem as TabItem)?.Content as WebView2;

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserTabs.Items.Count > 1)
            {
                TabItem tab = (sender as Button)?.Tag as TabItem;
                if (tab != null) BrowserTabs.Items.Remove(tab);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void MinBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void MaxBtn_Click(object sender, RoutedEventArgs e) => this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void NewTabButton_Click(object sender, RoutedEventArgs e) => CreateNewTab("https://www.google.com");
        private void BackButton_Click(object sender, RoutedEventArgs e) => GetWv()?.GoBack();
        private void ForwardButton_Click(object sender, RoutedEventArgs e) => GetWv()?.GoForward();
        private void RefreshButton_Click(object sender, RoutedEventArgs e) => GetWv()?.CoreWebView2?.Reload();
        private void OpenHistory_Click(object sender, RoutedEventArgs e) => ShowListWindow("History", HistoryList);
        private void OpenDownloads_Click(object sender, RoutedEventArgs e) => ShowListWindow("Downloads", DownloadList);
        private void OpenSettings_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Ghost Mode & Sleeping Tabs are Active.", "Titanium v6");

        private void ShowListWindow(string title, List<string> data)
        {
            Window win = new Window { Title = title, Width = 400, Height = 500, Background = new SolidColorBrush(Color.FromRgb(10,10,10)), WindowStyle = WindowStyle.ToolWindow, Topmost = true };
            ListBox lb = new ListBox { Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(10) };
            foreach (var item in data) lb.Items.Add(item);
            win.Content = lb;
            win.Show();
        }
    }
}
