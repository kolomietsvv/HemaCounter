using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
		}

		private void FightButton_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as Button)?.CommandParameter is Fight fight)
			{
				MainWindow.SetEnumerator(fight.RedName, fight.BlueName);
			}
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
	}
}
