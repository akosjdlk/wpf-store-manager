using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFLocalizeExtension.Engine;
using StoreManager.Classes;

namespace StoreManager
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private Frame _frame;
        private LoginMode _loginMode;
        private Type? _targetPageType;

        public enum LoginMode
        {
            Cashier,
            Storage
        }

        public LoginPage(Frame frame, LoginMode mode, Type? targetPageType)
        {
            InitializeComponent();
            _frame = frame;
            _loginMode = mode;
            _targetPageType = targetPageType;

            ConfigureLoginMode();
        }

        private void ConfigureLoginMode()
        {
            if (_loginMode == LoginMode.Cashier)
            {
                TitleText.Text = LocalizeDictionary.Instance.GetLocalizedObject("StoreManager", "Resources.Strings", "LoginPage_cashierTitle", LocalizeDictionary.Instance.Culture).ToString();
                IdentifierPanel.Visibility = Visibility.Visible;
                PasswordPanel.Visibility = Visibility.Collapsed;
                IdentifierTextBox.Focus();
            }
            else
            {
                TitleText.Text = LocalizeDictionary.Instance.GetLocalizedObject("StoreManager", "Resources.Strings", "LoginPage_storageTitle", LocalizeDictionary.Instance.Culture).ToString();
                IdentifierPanel.Visibility = Visibility.Visible;
                PasswordPanel.Visibility = Visibility.Visible;
                PasswordBox.Focus();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            string identifier = IdentifierTextBox.Text.Trim();
                
            if (string.IsNullOrEmpty(identifier))
            {
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            User? user = await User.GetUser(Convert.ToInt16(identifier));
            if (user == null)
            {
                // Error
                Console.WriteLine("Error");
                return;
            }

            if (_loginMode == LoginMode.Cashier)
            {
                NavigateToTargetPage(identifier);
            }
            else // Storage
            {
                string password = PasswordBox.Password;
                
                if (string.IsNullOrEmpty(password))
                {
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
                if (user.CanAccessStorage && user.Password == password)
                {
                    NavigateToTargetPage(password);
                } else
                {
                    Console.WriteLine("nem");
                }
                

            }
        }

        private void NavigateToTargetPage(string credential)
        {
            // Létrehozzuk a target page példányt a Frame paraméterrel
            var targetPage = Activator.CreateInstance(_targetPageType, _frame);
            _frame.Navigate(targetPage);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _frame.Navigate(new WelcomeSelector(_frame));
        }
    }
}
