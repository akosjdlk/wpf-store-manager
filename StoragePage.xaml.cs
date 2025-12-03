using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using StoreManager.Classes;
using WPFLocalizeExtension.Engine;

namespace StoreManager
{
    public partial class StoragePage : Page
    {
        private Frame _frame;
        private ObservableCollection<ProductViewModel> _products = new();
        private List<Product> _allProducts = new();
        private Product? _currentEditProduct = null;
        private bool _isAddingNew = false;

        public StoragePage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
            
            ProductsItemsControl.ItemsSource = _products;

            LocalizeDictionary.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LocalizeDictionary.Culture))
                {
                    RefreshProductList();
                }
            };

            Loaded += async (s, e) => await LoadProducts();
        }

        private string GetLocalizedString(string key)
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(
                "StoreManager",
                "Resources.Strings",
                key,
                LocalizeDictionary.Instance.Culture)?.ToString() ?? key;
        }

        private async Task LoadProducts()
        {
            try
            {
                _allProducts = await Product.GetAll();
                DisplayProducts(_allProducts.Take(150).ToList());
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError(
                    string.Format(GetLocalizedString("StoragePage_loadProductsError"), ex.Message),
                    GetLocalizedString("StoragePage_errorTitle"));
            }
        }

        private void DisplayProducts(List<Product> products)
        {
            _products.Clear();
            foreach (var product in products)
            {
                _products.Add(new ProductViewModel(product));
            }
            UpdateEmptyListVisibility();
        }

        private void RefreshProductList()
        {
            var tempProducts = _products.Select(vm => vm.Product).ToList();
            _products.Clear();
            foreach (var product in tempProducts)
            {
                _products.Add(new ProductViewModel(product));
            }
        }

        private void UpdateEmptyListVisibility()
        {
            EmptyListPanel.Visibility = _products.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim().ToLower();
            
            if (string.IsNullOrEmpty(searchText))
            {
                DisplayProducts(_allProducts.Take(150).ToList());
                return;
            }

            var filtered = _allProducts
                .Where(p => 
                    p.Id.ToString().Contains(searchText) ||
                    p.Name.ToLower().Contains(searchText))
                .ToList();

            DisplayProducts(filtered);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox_TextChanged(sender, new TextChangedEventArgs(e.RoutedEvent, UndoAction.None));
        }

        private void AddNewProduct_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = true;
            _currentEditProduct = null;
            
            EditIdTextBox.Text = GetLocalizedString("StoragePage_newProductId");
            EditNameTextBox.Text = "";
            EditUnitTextBox.Text = "";
            EditStockTextBox.Text = "0";
            EditSupplierPriceTextBox.Text = "0";
            EditSalePriceTextBox.Text = "0";
            EditVatTextBox.Text = "27";
            EditFractionableCheckBox.IsChecked = false;
            
            EditPanel.Visibility = Visibility.Visible;
            EditNameTextBox.Focus();
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProductViewModel viewModel)
            {
                _isAddingNew = false;
                _currentEditProduct = viewModel.Product;
                
                EditIdTextBox.Text = _currentEditProduct.Id.ToString();
                EditNameTextBox.Text = _currentEditProduct.Name;
                EditUnitTextBox.Text = _currentEditProduct.Unit;
                EditStockTextBox.Text = _currentEditProduct.Stock.ToString();
                EditSupplierPriceTextBox.Text = _currentEditProduct.SupplierPrice.ToString();
                EditSalePriceTextBox.Text = _currentEditProduct.SalePrice.ToString();
                EditVatTextBox.Text = _currentEditProduct.VatPercentage.ToString();
                EditFractionableCheckBox.IsChecked = _currentEditProduct.Fractionable;
                
                EditPanel.Visibility = Visibility.Visible;
                EditNameTextBox.Focus();
            }
        }

        private async void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditNameTextBox.Text))
                {
                    CustomMessageBox.ShowWarning(
                        GetLocalizedString("StoragePage_nameRequired"),
                        GetLocalizedString("StoragePage_warningTitle"));
                    return;
                }

                if (!decimal.TryParse(EditStockTextBox.Text, out decimal stock))
                {
                    CustomMessageBox.ShowWarning(
                        GetLocalizedString("StoragePage_invalidStock"),
                        GetLocalizedString("StoragePage_warningTitle"));
                    return;
                }

                if (!decimal.TryParse(EditSupplierPriceTextBox.Text, out decimal supplierPrice))
                {
                    CustomMessageBox.ShowWarning(
                        GetLocalizedString("StoragePage_invalidPrice"),
                        GetLocalizedString("StoragePage_warningTitle"));
                    return;
                }

                if (!decimal.TryParse(EditSalePriceTextBox.Text, out decimal salePrice))
                {
                    CustomMessageBox.ShowWarning(
                        GetLocalizedString("StoragePage_invalidPrice"),
                        GetLocalizedString("StoragePage_warningTitle"));
                    return;
                }

                if (!int.TryParse(EditVatTextBox.Text, out int vat))
                {
                    CustomMessageBox.ShowWarning(
                        GetLocalizedString("StoragePage_invalidVat"),
                        GetLocalizedString("StoragePage_warningTitle"));
                    return;
                }

                Product product;
                if (_isAddingNew)
                {
                    product = new Product();
                }
                else
                {
                    product = _currentEditProduct!;
                }

                product.Name = EditNameTextBox.Text.Trim();
                product.Unit = EditUnitTextBox.Text.Trim();
                product.Stock = stock;
                product.SupplierPrice = supplierPrice;
                product.SalePrice = salePrice;
                product.VatPercentage = vat;
                product.Fractionable = EditFractionableCheckBox.IsChecked ?? false;

                await product.Save();

                CustomMessageBox.ShowSuccess(
                    GetLocalizedString("StoragePage_saveSuccess"),
                    GetLocalizedString("StoragePage_successTitle"));

                EditPanel.Visibility = Visibility.Collapsed;
                await LoadProducts();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError(
                    string.Format(GetLocalizedString("StoragePage_saveError"), ex.Message),
                    GetLocalizedString("StoragePage_errorTitle"));
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            _currentEditProduct = null;
            _isAddingNew = false;
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProductViewModel viewModel)
            {
                var result = CustomMessageBox.ShowQuestion(
                    string.Format(GetLocalizedString("StoragePage_deleteConfirmation"), viewModel.Product.Name),
                    GetLocalizedString("StoragePage_confirmationTitle"),
                    CustomMessageBox.MessageBoxButtons.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        CustomMessageBox.ShowWarning(
                            GetLocalizedString("StoragePage_deleteNotImplemented"),
                            GetLocalizedString("StoragePage_warningTitle"));
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.ShowError(
                            string.Format(GetLocalizedString("StoragePage_deleteError"), ex.Message),
                            GetLocalizedString("StoragePage_errorTitle"));
                    }
                }
            }
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

    public class ProductViewModel : INotifyPropertyChanged
    {
        public Product Product { get; }

        public ProductViewModel(Product product)
        {
            Product = product;
        }

        public int Id => Product.Id;
        public string Name => Product.Name;
        public string Unit => Product.Unit;
        public string StockFormatted => $"{Product.Stock:N2}";
        
        public string SupplierPriceFormatted => $"{Product.SupplierPrice:N0} {GetCurrencyString()}";
        public string SalePriceFormatted => $"{Product.SalePrice:N0} {GetCurrencyString()}";

        private static string GetCurrencyString()
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(
                "StoreManager",
                "Resources.Strings",
                "CashierPage_currency",
                LocalizeDictionary.Instance.Culture)?.ToString() ?? "Ft";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
