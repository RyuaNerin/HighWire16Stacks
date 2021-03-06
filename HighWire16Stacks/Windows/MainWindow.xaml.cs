﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using HighWire16Stacks.Core;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace HighWire16Stacks.Windows
{
    internal partial class MainWindow : MetroWindow
    {
        private static MainWindow instance;
        public static MainWindow Instance => instance;

        private readonly List<Process> m_processList = new List<Process>();
        private readonly CollectionView m_processListView;

        private readonly ObservableCollection<FStatus> m_buffList = new ObservableCollection<FStatus>();
        private readonly CollectionView m_buffListView;
        private string m_buffListFilter = null;
        private bool m_buffShowBuff = false;
        private bool m_buffShowDebuff = false;
        private bool m_buffShowCheckedOnly = false;

        public IntPtr Handle { get; private set; }

        public MainWindow()
        {
            Settings.Load();
            MainWindow.instance = this;
            
            this.m_buffList = new ObservableCollection<FStatus>();
            this.m_buffListView = (CollectionView)CollectionViewSource.GetDefaultView(this.m_buffList);
            this.m_buffListView.Filter = this.BuffListView_Filter;
            ActiveLiveFiltering(this.m_buffListView, "IsChecked");

            this.m_processListView = (CollectionView)CollectionViewSource.GetDefaultView(this.m_processList);

            this.DataContext = Settings.Instance;
            InitializeComponent();
            
            var interop = new WindowInteropHelper(this);
            interop.EnsureHandle();
            this.Handle = interop.Handle;

            Sentry.AddHandler(this.Dispatcher);

            this.ctlContent.IsEnabled = false;
        }

        private static void ActiveLiveFiltering(ICollectionView collectionView, string propertyName)
        {
            var collectionViewLiveShaping = collectionView as ICollectionViewLiveShaping;
            if (collectionViewLiveShaping.CanChangeLiveFiltering)
            {
                collectionViewLiveShaping.LiveFilteringProperties.Add(propertyName);
                collectionViewLiveShaping.IsLiveFiltering = true;
            }
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
            var newVersionUrl = await Task.Factory.StartNew(() => GithubLastestRealease.CheckNewVersion("RyuaNerin", "HighWire16Stacks", (vs) => {
                    Version v;    
                    if (!Version.TryParse(vs, out v))
                        return false;

                    return v > System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
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

            FResource.DownloadProgressChanged += le => this.Dispatcher.Invoke(new Action(() => this.ctlProgress.Value = le));

            string msg = null;
            switch (await Task.Factory.StartNew(FResource.ReadResource))
            {
                case FResource.ResourceResult.NetworkError: msg = "최신 리소스 데이터를 읽지 못하였습니다."; break;
                case FResource.ResourceResult.UnknownError: msg = "알 수 없는 오류가 발생하였습니다.";       break;
                case FResource.ResourceResult.DataError:
                    msg = "리소스를 읽지 못하였습니다.";

                    try
                    {
                        File.Delete(FResource.ResourcePath);
                    }
                    catch
                    { }
                    break;
            }
            this.ctlProgress.Value = 0;

            if (msg != null)
            {
                await this.ShowMessageAsync(null, msg);
                App.Current.Shutdown();
                return;
            }

            this.m_buffListView.Refresh();

            for (int i = 0; i < FResource.StatusList.Count; ++i)
                this.m_buffList.Add(FResource.StatusList[i]);
            this.ctlBuffList.ItemsSource = this.m_buffListView;
            
            this.ctlProcessList.ItemsSource = this.m_processListView;

            this.ctlContent.IsEnabled = true;

            Worker.OverlayInstance.Show();
            Worker.OverlayInstance.Refresh();

            this.ctlShowingMode.SelectedIndex = (int)Settings.Instance.ShowingMode;

            this.ctlProcessRefresh_Click(null, null);
        }

        internal void ExitedProcess()
        {
            this.m_processList.Clear();
        }

        private void ctlProcessRefresh_Click(object sender, RoutedEventArgs e)
        {
            this.m_processList.Clear();
            this.m_processList.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
            this.m_processList.AddRange(Process.GetProcessesByName("ffxiv_dx11_multi"));
            
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

        private void ctlShowBuff_Checked(object sender, RoutedEventArgs e)
        {
            this.m_buffShowBuff = true;
            this.m_buffListView.Refresh();
        }

        private void ctlShowBuff_Unchecked(object sender, RoutedEventArgs e)
        {
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

        private void ctlBuffList_CheckAll_Click(object sender, RoutedEventArgs e)
        {
            FStatus status;
            for (int i = 0; i < this.ctlBuffList.SelectedItems.Count; ++i)
            {
                status = (FStatus)this.ctlBuffList.SelectedItems[i];
                status.IsChecked = true;
            }
        }

        private void ctlBuffList_UnCheckAll_Click(object sender, RoutedEventArgs e)
        {
            FStatus status;
            for (int i = 0; i < this.ctlBuffList.SelectedItems.Count; ++i)
            {
                status = (FStatus)this.ctlBuffList.SelectedItems[i];
                status.IsChecked = false;
            }
        }

        private void ctlBuffList_ReverseCheck_Click(object sender, RoutedEventArgs e)
        {
            FStatus status;
            for (int i = 0; i < this.ctlBuffList.SelectedItems.Count; ++i)
            {
                status = (FStatus)this.ctlBuffList.SelectedItems[i];
                status.IsChecked = !status.IsChecked;
            }
        }
    }
}
