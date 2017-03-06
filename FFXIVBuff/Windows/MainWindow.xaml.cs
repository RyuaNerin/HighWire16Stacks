using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using FFXIVBuff.Core;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace FFXIVBuff.Windows
{
    internal partial class MainWindow : MetroWindow
    {
        private static MainWindow m_instance;
        public static MainWindow Instance { get { return MainWindow.m_instance; } }

        private readonly List<Process> m_processList = new List<Process>();
        private readonly ICollectionView m_processListView;

        private readonly ICollectionView m_buffListView;
        private string m_buffListFilter = null;
        private bool m_buffShowBuff = false;
        private bool m_buffShowDebuff = false;
        private bool m_buffShowCheckedOnly = false;

        public IntPtr Handle { get; private set; }

        public MainWindow()
        {
            MainWindow.m_instance = this;

            this.m_buffListView = CollectionViewSource.GetDefaultView(FResource.StatusList);
            this.m_buffListView.Filter = this.BuffListView_Filter;

            this.m_processListView = CollectionViewSource.GetDefaultView(this.m_processList);

            InitializeComponent();

            var interop = new WindowInteropHelper(this);
            interop.EnsureHandle();
            this.Handle = interop.Handle;

            this.ctlContent.IsEnabled = false;

            Sentry.AddHandler(this.Dispatcher);
        }

        private void MetroWindow_Closed(object sender, System.EventArgs e)
        {
            Sentry.RemoveHandler(this.Dispatcher);

            Worker.Unload();
            App.Current.Shutdown();
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            var newVersionUrl = await Task.Factory.StartNew(() => GithubLastestRealease.CheckNewVersion("RyuaNerin", "FBOverlay", (vs) => {
                    Version v;    
                    if (!Version.TryParse(vs, out v))
                        return false;

                    return v > System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                }));

            if (newVersionUrl != null)
            {
                await this.ShowMessageAsync(
                    this.Title,
                    "새로운 업데이트가 있습니다",
                    MahApps.Metro.Controls.Dialogs.MessageDialogStyle.Affirmative);

                Application.Current.Shutdown();
                return;
            }
#endif

            Worker.Load();

            await Task.Factory.StartNew(Worker.Update);

            await Task.Factory.StartNew(FResource.ReadResources);
            this.m_buffListView.Refresh();

            this.ctlBuffList.ItemsSource = this.m_buffListView;
            this.ctlProcessList.ItemsSource = this.m_processListView;

            this.ctlContent.IsEnabled = true;

            this.ctlProcessRefresh_Click(null, null);

            Worker.OverlayInstance.Show();
            Worker.OverlayInstance.Refresh();

            await this.ShowMessageAsync("test", "123 456 13 135 153 135 135 153 435 13 51 35 43");
        }

        internal void ExitedProcess()
        {
            this.m_processList.Clear();
        }

        private void ctlProcessRefresh_Click(object sender, RoutedEventArgs e)
        {
            this.m_processList.Clear();
            this.m_processList.AddRange(Process.GetProcessesByName("ffxiv"));
            //this.m_processList.AddRange(Process.GetProcessesByName("ffxiv_multi"));
            this.m_processList.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
            //this.m_processList.AddRange(Process.GetProcessesByName("ffxiv_dx11_multi"));
            
            this.m_processListView.Refresh();

            this.ctlProcessList.IsEnabled = this.m_processList.Count > 0;

            if (this.m_processList.Count > 0)
            {
                this.ctlProcessList.SelectedIndex = 0;
                this.ctlProcessSelect.IsEnabled = true;

                if (this.m_processList.Count == 1)
                    this.ctlProcessSelect_Click(null, null);
            }
        }

        private async void ctlProcessSelect_Click(object sender, RoutedEventArgs e)
        {
            if (this.ctlProcessList.SelectedIndex >= 0)
            {
                var proc = (Process)this.ctlProcessList.SelectedItem;

                if (!Worker.SetProcess(proc))
                {
                    await this.ShowMessageAsync(
                        this.Title,
                        "지원되지 않는 클라이언트입니다",
                        MahApps.Metro.Controls.Dialogs.MessageDialogStyle.Affirmative);

                    return;
                }
                this.m_processList.RemoveAll(le => le != proc);

                this.m_processListView.Refresh();

                this.ctlProcessList.SelectedIndex = 0;
                this.ctlProcessList.IsEnabled = false;

                this.ctlProcessSelect.IsEnabled = false;
            }
        }

        private void ctlBuffListFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var txt = this.ctlBuffListFilter.Text;
            this.m_buffListFilter = string.IsNullOrWhiteSpace(txt) ? null : this.ctlBuffListFilter.Text;
            this.m_buffListView.Refresh();
        }

        private bool BuffListView_Filter(object obj)
        {
            var item = obj as FStatus;
            return
                (
                    item.Id != 0
                )
                &&
                (
                    (this.m_buffShowBuff   && !item.IsDebuff)
                    ||
                    (this.m_buffShowDebuff &&  item.IsDebuff)
                )
                &&
                (
                    !this.m_buffShowCheckedOnly
                    ||
                    item.IsChecked
                )
                &&
                (
                    this.m_buffListFilter == null
                    ||
                    item.Name.IndexOf(this.m_buffListFilter, StringComparison.CurrentCultureIgnoreCase) != -1
                );
        }

        private void SetListGroupName(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ctlListGroup.Header = this.ctlShowChecked.IsChecked.Value ? "보일 버프/디버프 선택" : "숨길 버프/디버프 선택";
            }
            catch
            {
            }
        }

        private void ctlShowBuff_Checked(object sender, RoutedEventArgs e)
        {
            this.m_buffShowBuff = true;
            this.m_buffListView.Refresh();
        }

        private void ctlShowBuff_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!this.ctlShowDebuff.IsChecked.Value)
                {
                    this.ctlShowBuff.IsChecked = true;
                    return;
                }
            }
            catch
            {
                return;
            }

            this.m_buffShowBuff = false;
            this.m_buffListView.Refresh();
        }

        private void ctlShowDebuff_Checked(object sender, RoutedEventArgs e)
        {
            this.m_buffShowDebuff = true;
            this.m_buffListView.Refresh();
        }

        private void ctlShowDebuff_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!this.ctlShowBuff.IsChecked.Value)
                {
                    this.ctlShowDebuff.IsChecked = true;
                    return;
                }
            }
            catch
            {
                return;
            }

            this.m_buffShowDebuff = false;
            this.m_buffListView.Refresh();
        }

        private void ctlShowChecked_Checked(object sender, RoutedEventArgs e)
        {
            this.m_buffShowCheckedOnly = true;
            this.m_buffListView.Refresh();
        }

        private void ctlShowChecked_Unchecked(object sender, RoutedEventArgs e)
        {
            this.m_buffShowCheckedOnly = false;
            this.m_buffListView.Refresh();
        }
    }
}
