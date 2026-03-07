using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using ClosedXML.Excel;

using Microsoft.Win32;

public static class ExcelExporter
{
	public static void ExportListViewToExcel(ListView listView, string sheetName = "Данные")
	{
		if (listView?.View is not GridView gridView)
		{
			MessageBox.Show("ListView не содержит GridView.");
			return;
		}

		var dialog = new SaveFileDialog
		{
			Filter = "Excel файлы (*.xlsx)|*.xlsx",
			FileName = $"{sheetName}.xlsx"
		};

		if (dialog.ShowDialog() != true)
			return;

		try
		{
			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add(sheetName);

			// === 1. Заголовки ===
			for (int c = 0; c < gridView.Columns.Count; c++)
			{
				string header = gridView.Columns[c].Header?.ToString() ?? $"Колонка {c + 1}";
				ws.Cell(1, c + 1).Value = header;
				ws.Cell(1, c + 1).Style.Font.Bold = true;
				ws.Cell(1, c + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

				var color = TryExtractHeaderColor(gridView.Columns[c].HeaderTemplate);
				if (color != null)
					ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(color);
			}

			// === 2. Данные ===
			int row = 2;
			foreach (var item in listView.Items)
			{
				for (int c = 0; c < gridView.Columns.Count; c++)
				{
					string text = "";

					// a) DisplayMemberBinding
					if (gridView.Columns[c].DisplayMemberBinding is Binding directBinding)
					{
						text = GetPropertyValue(item, directBinding.Path?.Path);
					}
					// b) DataTemplate — ищем Binding внутри шаблона
					else if (gridView.Columns[c].CellTemplate != null)
					{
						var template = gridView.Columns[c].CellTemplate;
						var bindings = ExtractBindingsFromTemplate(template);

						if (bindings.Any())
						{
							var values = bindings
								.Select(b => GetPropertyValue(item, b.Path?.Path))
								.Where(v => !string.IsNullOrWhiteSpace(v));
							text = string.Join("\n", values);
						}
					}

					var cell = ws.Cell(row, c + 1);
					cell.Value = text ?? "";
					cell.Style.Alignment.WrapText = true;

					// Цвет ячеек по шаблону заголовка
					var color2 = TryExtractHeaderColor(gridView.Columns[c].HeaderTemplate);
					if (color2 != null)
						cell.Style.Fill.BackgroundColor = XLColor.FromHtml(color2);
				}

				row++;
			}

			// === 3. Оформление ===
			ws.Columns().AdjustToContents();
			ws.SheetView.FreezeRows(1);
			ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

			wb.SaveAs(dialog.FileName);
			MessageBox.Show($"✅ Экспорт завершён!\nФайл: {dialog.FileName}");
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Ошибка экспорта: {ex.Message}");
		}
	}

	// Извлекает цвет из HeaderTemplate
	private static string TryExtractHeaderColor(DataTemplate template)
	{
		try
		{
			var border = template?.LoadContent() as Border;
			if (border?.Background is SolidColorBrush brush)
			{
				var color = brush.Color;
				return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
			}
		}
		catch { }
		return null;
	}

	// Достаёт текстовое значение свойства по Binding.Path
	private static string GetPropertyValue(object item, string? path)
	{
		if (string.IsNullOrEmpty(path))
			return "";
		try
		{
			var prop = item.GetType().GetProperty(path,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (prop != null)
			{
				var value = prop.GetValue(item);
				return value?.ToString() ?? "";
			}
		}
		catch { }
		return "";
	}

	// Рекурсивно извлекает все Bindings из шаблона (TextBlock, Label, т.п.)
	private static List<Binding> ExtractBindingsFromTemplate(DataTemplate template)
	{
		var result = new List<Binding>();
		try
		{
			var content = template.LoadContent();
			FindBindingsRecursive(content, result);
		}
		catch { }
		return result;
	}

	private static void FindBindingsRecursive(object obj, List<Binding> list)
	{
		if (obj is DependencyObject dep)
		{
			var localValues = dep.GetLocalValueEnumerator();
			while (localValues.MoveNext())
			{
				var entry = localValues.Current;
				if (entry.Value is Binding binding)
					list.Add(binding);
			}

			int count = VisualTreeHelper.GetChildrenCount(dep);
			for (int i = 0; i < count; i++)
				FindBindingsRecursive(VisualTreeHelper.GetChild(dep, i), list);
		}
	}
}
