using System;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	public partial class CategoryDialog : UserControl
	{
		private readonly Data db = new Data();
		private readonly User _currentUser;
		private readonly Category _existing; // null = add mode

		public event Action<bool> Closed;

		public CategoryDialog(User currentUser, Category existing)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_existing = existing;

			Loaded += CategoryDialog_Loaded;
		}

		private void CategoryDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (_existing == null)
			{
				return;
			}

			// Edit mode
			DialogTitle.Text = "Edit Category";
			SaveButton.Content = "Update Category";
			NameInput.Text = _existing.Name;

			// Match type
			foreach (ComboBoxItem item in TypeComboBox.Items)
			{
				if (item.Content.ToString().Equals(_existing.Type, StringComparison.OrdinalIgnoreCase))
				{
					TypeComboBox.SelectedItem = item;
					break;
				}
			}

			// Default categories: type is locked so Salary stays income
			if (_existing.IsDefault)
			{
				TypeComboBox.IsEnabled = false;
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			string name = NameInput.Text.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show("Please enter a name.",
					"Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			ComboBoxItem selectedType = TypeComboBox.SelectedItem as ComboBoxItem;
			string type = selectedType.Content.ToString();

			try
			{
				if (_existing == null)
				{
					Category cat = new Category(_currentUser.UserID, name, type);
					cat.IsDefault = false;
					cat.Create();
				}
				else
				{
					_existing.Update(name);
				}

				if (Closed != null)
				{
					Closed(true);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save category: " + ex.Message);
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			if (Closed != null)
			{
				Closed(false);
			}
		}
	}
}
