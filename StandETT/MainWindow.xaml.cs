﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StandETT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel vm = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = vm;
        }
        
        // private bool doClose = false;
        // private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        // {
        //     if (doClose) return;
        //     e.Cancel = true;
        //     await ClosingTasks();
        // }
        //
        // private async Task ClosingTasks()
        // {
        //     //TODO Вернуть!
        //     await vm.stand.ResetAllTests();
        //     doClose = true;
        //     await Task.Delay(TimeSpan.FromMilliseconds(100));
        //     Close();
        // }

    }
}