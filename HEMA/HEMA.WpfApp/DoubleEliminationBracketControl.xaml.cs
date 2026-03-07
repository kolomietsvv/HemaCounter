using System.Windows.Controls;

namespace HEMA.WpfApp.Controls;

public partial class DoubleEliminationBracketControl : UserControl
{
	private Action<List<Fight>> setCurrentFightAction;
	private DoubleEliminationBracketViewModel bracketVM;

	public DoubleEliminationBracketControl(List<Fighter> fighters, int participantsCount, Action<List<Fight>> setCurrentFightAction)
	{
		ArgumentNullException.ThrowIfNull(fighters);

		InitializeComponent();

		var bracket = DoubleEliminationBracketGenerator.Generate(
			fighters,
			participantsCount,
			includeGrandFinalReset: false);

		bracketVM = DoubleEliminationBracketViewModelFactory.Create(bracket);
		DataContext = bracketVM;
		this.setCurrentFightAction = setCurrentFightAction;
	}

	private void RunButton_Click(object sender, System.Windows.RoutedEventArgs e)
	{

		setCurrentFightAction?.Invoke([bracketVM.GrandFinal]);
	}
}