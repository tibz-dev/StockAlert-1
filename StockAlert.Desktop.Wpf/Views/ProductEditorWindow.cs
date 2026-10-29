using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using StockAlert.Desktop.Wpf.Models;

namespace StockAlert.Desktop.Wpf.Views;

public sealed class ProductEditorWindow : Window
{
    private readonly TextBox _name = new(), _description = new(), _barcode = new(),
        _price = new(), _stock = new(), _category = new(), _supplier = new(),
        _supplierEmail = new();
    private readonly bool _isNew;
    private readonly Guid _id;
    public ProductRow? Result { get; private set; }

    public ProductEditorWindow(ProductRow? product = null)
    {
        _isNew = product == null;
        _id = product?.Id ?? Guid.Empty;
        Title = _isNew ? "Add Product" : "Edit Product";
        Width = 560;
        Height = 700;
        MinWidth = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = System.Windows.Media.Brushes.White;
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        var panel = new StackPanel { Margin = new Thickness(25) };
        AddField(panel, "Product name", _name, product?.Name);
        AddField(panel, "Description", _description, product?.Description);
        AddField(panel, "Barcode (blank = auto-generate for new items)", _barcode, product?.Barcode);
        AddField(panel, "Selling price (R)", _price, product?.Price.ToString(CultureInfo.InvariantCulture));
        if (_isNew) AddField(panel, "Opening stock quantity", _stock, "0");
        AddField(panel, "Category", _category, product?.CategoryName);
        AddField(panel, "Supplier", _supplier, product?.SupplierName);
        AddField(panel, "Supplier email (optional)", _supplierEmail, product?.SupplierEmail);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 10, 10, 0) };
        cancel.Click += (_, _) => Close();
        var save = new Button { Content = "Save Product", Margin = new Thickness(0, 10, 0, 0) };
        save.Click += Save_Click;
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        panel.Children.Add(buttons);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static void AddField(StackPanel panel, string label, TextBox input, string? value)
    {
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 7, 0, 4), Foreground = System.Windows.Media.Brushes.SlateGray });
        input.Text = value ?? "";
        input.Padding = new Thickness(9);
        panel.Children.Add(input);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_category.Text) ||
            string.IsNullOrWhiteSpace(_supplier.Text) ||
            !decimal.TryParse(_price.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0 ||
            (_isNew && (!int.TryParse(_stock.Text, out var stock) || stock < 0)))
        {
            MessageBox.Show("Enter a name, category, supplier and positive price. Opening stock must be a non-negative whole number.", "Validation");
            return;
        }
        Result = new ProductRow
        {
            Id = _id,
            Name = _name.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(_description.Text) ? null : _description.Text.Trim(),
            Barcode = string.IsNullOrWhiteSpace(_barcode.Text) ? null : _barcode.Text.Trim(),
            Price = price,
            StockQuantity = _isNew ? int.Parse(_stock.Text) : 0,
            CategoryName = _category.Text.Trim(),
            SupplierName = _supplier.Text.Trim(),
            SupplierEmail = string.IsNullOrWhiteSpace(_supplierEmail.Text) ? null : _supplierEmail.Text.Trim()
        };
        DialogResult = true;
    }
}
