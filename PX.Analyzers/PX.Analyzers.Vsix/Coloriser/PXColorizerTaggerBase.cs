﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace PX.Analyzers.Coloriser
{
    /// <summary>
    /// Values that represent tagger types.
    /// </summary>
    public enum TaggerType
    {
        /// <summary>
        /// The general tagger which chooses other taggers according to the settings.
        /// </summary>
        General,

        /// <summary>
        /// The tagger based on Roslyn 
        /// </summary>
        Roslyn,

        /// <summary>
        /// The tagger based on regular expressions
        /// </summary>
        RegEx
    };

    /// <summary>
    /// A colorizer tagger base class.
    /// </summary>
    public abstract class PXColorizerTaggerBase : ITagger<IClassificationTag>, IDisposable
    {
#pragma warning disable CS0067
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore CS0067

        protected ITextBuffer Buffer { get; }

        protected PXColorizerTaggerProvider Provider { get; }

        protected ITextSnapshot Cache { get; private set; }

        protected bool ColoringSettingsChanged { get; private set; }

        protected bool SubscribedToSettingsChanges { get; private set; }

        private readonly bool cacheCheckingEnabled;

        /// <summary>
        /// The type of the tagger.
        /// </summary>      
        public abstract TaggerType TaggerType { get; }

        protected List<ITagSpan<IClassificationTag>> TagsList { get; } = new List<ITagSpan<IClassificationTag>>();

        public abstract IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans);

        protected PXColorizerTaggerBase(ITextBuffer buffer, PXColorizerTaggerProvider aProvider, bool subscribeToSettingsChanges, 
                                        bool useCacheChecking)
        {
            buffer.ThrowOnNull(nameof(buffer));
            aProvider.ThrowOnNull(nameof(aProvider));

            Buffer = buffer;
            Provider = aProvider;
            SubscribedToSettingsChanges = subscribeToSettingsChanges;
            cacheCheckingEnabled = useCacheChecking;

            if (SubscribedToSettingsChanges)
            {
                var genOptionsPage = Provider.Package?.GeneralOptionsPage;

                if (genOptionsPage != null)
                {
                    genOptionsPage.ColoringSettingChanged += ColoringSettingChangedHandler;
                }
            }
        }

        private void ColoringSettingChangedHandler(object sender, Vsix.SettingChangedEventArgs e)
        {
            ColoringSettingsChanged = true;
            RaiseTagsChanged();
        }

        protected bool TagsChangedIsNull() => TagsChanged == null;

        protected void RaiseTagsChanged() => TagsChanged?.Invoke(this, 
            new SnapshotSpanEventArgs(
                new SnapshotSpan(Buffer.CurrentSnapshot,
                    new Span(0, Buffer.CurrentSnapshot.Length))));

        protected internal virtual void ResetCacheAndFlags(ITextSnapshot newCache)
        {
            ColoringSettingsChanged = false;
            TagsList.Clear();
            Cache = newCache;
        }

        protected virtual bool CheckIfRetaggingIsNotNecessary(ITextSnapshot snapshot) =>
            cacheCheckingEnabled && Cache != null && Cache == snapshot && !ColoringSettingsChanged;
        
        public virtual void Dispose()
        {
            if (!SubscribedToSettingsChanges)
                return;

            var genOptionsPage = Provider.Package?.GeneralOptionsPage;

            if (genOptionsPage != null)
            {
                genOptionsPage.ColoringSettingChanged -= ColoringSettingChangedHandler;
            }
        }
    }
}