﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Acuminator.Utilities.Roslyn.PXFieldAttributes
{
	/// <summary>
	/// Information about the Acumatica field attributes.
	/// </summary>
	public class AttributeInformation
	{
		private readonly PXContext _context;
		public ImmutableHashSet<ITypeSymbol> BoundBaseTypes { get; }

		private readonly string _IsDBField = "IsDBField";

		public AttributeInformation(PXContext pxContext)
		{
			pxContext.ThrowOnNull(nameof(pxContext));

			_context = pxContext;

			var boundBaseTypes = GetBoundBaseTypes(_context);
			BoundBaseTypes = boundBaseTypes.ToImmutableHashSet();
		}

		private static HashSet<ITypeSymbol> GetBoundBaseTypes(PXContext context) =>
			new HashSet<ITypeSymbol>
			{
				context.FieldAttributes.PXDBFieldAttribute,
				context.FieldAttributes.PXDBCalcedAttribute
			};

		public IEnumerable<ITypeSymbol> AttributesListDerivedFromClass(ITypeSymbol attributeSymbol, bool expand = false)
		{
			HashSet<ITypeSymbol> results = new HashSet<ITypeSymbol>();

			results.Add(attributeSymbol);

			if (expand)
			{
				foreach (var type in attributeSymbol.GetBaseTypesAndThis())
				{
					if (!type.GetBaseTypes().Contains(_context.AttributeTypes.PXEventSubscriberAttribute))
						break;

					results.Add(type);
				}
			}

			var aggregateAttribute = _context.AttributeTypes.PXAggregateAttribute;
			var dynamicAggregateAttribute = _context.AttributeTypes.PXDynamicAggregateAttribute;

			if (attributeSymbol.InheritsFromOrEquals(aggregateAttribute) || attributeSymbol.InheritsFromOrEquals(dynamicAggregateAttribute))
			{
				var allAttributes = attributeSymbol.GetAllAttributesDefinedOnThisAndBaseTypes();
				foreach (var attribute in allAttributes)
				{
					if (!attribute.GetBaseTypes().Contains(_context.AttributeTypes.PXEventSubscriberAttribute))
						continue;

					results.Add(attribute);
					VisitAggregateAttribute(attribute, 10);
				}
			}
			return results;

			void VisitAggregateAttribute(ITypeSymbol _attributeSymbol, int depth)
			{
				if (depth < 0)
					return;

				if (expand)
				{
					foreach (var type in _attributeSymbol.GetBaseTypesAndThis())
					{
						if (!type.GetBaseTypes().Contains(_context.AttributeTypes.PXEventSubscriberAttribute))
							break;

						results.Add(type);
					}
				}

				if (_attributeSymbol.InheritsFromOrEquals(aggregateAttribute) || _attributeSymbol.InheritsFromOrEquals(dynamicAggregateAttribute))
				{
					var allAttributes = _attributeSymbol.GetAllAttributesDefinedOnThisAndBaseTypes();
					foreach (var attribute in allAttributes)
					{
						if (!attribute.GetBaseTypes().Contains(_context.AttributeTypes.PXEventSubscriberAttribute))
							continue;

						results.Add(attribute);
						VisitAggregateAttribute(attribute, depth - 1);
					}
				}
				return;
			}
		}

		public bool AttributeDerivedFromClass(ITypeSymbol attributeSymbol, ITypeSymbol type)
		{
			if (attributeSymbol.InheritsFromOrEquals(type))
				return true;

			var aggregateAttribute = _context.AttributeTypes.PXAggregateAttribute;
			var dynamicAggregateAttribute = _context.AttributeTypes.PXDynamicAggregateAttribute;

			if (attributeSymbol.InheritsFromOrEquals(aggregateAttribute) || attributeSymbol.InheritsFromOrEquals(dynamicAggregateAttribute))
			{
				var allAttributes = attributeSymbol.GetAllAttributesDefinedOnThisAndBaseTypes();
				foreach (var attribute in allAttributes)
				{
					if (!attribute.GetBaseTypes().Contains(_context.AttributeTypes.PXEventSubscriberAttribute))
						continue;

					var result = VisitAggregateAttribute(attribute, 10);

					if (result)
						return result;
				}
			}
			return false;

			bool VisitAggregateAttribute(ITypeSymbol _attributeSymbol, int depth)
			{
				if (depth < 0)
					return false;

				if (_attributeSymbol.InheritsFromOrEquals(type))
					return true;

				if (_attributeSymbol.InheritsFromOrEquals(aggregateAttribute) || _attributeSymbol.InheritsFromOrEquals(dynamicAggregateAttribute))
				{
					var allAttributes = _attributeSymbol.GetAllAttributesDefinedOnThisAndBaseTypes();
					foreach (var attribute in allAttributes)
					{
						if (!attribute.GetBaseTypes().Contains(_context.AttributeTypes.PXEventSubscriberAttribute))
							continue;

						var result = VisitAggregateAttribute(attribute, depth - 1);

						if (result)
							return result;
					}
				}
				return false;
			}
		}

		public BoundAttribute IsBoundAttribute(AttributeData attribute)
		{
			foreach (var baseType in BoundBaseTypes)
			{
				if (AttributeDerivedFromClass(attribute.AttributeClass, baseType))
					return BoundAttribute.DbBound;
			}
			if (attribute.AttributeClass.GetMembers().Select(a => a.Name).Contains(_IsDBField))
			{
				foreach (var argument in attribute.NamedArguments)
				{
					if (argument.Key.Equals(_IsDBField))
					{
						if (argument.Value.Value.Equals(true))
							return BoundAttribute.DbBound;
						else
							return BoundAttribute.Unbound;
					}

				}
				return BoundAttribute.Unknown;
			}
			return BoundAttribute.Unbound;
		}

		///TODO: refactoring arguments -> remove semanticModel to constructor? Is it nessesary.
		///TODO: Add DataFlow analyze to corner cases with defaul IsDBBound assigment.
		public bool? IsBoundField(PropertyDeclarationSyntax property, SemanticModel semanticModel)
		{
			var typeSymbol = semanticModel.GetDeclaredSymbol(property);
			var attributesData = typeSymbol.GetAttributes();

			foreach (var attribute in attributesData)
			{
				if (IsBoundAttribute(attribute) == BoundAttribute.DbBound)
					return true;
				foreach (var argument in attribute.NamedArguments)
				{
					if (argument.Key.Equals("IsDBField") && argument.Value.Value.Equals(true))
						return (bool)argument.Value.Value;
				}
			}
			return false;
		}

		public BoundAttribute ContainsBoundAttributes(IEnumerable<AttributeData> attributes)
		{
			foreach (var attribute in attributes)
			{
				BoundAttribute result = IsBoundAttribute(attribute);
				if (result == BoundAttribute.DbBound || result == BoundAttribute.Unknown)
					return result;
			}
			return BoundAttribute.Unbound;
		}

	}

	public enum BoundAttribute
	{
		Unbound = 0,
		DbBound = 1,
		Unknown = 2
	}
}