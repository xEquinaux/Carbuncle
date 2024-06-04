using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

using System.Diagnostics;
using System.Timers;

namespace Carbuncle_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            if (!File.Exists("ip_server.ini"))
            {
                using (File.CreateText("ip_server.ini")) ;
            }
        }

        public static ChatUser LocalUser;
        public string Username
        {
            get; set;
        }
        public string UserColor
        {
            get; set;
        }
        public string Message
        {
            get; set;
        }
        public Image[] Badge
        {
            get; private set;
        }
        public void DisplayMessage(string Username, string UserColor, string Message, bool emote = false)
        {
            MainWindow.Instance.Dispatcher.Invoke((Action)delegate
            {
                Bold bold = new Bold(new Run(Username));
                bold.Foreground = (Brush)new BrushConverter().ConvertFromString(UserColor);
                if (Badge != null)
                {
                    foreach (Image img in Badge)
                    {
                        text_chat.Inlines.Add(img);
                        text_chat.Inlines.Add(" ");
                    }
                }
                if (emote)
                {
                    Bold em = new Bold(new Run(string.Format(" {0}{1}", new object[] { Message, "\n" })));
                    em.Foreground = (Brush)new BrushConverter().ConvertFromString(UserColor);
                    text_chat.Inlines.Add(bold);
                    text_chat.Inlines.Add(em);
                }
                else
                {
                    text_chat.Inlines.Add(bold);
                    text_chat.Inlines.Add(string.Format("{0} {1}{2}", new object[] { ":", Message, "\n" }));
                }
                richtextbox.ScrollToEnd();
            });
        }

        private void On_ClickSend(object sender, RoutedEventArgs e)
        {
            LocalUser.SendNetworkMessage(textbox_message.Text);
            textbox_message.Clear();
        }

        private void On_ClickLogin(object sender, RoutedEventArgs e)
        {
            if (textbox_username.Text.Length > 3 && textbox_password.Password.Length == 4 && box_colors.SelectedIndex != -1)
            {
                LocalUser = new ChatUser()
                {
                    Color = box_colors.SelectedItem.ToString().Substring(box_colors.SelectedItem.ToString().LastIndexOf(':') + 2),
                    Name = textbox_username.Text.Replace(' ', '_')
                };
                if (!LocalUser.RemoteRegister(textbox_password.Password))
                {
                    return;
                }
                textbox_ip.Text = textbox_ipstart.Text;
                grid_login.Visibility = Visibility.Hidden;
                tabcontrol.Visibility = Visibility.Visible;
                (LocalUser.userThread = LocalUser.NetworkLoop()).Start();
            }
        }

        private void textbox_message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;
            if (e.Key == Key.Enter && !e.IsRepeat)
            {
                if (textbox_message.Text.Length != 0)
                {
                    LocalUser.SendNetworkMessage(textbox_message.Text);
                    textbox_message.Clear();
                }
            }
        }

        public string StringFromStream(NetworkStream stream)
        {
            string buf = "";
            byte[] buffer = new byte[256];
            using (MemoryStream mem = new MemoryStream())
            {
                int bytesRead = 0;
                while (stream.DataAvailable)
                {
                    bytesRead += stream.Read(buffer, 0, buffer.Length);
                }
                buf = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            return buf;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LocalUser?.client?.Close();
            ChatUser.Interrupt = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (IPAddress.TryParse(textbox_ip.Text, out _))
            {
                LocalUser?.client?.Close();
                ChatUser.Interrupt = true;
                grid_login.Visibility = Visibility.Visible;
                tabcontrol.Visibility = Visibility.Hidden;
            }
        }

        private void box_colors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (box_colors.SelectedIndex != -1)
                label_usercolor.Foreground = (Brush)new BrushConverter().ConvertFromString(box_colors.SelectedItem.ToString().Substring(box_colors.SelectedItem.ToString().LastIndexOf(':') + 2));
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textbox_ipstart.IsEnabled = box_ipdirectory.SelectedIndex == -1 || box_ipdirectory.SelectedIndex == 0;
            if (!textbox_ipstart.IsEnabled)
                textbox_ipstart.Text = ((ComboBoxItem)box_ipdirectory.SelectedItem).Content.ToString();
        }

        private void button_grab_Click(object sender, RoutedEventArgs e)
        {
            TcpClient client;
            NetworkStream stream;

            string text = "";
            using (StreamReader read = new StreamReader("ip_server.ini"))
                text = read.ReadLine().TrimEnd(new char[] { '\n', '\r' });
            
            if (IPAddress.TryParse(text, out IPAddress addr))
            {
                client = new TcpClient();
                client.Connect(addr, 1030);
                stream = client.GetStream();

                byte[] buffer = Encoding.ASCII.GetBytes("GRAB");
                if (!client.Connected)
                    return;

                stream.Write(buffer, 0, buffer.Length);

                int num = -1;
                const int max = 10;
                bool go = true;
                while (!stream.DataAvailable)
                {
                    Thread.Sleep(1000);
                    if (++num == max)
                    {
                        go = false;
                        break;
                    }
                }
                if (!go) return;

                string[] array = StringFromStream(stream).Split(',');
                for (int i = 0; i < array.Length; i++)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = array[i];
                    if (!box_ipdirectory.Items.Contains(item))
                    {
                        box_ipdirectory.Items.Add(item);
                    }
                }
            }
        }

        private void button_host_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(s_textbox_maxplayers.Text, out _) && s_box_worldsize.SelectedIndex != -1 && s_textbox_worldname.Text.Length > 2 && s_box_difficulty.SelectedIndex != -1)
            {
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Parse(textbox_ip.Text), 1040);

                if (!client.Connected)
                    return;

                NetworkStream stream = client.GetStream();

                //const byte _PLAYERS = 0, _SIZE = 1, _NAME = 2, _DIFFICULTY = 3, _SEED = 4, _PASS = 5;
                byte[] buffer = Encoding.ASCII.GetBytes(string.Format("{0},{1},{2},{3},{4},{5}", new object[]
                {
                    s_textbox_maxplayers.Text,
                    s_box_worldsize.Text,
                    s_textbox_worldname.Text,
                    s_box_difficulty.Text,
                    s_textbox_worldseed.Text,
                    s_passbox_serverpassword.Password
                }));
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        private void maxplayers_OnFocus(object sender, RoutedEventArgs e)
        {
            if (s_textbox_maxplayers.Text == "1-255")
            {
                s_textbox_maxplayers.Clear();
                s_textbox_maxplayers.MaxLength = 3;
            }
        }

        double[] progress = new double[4];
        private void s_textbox_maxplayers_LostFocus(object sender, RoutedEventArgs e)
        {
            if (s_textbox_maxplayers.Text == "")
            {
                s_textbox_maxplayers.MaxLength = 5;
                s_textbox_maxplayers.Text = "1-255";
            }

            if (s_textbox_maxplayers.Text.Length > 0 && s_textbox_maxplayers.Text != "1-255")
            {
                progress[0] = 25;
            }
            else progress[0] = 0;

            UpdateProgress();
        }
      

        private void worldsize_LostFocus(object sender, RoutedEventArgs e)
        {
            progress[1] = s_box_worldsize.SelectedIndex != -1 ? 25 : 0;
            UpdateProgress();
        }

        private void worldname_LostFocus(object sender, RoutedEventArgs e)
        {
            progress[2] = s_textbox_worldname.Text.Length > 3 ? 25 : 0;
            UpdateProgress();
        }

        private void difficulty_LostFocus(object sender, RoutedEventArgs e)
        {
            progress[3] = s_box_difficulty.SelectedIndex != -1 ? 25 : 0;
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            double num = 0;
            for (int i = 0; i < progress.Length; i++)
            {
                num += progress[i];
            }
            progress_server.Value = Math.Min(100, num);
        }
    }
}
