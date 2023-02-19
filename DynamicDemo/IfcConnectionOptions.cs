using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace DataContextDriverDemo.DynamicDemo
{
    public class IfcConnectionOptions
    {

		public IConnectionInfo ConnectionInfo { get; private set; }

	XElement DriverData => ConnectionInfo.DriverData;

	public IfcConnectionOptions(IConnectionInfo cxInfo)
	{
		ConnectionInfo = cxInfo;
	}

	// This is how to create custom properties.
	public string IfcFilePath
	{
		get => (string)DriverData.Element("IfcFilePath");
		set => DriverData.SetElementValue("IfcFilePath", value);
	}
}
}
