using System;
using System.Windows;

namespace FinancyApplication
{
	public partial class GoalDialog : Window
	{
		private readonly Data db = new Data();
		private readonly User _currentUser;
		private readonly Goal _existingGoal;

		public GoalDialog(User currentUser, Goal existingGoal)
		{
			InitializeComponent();
			_currentUser = currentUser;
			_existingGoal = existingGoal;

			Loaded += GoalDialog_Loaded;
		}

		private void GoalDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (_existingGoal != null)
			{
				DialogTitle.Text = "Edit Goal";
				SaveButton.Content = "Update Goal";

				NameInput.Text = _existingGoal.Name ?? "";
				TargetInput.Text = _existingGoal.TargetAmount.ToString("0.##");
				SavedInput.Text = _existingGoal.SavedAmount.ToString("0.##");
				DeadlinePicker.SelectedDate = _existingGoal.Deadline;
			}
			else
			{
				SavedInput.Text = "0";
			}
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// Name
			string name = NameInput.Text?.Trim() ?? "";
			if (string.IsNullOrEmpty(name))
			{
				MessageBox.Show("Please enter a goal name.", "Validation",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Target
			if (!decimal.TryParse(TargetInput.Text.Trim(), out decimal target) || target <= 0)
			{
				MessageBox.Show("Please enter a valid target amount greater than 0.", "Validation",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Saved
			decimal saved = 0;
			if (!string.IsNullOrWhiteSpace(SavedInput.Text))
			{
				if (!decimal.TryParse(SavedInput.Text.Trim(), out saved) || saved < 0)
				{
					MessageBox.Show("Saved amount must be 0 or greater.", "Validation",
						MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			try
			{
				if (_existingGoal == null)
				{
					Goal newGoal = new Goal
					{
						UserId = _currentUser.UserID,
						Name = name,
						TargetAmount = target,
						SavedAmount = saved,
						Deadline = DeadlinePicker.SelectedDate,
						CreatedAt = DateTime.Now
					};
					db.InsertGoal(newGoal);
				}
				else
				{
					_existingGoal.Name = name;
					_existingGoal.TargetAmount = target;
					_existingGoal.SavedAmount = saved;
					_existingGoal.Deadline = DeadlinePicker.SelectedDate;
					db.UpdateGoal(_existingGoal);
				}

				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not save goal: " + ex.Message,
					"Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}