using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using HEMA.WpfApp.Controls;

using Microsoft.Win32;

namespace HEMA.WpfApp
{
	/// <summary>
	/// Interaction logic for FightsListWindow.xaml
	/// </summary>
	public partial class FightsListWindow : Window
	{
		private Point _dragStartPoint;
		private object? _draggedItem;

		public MainWindow MainWindow { get; }

		public FightsListWindow(MainWindow mainWindow)
		{
			InitializeComponent();
			MainWindow = mainWindow;
			DataContext = mainWindow;

			FilterPopupContent.ApplyClicked += FilterPopupContent_ApplyClicked;
			FilterPopupContent.ResetClicked += FilterPopupContent_ResetClicked;

			HostsListPopupContent.CancelClicked += HostsListPopupContent_CancelClicked;
			HostsListPopupContent.ConnectClicked += HostsListPopupContent_ConnectClicked;
		}

		public void AcceptFights(string fightsMessage)
		{
			AddFights(fightsMessage);
		}

		public string HandleFightsRequest()
		{
			return "[]";
		}

		private void FightButton_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.CommandParameter is Fight fight)
			{
				MainWindow.SetEnumerator(fight.RedName, fight.BlueName);
			}
			MainWindow.RaitingWindow.Close();
			Close();
		}

		private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_dragStartPoint = e.GetPosition(null);
			_draggedItem = GetItemUnderMouse(e.OriginalSource);
		}

		private void List_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null)
				return;

			var pos = e.GetPosition(null);
			if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
				Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
				return;

			// запускаем D&D
			var data = new DataObject(typeof(object), _draggedItem);
			DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
		}

		private void List_DragOver(object sender, DragEventArgs e)
		{
			// Разрешаем только Move внутри этого ListView
			if (!e.Data.GetDataPresent(typeof(object)))
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;
				return;
			}
			e.Effects = DragDropEffects.Move;
			e.Handled = true;
		}

		private void List_Drop(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(typeof(object)) || _draggedItem == null)
				return;

			var droppedData = _draggedItem;
			var targetItem = GetItemUnderMouse(e.OriginalSource);

			if (ReferenceEquals(droppedData, targetItem))
				return;

			var collection = MainWindow.Fights;                 // ваша ObservableCollection
			int oldIndex = collection.IndexOf((Fight)droppedData);
			int newIndex = targetItem is null
				? collection.Count - 1                          // бросили в пустое место — в конец
				: collection.IndexOf((Fight)targetItem);

			if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
				collection.Move(oldIndex, newIndex);

			_draggedItem = null;
			MainWindow.SetEnumerator(MainWindow.Fight.RedName, MainWindow.Fight.BlueName);
		}

		private object? GetItemUnderMouse(object origin)
		{
			// Поднимаемся по визуальному дереву до ListViewItem
			DependencyObject? current = origin as DependencyObject;
			while (current != null && current is not ListViewItem)
				current = VisualTreeHelper.GetParent(current);

			return (current as ListViewItem)?.DataContext;
		}

		private async void AddFightsButton_Click(object sender, RoutedEventArgs e)
		{
			var fileDialog = new OpenFileDialog
			{
				Title = "Выберите файл",
				Filter = "JSON файлы(*.json)|*.json"
			};

			if (fileDialog.ShowDialog().GetValueOrDefault())
			{
				var extension = Path.GetExtension(fileDialog.FileName);
				var json = await File.ReadAllTextAsync(fileDialog.FileName);
				AddFights(json);
			}
		}

		private void AddFights(string json)
		{
			var fights = JsonSerializer.Deserialize<List<Fight>>(json);
			foreach (var fight in fights!)
			{
				MainWindow.Fights.Add(fight);
				MainWindow.OpenRaitingButton_Click(null!, null!);
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					if (FilterPopup.IsOpen)
					{
						FilterPopup.IsOpen = false;
					}
					else if (HostsListPopup.IsOpen)
					{
						HostsListPopup.IsOpen = false;
					}
					else
					{
						Close();
					}
					return;

				case Key.Enter:
					if (FilterPopup.IsOpen)
					{
						ApplyFilter(FilterPopupContent.FilterText);
					}
					return;

				case Key.F:
					if (!FilterPopup.IsOpen)
					{
						Filter_Click(null!, null!);
						e.Handled = true;
					}
					return;

				case Key.OemPlus:
					AddFightsButton_Click(null!, null!);
					return;

				case Key.F7:
					if (FilterPopup.IsOpen)
					{
						ResetFilter();
					}
					return;
			}
		}

		/// <summary>
		/// Не работает, написано нейронкой
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			ExcelExporter.ExportListViewToExcel(FightsListView, "Бои");
		}

		private async void UploadFights_Click(object sender, RoutedEventArgs e)
		{
			if (!HostsListPopup.IsOpen)
			{
				var hosts = await MainWindow.TcpService.GetAllHosts(1);
				MainWindow.DiscoveredHosts.Clear();

				foreach (var host in hosts)
				{
					MainWindow.DiscoveredHosts.Add(new HostInfo { Name = host.Key, IpAddress = host.Value });
				}

				HostsListPopup.IsOpen = true;
				HostsListPopup.Focus();
			}
		}

		private void DownloadFights_Click(object sender, RoutedEventArgs e)
		{
			if (!HostsListPopup.IsOpen)
			{
				HostsListPopup.IsOpen = true;
				HostsListPopup.Focus();
			}
		}

		private void FilterPopupContent_ApplyClicked(object? sender, string filterText)
		{
			ApplyFilter(filterText);
		}

		private void FilterPopupContent_ResetClicked(object? sender, EventArgs e)
		{
			ResetFilter();
		}

		private void HostsListPopupContent_CancelClicked(object? sender, EventArgs e)
		{
			HostsListPopup.IsOpen = false;
		}

		private async void HostsListPopupContent_ConnectClicked(object? sender, EventArgs e)
		{
			await ConnectToSelectedHostAsync();
		}

		private void ApplyFilter(string filterText)
		{
			if (!FilterPopup.IsOpen)
				return;

			string nameFilter = filterText.Trim().ToLower();

			var view = CollectionViewSource.GetDefaultView(FightsListView.ItemsSource);
			view.Filter = fightObj =>
			{
				if (fightObj is Fight fight)
				{
					return string.IsNullOrEmpty(nameFilter)
						|| fight.RedName.ToLower().Contains(nameFilter)
						|| fight.BlueName.ToLower().Contains(nameFilter);
				}

				return false;
			};

			view.Refresh();
			FilterPopup.IsOpen = false;
		}

		private void ResetFilter()
		{
			if (!FilterPopup.IsOpen)
				return;

			FilterPopupContent.Clear();

			var view = CollectionViewSource.GetDefaultView(FightsListView.ItemsSource);
			view.Filter = null;
			view.Refresh();

			FilterPopup.IsOpen = false;
		}

		private async Task ConnectToSelectedHostAsync()
		{
			if (MainWindow.SelectedHost == null)
				return;

			await MainWindow.TcpService.ConnectAndSendFights(
				MainWindow.SelectedHost.IpAddress.ToString(),
				MainWindow.GetSerializedFights(),
				CancellationToken.None);

			HostsListPopup.IsOpen = false;
		}

		private void Filter_Click(object sender, RoutedEventArgs e)
		{
			if (!FilterPopup.IsOpen)
			{
				FilterPopup.IsOpen = true;
				FilterPopupContent.FocusInput();
			}
		}
	}
}
