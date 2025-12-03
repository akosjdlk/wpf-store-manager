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

            LocalizeDictionary.Instance.PropertyChanged += OnLanguageChanged;
            
            Loaded += (s, e) => ConfigureLoginMode();
            
            Unloaded += (s, e) => LocalizeDictionary.Instance.PropertyChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizeDictionary.Culture))
            {
                ConfigureLoginMode();
            }
        }

        private void ConfigureLoginMode()
        {
            if (_loginMode == LoginMode.Cashier)
            {
                TitleText.Text = LocalizeDictionary.Instance.GetLocalizedObject("StoreManager", "Resources.Strings", "LoginPage_cashierTitle", LocalizeDictionary.Instance.Culture)?.ToString() ?? "Cashier Login";
                IdentifierPanel.Visibility = Visibility.Visible;
                PasswordPanel.Visibility = Visibility.Collapsed;
                IdentifierTextBox.Focus();
            }
            else
            {
                TitleText.Text = LocalizeDictionary.Instance.GetLocalizedObject("StoreManager", "Resources.Strings", "LoginPage_storageTitle", LocalizeDictionary.Instance.Culture)?.ToString() ?? "Storage Login";
                IdentifierPanel.Visibility = Visibility.Visible;
                PasswordPanel.Visibility = Visibility.Visible;
                PasswordBox.Focus();
            }
        }

        private string GetLocalizedString(string key)
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(
                "StoreManager",
                "Resources.Strings",
                key,
                LocalizeDictionary.Instance.Culture)?.ToString() ?? key;
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
                CustomMessageBox.ShowError(
                    GetLocalizedString("LoginPage_errorInvalidUser"),
                    GetLocalizedString("LoginPage_errorTitle"));
                return;
            }

            if (_loginMode == LoginMode.Cashier)
            {
                NavigateToTargetPage(identifier);
            }
            else
            {
                string password = PasswordBox.Password;
                
                if (string.IsNullOrEmpty(password))
                {
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
                if (!user.CanAccessStorage)
                {
                    CustomMessageBox.ShowError(
                        GetLocalizedString("LoginPage_errorNoStorageAccess"),
                        GetLocalizedString("LoginPage_errorTitle"));
                    return;
                }
                if (user.Password != password)
                {
                    CustomMessageBox.ShowError(
                        GetLocalizedString("LoginPage_errorWrongPassword"),
                        GetLocalizedString("LoginPage_errorTitle"));
                    return;
                }
                NavigateToTargetPage(password);
            }
        }

        private void NavigateToTargetPage(string credential)
        {
            var targetPage = Activator.CreateInstance(_targetPageType, _frame);
            _frame.Navigate(targetPage);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            while (_frame.CanGoBack)
            {
                _frame.RemoveBackEntry();
            }
            
            _frame.Navigate(new WelcomeSelector(_frame));
        }
    }
}
