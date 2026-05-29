using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FinancyApplication
{
	// Row VM for the categories lists
	public class CategoryRow
	{
		public int CategoryID { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public bool IsDefault { get; set; }

		public bool CanDelete => !IsDefault;
		public Visibility DefaultBadgeVisibility =>
			IsDefault ? Visibility.Visible : Visibility.Collapsed;
	}

	public partial class CategoriesView : UserControl
	{
		private readonly Data db = new Data();
		private readonly User _currentUser;

		private List<Category> _allCategories = new List<Category>();

		public CategoriesView(User user)
		{
			InitializeComponent();
			_currentUser = user;
			Loaded += (s, e) => LoadCategories();
		}

		private void LoadCategories()
		{
			try
			{
				_allCategories = db.GetCategoriesByUser(_currentUser.UserID);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not load categories: " + ex.Message);
				_allCategories = new List<Category>();
			}

			List<CategoryRow> income = _allCategories
				.Where(c => string.Equals(c.Type, "Income", StringComparison.OrdinalIgnoreCase))
				.OrderBy(c => c.Name)
				.Select(ToRow)
				.ToList();

			List<CategoryRow> expense = _allCategories
				.Where(c => !string.Equals(c.Type, "Income", StringComparison.OrdinalIgnoreCase))
				.OrderBy(c => c.Name)
				.Select(ToRow)
				.ToList();

			IncomeList.ItemsSource = income;
			ExpenseList.ItemsSource = expense;

			IncomeCount.Text = " (" + income.Count + ")";
			ExpenseCount.Text = " (" + expense.Count + ")";

			IncomeEmpty.Visibility = income.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			ExpenseEmpty.Visibility = expense.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private static CategoryRow ToRow(Category c) => new CategoryRow
		{
			CategoryID = c.CategoryID,
			Name = c.Name,
			Type = c.Type,
			IsDefault = c.IsDefault
		};

		// ADD / EDIT / DELETE

		private void AddCategory_Click(object sender, RoutedEventArgs e)
		{
			ShowCategoryForm(null);
		}

		private void EditCategory_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Category cat = _allCategories.Find(x => x.CategoryID == id);
			if (cat == null) return;
			ShowCategoryForm(cat);
		}

		// Embeds the CategoryDialog UserControl into the modal overlay (no popup Window).
		private void ShowCategoryForm(Category existing)
		{
			CategoryDialog dlg = new CategoryDialog(_currentUser, existing);
			dlg.Closed += didSave =>
			{
				ModalContent.Content = null;
				ModalHost.Visibility = Visibility.Collapsed;
				if (didSave) LoadCategories();
			};
			ModalContent.Content = dlg;
			ModalHost.Visibility = Visibility.Visible;
		}

		private void DeleteCategory_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button b || b.Tag == null) return;
			int id = (int)b.Tag;
			Category cat = _allCategories.Find(x => x.CategoryID == id);
			if (cat == null) return;

			if (cat.IsDefault)
			{
				MessageBox.Show("Default categories cannot be deleted.",
					"Not allowed", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			MessageBoxResult res = MessageBox.Show(
				"Delete the category \"" + cat.Name + "\"?\n\n" +
				"Any transactions using it will lose their category link.",
				"Confirm delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (res == MessageBoxResult.Yes)
			{
				try
				{
					cat.Delete();
					LoadCategories();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Could not delete category: " + ex.Message);
				}
			}
		}
	}
}
