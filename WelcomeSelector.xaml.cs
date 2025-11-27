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

namespace StoreManager
{
    /// <summary>
    /// Interaction logic for WelcomeSelector.xaml
    /// </summary>
    public partial class WelcomeSelector : Page
    {
        private Frame _frame;
        public WelcomeSelector(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
        }

        private void SwitchLanguageBtn(object sender, RoutedEventArgs e)
        {
            var currentLang = LocalizeDictionary.Instance.Culture;
            if (currentLang.TwoLetterISOLanguageName == "en")
            {
                LocalizeDictionary.Instance.Culture = System.Globalization.CultureInfo.InvariantCulture;
            }

            else
            {
                LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo("en");
            }
        }

        private void CashierCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Navigálás a LoginPage-re azonosító bekéréssel
            _frame.Navigate(new LoginPage(_frame, LoginPage.LoginMode.Cashier, typeof(CashierPage)));
        }

        private void StorageCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Navigálás a LoginPage-re jelszó bekéréssel
            _frame.Navigate(new LoginPage(_frame, LoginPage.LoginMode.Storage, typeof(StoragePage)));
        }
    }
}
