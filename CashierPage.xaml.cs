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
    /// <summary>
    /// Interaction logic for CashierPage.xaml
    /// </summary>
    public partial class CashierPage : Page
    {
        private Frame _frame;
        private ObservableCollection<CartItem> _cartItems = new();
        private ObservableCollection<SearchResultItem> _searchResults = new();
        private List<Product> _allProducts = new();
        private Dictionary<string, Product> _barcodeToProduct = new();

        public CashierPage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
            
            CartItemsControl.ItemsSource = _cartItems;
            SearchResultsListBox.ItemsSource = _searchResults;
            _cartItems.CollectionChanged += (s, e) => UpdateTotals();

            Loaded += async (s, e) => await LoadProducts();
            BarcodeTextBox.Focus();
        }

        // Helper method to get localized string
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
                
                // Load barcodes for all products
                foreach (var product in _allProducts)
                {
                    var barcodes = await product.GetBarcodes();
                    foreach (var barcode in barcodes)
                    {
                        _barcodeToProduct[barcode.Code] = product;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(GetLocalizedString("CashierPage_loadProductsError"), ex.Message),
                    GetLocalizedString("CashierPage_errorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BarcodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_searchResults.Count > 0)
                {
                    // Ha van találat a dropdown-ban, azt adjuk hozzá
                    AddProductToCart(_searchResults[0].Product);
                    BarcodeTextBox.Clear();
                    SearchPopup.IsOpen = false;
                }
                else
                {
                    SearchAndAddProduct();
                }
            }
            else if (e.Key == Key.Escape)
            {
                SearchPopup.IsOpen = false;
                BarcodeTextBox.Clear();
            }
            else if (e.Key == Key.Down && _searchResults.Count > 0)
            {
                SearchResultsListBox.SelectedIndex = 0;
                SearchResultsListBox.Focus();
            }
        }

        private void BarcodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = BarcodeTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                _searchResults.Clear();
                SearchPopup.IsOpen = false;
                return;
            }

            // Valós idejű keresés
            PerformSearch(searchText);
        }

        private void PerformSearch(string searchText)
        {
            _searchResults.Clear();

            // Keresés vonalkód, ID vagy név alapján
            var results = _allProducts
                .Where(p => 
                    p.Id.ToString().Contains(searchText) ||
                    p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    _barcodeToProduct.Any(kvp => kvp.Key.Contains(searchText) && kvp.Value.Id == p.Id))
                .Take(8)
                .Select(p => new SearchResultItem { Product = p })
                .ToList();

            foreach (var result in results)
            {
                _searchResults.Add(result);
            }

            SearchPopup.IsOpen = _searchResults.Count > 0;
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItem is SearchResultItem selectedItem)
            {
                AddProductToCart(selectedItem.Product);
                BarcodeTextBox.Clear();
                SearchPopup.IsOpen = false;
                BarcodeTextBox.Focus();
            }
        }

        private void SearchResultsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResultsListBox.SelectedItem is SearchResultItem selectedItem)
            {
                AddProductToCart(selectedItem.Product);
                BarcodeTextBox.Clear();
                SearchPopup.IsOpen = false;
                BarcodeTextBox.Focus();
            }
            else if (e.Key == Key.Escape)
            {
                SearchPopup.IsOpen = false;
                BarcodeTextBox.Focus();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchAndAddProduct();
        }

        private void SearchAndAddProduct()
        {
            string searchText = BarcodeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
                return;

            Product? product = null;

            // Search by barcode first
            if (_barcodeToProduct.TryGetValue(searchText, out var foundProduct))
            {
                product = foundProduct;
            }
            else
            {
                // Search by ID or name
                if (int.TryParse(searchText, out int productId))
                {
                    product = _allProducts.FirstOrDefault(p => p.Id == productId);
                }
                
                if (product == null)
                {
                    product = _allProducts.FirstOrDefault(p => 
                        p.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (product != null)
            {
                AddProductToCart(product);
                BarcodeTextBox.Clear();
                SearchPopup.IsOpen = false;
                BarcodeTextBox.Focus();
            }
            else
            {
                MessageBox.Show(
                    GetLocalizedString("CashierPage_productNotFound"),
                    GetLocalizedString("CashierPage_warningTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                BarcodeTextBox.SelectAll();
            }
        }

        private void AddProductToCart(Product product)
        {
            var existingItem = _cartItems.FirstOrDefault(item => item.ProductId == product.Id);
            
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                _cartItems.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductCode = product.Id.ToString(),
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitPrice = product.SalePrice,
                    Unit = product.Unit,
                    VatPercentage = product.VatPercentage
                });
            }

            UpdateEmptyCartVisibility();
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                item.Quantity += 1;
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                }
                else
                {
                    _cartItems.Remove(item);
                    UpdateEmptyCartVisibility();
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CartItem item)
            {
                _cartItems.Remove(item);
                UpdateEmptyCartVisibility();
            }
        }

        private void UpdateTotals()
        {
            decimal total = _cartItems.Sum(item => item.TotalPrice);
            decimal vatTotal = _cartItems.Sum(item => 
                item.TotalPrice * item.VatPercentage / (100 + item.VatPercentage));
            int itemCount = _cartItems.Count;

            string currency = GetLocalizedString("CashierPage_currency");
            TotalAmountText.Text = $"{total:N0} {currency}";
            VatAmountText.Text = $"{vatTotal:N0} {currency}";
            ItemCountText.Text = itemCount.ToString();
        }

        private void UpdateEmptyCartVisibility()
        {
            EmptyCartPanel.Visibility = _cartItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                QuickAmountTextBox.Text += button.Content.ToString();
            }
        }

        private void NumPadClear_Click(object sender, RoutedEventArgs e)
        {
            if (QuickAmountTextBox.Text.Length > 0)
            {
                QuickAmountTextBox.Text = QuickAmountTextBox.Text[..^1];
            }
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show(
                    GetLocalizedString("CashierPage_emptyCartWarning"),
                    GetLocalizedString("CashierPage_warningTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            decimal total = _cartItems.Sum(item => item.TotalPrice);
            decimal paidAmount = 0;

            // Számológépről beütött összeg használata
            if (!string.IsNullOrWhiteSpace(QuickAmountTextBox.Text))
            {
                if (decimal.TryParse(QuickAmountTextBox.Text, out decimal amount))
                {
                    paidAmount = amount;
                }
            }

            // Ha nincs megadva összeg, pontosan fizetés
            if (paidAmount == 0)
            {
                paidAmount = total;
            }

            decimal change = paidAmount - total;

            if (change < 0)
            {
                MessageBox.Show(
                    string.Format(GetLocalizedString("CashierPage_insufficientAmount"), 
                        total, paidAmount, Math.Abs(change)),
                    GetLocalizedString("CashierPage_errorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                string.Format(GetLocalizedString("CashierPage_paymentConfirmation"),
                    total, paidAmount, change),
                GetLocalizedString("CashierPage_paymentConfirmationTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show(
                    string.Format(GetLocalizedString("CashierPage_paymentSuccess"),
                        total, paidAmount, change),
                    GetLocalizedString("CashierPage_successTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ClearCart();
            }
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
                return;

            var result = MessageBox.Show(
                GetLocalizedString("CashierPage_clearCartConfirmation"),
                GetLocalizedString("CashierPage_confirmationTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ClearCart();
            }
        }

        private void ClearCart()
        {
            _cartItems.Clear();
            QuickAmountTextBox.Clear();
            UpdateEmptyCartVisibility();
            BarcodeTextBox.Focus();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _frame.GoBack();
        }
    }

    public class CartItem : INotifyPropertyChanged
    {
        private int _productId;
        private string _productName = string.Empty;
        private string _productCode = string.Empty;
        private decimal _quantity;
        private decimal _unitPrice;
        private string _unit = string.Empty;

        public int ProductId
        {
            get => _productId;
            set { _productId = value; OnPropertyChanged(); }
        }

        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        // Kombinált megjelenítés: cikkszám + név
        public string DisplayName => $"[{ProductCode}] {ProductName}";

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(UnitPriceFormatted));
                OnPropertyChanged(nameof(TotalPriceFormatted));
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(UnitPriceFormatted));
                OnPropertyChanged(nameof(TotalPriceFormatted));
            }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public decimal TotalPrice => Quantity * UnitPrice;

        // Lokalizált formázott árak
        public string UnitPriceFormatted => $"{UnitPrice:N0} {GetCurrencyString()}";
        public string TotalPriceFormatted => $"{TotalPrice:N0} {GetCurrencyString()}";

        private static string GetCurrencyString()
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(
                "StoreManager",
                "Resources.Strings",
                "CashierPage_currency",
                LocalizeDictionary.Instance.Culture)?.ToString() ?? "Ft";
        }

        public int VatPercentage { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SearchResultItem
    {
        public Product Product { get; set; } = null!;
        public string DisplayText => $"[{Product.Id}] {Product.Name} - {Product.SalePrice:N0} {GetCurrencyString()}";

        private static string GetCurrencyString()
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(
                "StoreManager",
                "Resources.Strings",
                "CashierPage_currency",
                LocalizeDictionary.Instance.Culture)?.ToString() ?? "Ft";
        }
    }
}
