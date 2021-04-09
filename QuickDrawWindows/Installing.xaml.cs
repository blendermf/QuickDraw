﻿using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace QuickDraw
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class InstallingWindow : Window
    {
        public void OnClosed(object sender, EventArgs args)
        {
            ((App)Application.Current).Shutdown();
        }

        public InstallingWindow()
        {
            InitializeComponent();

            this.Closed += OnClosed;
        }
    }
}
