﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive;
using System.Threading.Tasks;
using GitHub.Extensions;
using GitHub.Factories;
using GitHub.Models;
using GitHub.Primitives;
using GitHub.Services;
using ReactiveUI;

namespace GitHub.ViewModels.Documents
{
    /// <summary>
    /// View model for displaying a pull request in a document window.
    /// </summary>
    [Export(typeof(IPullRequestPageViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PullRequestPageViewModel : PullRequestViewModelBase, IPullRequestPageViewModel, IIssueishCommentThreadViewModel
    {
        readonly IViewViewModelFactory factory;
        readonly IPullRequestService service;
        readonly IPullRequestSessionManager sessionManager;
        readonly ITeamExplorerServices teServices;
        readonly IVisualStudioBrowser visualStudioBrowser;
        readonly IUsageTracker usageTracker;
        ActorModel currentUserModel;
        ReactiveList<IViewModel> timeline = new ReactiveList<IViewModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PullRequestPageViewModel"/> class.
        /// </summary>
        /// <param name="factory">The view model factory.</param>
        [ImportingConstructor]
        public PullRequestPageViewModel(
            IViewViewModelFactory factory,
            IPullRequestService service,
            IPullRequestSessionManager sessionManager,
            ITeamExplorerServices teServices,
            IVisualStudioBrowser visualStudioBrowser,
            IUsageTracker usageTracker)
        {
            Guard.ArgumentNotNull(factory, nameof(factory));
            Guard.ArgumentNotNull(service, nameof(service));
            Guard.ArgumentNotNull(sessionManager, nameof(sessionManager));
            Guard.ArgumentNotNull(visualStudioBrowser, nameof(visualStudioBrowser));
            Guard.ArgumentNotNull(teServices, nameof(teServices));

            this.factory = factory;
            this.service = service;
            this.sessionManager = sessionManager;
            this.teServices = teServices;
            this.visualStudioBrowser = visualStudioBrowser;
            this.usageTracker = usageTracker;

            timeline.ItemsRemoved.Subscribe(TimelineItemRemoved);

            ShowCommit = ReactiveCommand.CreateFromTask<string>(DoShowCommit);
            OpenOnGitHub = ReactiveCommand.Create(DoOpenOnGitHub);
        }

        /// <inheritdoc/>
        public IActorViewModel CurrentUser { get; private set; }

        /// <inheritdoc/>
        public int CommitCount { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<IViewModel> Timeline => timeline;

        /// <inheritdoc/>
        public ReactiveCommand<string, Unit> ShowCommit { get; }

        /// <inheritdoc/>
        public async Task InitializeAsync(
            RemoteRepositoryModel repository,
            LocalRepositoryModel localRepository,
            ActorModel currentUser,
            PullRequestDetailModel model)
        {
            await base.InitializeAsync(repository, localRepository, model).ConfigureAwait(true);

            timeline.Clear();
            CommitCount = 0;
            currentUserModel = currentUser;
            CurrentUser = new ActorViewModel(currentUser);

            var commits = new List<CommitSummaryViewModel>();

            foreach (var i in model.Timeline)
            {
                if (!(i is CommitModel) && commits.Count > 0)
                {
                    timeline.Add(new CommitListViewModel(commits));
                    commits.Clear();
                }

                switch (i)
                {
                    case CommitModel commit:
                        commits.Add(new CommitSummaryViewModel(commit));
                        ++CommitCount;
                        break;
                    case CommentModel comment:
                        await AddComment(comment).ConfigureAwait(true);
                        break;
                }
            }

            if (commits.Count > 0)
            {
                timeline.Add(new CommitListViewModel(commits));
            }

            await AddPlaceholder().ConfigureAwait(true);
            await usageTracker.IncrementCounter(x => x.NumberOfPRConversationsOpened);
        }

        /// <inheritdoc/>
        public async Task CloseOrReopen(ICommentViewModel comment)
        {
            var address = HostAddress.Create(Repository.CloneUrl);

            if (State == PullRequestState.Open)
            {
                await service.CloseIssueish(
                    address,
                    Repository.Owner,
                    Repository.Name,
                    Number).ConfigureAwait(true);
                State = PullRequestState.Closed;
            }
            else
            {
                await service.ReopenIssueish(
                    address,
                    Repository.Owner,
                    Repository.Name,
                    Number).ConfigureAwait(true);
                State = PullRequestState.Open;
            }
        }

        /// <inheritdoc/>
        public async Task PostComment(ICommentViewModel comment)
        {
            var address = HostAddress.Create(Repository.CloneUrl);
            var result = await service.PostComment(address, Id, comment.Body).ConfigureAwait(true);
            timeline.Remove(comment);
            await AddComment(result).ConfigureAwait(true);
            await AddPlaceholder().ConfigureAwait(true);
        }

        public async Task DeleteComment(ICommentViewModel comment)
        {
            await service.DeleteComment(
                HostAddress.Create(Repository.CloneUrl),
                Repository.Owner,
                Repository.Name,
                comment.DatabaseId).ConfigureAwait(true);
            timeline.Remove(comment);
        }

        public async Task EditComment(ICommentViewModel comment)
        {
            await service.EditComment(
                HostAddress.Create(Repository.CloneUrl),
                Repository.Owner,
                Repository.Name,
                comment.DatabaseId,
                comment.Body).ConfigureAwait(false);
        }

        async Task AddComment(CommentModel comment)
        {
            var vm = factory.CreateViewModel<IIssueishCommentViewModel>();
            await vm.InitializeAsync(
                this,
                currentUserModel,
                comment,
                true,
                false).ConfigureAwait(true);
            timeline.Add(vm);
        }

        async Task AddPlaceholder()
        {
            var placeholder = factory.CreateViewModel<IIssueishCommentViewModel>();
            await placeholder.InitializeAsync(
                this,
                currentUserModel,
                null,
                true,
                true,
                this.WhenAnyValue(x => x.State, x => x == PullRequestState.Open)).ConfigureAwait(true);
            timeline.Add(placeholder);
        }

        async Task DoShowCommit(string oid)
        {
            await service.FetchCommit(LocalRepository, Repository, oid).ConfigureAwait(true);
            teServices.ShowCommitDetails(oid);
        }

        void DoOpenOnGitHub()
        {
            visualStudioBrowser.OpenUrl(WebUrl);
        }

        void TimelineItemRemoved(IViewModel item)
        {
            (item as IDisposable)?.Dispose();
        }
    }
}
