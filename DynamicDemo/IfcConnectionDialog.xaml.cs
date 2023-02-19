using System.Windows;
using LINQPad.Extensibility.DataContext;

namespace DataContextDriverDemo.DynamicDemo
{
	/// <summary>
	/// Interaction logic for ConnectionDialog.xaml
	/// </summary>
	public partial class IfcConnectionDialog : Window
	{
		readonly IConnectionInfo _cxInfo;

		public IfcConnectionDialog(IConnectionInfo cxInfo)
		{
			_cxInfo = cxInfo;
			DataContext = new IfcConnectionOptions (cxInfo);
			InitializeComponent ();
		}

		void btnOK_Click (object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}		
	}
}
