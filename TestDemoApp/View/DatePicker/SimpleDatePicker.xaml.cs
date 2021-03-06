﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace TestDemoApp
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SimpleDatePicker : Page, INotifyPropertyChanged
    {
        public SimpleDatePicker()
        {
            this.InitializeComponent();
            this.CurrentDateTime = new DateTime(2013, 10, 29);
            this.DataContext = this;
        }

        /// <summary>
        /// 在此页将要在 Frame 中显示时进行调用。
        /// </summary>
        /// <param name="e">描述如何访问此页的事件数据。Parameter
        /// 属性通常用于配置页。</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private DateTime currentDateTime;

        public DateTime CurrentDateTime
        {
            get
            {
                return currentDateTime;
            }
            set
            {
                if (value == currentDateTime)
                    return;

                currentDateTime = value;

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDateTime"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDateTimeString"));

                }
            }
        }

        public String CurrentDateTimeString
        {
            get
            {
                return currentDateTime.ToString();
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentDateTime = new DateTime(1976, 10, 23);
        }
    }
}
