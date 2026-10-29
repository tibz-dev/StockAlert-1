using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using StockAlert.Desktop.Wpf.Models;

namespace StockAlert.Desktop.Wpf.Views;

/// <summary>Desktop quote entry, using the existing StockAlert API for pricing and VAT.</summary>
public sealed class QuoteEditorWindow : Window
{
    private readonly TextBox _customerName = new() { Margin = new Thickness(0, 5, 0, 12), Padding = new Thickness(8) };
    private readonly TextBox _notes = new() { Margin = new Thickness(0, 5, 0, 12), Padding = new Thickness(8) };
    private readonly ComboBox _product = new() { DisplayMemberPath = nameof(ProductRow.Name), Margin = new Thickness(0, 5, 8, 12), MinWidth = 230 };
    private readonly TextBox _quantity = new() { Text = "1", Width = 80, Padding = new Thickness(8), Margin = new Thickness(0, 5, 8, 12) };
    private readonly DataGrid _cart = new() { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, MinHeight = 200 };
    private readonly ObservableCollection<QuoteLine> _lines = new();

    public string CustomerName => _customerName.Text.Trim();
    public string? Notes => string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim();
    public IReadOnlyList<(Guid ProductId, int Quantity)> Items =>
        _lines.Select(x => (x.ProductId, x.Quantity)).ToList();

    public QuoteEditorWindow(IReadOnlyList<ProductRow> products)
    {
        Title = "Create Quote | StockAlert";
        Width = 760;
        Height = 680;
        MinWidth = 610;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        Background = System.Windows.Media.Brushes.White;

        var root = new DockPanel { Margin = new Thickness(26) };
        var heading = new TextBlock { Text = "New Quote", FontSize = 25, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(heading, Dock.Top);
        root.Children.Add(heading);

        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(18, 9, 18, 9), Margin = new Thickness(0, 0, 10, 0) };
        cancel.Click += (_, _) => Close();
        var save = new Button { Content = "Create Quote", Padding = new Thickness(18, 9, 18, 9), Background = System.Windows.Media.Brushes.RoyalBlue, Foreground = System.Windows.Media.Brushes.White };
        save.Click += Save_Click;
        footer.Children.Add(cancel);
        footer.Children.Add(save);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = "Customer name" });
        body.Children.Add(_customerName);
        body.Children.Add(new TextBlock { Text = "Notes (optional)" });
        body.Children.Add(_notes);
        body.Children.Add(new TextBlock { Text = "Quote items" });
        _product.ItemsSource = products.Where(p => p.Id != Guid.Empty).OrderBy(p => p.Name).ToList();
        var addRow = new StackPanel { Orientation = Orientation.Horizontal };
        addRow.Children.Add(_product);
        addRow.Children.Add(_quantity);
        var add = new Button { Content = "Add item", Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 5, 8, 12) };
        add.Click += Add_Click;
        addRow.Children.Add(add);
        var remove = new Button { Content = "Remove selected", Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 5, 0, 12) };
        remove.Click += (_, _) => { if (_cart.SelectedItem is QuoteLine row) _lines.Remove(row); };
        addRow.Children.Add(remove);
        body.Children.Add(addRow);
        _cart.Columns.Add(new DataGridTextColumn { Header = "Product", Binding = new System.Windows.Data.Binding(nameof(QuoteLine.Name)), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
        _cart.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new System.Windows.Data.Binding(nameof(QuoteLine.Quantity)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _cart.Columns.Add(new DataGridTextColumn { Header = "Current price (R)", Binding = new System.Windows.Data.Binding(nameof(QuoteLine.Price)) { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _cart.ItemsSource = _lines;
        body.Children.Add(_cart);
        body.Children.Add(new TextBlock { Text = "Final quote totals and VAT are calculated by the server.", Margin = new Thickness(0, 10, 0, 0), Foreground = System.Windows.Media.Brushes.SlateGray });
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Content = root;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (_product.SelectedItem is not ProductRow product ||
            !int.TryParse(_quantity.Text, out var qty) || qty <= 0)
        {
            MessageBox.Show("Select a product and enter a positive whole-number quantity.");
            return;
        }
        if (_lines.Any(x => x.ProductId == product.Id))
        {
            MessageBox.Show("That product is already in the quote. Remove it first to change its quantity.");
            return;
        }
        _lines.Add(new QuoteLine(product.Id, product.Name, qty, product.Price));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CustomerName.Length == 0 || _lines.Count == 0)
        {
            MessageBox.Show("Enter the customer name and add at least one product.");
            return;
        }
        DialogResult = true;
    }

    private sealed record QuoteLine(Guid ProductId, string Name, int Quantity, decimal Price);
}
