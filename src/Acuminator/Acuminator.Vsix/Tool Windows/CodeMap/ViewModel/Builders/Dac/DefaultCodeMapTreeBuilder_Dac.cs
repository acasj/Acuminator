﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.Dac;
using Acuminator.Utilities.Roslyn.Semantic.SharedInfo;

namespace Acuminator.Vsix.ToolWindows.CodeMap
{
	public partial class DefaultCodeMapTreeBuilder : TreeBuilderBase
	{
		protected virtual DacNodeViewModel CreateDacNode(DacSemanticModel dacSemanticModel, TreeViewModel tree) =>
			new DacNodeViewModel(dacSemanticModel, tree, ExpandCreatedNodes);

		public override IEnumerable<TreeNodeViewModel>? VisitNode(DacNodeViewModel dac)
		{
			var dacAttributesGroup = GetDacAttributesGroupNode(dac);

			if (dacAttributesGroup != null)
				yield return dacAttributesGroup;

			foreach (DacMemberCategory dacMemberCategory in GetDacMemberCategoriesInOrder())
			{
				Cancellation.ThrowIfCancellationRequested();
				var dacCategory = CreateCategory(dac, dacMemberCategory);

				if (dacCategory != null)
				{
					yield return dacCategory;
				}
			}
		}

		protected virtual DacAttributesGroupNodeViewModel GetDacAttributesGroupNode(DacNodeViewModel dac) =>
			new DacAttributesGroupNodeViewModel(dac.DacModel, dac, ExpandCreatedNodes);

		protected virtual IEnumerable<DacMemberCategory> GetDacMemberCategoriesInOrder()
		{
			yield return DacMemberCategory.InitializationAndActivation;
			yield return DacMemberCategory.Keys;
			yield return DacMemberCategory.Property;
			yield return DacMemberCategory.FieldsWithoutProperty;
		}

		protected virtual DacMemberCategoryNodeViewModel? CreateCategory(DacNodeViewModel dac, DacMemberCategory dacMemberCategory) =>
			dacMemberCategory switch
			{
				DacMemberCategory.InitializationAndActivation => new DacInitializationAndActivationCategoryNodeViewModel(dac, ExpandCreatedNodes),
				DacMemberCategory.Keys 						  => new KeyDacFieldsCategoryNodeViewModel(dac, ExpandCreatedNodes),
				DacMemberCategory.Property 					  => new AllDacFieldsDacCategoryNodeViewModel(dac, ExpandCreatedNodes),
				_ 											  => null,
			};

		public override IEnumerable<TreeNodeViewModel> VisitNode(DacAttributesGroupNodeViewModel attributeGroupNode) =>
			attributeGroupNode.AttributeInfos()
							  .Select(attrInfo => new DacAttributeNodeViewModel(attributeGroupNode, attrInfo, ExpandCreatedNodes));

		public override IEnumerable<TreeNodeViewModel>? VisitNode(DacInitializationAndActivationCategoryNodeViewModel dacInitializationAndActivationCategory)
		{
			Cancellation.ThrowIfCancellationRequested();

			if (dacInitializationAndActivationCategory?.DacModel.IsActiveMethodInfo != null)
			{
				var isActiveNode = new IsActiveDacMethodNodeViewModel(dacInitializationAndActivationCategory,
																	  dacInitializationAndActivationCategory.DacModel.IsActiveMethodInfo, 
																	  ExpandCreatedNodes);
				return [isActiveNode];
			}
			else
				return [];
		}

		public override IEnumerable<TreeNodeViewModel>? VisitNode(KeyDacFieldsCategoryNodeViewModel dacKeysCategory)
		{
			dacKeysCategory.ThrowOnNull();
			return CreateDacMemberCategoryChildren<DacPropertyInfo>(dacKeysCategory,
																	propertyInfo => new DacFieldGroupingNodeViewModel(dacKeysCategory, propertyInfo, ExpandCreatedNodes));
		}

		public override IEnumerable<TreeNodeViewModel>? VisitNode(AllDacFieldsDacCategoryNodeViewModel dacPropertiesCategory)
		{
			dacPropertiesCategory.ThrowOnNull();
			return CreateDacMemberCategoryChildren<DacPropertyInfo>(dacPropertiesCategory,
																	propertyInfo => new DacFieldGroupingNodeViewModel(dacPropertiesCategory, propertyInfo, ExpandCreatedNodes));
		}

		protected virtual IEnumerable<TreeNodeViewModel> CreateDacMemberCategoryChildren<TInfo>(DacMemberCategoryNodeViewModel dacMemberCategory,
																								Func<TInfo, TreeNodeViewModel> constructor)
		where TInfo : SymbolItem
		{
			var categorySymbols = dacMemberCategory?.GetCategoryDacNodeSymbols();

			if (categorySymbols == null)
			{
				yield break;
			}

			foreach (TInfo info in categorySymbols)
			{
				Cancellation.ThrowIfCancellationRequested();
				TreeNodeViewModel childNode = constructor(info);

				if (childNode != null)
				{
					yield return childNode;
				}
			}
		}

		public override IEnumerable<TreeNodeViewModel>? VisitNode(DacFieldGroupingNodeViewModel property)
		{
			var attributes = property.CheckIfNull().PropertyInfo.Attributes;
			return !attributes.IsDefaultOrEmpty
				? attributes.Select(attrInfo => new DacFieldAttributeNodeViewModel(property, attrInfo, ExpandCreatedNodes))
				: [];
		}
	}
}