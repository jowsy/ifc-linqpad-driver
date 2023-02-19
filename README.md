# XBIM IFC LINQPAD DRIVER
A LinqPad driver for IFC using XBim


# Example Query

This query tries to retrieve areas for every space/room in the building.
```
IfcSpaces.Select(space => 
   						new { Name = space.Name, 
							  Storey = space.Decomposes.Select(s => (s.RelatingObject as IIfcBuildingStorey)?.LongName),
							  Areas = space.IsDefinedBy.SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
							  						.OfType<IIfcElementQuantity>()
													.SelectMany(qset => qset.Quantities)
													.OfType<IIfcQuantityArea>()
													.Select(q => new { Name = q.Name, Value = q.AreaValue })
 							  }
						)
						.Dump();

```