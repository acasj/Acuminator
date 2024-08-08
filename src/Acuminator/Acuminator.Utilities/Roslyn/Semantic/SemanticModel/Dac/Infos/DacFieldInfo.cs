﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.PXFieldAttributes;

using Microsoft.CodeAnalysis;

namespace Acuminator.Utilities.Roslyn.Semantic.Dac
{
	/// <summary>
	/// Information about a DAC field - a pair consisting of a DAC field property and a DAC BQL field declared in the same type.
	/// </summary>
	public class DacFieldInfo : IWriteableBaseItem<DacFieldInfo>
	{
		public string Name { get; }

		public ITypeSymbol? DacType { get; }

		public DacPropertyInfo? PropertyInfo { get; }

		public DacBqlFieldInfo? BqlFieldInfo { get; }

		public DacFieldInfo? Base { get; set; }

		DacFieldInfo? IOverridableItem<DacFieldInfo>.Base => Base;

		public int DeclarationOrder => PropertyInfo?.DeclarationOrder ?? BqlFieldInfo!.DeclarationOrder;

		/// <summary>
		///  True if this property is DAC property - it has a corresponding DAC field.
		/// </summary
		public bool IsDacProperty { get; }

		/// <value>
		/// The type of the DAC field property.
		/// </value>
		public ITypeSymbol? FieldPropertyType { get; }

		/// <value>
		/// The effective type of the property. For reference types and non nullable value types it is the same as <see cref="PropertyType"/>. 
		/// For nulable value types it is the underlying type extracted from nullable. It is <c>T</c> for <see cref="Nullable{T}"/>.
		/// </value>
		public ITypeSymbol? EffectivePropertyType { get; }

		/// <summary>
		/// The DB boundness calculated from attributes declared on this DAC property.
		/// </summary>
		public DbBoundnessType DeclaredDbBoundness { get; }

		/// <summary>
		/// The effective bound type for this DAC field obtained by the combination of <see cref="DeclaredDbBoundness"/>s of this propety's override chain. 
		/// </summary>
		public DbBoundnessType EffectiveDbBoundness { get; }

		public bool IsIdentity { get; }

		public bool IsKey { get; }

		public bool IsAutoNumbering { get; }

		public DacFieldInfo(DacPropertyInfo? dacPropertyInfo, DacBqlFieldInfo? dacBqlFieldInfo)
		{
			if (dacPropertyInfo == null && dacBqlFieldInfo == null)
				throw new ArgumentNullException($"Both {nameof(dacPropertyInfo)} and {nameof(dacBqlFieldInfo)} parameters cannot be null.");

			PropertyInfo = dacPropertyInfo;
			BqlFieldInfo = dacBqlFieldInfo;
			Name 		 = PropertyInfo?.Name ?? BqlFieldInfo!.Name.ToPascalCase();
			DacType 	 = PropertyInfo?.Symbol.ContainingType ?? BqlFieldInfo!.Symbol.ContainingType;

			DacFieldMetadata metadata = DacFieldMetadata.FromDacFieldInfo(this);
			IsDacProperty 		  = metadata.IsDacProperty;
			IsIdentity 			  = metadata.IsIdentity;
			IsKey 				  = metadata.IsKey;
			IsAutoNumbering 	  = metadata.IsAutoNumbering;
			FieldPropertyType 	  = metadata.FieldPropertyType;
			EffectivePropertyType = metadata.EffectivePropertyType;
			DeclaredDbBoundness   = metadata.DeclaredDbBoundness;
			EffectiveDbBoundness  = metadata.EffectiveDbBoundness;
		}

		public bool IsDeclaredInType(ITypeSymbol? type) =>
			 PropertyInfo?.Symbol.IsDeclaredInType(type) ?? BqlFieldInfo!.Symbol.IsDeclaredInType(type);

		protected readonly record struct DacFieldMetadata(bool IsDacProperty, bool IsKey, bool IsIdentity, bool IsAutoNumbering,
														  ITypeSymbol? FieldPropertyType, ITypeSymbol? EffectivePropertyType, 
														  DbBoundnessType DeclaredDbBoundness, DbBoundnessType EffectiveDbBoundness)
		{
			public static DacFieldMetadata FromDacFieldInfo(DacFieldInfo fieldInfo)
			{
				var fieldsChain = fieldInfo.ThisAndOverridenItems();
				bool hasBqlField = false;
				bool hasFieldProperty = false;
				bool? isKey = null, isIdentity = null, isAutoNumbering = null;
				ITypeSymbol? fieldPropertyType = null, effectivePropertyType = null;
				DbBoundnessType? declaredDbBoundness = null, effectiveDbBoundness = null;

				foreach (DacFieldInfo fieldInChain in fieldsChain)
				{
					var propertyInfo = fieldInChain.PropertyInfo;

					if (propertyInfo != null)
					{
						hasFieldProperty = true;
						isKey 				  ??= propertyInfo.IsKey;
						isIdentity 			  ??= propertyInfo.IsIdentity;
						isAutoNumbering 	  ??= propertyInfo.IsAutoNumbering;
						fieldPropertyType 	  ??= propertyInfo.PropertyType;
						effectivePropertyType ??= propertyInfo.EffectivePropertyType;
						declaredDbBoundness   ??= propertyInfo.DeclaredDbBoundness;
						effectiveDbBoundness  ??= propertyInfo.EffectiveDbBoundness;
					}

					if (fieldInChain.BqlFieldInfo != null)
					{
						hasBqlField = true;
					}
				}

				bool isDacProperty = hasFieldProperty && hasBqlField;
				return new DacFieldMetadata(isDacProperty, isKey ?? false, isIdentity ?? false, isAutoNumbering ?? false,
											fieldPropertyType, effectivePropertyType, declaredDbBoundness ?? DbBoundnessType.NotDefined,
											effectiveDbBoundness ?? DbBoundnessType.NotDefined);
			}
		}
	}
}
