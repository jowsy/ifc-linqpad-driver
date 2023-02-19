﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LINQPad;
using LINQPad.Extensibility.DataContext;

namespace DataContextDriverDemo.Universal
{
	/// <summary>
	/// This static driver let users query any data source that looks like a Data Context - in other words,
	/// that exposes properties of type IEnumerable of T.
	/// </summary>
	public class UniversalStaticDriver : StaticDataContextDriver
	{
		static UniversalStaticDriver ()
		{
			// Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown.
			//AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
			//{
			//	if (args.Exception.StackTrace.Contains (typeof (UniversalStaticDriver).Namespace))
			//		Debugger.Launch ();
			//};
		}

		public override string Name => "Universal Demo Driver";

		public override string Author => "Joe Albahari";

		public override string GetConnectionDescription (IConnectionInfo cxInfo)
		{
			// For static drivers, we can use the description of the custom type & its assembly:
			return cxInfo.CustomTypeInfo.GetCustomTypeDescription();
		}

		public override bool ShowConnectionDialog (IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
		{
			// Prompt the user for a custom assembly and type name:
			return new ConnectionDialog (cxInfo).ShowDialog () == true;
		}

		public override void InitializeContext (IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
		{
			// No extra initialization required...
		}

		public override List<ExplorerItem> GetSchema (IConnectionInfo cxInfo, Type customType)
		{
			// Return the objects with which to populate the Schema Explorer by reflecting over customType.
			
			// We'll start by retrieving all the properties of the custom type that implement IEnumerable<T>:
			var topLevelProps =
			(
				from prop in customType.GetProperties ()
				where prop.PropertyType != typeof (string)

				// Display all properties of type IEnumerable<T> (except for string!)
				let ienumerableOfT = prop.PropertyType.GetInterface ("System.Collections.Generic.IEnumerable`1")
				where ienumerableOfT != null

				orderby prop.Name

				select new ExplorerItem (prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
				{
					IsEnumerable = true,
					ToolTipText = FormatTypeName (prop.PropertyType, false),

					// Store the entity type to the Tag property. We'll use it later.
					Tag = ienumerableOfT.GetGenericArguments () [0]
				}

			).ToList ();

			// Create a lookup keying each element type to the properties of that type. This will allow
			// us to build hyperlink targets allowing the user to click between associations:
			var elementTypeLookup = topLevelProps.ToLookup (tp => (Type)tp.Tag);

			// Populate the columns (properties) of each entity:
			foreach (ExplorerItem table in topLevelProps)
			{
				Type parentType = (Type)table.Tag;
				var props = parentType.GetProperties().Select (p => GetChildItem (elementTypeLookup, p.Name, p.PropertyType));
				var fields = parentType.GetFields().Select (f => GetChildItem (elementTypeLookup, f.Name, f.FieldType));
				table.Children = props.Union (fields).OrderBy (childItem => childItem.Kind).ToList ();
			}

			return topLevelProps;
		}

		ExplorerItem GetChildItem (ILookup<Type, ExplorerItem> elementTypeLookup, string childPropName, Type childPropType)
		{
			// If the property's type is in our list of entities, then it's a Many:1 (or 1:1) reference.
			// We'll assume it's a Many:1 (we can't reliably identify 1:1s purely from reflection).
			if (elementTypeLookup.Contains (childPropType))
				return new ExplorerItem (childPropName, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne)
				{
					HyperlinkTarget = elementTypeLookup [childPropType].First (),
					// FormatTypeName is a helper method that returns a nicely formatted type name.
					ToolTipText = FormatTypeName (childPropType, true)
				};

			// Is the property's type a collection of entities?
			Type ienumerableOfT = childPropType.GetInterface ("System.Collections.Generic.IEnumerable`1");
			if (ienumerableOfT != null)
			{
				Type elementType = ienumerableOfT.GetGenericArguments () [0];
				if (elementTypeLookup.Contains (elementType))
					return new ExplorerItem (childPropName, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany)
					{
						HyperlinkTarget = elementTypeLookup [elementType].First (),
						ToolTipText = FormatTypeName (elementType, true)
					};
			}

			// Ordinary property:
			return new ExplorerItem (childPropName + " (" + FormatTypeName (childPropType, false) + ")",
				ExplorerItemKind.Property, ExplorerIcon.Column);
		}
	}
}
