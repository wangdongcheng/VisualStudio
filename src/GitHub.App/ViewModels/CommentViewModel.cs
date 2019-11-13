﻿using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GitHub.Extensions;
using GitHub.Logging;
using GitHub.Models;
using GitHub.Services;
using ReactiveUI;
using Serilog;

namespace GitHub.ViewModels
{
    /// <summary>
    /// An issue or pull request comment.
    /// </summary>
    [Export(typeof(ICommentViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CommentViewModel : ViewModelBase, ICommentViewModel
    {
        static readonly ILogger log = LogManager.ForContext<CommentViewModel>();
        readonly ICommentService commentService;
        readonly ObservableAsPropertyHelper<bool> canCancel;
        readonly ObservableAsPropertyHelper<bool> canDelete;
        ObservableAsPropertyHelper<string> commitCaption;
        string id;
        IActorViewModel author;
        IActorViewModel currentUser;
        string body;
        string errorMessage;
        bool isReadOnly;
        bool isSubmitting;
        CommentEditState state;
        DateTimeOffset createdAt;
        ICommentThreadViewModel thread;
        string undoBody;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommentViewModel"/> class.
        /// </summary>
        /// <param name="commentService">The comment service.</param>
        /// <param name="autoCompleteAdvisor">The auto complete advisor.</param>
        [ImportingConstructor]
        public CommentViewModel(ICommentService commentService, IAutoCompleteAdvisor autoCompleteAdvisor)
        {
            Guard.ArgumentNotNull(commentService, nameof(commentService));
            Guard.ArgumentNotNull(autoCompleteAdvisor, nameof(autoCompleteAdvisor));

            AutoCompleteAdvisor = autoCompleteAdvisor;
            this.commentService = commentService;

            var canDeleteObservable = this.WhenAnyValue(
                x => x.EditState,
                x => x.Author,
                x => x.CurrentUser,
                (editState, author, currentUser) => editState == CommentEditState.None && author?.Login == currentUser?.Login);

            canDelete = canDeleteObservable.ToProperty(this, x => x.CanDelete);

            Delete = ReactiveCommand.CreateFromTask(DoDelete, canDeleteObservable);

            var canEdit = this.WhenAnyValue(
                x => x.EditState,
                x => x.Author,
                x => x.CurrentUser,
                (editState, author, currentUser) => editState == CommentEditState.Placeholder || 
                    (editState == CommentEditState.None && author?.Login == currentUser?.Login));

            BeginEdit = ReactiveCommand.Create(DoBeginEdit, canEdit);
            AddErrorHandler(BeginEdit);

            CommitEdit = ReactiveCommand.CreateFromTask(
                DoCommitEdit,
                this.WhenAnyValue(
                    x => x.IsReadOnly,
                    x => x.Body,
                    (ro, body) => !ro && !string.IsNullOrWhiteSpace(body)));
            AddErrorHandler(CommitEdit);

            canCancel = this.WhenAnyValue(x => x.Id)
                .Select(id => id != null)
                .ToProperty(this, x => x.CanCancel);
            CancelEdit = ReactiveCommand.Create(DoCancelEdit, CommitEdit.IsExecuting.Select(x => !x));
            AddErrorHandler(CancelEdit);

            OpenOnGitHub = ReactiveCommand.Create(
                () => { },
                this.WhenAnyValue(x => x.Id).Select(x => x != null));
        }

        /// <inheritdoc/>
        public string Id
        {
            get => id;
            private set => this.RaiseAndSetIfChanged(ref id, value);
        }

        /// <inheritdoc/>
        public int DatabaseId { get; private set; }

        /// <inheritdoc/>
        public int PullRequestId { get; private set; }
        
        /// <inheritdoc/>
        public IActorViewModel Author
        {
            get => author;
            private set => this.RaiseAndSetIfChanged(ref author, value);
        }

        /// <inheritdoc/>
        public IActorViewModel CurrentUser
        {
            get => currentUser;
            private set => this.RaiseAndSetIfChanged(ref currentUser, value);
        }

        /// <inheritdoc/>
        public string Body
        {
            get => body;
            set => this.RaiseAndSetIfChanged(ref body, value);
        }

        /// <inheritdoc/>
        public string ErrorMessage
        {
            get => errorMessage; 
            private set => this.RaiseAndSetIfChanged(ref errorMessage, value); 
        }

        /// <inheritdoc/>
        public CommentEditState EditState
        {
            get => state;
            private set => this.RaiseAndSetIfChanged(ref state, value);
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get => isReadOnly;
            set => this.RaiseAndSetIfChanged(ref isReadOnly, value);
        }

        /// <inheritdoc/>
        public bool IsSubmitting
        {
            get => isSubmitting;
            protected set => this.RaiseAndSetIfChanged(ref isSubmitting, value);
        }

        /// <inheritdoc/>
        public bool CanCancel => canCancel.Value;

        /// <inheritdoc/>
        public bool CanDelete => canDelete.Value;

        /// <inheritdoc/>
        public DateTimeOffset CreatedAt
        {
            get => createdAt;
            private set => this.RaiseAndSetIfChanged(ref createdAt, value);
        }

        /// <inheritdoc/>
        public string CommitCaption => commitCaption.Value;

        /// <inheritdoc/>
        public ICommentThreadViewModel Thread
        {
            get => thread;
            private set => this.RaiseAndSetIfChanged(ref thread, value);
        }

        /// <inheritdoc/>
        public Uri WebUrl { get; private set; }

        /// <inheritdoc/>
        public ReactiveCommand<Unit, Unit> BeginEdit { get; }

        /// <inheritdoc/>
        public ReactiveCommand<Unit, Unit> CancelEdit { get; }

        /// <inheritdoc/>
        public ReactiveCommand<Unit, Unit> CommitEdit { get; }

        /// <inheritdoc/>
        public ReactiveCommand<Unit, Unit> OpenOnGitHub { get; }

        /// <inheritdoc/>
        public ReactiveCommand<Unit, Unit> Delete { get; }

        /// <inheritdoc/>
        public IAutoCompleteAdvisor AutoCompleteAdvisor { get; }

        /// <inheritdoc/>
        public Task InitializeAsync(
            ICommentThreadViewModel thread,
            ActorModel currentUser,
            CommentModel comment,
            CommentEditState state)
        {
            Guard.ArgumentNotNull(thread, nameof(thread));
            Guard.ArgumentNotNull(currentUser, nameof(currentUser));

            Thread = thread;
            CurrentUser = new ActorViewModel(currentUser);
            Id = comment?.Id;
            DatabaseId = comment?.DatabaseId ?? 0;
            PullRequestId = (comment as PullRequestReviewCommentModel)?.PullRequestId ?? 0;
            Body = comment?.Body;
            EditState = state;
            Author = comment != null ? new ActorViewModel(comment.Author) : CurrentUser;
            CreatedAt = comment?.CreatedAt ?? DateTimeOffset.MinValue;
            WebUrl = comment?.Url != null ? new Uri(comment.Url) : null;

            commitCaption = GetCommitCaptionObservable().ToProperty(this, x => x.CommitCaption);

            return Task.CompletedTask;
        }

        protected void AddErrorHandler(ReactiveCommand command)
        {
            command.ThrownExceptions.Subscribe(x => ErrorMessage = x.Message);
        }

        protected virtual IObservable<string> GetCommitCaptionObservable()
        {
            return this.WhenAnyValue(x => x.Id)
                .Select(x => x == null ? Resources.Comment : Resources.UpdateComment);
        }

        async Task DoDelete()
        {
            if (commentService.ConfirmCommentDelete())
            {
                try
                {
                    ErrorMessage = null;
                    IsSubmitting = true;

                    await Thread.DeleteComment(this).ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    var message = e.Message;
                    ErrorMessage = message;
                    log.Error(e, "Error Deleting comment");
                }
                finally
                {
                    IsSubmitting = false;
                }
            }
        }

        void DoBeginEdit()
        {
            if (state != CommentEditState.Editing)
            {
                ErrorMessage = null;
                undoBody = Body;
                EditState = CommentEditState.Editing;
            }
        }

        void DoCancelEdit()
        {
            if (EditState == CommentEditState.Editing)
            {
                EditState = string.IsNullOrWhiteSpace(undoBody) ? CommentEditState.Placeholder : CommentEditState.None;
                Body = undoBody;
                ErrorMessage = null;
                undoBody = null;
            }
        }

        async Task DoCommitEdit()
        {
            try
            {
                ErrorMessage = null;
                IsSubmitting = true;

                if (Id == null)
                {
                    await Thread.PostComment(this).ConfigureAwait(true);
                }
                else
                {
                    await Thread.EditComment(this).ConfigureAwait(true);
                }

                EditState = CommentEditState.None;
            }
            catch (Exception e)
            {
                var message = e.Message;
                ErrorMessage = message;
                log.Error(e, "Error posting comment");
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
