﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.Dac;
using Acuminator.Vsix.Utilities;



namespace Acuminator.Vsix.ToolWindows.CodeMap
{
	public class DacPropertiesCategoryNodeViewModel : DacMemberCategoryNodeViewModel
	{
		public override Icon NodeIcon => Icon.DacPropertiesCategory;

		protected override bool AllowNavigation => true;

		public DacPropertiesCategoryNodeViewModel(DacNodeViewModel dacViewModel, bool isExpanded) : 
											 base(dacViewModel, DacMemberCategory.Property, isExpanded)
		{		
		}

		public override IEnumerable<SymbolItem> GetCategoryDacNodeSymbols() => DacModel.AllDeclaredProperties;

		protected override IEnumerable<TreeNodeViewModel> CreateChildren(TreeBuilderBase treeBuilder, bool expandChildren, CancellationToken cancellation) =>
			treeBuilder.VisitNodeAndBuildChildren(this, expandChildren, cancellation);
	}
}