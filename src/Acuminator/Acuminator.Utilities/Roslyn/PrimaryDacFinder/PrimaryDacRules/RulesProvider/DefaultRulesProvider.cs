﻿#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.PrimaryDacFinder.PrimaryDacRules.ActionRules;
using Acuminator.Utilities.Roslyn.PrimaryDacFinder.PrimaryDacRules.Base;
using Acuminator.Utilities.Roslyn.PrimaryDacFinder.PrimaryDacRules.DacRules;
using Acuminator.Utilities.Roslyn.PrimaryDacFinder.PrimaryDacRules.GraphRules;
using Acuminator.Utilities.Roslyn.PrimaryDacFinder.PrimaryDacRules.ViewRules;
using Acuminator.Utilities.Roslyn.Semantic;

namespace Acuminator.Utilities.Roslyn.PrimaryDacFinder.PrimaryDacRules.RulesProvider
{
	/// <summary>
	/// A default factory to create primary DAC rules.
	/// </summary>
	internal class DefaultRulesProvider : IRulesProvider
	{
		private readonly ImmutableArray<PrimaryDacRuleBase> _rules;

		public DefaultRulesProvider(PXContext context)
		{
			_rules = GetPrimaryDacCalculationRules(context.CheckIfNull()).ToImmutableArray();
		}

		public ImmutableArray<PrimaryDacRuleBase> GetRules() => _rules;

		private static PrimaryDacRuleBase[] GetPrimaryDacCalculationRules(PXContext context) =>
			[
				//AbsoluteRules
				new PrimaryDacSpecifiedGraphRule(),
				new PXImportAttributeGraphRule(),
				new PXFilteredProcessingGraphRule(context),

				// Heuristic rules
				// Graph rules
				new FirstViewsInGraphRule(numberOfViews: 1),
				new FirstViewsInGraphRule(numberOfViews: 3),
				new FirstViewsInGraphRule(numberOfViews: 5),
				new FirstViewsInGraphRule(numberOfViews: 10),

				new PairOfViewsWithSpecialNamesGraphRule(firstName: "Document", secondName: "CurrentDocument"),
				new PairOfViewsWithSpecialNamesGraphRule(firstName: "Entities", secondName: "CurrentEntity"),

				new NoReadOnlyViewGraphRule(),
				new ViewsWithoutPXViewNameAttributeGraphRule(context),

				// View rules
				new ForbiddenWordsInNameViewRule(useCaseSensitiveComparison: false),
				new HiddenAttributesViewRule(),
				new NoPXSetupViewRule(),
				new PXViewNameAttributeViewRule(context),			

				// Action rules
				new ScoreSimpleActionRule(),
				new ScoreSystemActionRule(context),

				// DAC rules
				new SameOrDescendingNamespaceDacRule()
			];
	}
}