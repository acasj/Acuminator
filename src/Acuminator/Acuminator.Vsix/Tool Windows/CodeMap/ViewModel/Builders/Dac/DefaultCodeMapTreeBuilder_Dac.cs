﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.Dac;
using Acuminator.Vsix.Utilities;


namespace Acuminator.Vsix.ToolWindows.CodeMap
{
	public partial class DefaultCodeMapTreeBuilder : TreeBuilderBase
	{
		protected virtual DacNodeViewModel CreateDacNode(DacSemanticModel dacSemanticModel, TreeViewModel tree) =>
			new DacNodeViewModel(dacSemanticModel, tree, ExpandCreatedNodes);

		public override IEnumerable<TreeNodeViewModel> VisitNodeAndBuildChildren(DacNodeViewModel dac, bool expandChildren, CancellationToken cancellation)
		{
			foreach (DacMemberCategory dacMemberCategory in GetDacMemberCategoriesInOrder())
			{
				cancellation.ThrowIfCancellationRequested();
				var dacCategory = CreateCategory(dac, dacMemberCategory, expandChildren);

				if (dacCategory != null)
				{
					yield return dacCategory;
				}
			}
		}

		protected virtual IEnumerable<DacMemberCategory> GetDacMemberCategoriesInOrder()
		{
			yield return DacMemberCategory.Keys;
			yield return DacMemberCategory.Property;
			yield return DacMemberCategory.FieldsWithoutProperty;
		}

		protected virtual DacMemberCategoryNodeViewModel CreateCategory(DacNodeViewModel dac, DacMemberCategory dacMemberCategory, bool isExpanded)
		{
			switch (dacMemberCategory)
			{
				case DacMemberCategory.Keys:
					return new DacKeysCategoryNodeViewModel(dac, isExpanded);

				case DacMemberCategory.Property:
					return new DacPropertiesCategoryNodeViewModel(dac, isExpanded);

				case DacMemberCategory.FieldsWithoutProperty:
				default:
					return null;
			}
		}

		public override IEnumerable<TreeNodeViewModel> VisitNodeAndBuildChildren(DacKeysCategoryNodeViewModel dacKeysCategory,
																				 bool expandChildren, CancellationToken cancellation)
		{
			dacKeysCategory.ThrowOnNull(nameof(dacKeysCategory));
			return CreateDacMemberCategoryChildren<DacPropertyInfo>(dacKeysCategory,
																	propertyInfo => new PropertyNodeViewModel(dacKeysCategory, propertyInfo, expandChildren),
																	cancellation);
		}

		public override IEnumerable<TreeNodeViewModel> VisitNodeAndBuildChildren(DacPropertiesCategoryNodeViewModel dacPropertiesCategory,
																				 bool expandChildren, CancellationToken cancellation)
		{
			dacPropertiesCategory.ThrowOnNull(nameof(dacPropertiesCategory));
			return CreateDacMemberCategoryChildren<DacPropertyInfo>(dacPropertiesCategory, 
																	propertyInfo => new PropertyNodeViewModel(dacPropertiesCategory, propertyInfo, expandChildren),
																	cancellation);
		}

		protected virtual IEnumerable<TreeNodeViewModel> CreateDacMemberCategoryChildren<TInfo>(DacMemberCategoryNodeViewModel dacMemberCategory,
																								Func<TInfo, TreeNodeViewModel> constructor,
																								CancellationToken cancellation)
		where TInfo : SymbolItem
		{
			var categorySymbols = dacMemberCategory?.GetCategoryDacNodeSymbols()
												   ?.OrderBy(symbol => symbol.DeclarationOrder);
			if (categorySymbols == null)
			{
				yield break;
			}

			foreach (TInfo info in categorySymbols)
			{
				cancellation.ThrowIfCancellationRequested();
				TreeNodeViewModel childNode = constructor(info);

				if (childNode != null)
				{
					yield return childNode;
				}
			}
		}

		public override IEnumerable<TreeNodeViewModel> VisitNodeAndBuildChildren(PropertyNodeViewModel property, bool expandChildren,
																				 CancellationToken cancellation)
		{
			property.ThrowOnNull(nameof(property));
			return property.PropertyInfo.Attributes.Select(a => new AttributeNodeViewModel(property, a.AttributeData, expandChildren));
		}
	}
}