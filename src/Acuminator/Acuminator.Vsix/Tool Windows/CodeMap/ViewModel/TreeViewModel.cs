﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Acuminator.Utilities.Common;
using Acuminator.Vsix.Utilities;


namespace Acuminator.Vsix.ToolWindows.CodeMap
{
	public class TreeViewModel : ViewModelBase
	{
		public CodeMapWindowViewModel CodeMapViewModel { get; } 

		public ExtendedObservableCollection<TreeNodeViewModel> RootItems { get; } = new ExtendedObservableCollection<TreeNodeViewModel>();

		private TreeNodeViewModel _selectedItem;

		public TreeNodeViewModel SelectedItem
		{
			get => _selectedItem;
			set
			{
				TreeNodeViewModel previousSelection = _selectedItem;
				_selectedItem = value;

				if (previousSelection != null)
					previousSelection.IsSelected = false;

				if (_selectedItem != null)
					_selectedItem.IsSelected = true;

				NotifyPropertyChanged();
			}
		}

		/// <summary>
		/// A workaround to avoid endless loop of TreeNodeViewModel IsSelected and TreeViewModel SelectedItem setting each other.
		/// </summary>
		/// <param name="selected">The selected.</param>
		internal void SetSelectedWithoutNotification(TreeNodeViewModel selected)
		{
			_selectedItem = selected;
			NotifyPropertyChanged(nameof(SelectedItem));
		}

		public TreeViewModel(CodeMapWindowViewModel windowViewModel)
		{
			windowViewModel.ThrowOnNull(nameof(windowViewModel));

			CodeMapViewModel = windowViewModel;
		}
	}
}