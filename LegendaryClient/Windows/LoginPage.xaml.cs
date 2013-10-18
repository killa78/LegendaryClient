﻿using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LegendaryClient.Logic;
using LegendaryClient.Logic.Region;
using LegendaryClient.Logic.SQLite;
using PVPNetConnect.RiotObjects.Platform.Clientfacade.Domain;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            //Get client data after patcher completed
            Client.SQLiteDatabase = new SQLite.SQLiteConnection("gameStats_en_US.sqlite");
            Client.Champions = from s in Client.SQLiteDatabase.Table<champions>()
                               orderby s.name
                               select s;
            Client.ChampionSkins = from s in Client.SQLiteDatabase.Table<championSkins>()
                                   orderby s.name
                                   select s;
            Client.ChampionAbilities = from s in Client.SQLiteDatabase.Table<championAbilities>()
                                       orderby s.name
                                       select s;
            Client.Items = from s in Client.SQLiteDatabase.Table<items>()
                           orderby s.name
                           select s;

            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedUsername))
            {
                RememberUsernameCheckbox.IsChecked = true;
                LoginUsernameBox.Text = Properties.Settings.Default.SavedUsername;
            }
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedPassword))
            {
                RememberPasswordCheckbox.IsChecked = true;
                LoginPasswordBox.Password = Properties.Settings.Default.SavedPassword;
            }
            var uriSource = new Uri(Path.Combine(Client.ExecutingDirectory, "Assets", "bg.jpg"), UriKind.Absolute);
            LoginImage.Source = new BitmapImage(uriSource);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (RememberPasswordCheckbox.IsChecked == true)
                Properties.Settings.Default.SavedPassword = LoginPasswordBox.Password;
            else
                Properties.Settings.Default.SavedPassword = "";

            if (RememberUsernameCheckbox.IsChecked == true)
                Properties.Settings.Default.SavedUsername = LoginUsernameBox.Text;
            else
                Properties.Settings.Default.SavedUsername = "";
            Properties.Settings.Default.Save();
            LoginGrid.Visibility = Visibility.Hidden;
            LoggingInLabel.Visibility = Visibility.Visible;
            ErrorLabel.Visibility = Visibility.Hidden;
            Client.PVPNet.OnError += PVPNet_OnError;
            Client.PVPNet.OnLogin += PVPNet_OnLogin;
            BaseRegion SelectedRegion = BaseRegion.GetRegion((string)RegionComboBox.SelectedValue);
            Client.Region = SelectedRegion;
            Client.PVPNet.Connect(LoginUsernameBox.Text, LoginPasswordBox.Password, SelectedRegion.PVPRegion, "3.12.13_10_08_16_20");
        }

        void PVPNet_OnLogin(object sender, string username, string ipAddress)
        {
            Client.PVPNet.GetLoginDataPacketForUser(new LoginDataPacket.Callback(GotLoginPacket));
        }

        void PVPNet_OnError(object sender, PVPNetConnect.Error error)
        {
            //Display error message
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                LoginGrid.Visibility = Visibility.Visible;
                LoggingInLabel.Visibility = Visibility.Hidden;
                ErrorLabel.Visibility = Visibility.Visible;
                ErrorLabel.Content = error.Message;
            }));
        }

        private void GotLoginPacket(LoginDataPacket packet)
        {
            Client.LoginPacket = packet;
            Client.PVPNet.OnError -= PVPNet_OnError;
            Client.GameConfigs = packet.GameTypeConfigs;
            Client.PVPNet.Subscribe("bc", packet.AllSummonerData.Summoner.AcctId);
            Client.PVPNet.Subscribe("cn", packet.AllSummonerData.Summoner.AcctId);
            Client.PVPNet.Subscribe("gn", packet.AllSummonerData.Summoner.AcctId);
            Client.IsLoggedIn = true;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                foreach (Button b in Client.EnableButtons)
                {
                    BrushConverter bc = new BrushConverter();
                    Brush brush = (Brush)bc.ConvertFrom("#FFFFFF");
                    b.Foreground = brush;
                    if ((string)b.Content == "LOGIN")
                    {
                        b.Content = "LOGOUT";
                    }
                }
                MainPage MainPage = new MainPage();
                Client.SwitchPage(MainPage, "");
                Client.ClearPage(this);
            }));
        }
    }
}