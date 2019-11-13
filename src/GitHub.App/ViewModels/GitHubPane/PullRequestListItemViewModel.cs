﻿using System;
using GitHub.Models;
using ReactiveUI;

namespace GitHub.ViewModels.GitHubPane
{
    /// <summary>
    /// A view model which displays an item in a <see cref="PullRequestListViewModel"/>.
    /// </summary>
    public class PullRequestListItemViewModel : ViewModelBase, IPullRequestListItemViewModel
    {
        bool isCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="PullRequestListItemViewModel"/> class.
        /// </summary>
        /// <param name="model">The underlying pull request item model.</param>
        public PullRequestListItemViewModel(PullRequestListItemModel model)
        {
            Id = model.Id;
            Author = new ActorViewModel(model.Author);
            ChecksSummary = model.ChecksSummary;
            ChecksErrorCount = model.ChecksErrorCount;
            ChecksPendingCount = model.ChecksPendingCount;
            ChecksSuccessCount = model.ChecksSuccessCount;
            CommentCount = model.CommentCount;
            Number = model.Number;
            Title = model.Title;
            UpdatedAt = model.UpdatedAt;
        }

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public IActorViewModel Author { get; }

        /// <inheritdoc/>
        public PullRequestChecksSummaryState ChecksSummary { get; }

        /// <inheritdoc/>
        public int ChecksSuccessCount { get; }

        /// <inheritdoc/>
        public int ChecksPendingCount { get; }

        /// <inheritdoc/>
        public int ChecksErrorCount { get; }

        /// <inheritdoc/>
        public int CommentCount { get; }

        /// <inheritdoc/>
        public bool IsCurrent
        {
            get { return isCurrent; }
            internal set { this.RaiseAndSetIfChanged(ref isCurrent, value); }
        }

        /// <inheritdoc/>
        public int Number { get; }

        /// <inheritdoc/>
        public string Title { get; }

        /// <inheritdoc/>
        public DateTimeOffset UpdatedAt { get; }
    }
}
