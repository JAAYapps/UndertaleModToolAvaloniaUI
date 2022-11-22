using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace UndertaleModToolAvalonia.Views
{
    public partial class MessageBox : Window
    {
        private TextBlock msg;

        public MessageBox()
        {

        }

        private MessageBox(string message) : this()
        {

            this.InitializeComponent();
            msg = this.FindControl<TextBlock>("message");
            msg.Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static void Show(string message)
        {
            try
            {
                MessageBox msg = new MessageBox(message);
                msg.Show();
            }
            catch (Exception)
            {
                System.AppDomain.Unload(AppDomain.CurrentDomain);
            }
        }

        public static void Show(string message, Window perent)
        {
            try
            {
                MessageBox msg = new MessageBox(message);
                msg.ShowDialog(perent);
            }
            catch (Exception)
            {

                System.AppDomain.Unload(AppDomain.CurrentDomain);
            }
        }

        public static void Show(string[] message, Window perent)
        {
            try
            {
                string text = "";
                foreach (string item in message)
                {
                    text += item + Environment.NewLine;
                }
                MessageBox msg = new MessageBox(text);
                msg.ShowDialog(perent);
            }
            catch (Exception)
            {
                System.AppDomain.Unload(AppDomain.CurrentDomain);
            }
        }
    }
}
