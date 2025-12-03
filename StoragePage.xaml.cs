using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using StoreManager.Classes;
using WPFLocalizeExtension.Engine;

namespace StoreManager
{
    public partial class StoragePage
    {
        private Frame _frame;
        private ObservableCollection<ProductViewModel> _products;
        private List<Product> _allProducts = new();
        // precomputed lowercase search index for quick search
        private List<(Product product, string idText, string nameLower)> _searchIndex;
        private CancellationTokenSource? _searchCts;
        private readonly object _searchLock = new object();
        private Product? _currentEditProduct;
        private bool _isAddingNew;

        public StoragePage(Frame frame)
        {
            InitializeComponent();
            _frame = frame;
            _products = new ObservableCollection<ProductViewModel>();
            _searchIndex = new List<(Product product, string idText, string nameLower)>();

            ProductsListView.ItemsSource = _products;

            LocalizeDictionary.Instance.PropertyChanged += Localize_PropertyChanged;

            Loaded += async (_, _) => await LoadProducts();
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
                // Build a light-weight search index to avoid repeated ToLower() and ToString() calls
                var builtIndex = _allProducts.Select(p => (product: p, idText: p.Id.ToString(), nameLower: p.Name.ToLowerInvariant())).ToList();
                lock (_searchLock)
                {
                    _searchIndex = builtIndex;
                }

                // Show a limited number of items initially for faster UI load
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

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim().ToLower();

            lock (_searchLock)
            {
                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _searchCts = new CancellationTokenSource();
            }

            var token = _searchCts.Token;

            // Short-circuit for empty search: show the top 150 quickly
            if (string.IsNullOrEmpty(searchText))
            {
                // Use Dispatcher to ensure UI update
                await Dispatcher.InvokeAsync(() => DisplayProducts(_allProducts.Take(150).ToList())).Task.ConfigureAwait(false);
                return;
            }

            await Task.Run((Func<Task>)(async () =>
            {
                try
                {
                    await Task.Delay(200, token).ConfigureAwait(false);

                    // perform search on the precomputed index (snapshot under lock)
                    List<(Product product, string idText, string nameLower)> localIndex;
                    lock (_searchLock)
                    {
                        localIndex = _searchIndex;
                    }

                    var matches = localIndex
                        .Where(t => t.idText.Contains(searchText) || t.nameLower.Contains(searchText))
                        .Select(t => t.product)
                        .ToList();

                    var toDisplay = matches.Take(1000).ToList();
                    if (token.IsCancellationRequested) return;

                    await Dispatcher.InvokeAsync(() => DisplayProducts(toDisplay)).Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    // benign: log or show error
                    await Dispatcher.InvokeAsync(() => CustomMessageBox.ShowError(ex.Message, GetLocalizedString("StoragePage_errorTitle"))).Task.ConfigureAwait(false);
                }
            }), token).ConfigureAwait(false);
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            lock (_searchLock)
            {
                _searchCts?.Cancel();
                _searchCts?.Dispose();
                _searchCts = new CancellationTokenSource();
            }

            var token = _searchCts.Token;
            string searchText = SearchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                DisplayProducts(_allProducts.Take(150).ToList());
                return;
            }

            await Task.Run((Func<Task>)(async () =>
            {
                List<(Product product, string idText, string nameLower)> localIndex;
                lock (_searchLock)
                {
                    localIndex = _searchIndex;
                }

                var matches = localIndex
                    .Where(t => t.idText.Contains(searchText) || t.nameLower.Contains(searchText))
                    .Select(t => t.product)
                    .ToList();

                var toDisplay = matches.Take(1000).ToList();
                if (token.IsCancellationRequested) return;
                await Dispatcher.InvokeAsync(() => DisplayProducts(toDisplay)).Task.ConfigureAwait(false);
            }), token).ConfigureAwait(false);
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
            if (sender is not Button button || button.Tag is not ProductViewModel viewModel) return;
            _isAddingNew = false;
            _currentEditProduct = viewModel.Product;
                
            EditIdTextBox.Text = _currentEditProduct.Id.ToString();
            EditNameTextBox.Text = _currentEditProduct.Name;
            EditUnitTextBox.Text = _currentEditProduct.Unit;
            EditStockTextBox.Text = _currentEditProduct.Stock.ToString(CultureInfo.InvariantCulture);
            EditSupplierPriceTextBox.Text = _currentEditProduct.SupplierPrice.ToString(CultureInfo.InvariantCulture);
            EditSalePriceTextBox.Text = _currentEditProduct.SalePrice.ToString(CultureInfo.InvariantCulture);
            EditVatTextBox.Text = _currentEditProduct.VatPercentage.ToString();
            EditFractionableCheckBox.IsChecked = _currentEditProduct.Fractionable;
                
            EditPanel.Visibility = Visibility.Visible;
            EditNameTextBox.Focus();
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

                var product = _isAddingNew ? new Product() : _currentEditProduct!;

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

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not ProductViewModel viewModel) return;
            var result = CustomMessageBox.ShowQuestion(
                string.Format(GetLocalizedString("StoragePage_deleteConfirmation"), viewModel.Product.Name),
                GetLocalizedString("StoragePage_confirmationTitle")
            );

            if (result != MessageBoxResult.Yes) return;
            
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            while (_frame.CanGoBack)
            {
                _frame.RemoveBackEntry();
            }
            
            _frame.Navigate(new WelcomeSelector(_frame));
        }

        private void Localize_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizeDictionary.Culture))
            {
                RefreshProductList();
            }
        }
    }

    public class ProductViewModel(Product product) : INotifyPropertyChanged
    {
        public Product Product { get; } = product;

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
